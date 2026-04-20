// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlForceJoinOrderHint : SqlHint
  {
    /// <summary>
    /// Gets the corresponding tables.
    /// </summary>
    public IReadOnlyList<SqlTable> Tables { get; }

    internal override SqlForceJoinOrderHint Clone(SqlNodeCloneContext context) =>
      context.GetOrAdd(this, static (t, c) => {
        if (t.Tables is null)
          return new SqlForceJoinOrderHint();
        var source = t.Tables;
        var tablesClone = new SqlTable[source.Count];
        for (int i = 0; i < source.Count; i++)
          tablesClone[i] = (SqlTable) source[i].Clone(c);
        return new SqlForceJoinOrderHint(tablesClone);
      });

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
      Tables = tables;
    }
  }
}
