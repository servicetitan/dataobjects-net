// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl;

[Serializable]
public class SqlAddColumn(TableColumn column) : SqlAction
{
  public TableColumn Column { get; } = column;

  internal override SqlAddColumn Clone(SqlNodeCloneContext context) =>
    context.GetOrAdd(this, static (t, c) => new(t.Column));
}

[Serializable]
public class SqlAlterColumn(TableColumn column) : SqlAction
{
  public TableColumn Column { get; } = column;

  internal override SqlAlterColumn Clone(SqlNodeCloneContext context) =>
    context.GetOrAdd(this, static (t, c) => new(t.Column));
}
