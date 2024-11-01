// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
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

      var expressionsClone = new List<SqlExpression>(expressions.Count);
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