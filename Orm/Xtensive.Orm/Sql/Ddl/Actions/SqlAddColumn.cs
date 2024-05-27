// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlAddColumn : SqlAction
  {
    public TableColumn Column { get; private set; }

    internal override SqlAddColumn Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Column));

    // Constructors

    internal SqlAddColumn(TableColumn column)
    {
      Column = column;
    }
  }
}
