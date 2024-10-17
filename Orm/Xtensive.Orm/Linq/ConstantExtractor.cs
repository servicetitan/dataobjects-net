// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.Linq
{
  /// <summary>
  /// An <see cref="ExpressionVisitor"/> specialized for extracting constants from specified <see cref="Expression"/>.
  /// This class can be used to produce "normalized" expression with all constants extracted to additional parameter.
  /// </summary>
  public sealed class ConstantExtractor : ExpressionVisitor
  {
    private static readonly ParameterExpression ConstantParameter = Expression.Parameter(WellKnownTypes.ObjectArray, "constants");
    private static readonly Expression[] ConstantExpressions = Enumerable.Range(0, 10).Select(i => Expression.ArrayIndex(ConstantParameter, Expr.Constant(i))).ToArray();

    private readonly Func<ConstantExpression, bool> constantFilter;
    private readonly LambdaExpression lambda;
    private List<object> constantValues;

    /// <summary>
    /// Gets an array of extracted constants.
    /// </summary>
    /// <value></value>
    public object[] GetConstants()
    {
      if (constantValues == null)
        throw new InvalidOperationException();
      return constantValues.ToArray();
    }

    /// <summary>
    /// Extracts constants from <see cref="LambdaExpression"/> specified in constructor.
    /// Result is a <see cref="LambdaExpression"/> with one additional parameter (array of objects).
    /// Extra parameter is added to first position.
    /// </summary>
    /// <returns><see cref="LambdaExpression"/> with all constants extracted to additional parameter.</returns>
    public LambdaExpression Process()
    {
      if (constantValues != null)
        throw new InvalidOperationException();
      constantValues = new List<object>();
      var parameters = lambda.Parameters.Prepend(ConstantParameter).ToArray();
      var body = Visit(lambda.Body);
      // Preserve original delegate type because it may differ from types of parameters / return value
      return FastExpression.Lambda(FixDelegateType(lambda.Type), body, parameters);
    }

    /// <inheritdoc/>
    protected override Expression VisitConstant(ConstantExpression c)
    {
      if (!constantFilter.Invoke(c))
        return c;
      var n = constantValues.Count;       
      var result = Expression.Convert(
        n < ConstantExpressions.Length
          ? ConstantExpressions[n]
          : Expression.ArrayIndex(ConstantParameter, Expr.Constant(n))
        , c.Type);
      constantValues.Add(c.Value);
      return result;
    }

    #region Private / internal method

    private static Type FixDelegateType(Type delegateType) =>
      Memoizer.Get(delegateType, static t =>
        DelegateHelper.GetDelegateSignature(t) switch {
          var signature => DelegateHelper.MakeDelegateType(signature.First, signature.Second.Prepend(ConstantParameter.Type), signature.Second.Length + 1)
        });

    private static bool DefaultConstantFilter(ConstantExpression constant)
    {
      // maybe: return !constant.Type.IsValueType;
      return true;
    }

    #endregion


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="lambda">An expression to process.</param>
    public ConstantExtractor(LambdaExpression lambda)
      : this(lambda, null)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="lambda">An expression to process.</param>
    /// <param name="constantFilter">The constant filter.
    /// This delegate invoked on each occurrence of <see cref="ConstantExpression"/>.
    /// If it returns <see langword="true"/>, constant is extracted, otherwise left untouched.
    /// </param>
    public ConstantExtractor(LambdaExpression lambda, Func<ConstantExpression, bool> constantFilter)
    {
      ArgumentNullException.ThrowIfNull(lambda);
      this.lambda = lambda;
      this.constantFilter = constantFilter ?? DefaultConstantFilter;
    }
  }
}
