// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.09.01

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlConcat : SqlExpressionList
  {
    internal override SqlConcat Clone(SqlNodeCloneContext? context = null)
    {
      var ctx = context ?? new();
      if (ctx.NodeMapping.TryGetValue(this, out var value)) {
        return (SqlConcat)value;
      }

      var expressionsClone = new List<SqlExpression>();
      foreach (var e in expressions)
        expressionsClone.Add(e.Clone(ctx));

      var clone = new SqlConcat(expressionsClone);
      return clone;
    }

    public override void ReplaceWith(SqlExpression expression)
    {
      expressions = ArgumentValidator.EnsureArgumentIs<SqlConcat>(expression).expressions;
    }
    
    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }


    // Constructors

    internal SqlConcat(IReadOnlyList<SqlExpression> expressions)
      : base(SqlNodeType.Concat, expressions)
    {
    }
  }
}