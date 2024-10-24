// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents order specification.
  /// </summary>
  [Serializable]
  public class SqlOrder : SqlNode
  {
    /// <summary>
    /// Gets the expression to sort by.
    /// </summary>
    /// <value>The expression.</value>
    public SqlExpression Expression { get; private set; }

    /// <summary>
    /// Gets the position of column to sort by.
    /// </summary>
    /// <value>The position.</value>
    public int Position { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="SqlOrder"/> is ascending.
    /// </summary>
    /// <value><see langword="true"/> if ascending; otherwise, <see langword="false"/>.</value>
    public bool Ascending { get; private set; }

    internal override SqlOrder Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        t.Expression is null
            ? new(t.Position, t.Ascending)
            : new(t.Expression.Clone(c), t.Ascending));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    // Constructors

    internal SqlOrder(SqlExpression expression, bool ascending)
      : base(SqlNodeType.Order)
    {
      Expression = expression;
      Ascending = ascending;
    }

    internal SqlOrder(int position, bool ascending) : base(SqlNodeType.Order)
    {
      Position = position;
      Ascending = ascending;
    }
  }
}
