using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;

namespace Xtensive.Orm.Linq.Model
{
  internal static class QueryableMethodInfo
  {
    public static readonly MethodInfo Select = MethodOf(q => q.Select(o => o));

    public static readonly MethodInfo GroupBy = MethodOf(q => q.GroupBy(o => o));
    internal static readonly MethodInfoParams GroupByParams = new(GroupBy);

    public static readonly MethodInfo GroupByWithElementSelector = MethodOf(q => q.GroupBy(o => o, o => o.ToString()));
    internal static readonly MethodInfoParams GroupByWithElementSelectorParams = new(GroupByWithElementSelector);

    public static readonly MethodInfo GroupByWithResultSelector = MethodOf(q => q.GroupBy(o => o, (key, items) => items.Count()));
    internal static readonly MethodInfoParams GroupByWithResultSelectorParams = new(GroupByWithResultSelector);

    public static readonly MethodInfo GroupByWithElementAndResultSelectors = MethodOf(q => q.GroupBy(o => o, o => o.ToString(), (key, items) => items.Count()));
    internal static readonly MethodInfoParams GroupByWithElementAndResultSelectorsParams = new(GroupByWithElementAndResultSelectors);

    private static MethodInfo MethodOf<T>(Expression<Func<IQueryable<object>, IQueryable<T>>> methodExpression)
    {
      var callExpression = (MethodCallExpression) methodExpression.Body.StripCasts();
      return callExpression.Method.GetGenericMethodDefinition();
    }
  }
}
