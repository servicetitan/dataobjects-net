// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlRow: SqlExpressionList
  {
    internal override SqlRow Clone(SqlNodeCloneContext context)
    {
      if (context.TryGet(this) is SqlRow existing) {
        return existing;
      }
      var count = expressions.Count;
      var clones = new SqlExpression[count];
      for (int i = 0; i < count; i++) {
        clones[i] = expressions[i].Clone(context);
      }
      return new SqlRow(clones);
    }

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
