// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.05.15

using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Linq;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Rse.Transformation
{
  internal sealed class ParameterRewriter : ExpressionVisitor
  {
    private ParameterExpression newParameter;
    private ParameterExpression oldParameter;

    public LambdaExpression Replace(LambdaExpression sourceExpression, ParameterExpression oldParameter,
      ParameterExpression newParameter)
    {
      ArgumentNullException.ThrowIfNull(sourceExpression);
      ArgumentNullException.ThrowIfNull(oldParameter);
      ArgumentNullException.ThrowIfNull(newParameter);
      this.oldParameter = oldParameter;
      this.newParameter = newParameter;
      return (LambdaExpression)Visit(sourceExpression);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
      return p == oldParameter ? newParameter : base.VisitParameter(p);
    }
  }
}