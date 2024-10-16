// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlDefaultValue : SqlExpression
  {
    public override void ReplaceWith(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentValidator.EnsureArgumentIs<SqlDefaultValue>(expression);
    }
    
    internal override SqlDefaultValue Clone(SqlNodeCloneContext? context = null) => this;
    
    internal SqlDefaultValue() : base(SqlNodeType.DefaultValue)
    {
    }

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}
