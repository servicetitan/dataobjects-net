// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.06.22

using System.Linq.Expressions;
using Xtensive.Orm.Linq.Expressions.Visitors;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class MarkerExpression : ExtendedExpression
  {
    public Expression Target { get; private set; }
    public MarkerType MarkerType { get; private set; }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitMarker(this);

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    public MarkerExpression(Expression target, MarkerType markerType)
      : base(ExtendedExpressionType.Marker, target.Type)
    {
      Target = target;
      MarkerType = markerType;
    }
  }
}