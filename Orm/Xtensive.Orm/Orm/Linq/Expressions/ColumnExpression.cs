// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class ColumnExpression : ParameterizedExpression
  {
    internal readonly Segment<ColNum> Mapping;

    public override ColumnExpression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (!CanRemap)
        return this;
      var newMapping = new Segment<ColNum>((ColNum) (Mapping.Offset + offset), 1);
      return new ColumnExpression(Type, newMapping, OuterParameter, DefaultIfEmpty);
    }

    public override ColumnExpression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (!CanRemap)
        return this;
      var newMapping = new Segment<ColNum>((ColNum) map.IndexOf(Mapping.Offset), 1);
      return new ColumnExpression(Type, newMapping, OuterParameter, DefaultIfEmpty);
    }

    public Expression BindParameter(ParameterExpression parameter)
    {
      return BindParameter(parameter, new Dictionary<Expression, Expression>());
    }

    public override ColumnExpression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      return new ColumnExpression(Type, Mapping, parameter, DefaultIfEmpty);
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      return new ColumnExpression(Type, Mapping, null, DefaultIfEmpty);
    }

    public static ColumnExpression Create(Type type, ColNum columnIndex)
    {
      var mapping = new Segment<ColNum>(columnIndex, 1);
      return new ColumnExpression(type, mapping, null, false);
    }

    public override string ToString()
    {
      return $"{base.ToString()}, Offset: {Mapping.Offset}";
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitColumnExpression(this);

    // Constructors

    private ColumnExpression(
      Type type,
      in Segment<ColNum> mapping,
      ParameterExpression parameterExpression,
      bool defaultIfEmpty)
      : base(ExtendedExpressionType.Column, type, parameterExpression, defaultIfEmpty)
    {
      this.Mapping = mapping;
    }
  }
}