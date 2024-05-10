// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Linq.Expressions;
using Xtensive.Linq;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Linq.Expressions.Visitors;

internal abstract class ExtendedExpressionVisitor : ExpressionVisitor
{
  protected override Expression VisitExtension(Expression node) =>
    (node as ExtendedExpression ?? throw new NotSupportedException(string.Format(Strings.ExpressionXIsUnknown, node)))
      .Accept(this);

  internal virtual Expression VisitFullTextExpression(FullTextExpression expression) => expression;
  internal virtual Expression VisitConstructorExpression(ConstructorExpression expression) => expression;
  internal virtual Expression VisitStructureExpression(StructureExpression expression) => expression;
  internal virtual Expression VisitLocalCollectionExpression(LocalCollectionExpression expression) => expression;
  internal virtual Expression VisitGroupingExpression(GroupingExpression expression) => expression;
  internal virtual Expression VisitSubQueryExpression(SubQueryExpression expression) => expression;
  internal virtual Expression VisitProjectionExpression(ProjectionExpression projectionExpression) => projectionExpression;
  internal virtual Expression VisitFieldExpression(FieldExpression expression) => expression;
  internal virtual Expression VisitStructureFieldExpression(StructureFieldExpression expression) => expression;
  internal virtual Expression VisitKeyExpression(KeyExpression expression) => expression;
  internal virtual Expression VisitEntityExpression(EntityExpression expression) => expression;
  internal virtual Expression VisitEntityFieldExpression(EntityFieldExpression expression) => expression;
  internal virtual Expression VisitEntitySetExpression(EntitySetExpression expression) => expression;
  internal virtual Expression VisitItemProjectorExpression(ItemProjectorExpression itemProjectorExpression) => itemProjectorExpression;
  internal virtual Expression VisitColumnExpression(ColumnExpression expression) => expression;

  internal virtual Expression VisitMarker(MarkerExpression expression)
  {
    var processedTarget = Visit(expression.Target);
    return processedTarget == expression.Target ? expression : new MarkerExpression(processedTarget, expression.MarkerType);
  }
}
