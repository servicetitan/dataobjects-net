// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents sub query expression.
  /// </summary>
  [Serializable]
  public class SqlSubQuery: SqlExpression
  {
    private ISqlQueryExpression query;

    /// <summary>
    /// Gets the query.
    /// </summary>
    /// <value>The query.</value>
    public ISqlQueryExpression Query
    {
      get { return query; }
    }

    /// <inheritdoc/>
    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlSubQuery>(expression);
      query = replacingExpression.Query;
    }

    internal override SqlSubQuery Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.query is SqlSelect select
          ? select.Clone(c)
          : (t.query as SqlQueryExpression).Clone(c)
          )
      );

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlSubQuery(ISqlQueryExpression query)
      : base(SqlNodeType.SubSelect)
    {
      this.query = query;
    }
  }
}