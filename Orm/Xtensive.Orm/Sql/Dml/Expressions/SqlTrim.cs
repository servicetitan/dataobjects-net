// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents Trim function call.
  /// </summary>
  [Serializable]
  public class SqlTrim : SqlExpression
  {
    private SqlExpression expression;
    private string trimCharacters;
    private SqlTrimType trimType;

    /// <summary>
    /// Gets the expression.
    /// </summary>
    /// <value>The expression.</value>
    public SqlExpression Expression {
      get {
        return expression;
      }
    }

    /// <summary>
    /// Gets the trim characters.
    /// </summary>
    public string TrimCharacters {
      get {
        return trimCharacters;
      }
    }

    public SqlTrimType TrimType
    {
      get { return trimType; }
    }

    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlTrim>(expression);
      this.expression = replacingExpression.expression;
      trimCharacters = replacingExpression.trimCharacters;
      trimType = replacingExpression.TrimType;
    }

    internal override SqlTrim Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.expression.Clone(c), t.trimCharacters, t.trimType));

    internal SqlTrim(SqlExpression expression, string trimCharacters, SqlTrimType trimType) : base (SqlNodeType.Trim)
    {
      this.expression = expression;
      this.trimCharacters = trimCharacters;
      this.trimType = trimType;
    }

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}
