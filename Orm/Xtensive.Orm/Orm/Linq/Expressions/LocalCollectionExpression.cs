// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.09.09

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  [Serializable]
  internal sealed class LocalCollectionExpression : ParameterizedExpression
  {
    // just to have good error
    private readonly string expressionAsString;

    public IDictionary<MemberInfo, IMappedExpression> Fields { get; set; }

    public MemberInfo MemberInfo { get; private set; }

    public IEnumerable<ColumnExpression> Columns =>
      Fields
        .SelectMany(field => field.Value is ColumnExpression
          ? new[] { field.Value }
          : ((LocalCollectionExpression) field.Value).Columns.Cast<IMappedExpression>())
        .Cast<ColumnExpression>();

    public override LocalCollectionExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<LocalCollectionExpression>(processedExpressions, out var value))
        return value;

      var result = new LocalCollectionExpression(Type, MemberInfo, expressionAsString);
      processedExpressions.Add(this, result);
      result.Fields = Fields.ToDictionary(f=>f.Key, f=>(IMappedExpression)f.Value.Remap(offset, processedExpressions));
      return result;
    }

    public override Expression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (TryProcessed<LocalCollectionExpression>(processedExpressions, out var value))
        return value;

      var result = new LocalCollectionExpression(Type, MemberInfo, expressionAsString);
      processedExpressions.Add(this, result);
      result.Fields = Fields.ToDictionary(f=>f.Key, f=>(IMappedExpression)f.Value.Remap(map, processedExpressions));
      return result;
    }

    public override Expression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value))
        return value;

      var result = new LocalCollectionExpression(Type, MemberInfo, expressionAsString);
      processedExpressions.Add(this, result);
      result.Fields = Fields.ToDictionary(f=>f.Key, f=>(IMappedExpression)f.Value.BindParameter(parameter, processedExpressions));
      return result;
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      if (processedExpressions.TryGetValue(this, out var value))
        return value;

      var result = new LocalCollectionExpression(Type, MemberInfo, expressionAsString);
      processedExpressions.Add(this, result);
      result.Fields = Fields.ToDictionary(f=>f.Key, f=>(IMappedExpression)f.Value.RemoveOuterParameter(processedExpressions));
      return result;
    }

    public override string ToString() => expressionAsString;

    public LocalCollectionExpression(Type type, MemberInfo memberInfo, Expression sourceExpression)
      : base(ExtendedExpressionType.LocalCollection, type, null, true)
    {
      Fields = new Dictionary<MemberInfo, IMappedExpression>();
      MemberInfo = memberInfo;
      expressionAsString = sourceExpression.ToString();
      ;
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitLocalCollectionExpression(this);

    private LocalCollectionExpression(Type type, MemberInfo memberInfo, in string stringRepresentation)
      : base(ExtendedExpressionType.LocalCollection, type, null, true)
    {
      Fields = new Dictionary<MemberInfo, IMappedExpression>();
      MemberInfo = memberInfo;
      expressionAsString = stringRepresentation;
    }
  }
}
