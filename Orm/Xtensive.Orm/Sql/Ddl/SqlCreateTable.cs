// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

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
