// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.11.06

using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  public class SqlDynamicFilter : SqlExpression
  {
    public object Id { get; private set; }

    public IReadOnlyList<SqlExpression> Expressions { get; private set; }

    internal override SqlDynamicFilter Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Id, t.Expressions.Select(e => e.Clone(c)).ToArray()));

    public override void AcceptVisitor(ISqlVisitor visitor) => visitor.Visit(this);

    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlDynamicFilter>(expression);
      Id = replacingExpression.Id;
      Expressions = replacingExpression.Expressions;
    }

    // Constructors

    internal SqlDynamicFilter(object id, IReadOnlyList<SqlExpression> expressions)
      : base(SqlNodeType.DynamicFilter)
    {
      Id = id;
      Expressions = expressions;
    }
  }

  public class SqlTvpDynamicFilter(object id, IReadOnlyList<SqlExpression> expressions) : SqlDynamicFilter(id, expressions)
  {
    internal override SqlTvpDynamicFilter Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) => new(t.Id, t.Expressions.Select(e => e.Clone(c)).ToArray()));
  }
}
