// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.04.17

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Rse.Transformation
{
  /// <summary>
  /// An expression visitor specialized for rewriting tuple access expressions.
  /// </summary>
  public class TupleAccessRewriter : ExpressionVisitor
  {
    private ParameterExpression tupleParameter;
    private bool ignoreMissing;
    protected readonly Func<ApplyParameter, ColNum, ColNum> resolveOuterColumn;

    public IReadOnlyList<ColNum> Mappings;

    /// <inheritdoc/>
    protected override Expression VisitUnknown(Expression e)
    {
      return e;
    }

    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      if (mc.IsTupleAccess(tupleParameter)) {
        var columnIndex = mc.GetTupleAccessArgument();
        var outerParameter = mc.GetApplyParameter();
        int newIndex = outerParameter != null
          ? resolveOuterColumn(outerParameter, columnIndex)
          : Mappings.IndexOf(columnIndex);
        if ((newIndex < 0 && ignoreMissing) || newIndex == columnIndex)
          return mc;
        return Expression.Call(mc.Object, mc.Method, Expr.Constant(newIndex));
      }
      return base.VisitMethodCall(mc);
    }

    /// <summary>
    /// Replaces column usages according to a specified map.
    /// </summary>
    /// <param name="expression">The predicate.</param>
    /// <returns></returns>
    public virtual Expression Rewrite(Expression expression)
    {
      return Visit(expression);
    }

    /// <summary>
    /// Replaces column usages according to a specified map.
    /// </summary>
    /// <param name="expression">The predicate.</param>
    /// <param name="parameter">The tuple parameter to be considered.</param>
    /// <returns></returns>
    public virtual Expression Rewrite(Expression expression, ParameterExpression parameter)
    {
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(parameter);
      tupleParameter = parameter;
      return Visit(expression);
    }
    
    private static ColNum DefaultResolveOuterColumn(ApplyParameter parameter, ColNum columnIndex)
    {
      throw new NotSupportedException();
    }

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="resolveOuterColumn">A <see langword="delegate"/> invoked when outer column usage is to be rewritten.</param>
    /// <param name="mappings">The mappings.</param>
    /// <param name="ignoreMissing">Indicates if the newly created <see cref="TupleAccessRewriter"/>
    /// should ignore rewriting accessors missing in the <paramref name="mappings"/> collection
    /// or not resolvable by <paramref name="resolveOuterColumn"/> delegate.</param>
    public TupleAccessRewriter(IReadOnlyList<ColNum> mappings, Func<ApplyParameter, ColNum, ColNum> resolveOuterColumn, bool ignoreMissing)
    {
      this.ignoreMissing = ignoreMissing;
      Mappings = mappings;
      this.resolveOuterColumn = resolveOuterColumn ?? DefaultResolveOuterColumn;
    }
  }
}