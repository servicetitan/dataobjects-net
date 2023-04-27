// Copyright (C) 2008-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2008.11.11

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Xtensive.Core;

namespace Xtensive.Linq
{
  /// <summary>
  /// An abstract base implementation of <see cref="ExpressionVisitor{TResult}"/>
  /// returning <see cref="Expression"/> as its visit result.
  /// </summary>
  public abstract class ExpressionVisitor : ExpressionVisitor<Expression>
  {
    protected override IReadOnlyList<Expression> VisitExpressionList(ReadOnlyCollection<Expression> expressions) =>
      VisitList(expressions, Visit);

    /// <summary>
    /// Visits the element initializer expression.
    /// </summary>
    /// <param name="initializer">The initializer.</param>
    /// <returns>Visit result.</returns>
    protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
    {
      var initializerArguments = initializer.Arguments;
      var arguments = VisitExpressionList(initializerArguments);
      if (arguments != initializerArguments) {
        return Expression.ElementInit(initializer.AddMethod, arguments);
      }
      return initializer;
    }

    /// <summary>
    /// Visits the element initializer list.
    /// </summary>
    /// <param name="original">The original element initializer list.</param>
    /// <returns>Visit result.</returns>
    protected virtual IReadOnlyList<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original) =>
      VisitList(original, VisitElementInitializer);

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression u) =>
      Visit(u, u.Operand, static (u, operand) => Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method));

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression b)
    {
      var bLeft = b.Left;
      var bRight = b.Right;
      Expression left = Visit(bLeft);
      Expression right = Visit(bRight);
      if ((left == bLeft) && (right == bRight))
        return b;
      return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
    }

    /// <inheritdoc/>
    protected override Expression VisitTypeIs(TypeBinaryExpression tb) =>
      Visit(tb, tb.Expression, static (tb, expression) => Expression.TypeIs(expression, tb.TypeOperand));

    /// <inheritdoc/>
    protected override Expression VisitConstant(ConstantExpression c)
    {
      return c;
    }

    /// <inheritdoc/>
    protected override Expression VisitDefault(DefaultExpression d)
    {
      return d.ToConstantExpression();
    }

    /// <inheritdoc/>
    protected override Expression VisitConditional(ConditionalExpression c)
    {
      var cTest = c.Test;
      var cIfTrue = c.IfTrue;
      var cIfFalse = c.IfFalse;
      Expression test = Visit(cTest);
      Expression ifTrue = Visit(cIfTrue);
      Expression ifFalse = Visit(cIfFalse);
      if (((test == cTest) && (ifTrue == cIfTrue)) && (ifFalse == cIfFalse))
        return c;
      return Expression.Condition(test, ifTrue, ifFalse);
    }

    /// <inheritdoc/>
    protected override Expression VisitParameter(ParameterExpression p)
    {
      return p;
    }

    /// <inheritdoc/>
    protected override Expression VisitMemberAccess(MemberExpression m) =>
      Visit(m, m.Expression, static (m, expression) => Expression.MakeMemberAccess(expression, m.Member));

    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      var mcObject = mc.Object;
      Expression instance = Visit(mcObject);
      var mcArguments = mc.Arguments;
      IEnumerable<Expression> arguments = VisitExpressionList(mcArguments);
      return instance == mcObject && arguments == mcArguments
        ? mc
        : Expression.Call(instance, mc.Method, arguments);
    }

    /// <summary>
    /// Visits the member assignment expression.
    /// </summary>
    /// <param name="ma">The member assignment expression.</param>
    /// <returns>Visit result.</returns>
    protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment ma) =>
      Visit(ma, ma.Expression, static (ma, expression) => Expression.Bind(ma.Member, expression));

    /// <inheritdoc/>
    protected override Expression VisitLambda(LambdaExpression l) =>
      Visit(l, l.Body, static (l, body) => FastExpression.Lambda(l.Type, body, l.Parameters));

    private TOriginal Visit<TOriginal, TSubExpression>(TOriginal original, TSubExpression subExpression, Func<TOriginal, Expression, TOriginal> func) where TSubExpression : Expression
    {
      var newExpr = Visit(subExpression);
      return newExpr != subExpression
        ? func(original, newExpr)
        : original;
    }

    /// <inheritdoc/>
    protected override Expression VisitNew(NewExpression n)
    {
      var nArguments = n.Arguments;
      IEnumerable<Expression> arguments = VisitExpressionList(nArguments);
      if (arguments == nArguments)
        return n;
      if (n.Members != null)
        return Expression.New(n.Constructor, arguments, n.Members);
      return Expression.New(n.Constructor, arguments);
    }

    /// <inheritdoc/>
    protected override Expression VisitMemberInit(MemberInitExpression mi)
    {
      var miNewExpression = mi.NewExpression;
      var miBindings = mi.Bindings;
      var newExpression = (NewExpression) VisitNew(miNewExpression);
      IEnumerable<MemberBinding> bindings = VisitBindingList(miBindings);
      if ((newExpression == miNewExpression) && (bindings == miBindings))
        return mi;
      return Expression.MemberInit(newExpression, bindings);
    }

    /// <inheritdoc/>
    protected override Expression VisitListInit(ListInitExpression li)
    {
      var liNewExpression = li.NewExpression;
      var liInitializers = li.Initializers;
      var newExpression = (NewExpression) VisitNew(liNewExpression);
      IEnumerable<ElementInit> initializers = VisitElementInitializerList(liInitializers);
      if ((newExpression == liNewExpression) && (initializers == liInitializers))
        return li;
      return Expression.ListInit(newExpression, initializers);
    }

    /// <inheritdoc/>
    protected override Expression VisitNewArray(NewArrayExpression na)
    {
      var naExpressions = na.Expressions;
      IEnumerable<Expression> initializers = VisitExpressionList(naExpressions);
      if (initializers == naExpressions)
        return na;
      if (na.NodeType == ExpressionType.NewArrayInit)
        return Expression.NewArrayInit(na.Type.GetElementType(), initializers);
      return Expression.NewArrayBounds(na.Type.GetElementType(), initializers);
    }

    /// <inheritdoc/>
    protected override Expression VisitInvocation(InvocationExpression i)
    {
      var iArguments = i.Arguments;
      IEnumerable<Expression> arguments = VisitExpressionList(iArguments);
      Expression expression = Visit(i.Expression);
      if ((arguments == iArguments) && (expression == i.Expression))
        return i;
      return Expression.Invoke(expression, arguments);
    }

    #region Member bindings methods

    /// <summary>
    /// Visits the member binding.
    /// </summary>
    /// <param name="binding">The member binding.</param>
    /// <returns>Visit result.</returns>
    protected virtual MemberBinding VisitBinding(MemberBinding binding)
    {
      switch (binding.BindingType) {
        case MemberBindingType.Assignment:
          return VisitMemberAssignment((MemberAssignment) binding);
        case MemberBindingType.MemberBinding:
          return VisitMemberMemberBinding((MemberMemberBinding) binding);
        case MemberBindingType.ListBinding:
          return VisitMemberListBinding((MemberListBinding) binding);
        default:
          throw new Exception($"Unhandled binding type '{binding.BindingType}'");
      }
    }

    /// <summary>
    /// Visits the member member binding.
    /// </summary>
    /// <param name="binding">The member member binding.</param>
    /// <returns>Visit result.</returns>
    protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
    {
      var bindingBindings = binding.Bindings;
      IEnumerable<MemberBinding> bindings = VisitBindingList(bindingBindings);
      if (bindings != bindingBindings) {
        return Expression.MemberBind(binding.Member, bindings);
      }
      return binding;
    }

    /// <summary>
    /// Visits the binding list.
    /// </summary>
    /// <param name="original">The original binding list.</param>
    /// <returns>Visit result.</returns>
    protected virtual IReadOnlyList<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original) =>
      VisitList(original, VisitBinding);

    private static IReadOnlyList<T> VisitList<T>(ReadOnlyCollection<T> original, Func<T, T> func) where T : class
    {
      var n = original.Count;
      var results = new T[n];
      bool isChanged = false;
      for (int i = 0; i < n; i++) {
        var originalValue = original[i];
        isChanged |= !ReferenceEquals(originalValue, results[i] = func(originalValue));
      }
      return isChanged ? results.AsSafeWrapper() : original;
    }

    protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
    {
      var bindingInitializers = binding.Initializers;
      IEnumerable<ElementInit> initializers = VisitElementInitializerList(bindingInitializers);
      if (initializers != bindingInitializers)
        return Expression.ListBind(binding.Member, initializers);
      return binding;
    }

    #endregion

    // Constructors

    /// <inheritdoc/>
    protected ExpressionVisitor()
    {
    }

    /// <inheritdoc/>
    protected ExpressionVisitor(bool isCaching)
      : base(isCaching)
    {
    }
  }
}
