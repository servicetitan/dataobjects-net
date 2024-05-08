// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.05

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Model;
using TypeInfo = Xtensive.Orm.Model.TypeInfo;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class KeyExpression : PersistentFieldExpression
  {
    public TypeInfo EntityType { get; }
    public IReadOnlyList<FieldExpression> KeyFields { get; }

    public override KeyExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<KeyExpression>(processedExpressions, out var value))
        return value;
      return RemapWithNoCheck(offset, processedExpressions);
    }

    // Having this code as a separate method helps to avoid closure allocation during Remap call
    // in case processedExpressions dictionary already contains a result.
    private KeyExpression RemapWithNoCheck(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      var newMapping = new Segment<ColNum>((ColNum)(Mapping.Offset + offset), Mapping.Length);

      var n = KeyFields.Count;
      var fields = new FieldExpression[n];
      for (int i = 0; i < n; ++i) {
        fields[i] = KeyFields[i].Remap(offset, processedExpressions);
      }
      var result = new KeyExpression(EntityType, fields, newMapping, UnderlyingProperty, OuterParameter, DefaultIfEmpty);

      processedExpressions.Add(this, result);
      return result;
    }

    public override KeyExpression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<KeyExpression>(processedExpressions, out var value))
        return value;

      var segment = new Segment<ColNum>((ColNum)map.IndexOf(Mapping.Offset), Mapping.Length);
      var fields = new FieldExpression[KeyFields.Count];
      using (new SkipOwnerCheckScope()) {
        for (var index = 0; index < fields.Length; index++) {
          var field = KeyFields[index].Remap(map, processedExpressions);
          if (field == null) {
            if (SkipOwnerCheckScope.IsActive) {
              processedExpressions.Add(this, null);
              return null;
            }
            throw Exceptions.InternalError(Strings.ExUnableToRemapKeyExpression, OrmLog.Instance);
          }

          fields[index] = field;
        }
      }
      var result = new KeyExpression(EntityType, fields, segment, UnderlyingProperty, OuterParameter, DefaultIfEmpty);

      processedExpressions.Add(this, result);
      return result;
    }

    public override KeyExpression BindParameter(
      ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value)) {
        return (KeyExpression)value;
      }

      return BindParameterWithNoCheck(parameter, processedExpressions);
    }

    // Having this code as a separate method helps to avoid closure allocation during BindParameter call
    // in case processedExpressions dictionary already contains a result.
    private KeyExpression BindParameterWithNoCheck(
      ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      var n = KeyFields.Count;
      var fields = new FieldExpression[n];
      for (int i = 0; i < n; ++i) {
        fields[i] = KeyFields[i].BindParameter(parameter, processedExpressions);
      }
      var result = new KeyExpression(EntityType, fields, Mapping, UnderlyingProperty, parameter, DefaultIfEmpty);

      processedExpressions.Add(this, result);
      return result;
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value)) {
        return value;
      }

      return RemoveOuterParameterWithNoCheck(processedExpressions);
    }

    // Having this code as a separate method helps to avoid closure allocation during RemoveOuterParameter call
    // in case processedExpressions dictionary already contains a result.
    private Expression RemoveOuterParameterWithNoCheck(Dictionary<Expression, Expression> processedExpressions)
    {
      var n = KeyFields.Count;
      var fields = new FieldExpression[n];
      for (int i = 0; i < n; ++i) {
        fields[i] = (FieldExpression) KeyFields[i].RemoveOuterParameter(processedExpressions);
      }
      var result = new KeyExpression(EntityType, fields, Mapping, UnderlyingProperty, null, DefaultIfEmpty);

      processedExpressions.Add(this, result);
      return result;
    }

    public static KeyExpression Create(TypeInfo entityType, ColNum offset)
    {
      var mapping = new Segment<ColNum>(offset, entityType.Key.TupleDescriptor.Count);

      FieldExpression CreateField(ColumnInfo c) => FieldExpression.CreateField(c.Field, offset);

      var fields = entityType.IsLocked
        ? entityType.Key.Columns.Select(CreateField).ToArray(entityType.Key.Columns.Count)
        : entityType.Columns
          .Where(c => c.IsPrimaryKey)
          .OrderBy(c => c.Field.MappingInfo.Offset)
          .Select(CreateField)
          .ToArray();
      return new KeyExpression(entityType, fields, mapping, WellKnownMembers.IEntityKey, null, false);
    }


    // Constructors

    private KeyExpression(
      TypeInfo entityType, 
      IReadOnlyList<FieldExpression> keyFields,
      in Segment<ColNum> segment,
      PropertyInfo underlyingProperty, 
      ParameterExpression parameterExpression, 
      bool defaultIfEmpty)
      : base(ExtendedExpressionType.Key, WellKnown.KeyFieldName, WellKnownOrmTypes.Key, segment, underlyingProperty, parameterExpression, defaultIfEmpty)
    {
      EntityType = entityType;
      KeyFields = keyFields;
    }
  }
}