// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Linq;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlForceJoinOrderHint : SqlHint
  {
    private SqlTable[] tables;

    /// <summary>
    /// Gets the corresponding tables.
    /// </summary>
    public IEnumerable<SqlTable> Tables { get { return tables; } }

    internal override SqlForceJoinOrderHint Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.tables?.Select(table => table.Clone()).ToArray(t.tables.Length)));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    // Constructors

    internal SqlForceJoinOrderHint()
    {
    }

    internal SqlForceJoinOrderHint(SqlTable[] tables)
    {
      this.tables = tables;
    }
  }
}
