// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  public class SqlJoinedTable : SqlTable
  {
    /// <summary>
    /// Gets the join expression.
    /// </summary>
    /// <value>The join expression.</value>
    public SqlJoinExpression JoinExpression { get; }

    /// <summary>
    /// Gets or sets the aliased columns.
    /// </summary>
    /// <value>Aliased columns.</value>
    public SqlColumnCollection AliasedColumns { get; init; }

    internal override SqlJoinedTable Clone(SqlNodeCloneContext context) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.JoinExpression.Clone(c)) {
            AliasedColumns = new(t.AliasedColumns)
          });

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      JoinExpression.AcceptVisitor(visitor);
    }

    /// <inheritdoc/>
    public override IEnumerator<SqlTable> GetEnumerator()
    {
      return JoinExpression.GetEnumerator();
    }


    // Constructor

    internal SqlJoinedTable(SqlJoinExpression joinExpression)
      : this(joinExpression, joinExpression.Left.Columns, joinExpression.Right.Columns)
    {
    }

    internal SqlJoinedTable(SqlJoinExpression joinExpression, IReadOnlyList<SqlColumn> leftColumns, IReadOnlyList<SqlColumn> rightColumns)
    {
      JoinExpression = joinExpression;
      var allLeftColumns = joinExpression.Left.Columns;
      var allRightColumns = joinExpression.Right.Columns;

      columns = new SqlTableColumnCollection(allLeftColumns.Concat(allRightColumns).ToArray());

      AliasedColumns = new(leftColumns.Concat(rightColumns));
    }
  }
}
