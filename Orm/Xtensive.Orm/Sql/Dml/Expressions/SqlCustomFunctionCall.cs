// Copyright (C) 2014-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alena Mikshina
// Created:    2014.05.06

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlCustomFunctionCall : SqlFunctionCallBase
  {
    /// <summary>
    /// Gets the custom function type.
    /// </summary>
    public SqlCustomFunctionType FunctionType { get; private set; }

    public override void ReplaceWith(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression);
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlCustomFunctionCall>(expression);
      FunctionType = replacingExpression.FunctionType;
      Arguments = replacingExpression.Arguments;
    }

    internal override SqlCustomFunctionCall Clone(SqlNodeCloneContext context) =>
      context.GetOrAdd(this, static (t, c) => {
        var args = t.Arguments;
        var count = args.Count;
        var clones = new SqlExpression[count];
        for (int i = 0; i < count; i++) {
          clones[i] = args[i].Clone(c);
        }
        return new SqlCustomFunctionCall(t.FunctionType, clones);
      });

    public override void AcceptVisitor(ISqlVisitor visitor) => visitor.Visit(this);

    public SqlCustomFunctionCall(SqlCustomFunctionType sqlCustomFunctionType, IReadOnlyList<SqlExpression> arguments)
      : base(SqlNodeType.CustomFunctionCall, arguments)
    {
      FunctionType = sqlCustomFunctionType;
    }

    public SqlCustomFunctionCall(SqlCustomFunctionType sqlCustomFunctionType, params SqlExpression[] arguments)
      : base(SqlNodeType.CustomFunctionCall, arguments)
    {
      FunctionType = sqlCustomFunctionType;
    }
  }
}
