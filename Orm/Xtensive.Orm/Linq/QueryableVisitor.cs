// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.02.25

using System.Linq.Expressions;
using Xtensive.Reflection;

namespace Xtensive.Linq;

/// <summary>
/// Abstract base visitor that handles methods of <see cref="IQueryable"/> and <see cref="IEnumerable{T}"/> by calling <see cref="VisitQueryableMethod"/>.
/// </summary>
[Serializable]
public abstract class QueryableVisitor : ExpressionVisitor
{
  private static readonly Dictionary<string, QueryableMethodKind> QueryableMethodKindFromName = new() {
    [nameof(Queryable.Aggregate)] = QueryableMethodKind.Aggregate,
    [nameof(Queryable.All)] = QueryableMethodKind.All,
    [nameof(Queryable.Any)] = QueryableMethodKind.Any,
    ["AsEnumerable"] = QueryableMethodKind.AsEnumerable,
    ["AsQueryable"] = QueryableMethodKind.AsQueryable,
    [nameof(Queryable.Average)] = QueryableMethodKind.Average,
    [nameof(Queryable.Cast)] = QueryableMethodKind.Cast,
    [nameof(Queryable.Concat)] = QueryableMethodKind.Concat,
    [nameof(Queryable.Contains)] = QueryableMethodKind.Contains,
    [nameof(Queryable.Count)] = QueryableMethodKind.Count,
    [nameof(Queryable.DefaultIfEmpty)] = QueryableMethodKind.DefaultIfEmpty,
    [nameof(Queryable.Distinct)] = QueryableMethodKind.Distinct,
    [nameof(Queryable.DistinctBy)] = QueryableMethodKind.DistinctBy,
    [nameof(Queryable.ElementAt)] = QueryableMethodKind.ElementAt,
    [nameof(Queryable.ElementAtOrDefault)] = QueryableMethodKind.ElementAtOrDefault,
    [nameof(Queryable.Except)] = QueryableMethodKind.Except,
    [nameof(Queryable.First)] = QueryableMethodKind.First,
    [nameof(Queryable.FirstOrDefault)] = QueryableMethodKind.FirstOrDefault,
    [nameof(Queryable.GroupBy)] = QueryableMethodKind.GroupBy,
    [nameof(Queryable.GroupJoin)] = QueryableMethodKind.GroupJoin,
    [nameof(Queryable.Intersect)] = QueryableMethodKind.Intersect,
    [nameof(Queryable.Join)] = QueryableMethodKind.Join,
    [nameof(Queryable.Last)] = QueryableMethodKind.Last,
    [nameof(Queryable.LastOrDefault)] = QueryableMethodKind.LastOrDefault,
    [nameof(Queryable.LongCount)] = QueryableMethodKind.LongCount,
    [nameof(Queryable.Max)] = QueryableMethodKind.Max,
    [nameof(Queryable.Min)] = QueryableMethodKind.Min,
    [nameof(Queryable.OfType)] = QueryableMethodKind.OfType,
    [nameof(Queryable.OrderBy)] = QueryableMethodKind.OrderBy,
    [nameof(Queryable.OrderByDescending)] = QueryableMethodKind.OrderByDescending,
    [nameof(Queryable.Reverse)] = QueryableMethodKind.Reverse,
    [nameof(Queryable.Select)] = QueryableMethodKind.Select,
    [nameof(Queryable.SelectMany)] = QueryableMethodKind.SelectMany,
    [nameof(Queryable.SequenceEqual)] = QueryableMethodKind.SequenceEqual,
    [nameof(Queryable.Single)] = QueryableMethodKind.Single,
    [nameof(Queryable.SingleOrDefault)] = QueryableMethodKind.SingleOrDefault,
    [nameof(Queryable.Skip)] = QueryableMethodKind.Skip,
    [nameof(Queryable.SkipWhile)] = QueryableMethodKind.SkipWhile,
    [nameof(Queryable.Sum)] = QueryableMethodKind.Sum,
    [nameof(Queryable.Take)] = QueryableMethodKind.Take,
    [nameof(Queryable.TakeWhile)] = QueryableMethodKind.TakeWhile,
    [nameof(Queryable.ThenBy)] = QueryableMethodKind.ThenBy,
    [nameof(Queryable.ThenByDescending)] = QueryableMethodKind.ThenByDescending,
    ["ToArray"] = QueryableMethodKind.ToArray,
    ["ToList"] = QueryableMethodKind.ToList,
    [nameof(Queryable.Union)] = QueryableMethodKind.Union,
    [nameof(Queryable.Where)] = QueryableMethodKind.Where
  };

  /// <inheritdoc/>
  protected override Expression VisitMethodCall(MethodCallExpression mc)
  {
    var mcArguments = mc.Arguments;
    return (mcArguments.Count > 0 && mcArguments[0].Type == WellKnownTypes.String)
           || !(GetQueryableMethod(mc) is { } method)
      ? base.VisitMethodCall(mc)
      : VisitQueryableMethod(mc, method);
  }

  /// <summary>
  /// Visits method of <see cref="IQueryable"/> or <see cref="IEnumerable{T}"/>.
  /// </summary>
  /// <param name="mc">The method call expression.</param>
  /// <param name="methodKind">Kind of the method.</param>
  protected abstract Expression VisitQueryableMethod(MethodCallExpression mc, QueryableMethodKind methodKind);

  /// <summary>
  /// Parses <see cref="QueryableMethodKind"/> for the specified expression.
  /// </summary>
  /// <param name="call">A call to process.</param>
  /// <returns><see cref="QueryableMethodKind"/> for the specified expression,
  /// or null if method is not a LINQ method.</returns>
  public static QueryableMethodKind? GetQueryableMethod(MethodCallExpression call) =>
    call?.Method.DeclaringType is { } declaringType
    && (declaringType == WellKnownTypes.Queryable || declaringType == WellKnownTypes.Enumerable)
      ? QueryableMethodKindFromName.GetValueOrDefault(call.Method.Name)
      : null;
}
