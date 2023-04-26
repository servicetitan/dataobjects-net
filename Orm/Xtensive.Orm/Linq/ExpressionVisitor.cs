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
    protected override IReadOnlyList<Expression> VisitExpressionList(ReadOnlyCollection<Expression> expressions)
    {
      bool isChanged = false;
      var expressionCount = expressions.Count;
      var results = new Expression[expressionCount];
      for (int i = 0, n = expressionCount; i < n; i++) {
        var expression = expressions[i];
        var p = Visit(expression);
        results[i] = p;
        isChanged |= !ReferenceEquals(expression, p);
      }
      return isChanged ? results.AsSafeWrapper() : expressions;
    }

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
    protected virtual IReadOnlyList<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
    {
      var results = new List<ElementInit>();
      bool isChanged = false;
      for (int i = 0, n = original.Count; i < n; i++) {
        var originalIntializer = original[i];
        ElementInit p = VisitElementInitializer(originalIntializer);
        results.Add(p);
        isChanged |= !ReferenceEquals(originalIntializer, p);
      }
      return isChanged ? results.AsSafeWrapper() : original;
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression u)
    {
      var uOperand = u.Operand;
      Expression operand = Visit(uOperand);
      if (operand != uOperand)
        return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
      return u;
    }

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
    protected override Expression VisitTypeIs(TypeBinaryExpression tb)
    {
      var tbExpression = tb.Expression;
      Expression expression = Visit(tbExpression);
      if (expression != tbExpression)
        return Expression.TypeIs(expression, tb.TypeOperand);
      return tb;
    }

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
    protected override Expression VisitMemberAccess(MemberExpression m)
    {
      Expression expression = Visit(m.Expression);
      if (expression!=m.Expression)
        return Expression.MakeMemberAccess(expression, m.Member);
      return m;
    }

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
    protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment ma)
    {
      var maExpression = ma.Expression;
      Expression expression = Visit(maExpression);
      if (expression != maExpression)
        return Expression.Bind(ma.Member, expression);
      return ma;
    }

    /// <inheritdoc/>
    protected override Expression VisitLambda(LambdaExpression l)
    {
      var lBody = l.Body;
      Expression body = Visit(lBody);
      if (body != lBody)
        return FastExpression.Lambda(l.Type, body, l.Parameters);
      return l;
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
    protected virtual IReadOnlyList<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
    {
      var results = new List<MemberBinding>();
      bool isChanged = false;
      for (int i = 0, n = original.Count; i < n; i++) {
        var originalBinding = original[i];
        MemberBinding p = VisitBinding(originalBinding);
        results.Add(p);
        isChanged |= !ReferenceEquals(originalBinding, p);
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
