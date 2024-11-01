// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using Xtensive.Core;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents binary expression.
  /// </summary>
  [Serializable]
  public class SqlBinary : SqlExpression
  {
    /// <summary>
    /// Gets the left operand of the binary operator.
    /// </summary>
    /// <value>The left operand of the binary operator.</value>
    public SqlExpression Left { get; private set; }

    /// <summary>
    /// Gets the right operand of the binary operator.
    /// </summary>
    /// <value>The right operand of the binary operator.</value>
    public SqlExpression Right { get; private set; }

    public static bool operator true(SqlBinary operand) =>
      operand.NodeType switch {
        SqlNodeType.Equals => (object) operand.Right == (object) operand.Left,
        SqlNodeType.NotEquals => (object) operand.Right != (object) operand.Left,
        SqlNodeType.And => ((SqlBinary)operand.Left ? true : false) && ((SqlBinary)operand.Right ? true : false),
        SqlNodeType.Or => ((SqlBinary)operand.Left ? true : false) || ((SqlBinary)operand.Right ? true : false),
        _ =>  false
      };

    public static bool operator false(SqlBinary operand) =>
      operand.NodeType switch {
        SqlNodeType.Equals => (object) operand.Right != (object) operand.Left,
        SqlNodeType.NotEquals => (object) operand.Right == (object) operand.Left,
        SqlNodeType.And => !((SqlBinary) operand.Left ? true : false) && ((SqlBinary) operand.Right ? true : false),
        SqlNodeType.Or => !((SqlBinary) operand.Left ? true : false) || ((SqlBinary) operand.Right ? true : false),
        _ => false
      };

    public override void ReplaceWith(SqlExpression expression)
    {
      var replacingExpression = ArgumentValidator.EnsureArgumentIs<SqlBinary>(expression);
      NodeType = replacingExpression.NodeType;
      Left = replacingExpression.Left;
      Right = replacingExpression.Right;
    }

    internal override SqlBinary Clone(SqlNodeCloneContext? context = null) =>
      context.GetOrAdd(this, static (t, c) =>
        new(t.NodeType, t.Left.Clone(c), t.Right.Clone(c)));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    // Constructors

    internal SqlBinary(SqlNodeType nodeType, SqlExpression left, SqlExpression right) : base(nodeType)
    {
      Left = left;
      Right = right;
    }
  }
}
