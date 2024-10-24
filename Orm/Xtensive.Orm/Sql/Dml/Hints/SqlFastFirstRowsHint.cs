// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlFastFirstRowsHint : SqlHint
  {
    /// <summary>
    /// Gets the rows amount.
    /// </summary>
    /// <value>The row amount.</value>
    public int Amount { get; private set; }

    internal override SqlFastFirstRowsHint Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Amount));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    // Constructors

    internal SqlFastFirstRowsHint(int amount)
    {
      Amount = amount;
    }
  }
}
