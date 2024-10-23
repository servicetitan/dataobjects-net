// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl;

[Serializable]
public class SqlCreateTable(Table table) : SqlStatement(SqlNodeType.Create), ISqlCompileUnit
{
  public Table Table { get; } = table;

  internal override SqlCreateTable Clone(SqlNodeCloneContext? context = null) =>
    context.GetOrAdd(this, static (t, c) => new(t.Table));

  public override void AcceptVisitor(ISqlVisitor visitor) => visitor.Visit(this);
}
