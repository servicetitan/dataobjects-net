// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

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
    public SqlColumnCollection AliasedColumns { get; private set; }

    internal override SqlJoinedTable Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.joinExpression.Clone(c)) {
            AliasedColumns = new SqlColumnCollection(new List<SqlColumn>(t.AliasedColumns))
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

      var aliasedColumns = new List<SqlColumn>(leftColumns.Count + rightColumns.Count);
      aliasedColumns.AddRange(leftColumns);
      aliasedColumns.AddRange(rightColumns);
      AliasedColumns = new SqlColumnCollection(aliasedColumns);
    }
  }
}