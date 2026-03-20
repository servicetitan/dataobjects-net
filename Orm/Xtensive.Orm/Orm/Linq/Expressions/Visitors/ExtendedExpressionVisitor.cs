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

  internal protected virtual Expression VisitFullTextExpression(FullTextExpression expression) => expression;
  internal protected virtual Expression VisitConstructorExpression(ConstructorExpression expression) => expression;
  internal protected virtual Expression VisitStructureExpression(StructureExpression expression) => expression;
  internal protected virtual Expression VisitLocalCollectionExpression(LocalCollectionExpression expression) => expression;
  internal protected virtual Expression VisitGroupingExpression(GroupingExpression expression) => expression;
  internal protected virtual Expression VisitSubQueryExpression(SubQueryExpression expression) => expression;
  internal protected virtual Expression VisitProjectionExpression(ProjectionExpression projectionExpression) => projectionExpression;
  internal protected virtual Expression VisitFieldExpression(FieldExpression expression) => expression;
  internal protected virtual Expression VisitStructureFieldExpression(StructureFieldExpression expression) => expression;
  internal protected virtual Expression VisitKeyExpression(KeyExpression expression) => expression;
  internal protected virtual Expression VisitEntityExpression(EntityExpression expression) => expression;
  internal protected virtual Expression VisitEntityFieldExpression(EntityFieldExpression expression) => expression;
  internal protected virtual Expression VisitEntitySetExpression(EntitySetExpression expression) => expression;
  internal protected virtual Expression VisitItemProjectorExpression(ItemProjectorExpression itemProjectorExpression) => itemProjectorExpression;
  internal protected virtual Expression VisitColumnExpression(ColumnExpression expression) => expression;

  internal protected virtual Expression VisitMarker(MarkerExpression expression)
  {
    var processedTarget = Visit(expression.Target);
    return processedTarget == expression.Target ? expression : new MarkerExpression(processedTarget, expression.MarkerType);
  }
}
