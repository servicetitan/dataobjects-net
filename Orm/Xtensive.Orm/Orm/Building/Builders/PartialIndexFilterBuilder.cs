// Copyright (C) 2011-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2011.10.07

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm;
using Xtensive.Orm.Internals;
using Xtensive.Reflection;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;

using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;
using TypeInfo = Xtensive.Orm.Model.TypeInfo;
using System.Text;

namespace Xtensive.Orm.Building.Builders
{
  internal sealed class PartialIndexFilterBuilder : ExpressionVisitor
  {
    private static readonly ParameterExpression Parameter = Expression.Parameter(WellKnownOrmTypes.Tuple, "tuple");
    private static readonly IReadOnlyList<ParameterExpression> Parameters = [Parameter];

    private readonly TypeInfo declaringType;
    private readonly TypeInfo reflectedType;
    private readonly IndexInfo index;
    private readonly List<FieldInfo> usedFields = new List<FieldInfo>();
    private readonly Dictionary<Expression, FieldInfo> entityAccessMap = new Dictionary<Expression, FieldInfo>();

    public static void BuildFilter(IndexInfo index)
    {
      ArgumentNullException.ThrowIfNull(index);
      var builder = new PartialIndexFilterBuilder(index);
      var body = builder.Visit(index.FilterExpression.Body);
      var filter = new PartialIndexFilterInfo {
        Expression = FastExpression.Lambda(body, Parameters),
        Fields = builder.usedFields,
      };
      index.Filter = filter;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
      if (EnumRewritableOperations(b)) {
        var leftNoCasts = b.Left.StripCasts();
        var leftNoCastsType = leftNoCasts.Type;
        var bareLeftType = leftNoCastsType.StripNullable();
        var rightNoCasts = b.Right.StripCasts();
        var rightNoCastsType = rightNoCasts.Type;
        var bareRightType = rightNoCastsType.StripNullable();

        if (bareLeftType.IsEnum && rightNoCasts.NodeType == ExpressionType.Constant) {
          var typeToCast = leftNoCastsType.IsNullable()
            ? bareLeftType.GetEnumUnderlyingType().ToNullable()
            : leftNoCastsType.GetEnumUnderlyingType();

          return base.VisitBinary(Expression.MakeBinary(
            b.NodeType,
            Expression.Convert(leftNoCasts, typeToCast),
            Expression.Convert(b.Right, typeToCast)));
        }
        else if (bareRightType.IsEnum && leftNoCasts.NodeType == ExpressionType.Constant) {
          var typeToCast = rightNoCastsType.IsNullable()
            ? bareRightType.GetEnumUnderlyingType().ToNullable()
            : rightNoCastsType.GetEnumUnderlyingType();

          return base.VisitBinary(Expression.MakeBinary(
            b.NodeType,
            Expression.Convert(rightNoCasts, typeToCast),
            Expression.Convert(b.Left, typeToCast)));
        }
      }

      // Detect f!=null and f==null for entity fields

      if (!(b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual))
        return base.VisitBinary(b);

      var left = Visit(b.Left);
      var right = Visit(b.Right);

      if (entityAccessMap.TryGetValue(left, out var field) && IsNull(right))
        return BuildEntityCheck(field, b.NodeType);
      if (entityAccessMap.TryGetValue(right, out field) && IsNull(left))
        return BuildEntityCheck(field, b.NodeType);

      return base.VisitBinary(b);

      static bool EnumRewritableOperations(BinaryExpression b) =>
        b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
          or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
    }

    protected override Expression VisitMember(MemberExpression originalMemberAccess)
    {
      // Try to collapse series of member access expressions.
      // Finally we should reach original parameter.

      var memberAccess = originalMemberAccess;
      var memberAccessSequence = new List<MemberExpression>();
      for (; ; ) {
        if (!IsPersistentFieldAccess(memberAccess)) {
          break;
        }
        memberAccessSequence.Add(memberAccess);
        if (memberAccess.Expression.NodeType != ExpressionType.MemberAccess) {
          break;
        }
        memberAccess = (MemberExpression) memberAccess.Expression;
      }
      if (memberAccessSequence.Count == 0 || !IsEntityParameter(memberAccess.Expression)) {
        return base.VisitMember(originalMemberAccess);
      }
      var nameBuilder = new StringBuilder();
      for (var i = memberAccessSequence.Count; i-- > 0;) {
        nameBuilder
          .Append(memberAccessSequence[i].Member.Name)
          .Append('.');
      }
      nameBuilder.Length -= 1;
      var fieldName = nameBuilder.ToString();
      var field = reflectedType.Fields[fieldName];
      if (field == null) {
        throw UnableToTranslate(originalMemberAccess, Strings.MemberAccessSequenceContainsNonPersistentFields);
      }
      if (field.IsEntity) {
        EnsureCanBeUsedInFilter(originalMemberAccess, field);
        entityAccessMap[originalMemberAccess] = field;
        return originalMemberAccess;
      }
      if (field.IsPrimitive) {
        EnsureCanBeUsedInFilter(originalMemberAccess, field);
        return BuildFieldAccess(field, field.ValueType);
      }
      throw UnableToTranslate(originalMemberAccess, Strings.OnlyPrimitiveAndReferenceFieldsAreSupported);
    }

    private void EnsureCanBeUsedInFilter(Expression expression, FieldInfo field)
    {
      var canBeUsed = field.ReflectedType == field.DeclaringType
        || field.IsPrimaryKey
        || field.DeclaringType.Hierarchy.InheritanceSchema!=InheritanceSchema.ClassTable;
      if (!canBeUsed)
        throw UnableToTranslate(expression, string.Format(Strings.FieldXDoesNotExistInTableForY, field.Name, field.ReflectedType));
    }

    private Expression BuildFieldAccess(FieldInfo field, Type valueType)
    {
      var fieldIndex = usedFields.Count;
      usedFields.Add(field);
      return Expression.Call(Parameter,
        WellKnownMembers.Tuple.GenericAccessor.CachedMakeGenericMethod(valueType),
        Expr.Constant(fieldIndex));
    }

    private Expression BuildFieldCheck(FieldInfo field, ExpressionType nodeType)
    {
      var nullableValueType = field.ValueType.ToNullable();
      return Expression.MakeBinary(nodeType, BuildFieldAccess(field, nullableValueType), Expression.Constant(null, nullableValueType));
    }

    private Expression BuildEntityCheck(FieldInfo field, ExpressionType nodeType) =>
      field.Fields.Where(f => f.Column != null).Select(f => BuildFieldCheck(f, nodeType)).Aggregate(Expression.AndAlso);

    private bool IsNull(Expression expression)
    {
      return expression.NodeType==ExpressionType.Constant && ((ConstantExpression) expression).Value==null;
    }

    private bool IsEntityParameter(Expression expression)
    {
      return expression.NodeType==ExpressionType.Parameter
        && declaringType.UnderlyingType.IsAssignableFrom(expression.Type);
    }

    private bool IsPersistentFieldAccess(MemberExpression expression)
    {
      if (!(expression.Member is PropertyInfo))
        return false;
      var ownerType = expression.Expression.Type;
      return WellKnownOrmInterfaces.Entity.IsAssignableFrom(ownerType)
        || WellKnownOrmTypes.Structure.IsAssignableFrom(ownerType);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
      // Parameters should be wiped in VisitMemberAccess.
      // If they are not for some reason fail here.
      throw UnableToTranslate(p, string.Format(Strings.ParametersOfTypeOtherThanXAreNotSupported, declaringType.UnderlyingType));
    }

    protected override Expression VisitLambda<T>(Expression<T> l)
    {
      throw UnableToTranslate(l);
    }

    private DomainBuilderException UnableToTranslate(Expression expression, string reason)
    {
      return new DomainBuilderException(string.Format(Strings.ExUnableToTranslateXInPartialIndexDefinitionForIndexYReasonZ, expression, index, reason));
    }

    private DomainBuilderException UnableToTranslate(Expression expression)
    {
      return UnableToTranslate(expression, string.Format(Strings.ExpressionsOfTypeXAreNotSupported, expression.NodeType));
    }

    private PartialIndexFilterBuilder(IndexInfo index)
    {
      this.index = index;

      declaringType = index.DeclaringType;
      reflectedType = index.ReflectedType;
    }
  }
}
