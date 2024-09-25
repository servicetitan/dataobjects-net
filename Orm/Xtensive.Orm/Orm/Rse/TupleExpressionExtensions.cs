// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Elena Vakhtina
// Created:    2009.03.20

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.Internals;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse
{
  /// <summary>
  /// Various extension methods for manipulating expressions with <see cref="Tuple"/>.
  /// </summary>
  public static class TupleExpressionExtensions
  {
    /// <summary>
    /// Checks if expression is access to tuple.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <param name="tupleParameter">Tuple parameter that access must be on.</param>
    /// <returns></returns>
    public static bool IsTupleAccess(this Expression expression, ParameterExpression tupleParameter) =>
      (tupleParameter == null ? expression.AsTupleAccess() : expression.AsTupleAccess(tupleParameter)) != null;

    /// <summary>
    /// Checks if expression is access to tuple.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <returns></returns>
    public static bool IsTupleAccess(this Expression expression) => expression.IsTupleAccess(null);

    /// <summary>
    /// If <paramref name="expression"/> is an access to tuple element
    /// returns <paramref name="expression"/> casted to <see cref="MethodCallExpression"/>.
    /// Otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="expression">An expression to check.</param>
    /// <returns></returns>
    public static MethodCallExpression AsTupleAccess(this Expression expression) =>
      expression.NodeType == ExpressionType.Call
          && (MethodCallExpression) expression is var mc
          && mc.Object?.Type == WellKnownOrmTypes.Tuple
          && mc.Method.Name is Reflection.WellKnown.Tuple.GetValue or Reflection.WellKnown.Tuple.GetValueOrDefault
        ? mc
        : null;

    /// <summary>
    /// If <paramref name="expression"/> is an access to tuple element.
    /// returns <paramref name="expression"/> casted to <see cref="MethodCallExpression"/>.
    /// Otherwise returns <see langword="null"/>.
    /// This method only accepts access to specified parameter and access to outer parameters (<see cref="ApplyParameter"/>).
    /// </summary>
    /// <param name="expression">An expression to check.</param>
    /// <param name="currentParameter"><see cref="ParameterExpression"/> considered as current parameter.</param>
    /// <returns></returns>
    public static MethodCallExpression AsTupleAccess(this Expression expression, ParameterExpression currentParameter) =>
      expression.AsTupleAccess() is { } tupleAccess
          && (tupleAccess.Object == currentParameter || GetApplyParameterExpression(tupleAccess) != null)
        ? tupleAccess
        : null;

    /// <summary>
    /// If <paramref name="expression"/> is an access to tuple element.
    /// returns <paramref name="expression"/> casted to <see cref="MethodCallExpression"/>.
    /// Otherwise returns <see langword="null"/>.
    /// This method only accepts access to specified parameters and access to outer parameters (<see cref="ApplyParameter"/>).
    /// </summary>
    /// <param name="expression">An expression to check.</param>
    /// <param name="currentParameters"><see cref="ParameterExpression"/>s  considered as current parameters.</param>
    /// <returns></returns>
    public static MethodCallExpression AsTupleAccess(this Expression expression, IEnumerable<ParameterExpression> currentParameters) =>
      expression.AsTupleAccess() is { } tupleAccess
          && (tupleAccess.Object is ParameterExpression target && currentParameters.Contains(target)
              || GetApplyParameterExpression(tupleAccess) != null)
        ? tupleAccess
        : null;

    /// <summary>
    /// Gets the tuple access argument (column index).
    /// </summary>
    /// <param name="expression">An expression describing an access to tuple element.</param>
    /// <returns></returns>
    public static ColNum GetTupleAccessArgument(this Expression expression) =>
      expression.AsTupleAccess() is { } mc
        ? (ColNum) Evaluate<int>(mc.Arguments[0])
        : throw new ArgumentException(string.Format(Strings.ExParameterXIsNotATupleAccessExpression, "expression"));

    /// <summary>
    /// Tries to extract apply parameter from <paramref name="expression"/>.
    /// If <paramref name="expression"/> is an access to column of outer tuple returns <see cref="ApplyParameter"/> instance.
    /// Otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="expression">The expression describing an access to outer tuple.</param>
    /// <returns></returns>
    public static ApplyParameter GetApplyParameter(this Expression expression) =>
      GetApplyParameterExpression(expression) is { } e
        ? Evaluate<ApplyParameter>(e)
        : null;

    private static Expression GetApplyParameterExpression(Expression expression) =>
      expression.AsTupleAccess()?.Object is { } tupleAccessObject
          && tupleAccessObject.NodeType == ExpressionType.MemberAccess
          && (MemberExpression) tupleAccessObject is var memberAccess
          && memberAccess.Expression is { } memberAccessExpression
          && memberAccessExpression.Type == WellKnownOrmTypes.ApplyParameter
          && memberAccess.Member.Name == "Value"
      ? memberAccessExpression
      : null;

    private static T Evaluate<T>(Expression expression)
    {
      if (expression.NodeType==ExpressionType.Constant)
        return (T) ((ConstantExpression) expression).Value;
      return FastExpression.Lambda<Func<T>>(expression).CachingCompile().Invoke();
    }
  }
}
