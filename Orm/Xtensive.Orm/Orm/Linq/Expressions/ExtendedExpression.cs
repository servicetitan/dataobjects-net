// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Linq.Expressions;
using Xtensive.Orm.Linq.Expressions.Visitors;
using Xtensive.Reflection;

namespace Xtensive.Orm.Linq.Expressions;

internal abstract class ExtendedExpression(ExtendedExpressionType expressionType, Type type) : Expression
{
  public ExtendedExpressionType ExtendedType { get; } = expressionType;

  public sealed override ExpressionType NodeType => (ExpressionType) ExtendedType;

  public override Type Type => type;

  internal virtual Expression Accept(ExtendedExpressionVisitor visitor) =>
    throw new NotSupportedException(string.Format(Strings.ExUnknownExpressionType, visitor.GetType().GetShortName(), NodeType));
}
