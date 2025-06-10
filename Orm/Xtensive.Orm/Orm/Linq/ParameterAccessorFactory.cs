// Copyright (C) 2020-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.Internals;
using Xtensive.Reflection;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Linq;

internal static class ParameterAccessorFactory
{
  private static readonly MethodInfo GetParameterValueMethod =
    WellKnownOrmTypes.ParameterContext.GetMethod(nameof(ParameterContext.GetValue));

  private static readonly ParameterExpression ParameterContextArgument = Expression.Parameter(WellKnownOrmTypes.ParameterContext, "context");

  private class ParameterAccessorFactoryImpl<T>(ParameterExpression parameterContextArgument) : ExpressionVisitor
  {
    public static readonly ParameterAccessorFactoryImpl<T> Instance = new(ParameterContextArgument);

    public Expression<Func<ParameterContext,T>> BindToParameterContext(Expression parameterExpression)
    {
      var body = Visit(parameterExpression);
      var resultType = typeof(T);
      if (resultType != body.Type) {
        body = Expression.Convert(body, resultType);
      }
      return FastExpression.Lambda<Func<ParameterContext, T>>(body, parameterContextArgument);
    }

    protected override Expression VisitMember(MemberExpression ma)
    {
      if (string.Equals(nameof(Parameter<T>.Value), ma.Member.Name, StringComparison.Ordinal)
        && WellKnownOrmTypes.Parameter.IsAssignableFrom(ma.Expression.Type)) {
        var parameterType = ma.Expression.Type;
        var parameterValueType = parameterType.IsGenericType
          ? parameterType.GetGenericArguments()[0]
          : WellKnownTypes.Object;
        return Expression.Call(parameterContextArgument,
          GetParameterValueMethod.CachedMakeGenericMethod(parameterValueType), ma.Expression);
      }

      return base.VisitMember(ma);
    }
  }

  public static Expression<Func<ParameterContext, T>> CreateAccessorExpression<T>(Expression parameterExpression) =>
    ParameterAccessorFactoryImpl<T>.Instance.BindToParameterContext(parameterExpression);
}
