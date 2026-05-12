// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.21

using System.Linq.Expressions;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Linq.Expressions.Visitors;

internal sealed class GenericExpressionVisitor<T>(Func<T, Expression> genericProcessor) : ExpressionVisitor
  where T : class
{
  public static Expression Process(Expression target, Func<T, Expression> genericProcessor) =>
    new GenericExpressionVisitor<T>(genericProcessor).Process(target);

  public Expression Process(Expression target)
  {
    if (RemapScope.CurrentContext!=null)
      return Visit(target);

    using (new RemapScope())
      return Visit(target);
  }

  protected override Expression VisitUnknown(Expression e)
  {
    if (e is T mapped)
      return VisitGenericExpression(mapped);

    if (e is MarkerExpression { Target: var target } marker) {
      var result = Visit(target);
      if (result == target)
        return result;
      return new MarkerExpression(result, marker.MarkerType);
    }

    return base.VisitUnknown(e);
  }

  private Expression VisitGenericExpression(T generic)
  {
    if (genericProcessor!=null)
      return genericProcessor.Invoke(generic);
    throw new NotSupportedException(Strings.ExUnableToUseBaseImplementationOfVisitGenericExpressionWithoutSpecifyingGenericProcessorDelegate);
  }
}
