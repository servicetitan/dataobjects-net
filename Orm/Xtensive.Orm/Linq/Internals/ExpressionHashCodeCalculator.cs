// Copyright (C) 2008-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.05.06

using System.Linq.Expressions;
using Xtensive.Core;

namespace Xtensive.Linq
{
  internal sealed class ExpressionHashCodeCalculator : ExpressionVisitor<int>
  {
    private const int NullHashCode = 0x7abf3456;
    private readonly ParameterExpressionRegistry parameters = new();

    public int CalculateHashCode(Expression expression)
    {
      ArgumentNullException.ThrowIfNull(expression);
      try {
        return Visit(expression);
      }
      finally {
        parameters.Reset();
      }
    }

    #region ExpressionVisitor<int> implementation

    protected override int Visit(Expression e) =>
      e == null ? NullHashCode : HashCode.Combine(base.Visit(e), e.NodeType, e.Type);

    protected override int VisitUnary(UnaryExpression u) =>
      HashCode.Combine(Visit(u.Operand), u.Method);

    protected override int VisitBinary(BinaryExpression b) =>
      HashCode.Combine(Visit(b.Left), Visit(b.Right), b.Method);

    protected override int VisitTypeIs(TypeBinaryExpression tb) =>
      HashCode.Combine(Visit(tb.Expression), tb.TypeOperand);

    protected override int VisitConstant(ConstantExpression c) =>
      c.Value?.GetHashCode() ?? NullHashCode;

    protected override int VisitDefault(DefaultExpression d) =>
      d.Type.IsValueType
        ? d.ToConstantExpression().Value.GetHashCode()
        : NullHashCode;

    protected override int VisitConditional(ConditionalExpression c) =>
      HashCode.Combine(Visit(c.Test), Visit(c.IfTrue), Visit(c.IfFalse));

    protected override int VisitParameter(ParameterExpression p) => parameters.GetIndex(p);

    protected override int VisitMemberAccess(MemberExpression m) =>
      HashCode.Combine(Visit(m.Expression), m.Member);

    protected override int VisitMethodCall(MethodCallExpression mc) => HashCode.Combine(Visit(mc.Object), mc.Method, HashExpressionSequence(mc.Arguments));

    protected override int VisitLambda(LambdaExpression l)
    {
      parameters.AddRange(l.Parameters);
      return HashCode.Combine(HashExpressionSequence(l.Parameters.Cast<Expression>()), Visit(l.Body));
    }

    protected override int VisitNew(NewExpression n) =>
      HashCode.Combine(n.Constructor, n.Members.CalculateHashCode(), HashExpressionSequence(n.Arguments));

    protected override int VisitMemberInit(MemberInitExpression mi)
    {
      HashCode hashCode = new();
      hashCode.Add(Visit(mi.NewExpression));
      foreach (var b in mi.Bindings) {
        hashCode.Add(b.BindingType);
        hashCode.Add(b.Member);
      }
      return hashCode.ToHashCode();
    }

    protected override int VisitListInit(ListInitExpression li)
    {
      HashCode hashCode = new();
      hashCode.Add(VisitNew(li.NewExpression));
      foreach (var e in li.Initializers) {
        hashCode.Add(e.AddMethod);
        hashCode.Add(HashExpressionSequence(e.Arguments));
      }
      return hashCode.ToHashCode();
    }

    protected override int VisitNewArray(NewArrayExpression na) =>
      HashExpressionSequence(na.Expressions);

    protected override int VisitInvocation(InvocationExpression i) =>
      HashCode.Combine(HashExpressionSequence(i.Arguments), Visit(i.Expression));

    protected override int VisitUnknown(Expression e) => e.GetHashCode();

    #endregion

    #region Private / internal methods

    private int HashExpressionSequence(IEnumerable<Expression> expressions)
    {
      HashCode hashCode = new();
      foreach (var e in expressions)
        hashCode.Add(Visit(e));
      return hashCode.ToHashCode();
    }

    #endregion
  }
}
