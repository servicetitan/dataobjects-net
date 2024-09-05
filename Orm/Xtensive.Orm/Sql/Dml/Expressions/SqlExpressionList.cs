// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.09.01

using System.Collections;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  public abstract class SqlExpressionList : SqlExpression, IReadOnlyList<SqlExpression>
  {
    protected IReadOnlyList<SqlExpression> expressions;

    /// <inheritdoc/>
    public SqlExpression this[int index] => expressions[index];

    /// <inheritdoc/>
    public int Count => expressions.Count;

    /// <inheritdoc/>
    public IEnumerator<SqlExpression> GetEnumerator() => expressions.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(SqlExpression item) => expressions.IndexOf(item);

    protected SqlExpressionList(SqlNodeType nodeType, IReadOnlyList<SqlExpression> expressions)
      : base(nodeType)
    {
      this.expressions = expressions;
    }
  }
}