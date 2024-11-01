// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Core;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Defines a reference to <see cref="DataTableColumn"/> object
  /// </summary>
  [Serializable]
  public class SqlTableColumn : SqlColumn, ISqlLValue
  {
    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = (SqlColumn) expression;
      ArgumentNullException.ThrowIfNull(replacingExpression.SqlTable, "SqlTable");
      ArgumentNullException.ThrowIfNull(replacingExpression.Name, "Name");
      base.ReplaceWith(expression);
    }

    internal override SqlTableColumn Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => {
        var table = t.SqlTable;
        if (c.NodeMapping.TryGetValue(t.SqlTable, out var clonedTable)) {
          table = (SqlTable) clonedTable;
        }
        return new(table, t.Name);
      });

    // Constructors

    internal SqlTableColumn(SqlTable sqlTable, string name)
      : base(sqlTable, name)
    {
    }

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}
