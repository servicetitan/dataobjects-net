// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlRow: SqlExpressionList
  {
    internal override SqlRow Clone(SqlNodeCloneContext context) =>
      context.GetOrAdd(this, static (t, c) => {
        var source = t.expressions;
        var expressionsClone = new SqlExpression[source.Count];
        for (int i = 0; i < source.Count; i++)
          expressionsClone[i] = source[i].Clone(c);
        return new SqlRow(expressionsClone);
      });

    public override void ReplaceWith(SqlExpression expression) =>
      expressions = ArgumentValidator.EnsureArgumentIs<SqlRow>(expression).expressions;

    public override void AcceptVisitor(ISqlVisitor visitor) => visitor.Visit(this);

    // Constructors

    internal SqlRow(IReadOnlyList<SqlExpression> expressions)
      : base(SqlNodeType.Row, expressions)
    {
    }
  }
}
