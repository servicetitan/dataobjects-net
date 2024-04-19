using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Linq;
using Xtensive.Linq.SerializableExpressions;
using Xtensive.Linq.SerializableExpressions.Internals;

using Xtensive.Reflection;
using System.Collections.Concurrent;

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
