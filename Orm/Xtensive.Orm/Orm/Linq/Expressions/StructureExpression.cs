// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.09.29

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class StructureExpression : ParameterizedExpression, IPersistentExpression
  {
    private IReadOnlyList<PersistentFieldExpression> fields;
    private bool isNullable;

    internal Segment<ColNum> Mapping;
    public TypeInfo PersistentType { get; }

    public bool IsNullable => isNullable;

    public IReadOnlyList<PersistentFieldExpression> Fields => fields;

    private void SetFields(List<PersistentFieldExpression> value)
    {
      fields = value;
      foreach (var fieldExpression in fields.OfType<FieldExpression>()) {
        fieldExpression.Owner = this;
      }
    }

    public override Expression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<StructureExpression>(processedExpressions, out var value))
        return value;

      var mapping = new Segment<ColNum>((ColNum) (Mapping.Offset + offset), Mapping.Length);
      var result = new StructureExpression(PersistentType, mapping);
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We intentionally avoiding closure creation here
        processedFields.Add(field.Remap(offset, processedExpressions));
      }

      result.SetFields(processedFields);
      result.isNullable = isNullable;
      return result;
    }

    
    public override Expression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<StructureExpression>(processedExpressions, out var value))
        return value;

      var result = new StructureExpression(PersistentType, default);
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      var offset = ColNum.MaxValue;
      foreach (var field in fields) {
        var mappedField = (PersistentFieldExpression) field.Remap(map, processedExpressions);
        if (mappedField == null) {
          continue;
        }

        var mappingOffset = mappedField.Mapping.Offset;
        if (mappingOffset < offset) {
          offset = mappingOffset;
        }

        processedFields.Add(mappedField);
      }

      if (processedFields.Count == 0) {
        processedExpressions[this] = null;
        return null;
      }

      result.Mapping = new Segment<ColNum>(offset, (ColNum) processedFields.Count);
      result.SetFields(processedFields);
      result.isNullable = isNullable;
      return result;
    }

    public override Expression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value)) {
        return value;
      }

      var result = new StructureExpression(PersistentType, Mapping);
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We intentionally avoiding closure creation here
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

      var result = new StructureExpression(PersistentType, Mapping);
      processedExpressions.Add(this, result);
      var processedFields = new List<PersistentFieldExpression>(fields.Count);
      foreach (var field in fields) {
        // Do not convert to LINQ. We intentionally avoiding closure creation here
        processedFields.Add((PersistentFieldExpression) field.RemoveOuterParameter(processedExpressions));
      }

      result.SetFields(processedFields);
      return result;
    }

    public static StructureExpression CreateLocalCollectionStructure(TypeInfo typeInfo, in Segment<ColNum> mapping)
    {
      if (!typeInfo.IsStructure) {
        throw new ArgumentException(string.Format(Strings.ExTypeXIsNotStructure, typeInfo.Name));
      }

      var sourceFields = typeInfo.Fields;
      var destinationFields = new List<PersistentFieldExpression>(sourceFields.Count);
      var result = new StructureExpression(typeInfo, mapping);
      result.SetFields(destinationFields);
      foreach (var field in sourceFields) {
        // Do not convert to LINQ. We intentionally avoiding closure creation here
        destinationFields.Add(BuildNestedFieldExpression(field, mapping.Offset));
      }

      return result;
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

      throw new NotSupportedException(string.Format(Strings.ExNestedFieldXIsNotSupported, nestedField.Attributes));
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitStructureExpression(this);

    // Constructors

    private StructureExpression(
      TypeInfo persistentType, 
      in Segment<ColNum> mapping)
      : base(ExtendedExpressionType.Structure, persistentType.UnderlyingType, null, false)
    {
      Mapping = mapping;
      PersistentType = persistentType;
    }
  }
}