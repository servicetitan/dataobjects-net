// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

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
      ArgumentValidator.EnsureArgumentNotNull(replacingExpression.SqlTable, "SqlTable");
      ArgumentValidator.EnsureArgumentNotNull(replacingExpression.Name, "Name");
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
