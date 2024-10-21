// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.05.06

using System.Diagnostics;
using System.Linq.Expressions;
using Xtensive.Core;

namespace Xtensive.Linq
{
  /// <summary>
  /// A wrapper for <see cref="System.Linq.Expressions.Expression"/>.
  /// that can be used for comparing expression trees and calculating their hash codes.
  /// </summary>
  [DebuggerDisplay("{expression}")]
  internal readonly struct ExpressionTree(Expression expr) : IEquatable<ExpressionTree>
  {
    private readonly Expression expression = expr;
    private readonly int hashCode = new ExpressionHashCodeCalculator().CalculateHashCode(expr);

    /// <summary>
    /// Gets the underlying <see cref="Expression"/>.
    /// </summary>
    /// <returns></returns>
    public Expression ToExpression() => expression;

    #region ToString, GetHashCode, Equals, ==, != implementation

    /// <inheritdoc/>
    public override string ToString() => expression.ToString(true);

    /// <inheritdoc/>
    public override int GetHashCode() => hashCode;

    /// <inheritdoc/>
    public bool Equals(ExpressionTree other) => new ExpressionComparer().AreEqual(expression, other.expression);

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is ExpressionTree other && Equals(other);

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>.
    /// Otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator == (ExpressionTree left, ExpressionTree right) => left.Equals(right);

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is not equal to <paramref name="right"/>.
    /// Otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator != (ExpressionTree left, ExpressionTree right) => !left.Equals(right);

    #endregion

    /// <summary>
    /// Compares specified <see cref="Expression"/>s by value.
    /// </summary>
    /// <param name="left">First expression to compare.</param>
    /// <param name="right">Second expression to compare.</param>
    /// <returns>true, if <paramref name="left"/> and <paramref name="right"/>
    /// are equal by value, otherwise false.</returns>
    public static bool Equals(Expression left, Expression right) => new ExpressionComparer().AreEqual(left, right);

    /// <summary>
    /// Calculates hash code by value for the specified <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">Expression to calculate hash code for.</param>
    /// <returns>Hash code for <paramref name="expression"/>.</returns>
    public static int GetHashCode(Expression expression) => new ExpressionHashCodeCalculator().CalculateHashCode(expression);
  }
}
