// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.10.01

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.Internals;
using Xtensive.Reflection;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Orm.Linq.Expressions;
using Xtensive.Orm.Model;

using FieldInfo=System.Reflection.FieldInfo;
using TypeInfo = Xtensive.Orm.Model.TypeInfo;

namespace Xtensive.Orm.Linq
{
  [Serializable]
  internal sealed class ItemToTupleConverter<TItem> : ItemToTupleConverter
  {
    private static readonly bool IsKeyConverter = typeof(TItem).IsAssignableFrom(WellKnownOrmTypes.Key); 
    
    private class TupleTypeCollection: IReadOnlyCollection<Type>
    {
      private IEnumerable<Type> types;
      private int count;

      public int Count => count;

      public IEnumerator<Type> GetEnumerator() => (types ?? Enumerable.Empty<Type>()).GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public void Add(Type type)
      {
        count++;
        types = types==null ? [type] : types.Concat([type]);
      }

      public void AddRange(IReadOnlyCollection<Type> newTypes)
      {
        count += newTypes.Count;
        types = types == null ? newTypes : types.Concat(newTypes);
      }
    }

    private static readonly IReadOnlyList<ParameterExpression> ParamContextParams = [Expression.Parameter(WellKnownOrmTypes.ParameterContext, "context")];
    private static readonly MethodInfo SelectMethod = WellKnownMembers.Enumerable.Select.MakeGenericMethod(typeof(TItem), WellKnownOrmTypes.Tuple);

    private readonly Func<ParameterContext, IEnumerable<TItem>> enumerableFunc;
    private readonly DomainModel model;
    private readonly Type entityTypestoredInKey;

    private readonly ConstantExpression converterExpression;

    public override Expression<Func<ParameterContext, IEnumerable<Tuple>>> GetEnumerable()
    {
      var call = Expression.Call(Expression.Constant(enumerableFunc.Target), enumerableFunc.Method, ParamContextParams);
      var select = Expression.Call(SelectMethod, call, converterExpression);
      return FastExpression.Lambda<Func<ParameterContext, IEnumerable<Tuple>>>(select, ParamContextParams);
    }

    /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
    private bool IsPersistableType(Type type)
    {
      if (type==WellKnownOrmTypes.Entity
        || type.IsSubclassOf(WellKnownOrmTypes.Entity)
          || type==WellKnownOrmTypes.Structure
            || type.IsSubclassOf(WellKnownOrmTypes.Structure)) {
        if (!model.Types.Contains(type))
          throw new InvalidOperationException(string.Format(Strings.ExTypeNotFoundInModel, type.FullName));
        return true;
      }
      if (type.IsOfGenericType(RefOfTType)) {
        var entityType = type.GetGenericType(RefOfTType).GetGenericArguments()[0];
        if (!model.Types.Contains(entityType))
          throw new InvalidOperationException(string.Format(Strings.ExTypeNotFoundInModel, type.FullName));
        return true;
      }
      return TypeIsStorageMappable(type);
    }

    private static bool TypeIsStorageMappable(Type type)
    {
      // TODO: AG: Take info from storage!
      type = type.StripNullable();
      return type.IsPrimitive ||
        type.IsEnum ||
        type == WellKnownTypes.ByteArray ||
        type == WellKnownTypes.Decimal ||
        type == WellKnownTypes.String ||
        type == WellKnownTypes.DateTime ||
        type == WellKnownTypes.DateTimeOffset ||
        type == WellKnownTypes.DateOnly ||
        type == WellKnownTypes.TimeOnly ||
        type == WellKnownTypes.Guid ||
        type == WellKnownTypes.TimeSpan;
    }


    private static void FillLocalCollectionField(object item, Tuple tuple, Expression expression)
    {
      if (item==null)
        return;
      // LocalCollectionExpression
      switch (expression) {
        case LocalCollectionExpression itemExpression:
          foreach (var field in itemExpression.Fields) {
            object value;
            if (field.Key is PropertyInfo propertyInfo) {
              value = propertyInfo.GetValue(item, BindingFlags.InvokeMethod, null, null, null);
            }
            else {
              value = ((FieldInfo) field.Key).GetValue(item);
            }
            if (value != null)
              FillLocalCollectionField(value, tuple, (Expression) field.Value);
          }
          break;
        case ColumnExpression columnExpression:
          tuple.SetValue(columnExpression.Mapping.Offset, item);
          break;
        case StructureExpression structureExpression:
          var structure = (Structure) item;
          var typeInfo = structureExpression.PersistentType;
          var tupleDescriptor = typeInfo.TupleDescriptor;
          var tupleSegment = new Segment<ColNum>(0, tupleDescriptor.Count);
          var structureTuple = structure.Tuple.GetSegment(tupleSegment);
          structureTuple.CopyTo(tuple, 0, structureExpression.Mapping.Offset, structureTuple.Count);
          break;
        case EntityExpression entityExpression: {
          var entity = (Entity) item;
          var keyTuple = entity.Key.Value;
          keyTuple.CopyTo(tuple, 0, entityExpression.Key.Mapping.Offset, keyTuple.Count);
        }
        break;
        case KeyExpression keyExpression: {
          var key = (Key) item;
          var keyTuple = key.Value;
          keyTuple.CopyTo(tuple, 0, keyExpression.Mapping.Offset, keyTuple.Count);
        }
        break;
        default:
          throw new NotSupportedException();
      }
    }

    private LocalCollectionExpression BuildLocalCollectionExpression(Type type,
      HashSet<Type> processedTypes, ref ColNum columnIndex, MemberInfo parentMember, TupleTypeCollection types, Expression sourceExpression)
    {
      if (type.IsAssignableFrom(WellKnownOrmTypes.Key))
        throw new InvalidOperationException(string.Format(Strings.ExUnableToStoreUntypedKeyToStorage, RefOfTType.GetShortName()));
      if (!processedTypes.Add(type))
        throw new InvalidOperationException(string.Format(Strings.ExUnableToPersistTypeXBecauseOfLoopReference, type.FullName));

      var members = type
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(propertyInfo => propertyInfo.CanRead)
        .Cast<MemberInfo>()
        .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public));
      var fields = new Dictionary<MemberInfo, IMappedExpression>();
      foreach (var memberInfo in members) {
        var memberType = memberInfo switch {
          PropertyInfo propertyInfo => propertyInfo.PropertyType,
          FieldInfo fieldInfo => fieldInfo.FieldType,
          _ => throw new NotSupportedException()
        };
        var expression = IsPersistableType(memberType)
          ? BuildField(memberType, ref columnIndex, types)
          : BuildLocalCollectionExpression(memberType, new HashSet<Type>(processedTypes), ref columnIndex, memberInfo, types, sourceExpression);
        fields.Add(memberInfo, expression);
      }
      if (fields.Count==0)
        throw new InvalidOperationException(string.Format(Strings.ExTypeXDoesNotHasAnyPublicReadablePropertiesOrFieldsSoItCanTBePersistedToStorage, type.FullName));

      return new LocalCollectionExpression(type, parentMember, sourceExpression) { Fields = fields };
    }


    private ParameterizedExpression BuildField(Type type, ref ColNum index, TupleTypeCollection types)
    {
//      if (type.IsOfGenericType(typeof (Ref<>))) {
//        var entityType = type.GetGenericType(typeof (Ref<>)).GetGenericArguments()[0];
//        TypeInfo typeInfo = model.Types[entityType];
//        KeyInfo keyProviderInfo = typeInfo.KeyInfo;
//        TupleDescriptor keyTupleDescriptor = keyProviderInfo.TupleDescriptor;
//        KeyExpression entityExpression = KeyExpression.Create(typeInfo, index);
//        index += keyTupleDescriptor.Count;
//        types = types.Concat(keyTupleDescriptor);
//        return Expression.Convert(entityExpression, type);
//      }

      if (type.IsSubclassOf(WellKnownOrmTypes.Entity)) {
        var typeInfo = model.Types[type];
        var keyInfo = typeInfo.Key;
        var keyTupleDescriptor = keyInfo.TupleDescriptor;
        ParameterizedExpression expression;
        if (IsKeyConverter)
          expression = KeyExpression.Create(typeInfo, index);
        else {
          var entityExpression = EntityExpression.Create(typeInfo, index, true);
          entityExpression.IsNullable = true;
          expression = entityExpression;
        }
        index += (ColNum)keyTupleDescriptor.Count;
        types.AddRange(keyTupleDescriptor);
        return expression;
      }

      if (type.IsSubclassOf(WellKnownOrmTypes.Structure)) {
        var typeInfo = model.Types[type];
        var tupleDescriptor = typeInfo.TupleDescriptor;
        var tupleSegment = new Segment<ColNum>(index, tupleDescriptor.Count);
        var structureExpression = StructureExpression.CreateLocalCollectionStructure(typeInfo, tupleSegment);
        index += (ColNum)tupleDescriptor.Count;
        types.AddRange(tupleDescriptor);
        return structureExpression;
      }

      if (TypeIsStorageMappable(type)) {
        ColumnExpression columnExpression = ColumnExpression.Create(type, index);
        types.Add(type);
        index++;
        return columnExpression;
      }

      throw new NotSupportedException();
    }

    private Func<TItem, Tuple> BuildConverter(Expression sourceExpression)
    {
      var itemType = IsKeyConverter ? entityTypestoredInKey : typeof (TItem);
      ColNum index = 0;
      var types = new TupleTypeCollection();
      Expression = IsPersistableType(itemType)
        ? BuildField(itemType, ref index, types)
        : BuildLocalCollectionExpression(itemType, new HashSet<Type>(), ref index, null, types, sourceExpression);
      TupleDescriptor = TupleDescriptor.Create(types.ToArray(types.Count));

      return delegate(TItem item) {
        var tuple = Tuple.Create(TupleDescriptor);
        if (ReferenceEquals(item, null)) {
          return tuple;
        }
        FillLocalCollectionField(item, tuple, Expression);
        return tuple;
      };
    }

    public ItemToTupleConverter(Func<ParameterContext, IEnumerable<TItem>> enumerableFunc, DomainModel model, Expression sourceExpression, Type storedEntityType)
    {
      this.model = model;
      this.enumerableFunc = enumerableFunc;
      entityTypestoredInKey = storedEntityType;
      converterExpression = Expression.Constant(BuildConverter(sourceExpression));
    }
  }
}
