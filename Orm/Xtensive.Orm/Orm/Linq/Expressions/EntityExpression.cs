// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.05

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Orm.Model;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class EntityExpression : ParameterizedExpression, IEntityExpression
  {
    private List<PersistentFieldExpression> fields;

    public TypeInfo PersistentType { get; }

    public KeyExpression Key { get; }

    public IReadOnlyList<PersistentFieldExpression> Fields => fields;

    private void SetFields(List<PersistentFieldExpression> value) {
      fields = value;
      foreach (var fieldExpression in fields.OfType<FieldExpression>()) {
        fieldExpression.Owner = this;
      }
    }

    public bool IsNullable { get; set; }

    public override EntityExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<EntityExpression>(processedExpressions, out var value))
        return value;

      var keyExpression = Key.Remap(offset, processedExpressions);
      var result = new EntityExpression(PersistentType, keyExpression, OuterParameter, DefaultIfEmpty);
      processedExpressions.Add(this, result);
      result.IsNullable = IsNullable;
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        processedFields.Add(field.Remap(offset, processedExpressions));
      }

      result.SetFields(processedFields);
      return result;
    }

    public override EntityExpression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<EntityExpression>(processedExpressions, out var value))
        return value;

      var keyExpression = Key.Remap(map, processedExpressions);
      if (keyExpression == null) {
        return null;
      }

      var result = new EntityExpression(PersistentType, keyExpression, OuterParameter, DefaultIfEmpty);
      processedExpressions.Add(this, result);
      result.IsNullable = IsNullable;
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        var mappedField = (PersistentFieldExpression) field.Remap(map, processedExpressions);
        if (mappedField == null) {
          continue;
        }

        processedFields.Add(mappedField);
      }

      result.SetFields(processedFields);
      return result;
    }

    public override Expression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value)) {
        return value;
      }

      var keyExpression = (KeyExpression) Key.BindParameter(parameter, processedExpressions);
      var result = new EntityExpression(PersistentType, keyExpression, parameter, DefaultIfEmpty);
      result.IsNullable = IsNullable;
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        processedFields.Add((PersistentFieldExpression) field.BindParameter(parameter, processedExpressions));
      }

      result.SetFields(processedFields);
      return result;
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value)) {
        return value;
      }

      var keyExpression = (KeyExpression) Key.RemoveOuterParameter(processedExpressions);
      var result = new EntityExpression(PersistentType, keyExpression, null, DefaultIfEmpty);
      result.IsNullable = IsNullable;
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        processedFields.Add((PersistentFieldExpression) field.RemoveOuterParameter(processedExpressions));
      }

      result.SetFields(processedFields);
      return result;
    }

    public static void Fill(EntityExpression entityExpression, ColNum offset)
    {
      using (new RemapScope()) {
        _ = entityExpression.Remap(offset, new Dictionary<Expression, Expression>());
      }
      var typeInfo = entityExpression.PersistentType;
      foreach (var nestedField in typeInfo.Fields.Except(entityExpression.Fields.OfType<FieldExpression>().Select(field => field.Field))) {
        var nestedFieldExpression = BuildNestedFieldExpression(nestedField, offset);
        if (nestedFieldExpression is FieldExpression fieldExpression) {
          fieldExpression.Owner = entityExpression;
        }

        entityExpression.fields.Add(nestedFieldExpression);
      }
    }

    public static EntityExpression Create(TypeInfo typeInfo, ColNum offset, bool keyFieldsOnly)
    {
      if (!typeInfo.IsEntity && !typeInfo.IsInterface) {
        throw new ArgumentException(
          string.Format(Strings.ExPersistentTypeXIsNotEntityOrPersistentInterface, typeInfo.Name), nameof(typeInfo));
      }

      var keyExpression = KeyExpression.Create(typeInfo, offset);

      List<PersistentFieldExpression> fields;
      var result = new EntityExpression(typeInfo, keyExpression, null, false);
      if (keyFieldsOnly) {
        fields = new List<PersistentFieldExpression>(keyExpression.KeyFields.Count + 1) {keyExpression};
        // Add key fields to field collection
        foreach (var keyField in keyExpression.KeyFields) {
          // Do not convert to LINQ. We want to avoid a closure creation here.
          fields.Add(FieldExpression.CreateField(keyField.Field, offset));
        }
      }
      else {
        fields = new List<PersistentFieldExpression>(typeInfo.Fields.Count + 1) {keyExpression};
        foreach (var nestedField in typeInfo.Fields) {
          // Do not convert to LINQ. We want to avoid a closure creation here.
          fields.Add(BuildNestedFieldExpression(nestedField, offset));
        }
      }

      result.SetFields(fields);
      return result;
    }

    public static EntityExpression Create(EntityFieldExpression entityFieldExpression, ColNum offset)
    {
      var typeInfo = entityFieldExpression.PersistentType;
      var keyExpression = KeyExpression.Create(typeInfo, offset);
      var fields = new List<PersistentFieldExpression>(typeInfo.Fields.Count + 1) {keyExpression};
      foreach (var nestedField in typeInfo.Fields) {
        // Do not convert to LINQ. We want to avoid a closure creation here.
        fields.Add(BuildNestedFieldExpression(nestedField, offset));
      }

      var result = new EntityExpression(typeInfo, keyExpression, null, entityFieldExpression.DefaultIfEmpty);
      result.SetFields(fields);
      return entityFieldExpression.OuterParameter == null
        ? result
        : (EntityExpression) result.BindParameter(
          entityFieldExpression.OuterParameter, new Dictionary<Expression, Expression>());
    }

    private static PersistentFieldExpression BuildNestedFieldExpression(FieldInfo nestedField, ColNum offset)
    {
      if (nestedField.IsPrimitive) {
        return FieldExpression.CreateField(nestedField, offset);
      }

      if (nestedField.IsStructure) {
        return StructureFieldExpression.CreateStructure(nestedField, offset);
      }

      if (nestedField.IsEntity) {
        return EntityFieldExpression.CreateEntityField(nestedField, offset);
      }

      if (nestedField.IsEntitySet) {
        return EntitySetExpression.CreateEntitySet(nestedField);
      }

      throw new NotSupportedException(string.Format(Strings.ExNestedFieldXIsNotSupported, nestedField.Attributes));
    }

    public override string ToString() => $"{base.ToString()} {PersistentType.Name}";

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitEntityExpression(this);

    // Constructors

    private EntityExpression(
      TypeInfo entityType, 
      KeyExpression key, 
      ParameterExpression parameterExpression, 
      bool defaultIfEmpty)
      : base(ExtendedExpressionType.Entity, entityType.UnderlyingType, parameterExpression, defaultIfEmpty)
    {
      PersistentType = entityType;
      Key = key;
    }
  }
}