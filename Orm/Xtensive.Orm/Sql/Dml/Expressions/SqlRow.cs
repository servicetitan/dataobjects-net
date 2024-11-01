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
    internal override SqlRow Clone(SqlNodeCloneContext? context = null)
    {
      SqlNodeCloneContext ctx = context ?? new();
      return (ctx.TryGet(this) as SqlRow) ?? new(expressions.Select(e => e.Clone(ctx)).ToArray());
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