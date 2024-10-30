// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlJoinedTable : SqlTable
  {
    private SqlJoinExpression joinExpression;

    /// <summary>
    /// Gets the join expression.
    /// </summary>
    /// <value>The join expression.</value>
    public SqlJoinExpression JoinExpression
    {
      get { return joinExpression; }
    }

    /// <summary>
    /// Gets or sets the aliased columns.
    /// </summary>
    /// <value>Aliased columns.</value>
    public SqlColumnCollection AliasedColumns { get; init; }

    internal override SqlJoinedTable Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.joinExpression.Clone(c)) {
            AliasedColumns = new(t.AliasedColumns)
          });

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      joinExpression.AcceptVisitor(visitor);
    }

    /// <inheritdoc/>
    public override IEnumerator<SqlTable> GetEnumerator()
    {
      return joinExpression.GetEnumerator();
    }


    // Constructor

    internal SqlJoinedTable(SqlJoinExpression joinExpression)
      : this(joinExpression, joinExpression.Left.Columns, joinExpression.Right.Columns)
    {
    }

    internal SqlJoinedTable(SqlJoinExpression joinExpression, IReadOnlyList<SqlColumn> leftColumns, IReadOnlyList<SqlColumn> rightColumns)
    {
      this.joinExpression = joinExpression;
      var allLeftColumns = joinExpression.Left.Columns;
      var allRightColumns = joinExpression.Right.Columns;

      columns = new SqlTableColumnCollection(allLeftColumns.Concat(allRightColumns).ToArray(allLeftColumns.Count + allRightColumns.Count));

      AliasedColumns = new(leftColumns.Concat(rightColumns));
    }
  }
}
