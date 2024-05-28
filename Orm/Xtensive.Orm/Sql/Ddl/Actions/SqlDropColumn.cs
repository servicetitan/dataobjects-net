// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlDropColumn : SqlCascadableAction
  {
    public TableColumn Column { get; private set; }
    
    internal override SqlDropColumn Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Column));

    // Constructors

    internal SqlDropColumn(TableColumn column)
      : base(true)
    {
      Column = column;
    }

    internal SqlDropColumn(TableColumn column, bool cascade)
      : base(cascade)
    {
      Column = column;
    }
  }
}
