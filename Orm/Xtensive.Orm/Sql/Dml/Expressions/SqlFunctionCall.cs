// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlFunctionCall: SqlExpression
  {
    /// <summary>
    /// Gets the expressions.
    /// </summary>
    public List<SqlExpression> Arguments { get; }

    /// <summary>
    /// Gets the function type.
    /// </summary>
    public SqlFunctionType FunctionType { get; private set; }

    public override void ReplaceWith(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      ArgumentValidator.EnsureArgumentIs<SqlFunctionCall>(expression, "expression");
      var replacingExpression = (SqlFunctionCall) expression;
      FunctionType = replacingExpression.FunctionType;
      Arguments.Clear();
      Arguments.AddRange(replacingExpression.Arguments);
    }

    internal override object Clone(SqlNodeCloneContext context)
    {
      if (context.NodeMapping.TryGetValue(this, out var value)) {
        return value;
      }

      var clone = new SqlFunctionCall(FunctionType);
      for (int i = 0, l = Arguments.Count; i < l; i++)
        clone.Arguments.Add((SqlExpression)Arguments[i].Clone(context));
      context.NodeMapping[this] = clone;
      return clone;
    }

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    // Constructors

    internal SqlFunctionCall(SqlFunctionType functionType, IEnumerable<SqlExpression> arguments)
      : base(SqlNodeType.FunctionCall)
    {
      FunctionType = functionType;
      Arguments = new List<SqlExpression>();
      Arguments.AddRange(arguments);
    }

    internal SqlFunctionCall(SqlFunctionType functionType, params SqlExpression[] arguments)
      : base(SqlNodeType.FunctionCall)
    {
      FunctionType = functionType;
      Arguments = new List<SqlExpression>(arguments?.Length ?? 0);
      if (arguments != null) {
        Arguments.AddRange(arguments);
      }
    }
  }
}