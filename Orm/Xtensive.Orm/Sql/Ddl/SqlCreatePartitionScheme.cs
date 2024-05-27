// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Ddl
{
  [Serializable]
  public class SqlCreatePartitionScheme : SqlStatement, ISqlCompileUnit
  {
    private PartitionSchema partitionSchema;

    public PartitionSchema PartitionSchema {
      get {
        return partitionSchema;
      }
    }

    internal override SqlCreatePartitionScheme Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.partitionSchema));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlCreatePartitionScheme(PartitionSchema partitionSchema)
      : base(SqlNodeType.Create)
    {
      this.partitionSchema = partitionSchema;
    }
  }
}
