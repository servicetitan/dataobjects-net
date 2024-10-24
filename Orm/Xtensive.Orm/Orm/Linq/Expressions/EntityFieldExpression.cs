// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class EntityFieldExpression : FieldExpression,
    IEntityExpression
  {
    private readonly IReadOnlyList<PersistentFieldExpression> fields;

    public TypeInfo PersistentType { get; }
    public IReadOnlyList<PersistentFieldExpression> Fields => fields;
    public KeyExpression Key { get; }
    public EntityExpression Entity { get; private set; }

    public bool IsNullable => (Owner != null && Owner.IsNullable) || Field.IsNullable;

    public void RegisterEntityExpression(ColNum offset)
    {
      Entity = EntityExpression.Create(this, offset);
      Entity.IsNullable = IsNullable;
    }

    public override EntityFieldExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<EntityFieldExpression>(processedExpressions, out var value))
        return value;

      var newFields = new PersistentFieldExpression[fields.Count];
      int i = 0;
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        newFields[i++] = field.Remap(offset, processedExpressions);
      }

      var keyExpression = Key.Remap(offset, processedExpressions);
      var entity = Entity?.Remap(offset, processedExpressions);
      var result = new EntityFieldExpression(
        PersistentType, Field, newFields, keyExpression.Mapping, keyExpression, entity, OuterParameter, DefaultIfEmpty);
      if (Owner == null) {
        return result;
      }

      processedExpressions.Add(this, result);
      Owner.Remap(offset, processedExpressions);
      return result;
    }

    public override EntityFieldExpression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<EntityFieldExpression>(processedExpressions, out var value))
        return value;

      var newFields = new List<PersistentFieldExpression>(fields.Count);
      using (new SkipOwnerCheckScope()) {
        foreach (var field in fields) {
          // Do not convert to LINQ. We want to avoid a closure creation here.
          var mappedField = (PersistentFieldExpression) field.Remap(map, processedExpressions);
          if (mappedField == null) {
            continue;
          }

          newFields.Add(mappedField);
        }
      }

      if (newFields.Count != Fields.Count) {
        processedExpressions.Add(this, null);
        return null;
      }

      var keyExpression = Key.Remap(map, processedExpressions);
      EntityExpression entity;
      using (new SkipOwnerCheckScope()) {
        entity = Entity?.Remap(map, processedExpressions);
      }

      var result = new EntityFieldExpression(
        PersistentType, Field, newFields, keyExpression.Mapping, keyExpression, entity, OuterParameter, DefaultIfEmpty);
      if (Owner == null) {
        return result;
      }

      processedExpressions.Add(this, result);
      Owner.Remap(map, processedExpressions);
      return result;
    }

    public override EntityFieldExpression BindParameter(
      ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var r)) {
        return (EntityFieldExpression)r;
      }

      var newFields = new PersistentFieldExpression[fields.Count];
      int i = 0;
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        newFields[i++] = (PersistentFieldExpression) field.BindParameter(parameter, processedExpressions);
      }
      var keyExpression = (KeyExpression) Key.BindParameter(parameter, processedExpressions);
      var entity = (EntityExpression) Entity?.BindParameter(parameter, processedExpressions);
      var result = new EntityFieldExpression(
        PersistentType, Field, newFields, Mapping, keyExpression, entity, parameter, DefaultIfEmpty);
      if (Owner == null) {
        return result;
      }

      processedExpressions.Add(this, result);
      Owner.BindParameter(parameter, processedExpressions);
      return result;
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var result)) {
        return result;
      }

      var newFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        newFields.Add((PersistentFieldExpression) field.RemoveOuterParameter(processedExpressions));
      }
      var keyExpression = (KeyExpression) Key.RemoveOuterParameter(processedExpressions);
      var entity = (EntityExpression) Entity?.RemoveOuterParameter(processedExpressions);
      result = new EntityFieldExpression(
        PersistentType, Field, newFields, Mapping, keyExpression, entity, null, DefaultIfEmpty);
      if (Owner == null) {
        return result;
      }

      processedExpressions.Add(this, result);
      Owner.RemoveOuterParameter(processedExpressions);
      return result;
    }

    public override FieldExpression RemoveOwner() =>
      new EntityFieldExpression(PersistentType, Field, Fields, Mapping, Key, Entity, OuterParameter, DefaultIfEmpty);

    public static EntityFieldExpression CreateEntityField(FieldInfo entityField, ColNum offset)
    {
      if (!entityField.IsEntity) {
        throw new ArgumentException(string.Format(Strings.ExFieldXIsNotEntity, entityField.Name), nameof(entityField));
      }

      var entityType = entityField.ValueType;
      var persistentType = entityField.ReflectedType.Model.Types[entityType];

      var mappingInfo = entityField.MappingInfo;
      var mapping = new Segment<ColNum>((ColNum) (mappingInfo.Offset + offset), mappingInfo.Length);
      var keyFields = persistentType.Key.Fields;
      var keyExpression = KeyExpression.Create(persistentType, (ColNum) (offset + mappingInfo.Offset));
      var fields = new List<PersistentFieldExpression>(keyFields.Count + 1) { keyExpression };
      foreach (var field in keyFields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        fields.Add(BuildNestedFieldExpression(field, (ColNum) (offset + mappingInfo.Offset)));
      }

      return new EntityFieldExpression(persistentType, entityField, fields, mapping, keyExpression, null, null, false);
    }

    private static PersistentFieldExpression BuildNestedFieldExpression(FieldInfo nestedField, ColNum offset)
    {
      if (nestedField.IsPrimitive) {
        return CreateField(nestedField, offset);
      }

      if (nestedField.IsEntity) {
        return CreateEntityField(nestedField, offset);
      }

      throw new NotSupportedException(string.Format(Strings.ExNestedFieldXIsNotSupported, nestedField.Attributes));
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitEntityFieldExpression(this);

    // Constructors

    private EntityFieldExpression(
      TypeInfo persistentType,
      FieldInfo field,
      IReadOnlyList<PersistentFieldExpression> fields,
      in Segment<ColNum> mapping,
      KeyExpression key,
      EntityExpression entity,
      ParameterExpression parameterExpression,
      bool defaultIfEmpty)
      : base(ExtendedExpressionType.EntityField, field, mapping, parameterExpression, defaultIfEmpty)
    {
      PersistentType = persistentType;
      this.fields = fields;
      Key = key;
      Entity = entity;
    }
  }
}