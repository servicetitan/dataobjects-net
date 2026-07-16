// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

namespace Xtensive.Sql.Dml;

/// <summary>
/// Base class for DML statements.
/// </summary>
[Serializable]
public abstract class SqlQueryStatement(SqlNodeType nodeType) : SqlStatement(nodeType)
{
  private List<SqlHint> hints;

  /// <summary>
  /// Gets or sets the tag comment attached to this statement.
  /// </summary>
  public SqlComment Comment { get; set; }

  /// <summary>
  /// Gets the collection of join hints.
  /// </summary>
  /// <value>The collection of join hints.</value>
  public IReadOnlyList<SqlHint> Hints => hints ?? [];

  public void AddHint(SqlHint hint) => (hints ??= new(1)).Add(hint);
}
