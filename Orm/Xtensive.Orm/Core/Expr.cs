using System.Linq.Expressions;

namespace Xtensive.Core
{
  internal static class Expr
  {
    private static readonly ConstantExpression[] IntConstantExpressions = new ConstantExpression[2000];

    public static ConstantExpression Constant(int v) =>
        (uint)v < IntConstantExpressions.Length
            ? IntConstantExpressions[v] ??= Expression.Constant(v)
            : Expression.Constant(v);
  }
}
