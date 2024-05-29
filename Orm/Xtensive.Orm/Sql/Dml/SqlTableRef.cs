// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Describes a reference to <see cref="Table"/> object;
  /// </summary>
  [Serializable]
  public class SqlTableRef : SqlTable
  {
    /// <summary>
    /// Gets the name of the instance.
    /// </summary>
    /// <value>The name.</value>
    public override string Name => string.IsNullOrEmpty(base.Name) ? DataTable.DbName : base.Name;

    /// <summary>
    /// Gets the referenced table.
    /// </summary>
    /// <value>The table.</value>
    public DataTable DataTable { get; private set; }

    internal override SqlTableRef Clone(SqlNodeCloneContext? context = null)
    {
      var ctx = context ?? new();
      return ctx.NodeMapping.TryGetValue(this, out var clone)
        ? (SqlTableRef) clone
        : CreateClone(ctx);
    }

    private SqlTableRef CreateClone(SqlNodeCloneContext context)
    {
      var clone = new SqlTableRef {Name = Name, DataTable = DataTable};
      context.NodeMapping[this] = clone;

      clone.columns = new SqlTableColumnCollection(columns.Select(column => (SqlTableColumn) column.Clone(context)).ToArray(columns.Count));

      return clone;
    }

    public override void AcceptVisitor(ISqlVisitor visitor) => visitor.Visit(this);

    // Constructors

    private SqlTableRef()
    { }

    internal SqlTableRef(DataTable dataTable)
      : this(dataTable, string.Empty, Array.Empty<string>())
    { }

    internal SqlTableRef(DataTable dataTable, string name)
      : this(dataTable, name, Array.Empty<string>())
    { }

    internal SqlTableRef(DataTable dataTable, string name, params string[] columnNames)
      : base(name)
    {
      DataTable = dataTable;
      var  tableColumns = columnNames.Length == 0
        ? dataTable.Columns.Select(column => SqlDml.TableColumn(this, column.Name)).ToArray(dataTable.Columns.Count)
        : columnNames.Select(columnName => SqlDml.TableColumn(this, columnName)).ToArray(columnNames.Length);
      columns = new SqlTableColumnCollection(tableColumns);
    }
  }
}