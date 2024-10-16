// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlContinue : SqlStatement
  {
    internal override SqlContinue Clone(SqlNodeCloneContext? context = null) => this;

    internal SqlContinue()
      : base(SqlNodeType.Continue)
    {
    }

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}
