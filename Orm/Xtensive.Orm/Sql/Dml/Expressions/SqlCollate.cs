// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Core;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlCollate : SqlExpression
  {
    private Collation collation;
    private SqlExpression operand;

    /// <summary>
    /// Gets the collation.
    /// </summary>
    /// <value>The collation.</value>
    public Collation Collation {
      get {
        return collation;
      }
    }

    /// <summary>
    /// Gets the operand.
    /// </summary>
    /// <value>The operand.</value>
    public SqlExpression Operand {
      get {
        return operand;
      }
    }

    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlCollate>(expression);
      operand = replacingExpression.Operand;
      collation = replacingExpression.Collation;
    }

    internal override SqlCollate Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.operand.Clone(c), t.collation));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlCollate(SqlExpression operand, Collation collation) : base(SqlNodeType.Collate)
    {
      this.operand = operand;
      this.collation = collation;
    }
  }
}
