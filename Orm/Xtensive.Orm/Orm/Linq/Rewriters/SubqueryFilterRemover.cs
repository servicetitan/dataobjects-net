// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2010.04.27

using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Linq.Rewriters
{
  internal sealed class SubqueryFilterRemover : CompilableProviderVisitor
  {
    private class SubqueryFilterChecker : ExpressionVisitor
    {
      private readonly ApplyParameter filterParameter;
      private readonly Stack<Expression> meaningfulLefts;
      private readonly Stack<Expression> meaningfulRights;

      private bool @continue;
      private bool matchFound;

      public bool Check(Expression expression)
      {
        matchFound = false;
        @continue = true;
        Visit(expression);
        return matchFound && @continue && meaningfulLefts.Count==meaningfulRights.Count;
      }

      protected override ConstantExpression VisitConstant(ConstantExpression c)
      {
        if (c.Value==filterParameter)
          matchFound = true;
        return c;
      }

      protected override BinaryExpression VisitBinary(BinaryExpression b)
      {
        if (b.NodeType==ExpressionType.Equal) {
          var leftAccess = b.Left.AsTupleAccess();
          var rightAccess = b.Right.AsTupleAccess();
          var rightConstant = b.Right as ConstantExpression;

          @continue &= (leftAccess!=null && rightAccess!=null) || (leftAccess!=null && rightConstant!=null);
          if (@continue && rightConstant==null) {
            var leftIsParameterBound = leftAccess.Object.NodeType==ExpressionType.Parameter;
            var rightIsParameterBound = rightAccess.Object.NodeType==ExpressionType.Parameter;
            @continue = leftIsParameterBound!=rightIsParameterBound;
            if (@continue) {
              meaningfulLefts.Push(leftAccess.Object);
              meaningfulRights.Push(rightAccess.Object);
            }
          }
          else if (@continue && rightConstant!=null) {
            var rightIsNullValue = rightConstant.Value==null;
            if (!rightIsNullValue) {
              @continue = false;
            }
            else {
              // A bare null-check such as `x.Field == null` may appear in a
              // FilterProvider predicate that the checker reaches before any
              // correlation equality has pushed anything onto the stacks
              // (e.g. `GroupBy(k).Select(g => g.Count(x => x.Field == null))`).
              // The legacy implementation called Pop() on an empty stack here
              // and crashed with InvalidOperationException("Stack empty.").
              // Treat it as "not a subquery-correlation filter" instead.
              var leftIsParameter = leftAccess.Object.NodeType==ExpressionType.Parameter;
              var stack = leftIsParameter ? meaningfulLefts : meaningfulRights;
              @continue = stack.TryPop(out var top) && top == leftAccess.Object;
            }
          }
          else {
            @continue = false;
          }
        }
        else if (b.NodeType==ExpressionType.AndAlso || b.NodeType==ExpressionType.OrElse) {
          @continue &= b.Left is BinaryExpression && b.Right is BinaryExpression;
        }
        else
          @continue = false;
        if (@continue) {
          Visit(b.Left);
          Visit(b.Right);
        }
        return b;

      }


      // Constructors

      public SubqueryFilterChecker(ApplyParameter filterParameter)
      {
        this.filterParameter = filterParameter;
        meaningfulLefts = new Stack<Expression>();
        meaningfulRights = new Stack<Expression>();
      }
    }

    private readonly SubqueryFilterChecker checker;

    internal protected override CompilableProvider VisitFilter(FilterProvider provider)
    {
      return checker.Check(provider.Predicate.Body)
        ? provider.Source
        : base.VisitFilter(provider);
    }

    public static CompilableProvider Process(CompilableProvider target, ApplyParameter filterParameter)
    {
      return new SubqueryFilterRemover(filterParameter).VisitCompilable(target);
    }


    // Constructors

    private SubqueryFilterRemover(ApplyParameter filterParameter)
    {
      checker = new SubqueryFilterChecker(filterParameter);
    }
  }
}
