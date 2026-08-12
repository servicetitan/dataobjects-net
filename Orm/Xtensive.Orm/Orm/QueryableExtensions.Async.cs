// Copyright (C) 2020-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.Linq;
using Xtensive.Reflection;

namespace Xtensive.Orm
{
  /// <summary>
  /// Extends LINQ methods for <see cref="Xtensive.Orm.Linq"/> queries.
  /// </summary>
  public static partial class QueryableExtensionsEx
  {
    private static class ParameterTraits<TSource>
    {
      public static readonly ParameterExpression[] ItemParam = [Expression.Parameter(typeof(TSource), "item")];
    }

    private static readonly object BoxedZero = 0;

    /// <summary>
    /// A wrapper to transform non-<see cref="IAsyncEnumerable{T}"/>, yet based on <see cref="QueryProvider"/>,
    /// <see cref="IQueryable{T}"/> implementation, such as <see cref="EntitySet{TItem}"/>, into <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    private sealed class QueryAsAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
      private readonly QueryProvider queryProvider;
      private readonly Expression expression;

      /// <inheritdoc/>
      public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
      {
        var result = await queryProvider.ExecuteSequenceAsync<T>(expression, cancellationToken).ConfigureAwaitFalse();
        var asyncSource = result.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwaitFalse();
        await foreach (var element in asyncSource) {
          yield return element;
        }
      }

      public QueryAsAsyncEnumerable(QueryProvider queryProvider, Expression expression)
      {
        this.queryProvider = queryProvider;
        this.expression = expression;
      }
    }

    private static class AllTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, bool>(WellKnownMembers.Queryable.All);
    }

    /// <summary>
    /// Asynchronously determines whether all the elements of a sequence satisfy a condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> whose elements to test for a condition.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true
    /// if every element of the source sequence passes the test in the specified predicate;
    /// otherwise, false. </returns>
    public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<bool>(AllTraits<TSource>.Method, source, predicate, cancellationToken);

    private static class AnyTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, bool>(WellKnownMembers.Queryable.Any);
    }

    /// <summary>
    /// Asynchronously determines whether a sequence contains any elements.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to check for being empty.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true
    /// if the source sequence contains any elements; otherwise, false.</returns>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<bool>(AnyTraits<TSource>.Method, source, cancellationToken);

    private static class AnyWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, bool>(WellKnownMembers.Queryable.AnyWithPredicate);
    }

    /// <summary>
    /// Asynchronously determines whether any element of a sequence satisfies a condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> whose elements to test for a condition.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true
    /// if any elements in the source sequence pass the test in the specified predicate;
    /// otherwise, false.</returns>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<bool>(AnyWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #region AverageAsync

    // Average<int>

    private static readonly MethodInfo AverageIntDoubleMethod = NormalizeOperation<int, double>(WellKnownMembers.Queryable.AverageInt32);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageIntDoubleMethod, source, cancellationToken);

    private static readonly MethodInfo AverageNullableIntDoubleMethod = NormalizeOperation<int?, double?>(WellKnownMembers.Queryable.AverageNullableInt32);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageNullableIntDoubleMethod, source, cancellationToken);

    private static class AverageWithSelectorInt32Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double>(WellKnownMembers.Queryable.AverageWithSelectorInt32);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageWithSelectorInt32Traits<TSource>.Method, source, selector, cancellationToken);

    private static class AverageWithSelectorNullableInt32Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double?>(WellKnownMembers.Queryable.AverageWithSelectorNullableInt32);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageWithSelectorNullableInt32Traits<TSource>.Method, source, selector, cancellationToken);

    // Average<long>

    private static readonly MethodInfo AverageLongDoubleMethod = NormalizeOperation<long, double>(WellKnownMembers.Queryable.AverageInt64);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageLongDoubleMethod, source, cancellationToken);

    private static readonly MethodInfo AverageNullableLongDoubleMethod = NormalizeOperation<long?, double?>(WellKnownMembers.Queryable.AverageNullableInt64);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageNullableLongDoubleMethod, source, cancellationToken);

    private static class AverageWithSelectorInt64Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double>(WellKnownMembers.Queryable.AverageWithSelectorInt64);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageWithSelectorInt64Traits<TSource>.Method, source, selector, cancellationToken);

    private static class AverageWithSelectorNullableInt64Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double?>(WellKnownMembers.Queryable.AverageWithSelectorNullableInt64);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageWithSelectorNullableInt64Traits<TSource>.Method, source, selector, cancellationToken);

    // Average<double>

    private static readonly MethodInfo AverageDoubleMethod = NormalizeOperation<double, double>(WellKnownMembers.Queryable.AverageDouble);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageDoubleMethod, source, cancellationToken);

    private static readonly MethodInfo AverageNullableDoubleMethod = NormalizeOperation<double?, double?>(WellKnownMembers.Queryable.AverageNullableDouble);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageNullableDoubleMethod, source, cancellationToken);

    private static class AverageWithSelectorDoubleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double>(WellKnownMembers.Queryable.AverageWithSelectorDouble);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(AverageWithSelectorDoubleTraits<TSource>.Method, source, selector, cancellationToken);

    private static class AverageWithSelectorNullableDoubleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double?>(WellKnownMembers.Queryable.AverageWithSelectorNullableDouble);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(AverageWithSelectorNullableDoubleTraits<TSource>.Method, source, selector, cancellationToken);

    // Average<float>

    private static readonly MethodInfo AverageSingleMethod = NormalizeOperation<float, float>(WellKnownMembers.Queryable.AverageSingle);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float>(AverageSingleMethod, source, cancellationToken);

    private static readonly MethodInfo AverageNullableSingleMethod = NormalizeOperation<float?, float?>(WellKnownMembers.Queryable.AverageNullableSingle);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float?>(AverageNullableSingleMethod, source, cancellationToken);

    private static class AverageWithSelectorSingleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, float>(WellKnownMembers.Queryable.AverageWithSelectorSingle);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float>(AverageWithSelectorSingleTraits<TSource>.Method, source, selector, cancellationToken);

    private static class AverageWithSelectorNullableSingleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, float?>(WellKnownMembers.Queryable.AverageWithSelectorNullableSingle);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float?>(AverageWithSelectorNullableSingleTraits<TSource>.Method, source, selector, cancellationToken);

    // Average<decimal>

    private static readonly MethodInfo AverageDecimalMethod = NormalizeOperation<decimal, decimal>(WellKnownMembers.Queryable.AverageDecimal);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal>(AverageDecimalMethod, source, cancellationToken);

    private static readonly MethodInfo AverageNullableDecimalMethod = NormalizeOperation<decimal?, decimal?>(WellKnownMembers.Queryable.AverageNullableDecimal);

    /// <summary>
    /// Asynchronously computes the average of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the sequence of values.</returns>
    public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal?>(AverageNullableDecimalMethod, source, cancellationToken);

    private static class AverageWithSelectorDecimalTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, decimal>(WellKnownMembers.Queryable.AverageWithSelectorDecimal);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal>(AverageWithSelectorDecimalTraits<TSource>.Method, source, selector, cancellationToken);

    private static class AverageWithSelectorNullableDecimalTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, decimal?>(WellKnownMembers.Queryable.AverageWithSelectorNullableDecimal);
    }

    /// <summary>
    /// Asynchronously computes the average of a sequence of values that is obtained
    /// by invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// average of the projected values.</returns>
    public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal?>(AverageWithSelectorNullableDecimalTraits<TSource>.Method, source, selector, cancellationToken);

    #endregion

    // Contains

    private static class ContainsTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, bool>(WellKnownMembers.Queryable.Contains);
    }

    /// <summary>
    /// Asynchronously determines whether a sequence contains a specified element.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the ssingle element of.</param>
    /// <param name="item">The object to locate in the sequence.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true
    /// if the input sequence contains the specified value; otherwise, false.</returns>
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<bool>(ContainsTraits<TSource>.Method, source, Expression.Constant(item, typeof(TSource)), cancellationToken);

    #region Count

    private static class CountTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, int>(WellKnownMembers.Queryable.Count);
    }

    /// <summary>
    /// Asynchronously returns the number of elements in a sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains elements to be counted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// number of elements in the input sequence.</returns>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int>(CountTraits<TSource>.Method, source, cancellationToken);

    private static class CountWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, int>(WellKnownMembers.Queryable.CountWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the number of elements in a sequence that satisfy a condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains elements to be counted.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// number of elements in the sequence that satisfy the condition in the predicate function.</returns>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int>(CountWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #endregion

    #region First, FirstOrDefault

    // First

    private static class FirstTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.First);
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the first element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    ///  first element in source.</returns>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(FirstTraits<TSource>.Method, source, cancellationToken);

    private static class FirstWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.FirstWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence that satisfies a specified condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the first element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    ///  first element in source that passes the test in predicate.</returns>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(FirstWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    // FirstOrDefault

    private static class FirstOrDefaultTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.FirstOrDefault);
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence, or a default value if
    /// the sequence contains no elements.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the first element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains default
    /// (TSource) if source is empty; otherwise, the first element in source.</returns>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(FirstOrDefaultTraits<TSource>.Method, source, cancellationToken);

    private static class FirstOrDefaultWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.FirstOrDefaultWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the first element of a sequence that satisfies a specified
    /// condition or a default value if no such element is found.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the first element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains default
    /// (TSource) if source is empty or if no element passes the test specified by predicate;
    /// otherwise, the first element in source that passes the test specified by predicate.</returns>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(FirstOrDefaultWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #endregion

    #region Last, LastOrDefault
    // Last

    private static class LastTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.Last);
    }

    /// <summary>
    /// Asynchronously returns the last element of a sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the last element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// last element in source.</returns>
    public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(LastTraits<TSource>.Method, source, cancellationToken);

    private static class LastWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.LastWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the last element of a sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the last element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// last element in source.</returns>
    public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(LastWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    // LastOrDefault

    private static class LastOrDefaultTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.LastOrDefault);
    }

    /// <summary>
    /// Asynchronously returns the last element of a sequence, or a default value if
    /// the sequence contains no elements.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the last element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains default
    /// (TSource) if source is empty; otherwise, the last element in source.</returns>
    public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(LastOrDefaultTraits<TSource>.Method, source, cancellationToken);

    private static class LastOrDefaultWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.LastOrDefaultWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the last element of a sequence that satisfies a specified
    /// condition or a default value if no such element is found.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the last element of.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains default
    /// (TSource) if source is empty or if no element passes the test specified by predicate;
    /// otherwise, the last element in source that passes the test specified by predicate.</returns>
    public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(LastOrDefaultWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #endregion

    #region LongCount

    private static class LongCountTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, long>(WellKnownMembers.Queryable.LongCount);
    }

    /// <summary>
    /// Asynchronously returns an System.Int64 that represents the number of elements
    /// in a sequence that satisfy a condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements to be counted.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// number of elements in the input sequence.</returns>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long>(LongCountTraits<TSource>.Method, source, cancellationToken);

    private static class LongCountWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, long>(WellKnownMembers.Queryable.LongCountWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns an System.Int64 that represents the number of elements
    /// in a sequence that satisfy a condition.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements to be counted.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// number of elements in the sequence that satisfy the condition in the predicate
    /// function.</returns>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long>(LongCountWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #endregion

    #region Min, Max

    // Max

    private static class MaxTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.Max);
    }

    /// <summary>
    /// Asynchronously returns the maximum value of a sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements ot determine the maximum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// maximum value in the sequence.</returns>
    public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(MaxTraits<TSource>.Method, source, cancellationToken);

    private static class MaxWithSelectorTraits<TSource, TResult>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TResult>(WellKnownMembers.Queryable.MaxWithSelector);
    }

    /// <summary>
    /// Asynchronously invokes a projection function on each element of a sequence and
    /// returns the maximum resulting value.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TResult">he type of the value returned by the function represented by selector.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements ot determine the maximum of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// maximum value in the sequence.</returns>
    public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TResult>(MaxWithSelectorTraits<TSource, TResult>.Method, source, selector, cancellationToken);

    // Min

    private static class MinTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.Min);
    }

    /// <summary>
    /// Asynchronously returns the minimum value of a sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements ot determine the minimum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(MinTraits<TSource>.Method, source, cancellationToken);

    private static class MinWithSelectorTraits<TSource, TResult>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TResult>(WellKnownMembers.Queryable.MinWithSelector);
    }

    /// <summary>
    /// Asynchronously invokes a projection function on each element of a sequence and
    /// returns the minimum resulting value.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the function represented by selector.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> that contains the elements ot determine the minimum of.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TResult>(MinWithSelectorTraits<TSource, TResult>.Method, source, selector, cancellationToken);

    #endregion

    #region Single, SingleOrDefault

    // Single

    private static class SingleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.Single);
    }

    /// <summary>
    /// Asynchronously returns the only element of a sequence that satisfies a specified
    /// condition, and throws an exception if more than one such element exists.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the single element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// single element of the input sequence that satisfies the condition in predicate.</returns>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(SingleTraits<TSource>.Method, source, cancellationToken);

    private static class SingleWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.SingleWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the only element of a sequence that satisfies a specified
    /// condition, and throws an exception if more than one such element exists.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the single element of.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// single element of the input sequence that satisfies the condition in predicate.</returns>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(SingleWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    // SingleOrDefault

    private static class SingleOrDefaultTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.SingleOrDefault);
    }

    /// <summary>
    /// Asynchronously returns the only element of a sequence, or a default value if
    /// the sequence is empty; this method throws an exception if there is more than
    /// one element in the sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the single element of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// single element of the input sequence, or default (TSource) if the sequence contains
    /// no elements.</returns>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(SingleOrDefaultTraits<TSource>.Method, source, cancellationToken);

    private static class SingleOrDefaultWithPredicateTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, TSource>(WellKnownMembers.Queryable.SingleOrDefaultWithPredicate);
    }

    /// <summary>
    /// Asynchronously returns the only element of a sequence, or a default value if
    /// the sequence is empty; this method throws an exception if there is more than
    /// one element in the sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to return the single element of.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// single element of the input sequence that satisfies the condition in predicate,
    /// or default (TSource) if no such element is found.</returns>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<TSource>(SingleOrDefaultWithPredicateTraits<TSource>.Method, source, predicate, cancellationToken);

    #endregion

    #region Sum

    // Sum<int>

    private static readonly MethodInfo IntSumMethod = NormalizeOperation<int, int>(WellKnownMembers.Queryable.SumInt32);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int>(IntSumMethod, source, cancellationToken);

    private static readonly MethodInfo NullableIntSumMethod = NormalizeOperation<int?, int?>(WellKnownMembers.Queryable.SumNullableInt32);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int?>(NullableIntSumMethod, source, cancellationToken);

    private static class SumWithSelectorInt32Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, int>(WellKnownMembers.Queryable.SumWithSelectorInt32);
    }


    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int>(SumWithSelectorInt32Traits<TSource>.Method, source, selector, cancellationToken);

    private static class SumWithSelectorNullableInt32Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, int?>(WellKnownMembers.Queryable.SumWithSelectorNullableInt32);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<int?>(SumWithSelectorNullableInt32Traits<TSource>.Method, source, selector, cancellationToken);

    // Sum<long>

    private static readonly MethodInfo LongSumMethod = NormalizeOperation<long, long>(WellKnownMembers.Queryable.SumInt64);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long>(LongSumMethod, source, cancellationToken);

    private static readonly MethodInfo NullableLongSumMethod = NormalizeOperation<long?, long?>(WellKnownMembers.Queryable.SumNullableInt64);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long?>(NullableLongSumMethod, source, cancellationToken);

    private static class SumWithSelectorInt64Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, long>(WellKnownMembers.Queryable.SumWithSelectorInt64);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long>(SumWithSelectorInt64Traits<TSource>.Method, source, selector, cancellationToken);

    private static class SumWithSelectorNullableInt64Traits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, long?>(WellKnownMembers.Queryable.SumWithSelectorNullableInt64);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<long?>(SumWithSelectorNullableInt64Traits<TSource>.Method, source, selector, cancellationToken);

    // Sum<double>

    private static readonly MethodInfo SumDoubleMethod = NormalizeOperation<double, double>(WellKnownMembers.Queryable.SumDouble);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(SumDoubleMethod, source, cancellationToken);

    private static readonly MethodInfo NullableDoubleSumMethod = NormalizeOperation<double?, double?>(WellKnownMembers.Queryable.SumNullableDouble);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(NullableDoubleSumMethod, source, cancellationToken);

    private static class SumWithSelectorDoubleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double>(WellKnownMembers.Queryable.SumWithSelectorDouble);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double>(SumWithSelectorDoubleTraits<TSource>.Method, source, selector, cancellationToken);

    private static class SumWithSelectorNullableDoubleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, double?>(WellKnownMembers.Queryable.SumWithSelectorNullableDouble);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<double?>(SumWithSelectorNullableDoubleTraits<TSource>.Method, source, selector, cancellationToken);

    // Sum<float>

    private static readonly MethodInfo SumSingleMethod = NormalizeOperation<float, float>(WellKnownMembers.Queryable.SumSingle);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float>(SumSingleMethod, source, cancellationToken);

    private static readonly MethodInfo SumNullableSingleMethod = NormalizeOperation<float?, float?>(WellKnownMembers.Queryable.SumNullableSingle);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float?>(SumNullableSingleMethod, source, cancellationToken);

    private static class SumWithSelectorSingleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, float>(WellKnownMembers.Queryable.SumWithSelectorSingle);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float>(SumWithSelectorSingleTraits<TSource>.Method, source, selector, cancellationToken);

    private static class SumWithSelectorNullableSingleTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, float?>(WellKnownMembers.Queryable.SumWithSelectorNullableSingle);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<float?>(SumWithSelectorNullableSingleTraits<TSource>.Method, source, selector, cancellationToken);

    // Sum<decimal>

    private static readonly MethodInfo SumDecimalMethod = NormalizeOperation<decimal, decimal>(WellKnownMembers.Queryable.SumDecimal);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal>(SumDecimalMethod, source, cancellationToken);

    private static readonly MethodInfo SumNullableDecimalMethod = NormalizeOperation<decimal?, decimal?>(WellKnownMembers.Queryable.SumNullableDecimal);

    /// <summary>
    /// Asynchronously computes the sum of a sequence of values.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <param name="source">A sequence of values to calculate the sum of.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the values in the sequence.</returns>
    public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal?>(SumNullableDecimalMethod, source, cancellationToken);

    private static class SumWithSelectorDecimalTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, decimal>(WellKnownMembers.Queryable.SumWithSelectorDecimal);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal>(SumWithSelectorDecimalTraits<TSource>.Method, source, selector, cancellationToken);

    private static class SumWithSelectorNullableDecimalTraits<TSource>
    {
      public static readonly MethodInfo Method = NormalizeOperation<TSource, decimal?>(WellKnownMembers.Queryable.SumWithSelectorNullableDecimal);
    }

    /// <summary>
    /// Asynchronously computes the sum of the sequence of values that is obtained by
    /// invoking a projection function on each element of the input sequence.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values of type <typeparamref name="TSource"/>.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the
    /// sum of the projected values.</returns>
    public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) =>
      ExecuteScalarAsync<decimal?>(SumWithSelectorNullableDecimalTraits<TSource>.Method, source, selector, cancellationToken);

    #endregion

    #region Collection methods

    private static readonly MethodInfo TupleCreateMethod =
      typeof(ValueTuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(mi => mi.Name == nameof(ValueTuple.Create) && mi.GetGenericArguments().Length == 2);

    private static class Traits<TKey, TSource>
    {
      public static readonly MethodInfo TupleFactoryMethod = TupleCreateMethod.CachedMakeGenericMethod(typeof(TKey), typeof(TSource));
    }

    /// <summary>
    /// Asynchronously creates a <see cref="List{TSource}"/> from an <see cref="IQueryable{TSource}"/>
    /// by enumerating it asynchronously.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="List{TSource}"/> from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="List{TSource}"/> that contains values from the input sequence.</returns>
    public static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source,
      CancellationToken cancellationToken = default)
    {
      if (source is not IAsyncEnumerable<TSource> asyncEnumerable) {
        return source.ToList();
      }
      var list = new List<TSource>();
      await foreach (var element in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwaitFalse()) {
        list.Add(element);
      }

      return list;
    }

    /// <summary>
    /// Asynchronously creates an array from an <see cref="IQueryable{TSource}"/> System.Linq.IQueryable`1
    /// by enumerating it asynchronously.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create an array from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an
    /// array that contains values from the input sequence.</returns>
    public static async Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source,
      CancellationToken cancellationToken = default) =>
      (await source.ToListAsync(cancellationToken).ConfigureAwaitFalse()).ToArray();

    private static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(this IQueryable<(TKey, TValue)> query, CancellationToken cancellationToken)
    {
      Dictionary<TKey, TValue> dictionary = [];
      await foreach (var (k, v) in query.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwaitFalse()) {
        dictionary.Add(k, v);
      }
      return dictionary;
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey, TSource}"/> from an <see cref="IQueryable{TSource}"/>
    /// by enumerating it asynchronously according to a specified key selector function.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TKey">>The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="Dictionary{TKey, TSource}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/>
    /// selected from the input sequence.</returns>
    public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TKey, TSource>(
      this IQueryable<TSource> source,
      Expression<Func<TSource, TKey>> keySelector, CancellationToken cancellationToken = default)
    {
      var itemParam = ParameterTraits<TSource>.ItemParam;
      var body = Expression.Call(Traits<TKey, TSource>.TupleFactoryMethod,
        ExpressionReplacer.ReplaceAll(keySelector.Body, keySelector.Parameters, itemParam),
        itemParam[0]);
      var query = source.Select(FastExpression.Lambda<Func<TSource, ValueTuple<TKey, TSource>>>(body, itemParam));
      return ToDictionaryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IQueryable{TSource}"/>
    /// by enumerating it asynchronously according to a specified key selector and value selector functions.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TKey">>The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TValue">>The type of the key returned by <paramref name="valueSelector"/>.</typeparam>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="valueSelector">A function to extract a value from each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TValue"/>
    /// selected from the input sequence.</returns>
    public static Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue, TSource>(
      this IQueryable<TSource> source,
      Expression<Func<TSource, TKey>> keySelector,
      Expression<Func<TSource, TValue>> valueSelector,
      CancellationToken cancellationToken = default)
    {
      var itemParam = ParameterTraits<TSource>.ItemParam;
      var body = Expression.Call(Traits<TKey, TValue>.TupleFactoryMethod,
        ExpressionReplacer.ReplaceAll(keySelector.Body, keySelector.Parameters, itemParam),
        ExpressionReplacer.ReplaceAll(valueSelector.Body, valueSelector.Parameters, itemParam));
      var query = source.Select(FastExpression.Lambda<Func<TSource, ValueTuple<TKey, TValue>>>(body, itemParam));
      return ToDictionaryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Asynchronously creates a <see cref="HashSet{TSource}"/> from an <see cref="IQueryable{TSource}"/>
    /// by enumerating it asynchronously.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="HashSet{TSource}"/> from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="HashSet{TSource}"/> that contains values of type <typeparamref name="TSource"/> from the input sequence.</returns>
    public static async Task<HashSet<TSource>> ToHashSetAsync<TSource>(this IQueryable<TSource> source,
      CancellationToken cancellationToken = default)
    {
      var hashSet = new HashSet<TSource>();
      var asyncSource = source.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwaitFalse();
      await foreach (var element in asyncSource) {
        _ = hashSet.Add(element);
      }

      return hashSet;
    }

    private static async Task<ILookup<TKey, TValue>> ToLookupAsync<TKey, TValue>(this IQueryable<(TKey, TValue)> query, CancellationToken cancellationToken) =>
      (await query.ExecuteAsync(cancellationToken).ConfigureAwaitFalse())
      .ToLookup(tuple => tuple.Item1, tuple => tuple.Item2);

    /// <summary>
    /// Asynchronously creates a <see cref="ILookup{TKey, TSource}"/> from an <see cref="IQueryable{T}"/>
    /// by enumerating it asynchronously according to a specified key selector function.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey, TSource}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="ILookup{TKey, TSource}"/> that contains values of type <typeparamref name="TSource"/>
    /// selected from the input sequence.</returns>
    public static Task<ILookup<TKey, TSource>> ToLookupAsync<TKey, TSource>(this IQueryable<TSource> source,
      Expression<Func<TSource, TKey>> keySelector, CancellationToken cancellationToken = default)
    {
      var itemParam = ParameterTraits<TSource>.ItemParam;
      var body = Expression.Call(Traits<TKey, TSource>.TupleFactoryMethod,
        ExpressionReplacer.ReplaceAll(keySelector.Body, keySelector.Parameters, itemParam),
        itemParam[0]);
      var query = source.Select(FastExpression.Lambda<Func<TSource, ValueTuple<TKey, TSource>>>(body, itemParam));
      return ToLookupAsync(query, cancellationToken);
    }

    /// <summary>
    /// Asynchronously creates a <see cref="ILookup{TKey, TValue}"/> from an <see cref="IQueryable{T}"/>
    /// by enumerating it asynchronously according to a specified key selector and an
    /// element selector function.
    /// </summary>
    /// <remarks>
    /// Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.
    /// Notice that operation executes query so with some session options (like <see cref="Configuration.SessionOptions.ClientProfile"/>)
    /// result may not include newly created or locally removed entities or their data. Save local changes for them to be taken into account.
    /// </remarks>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TValue">The type of the value returned by <paramref name="valueSelector"/>.</typeparam>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey, TValue}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="valueSelector">A function to extract a value from each element.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="ILookup{TKey, TElement}"/> that contains values of type <typeparamref name="TValue"/>
    /// selected from the input sequence.</returns>
    public static Task<ILookup<TKey, TValue>> ToLookupAsync<TKey, TValue, TSource>(this IQueryable<TSource> source,
      Expression<Func<TSource, TKey>> keySelector,
      Expression<Func<TSource, TValue>> valueSelector,
      CancellationToken cancellationToken = default)
    {
      var itemParam = ParameterTraits<TSource>.ItemParam;
      var body = Expression.Call(Traits<TKey, TValue>.TupleFactoryMethod,
        ExpressionReplacer.ReplaceAll(keySelector.Body, keySelector.Parameters, itemParam),
        ExpressionReplacer.ReplaceAll(valueSelector.Body, valueSelector.Parameters, itemParam));
      var query = source.Select(FastExpression.Lambda<Func<TSource, ValueTuple<TKey, TValue>>>(body, itemParam));
      return ToLookupAsync(query, cancellationToken);
    }

    #endregion

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{TSource}"/> which can be enumerated asynchronously.
    /// </summary>
    /// <remarks>Multiple active operations in the same session instance are not supported. Use
    /// <see langword="await"/> to ensure that all asynchronous operations have completed before calling
    /// another method in this session.</remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IQueryable{T}"/> to enumerate.</param>
    /// <returns>The query results.</returns>
    public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
    {
      ArgumentNullException.ThrowIfNull(source);

      if (source is IAsyncEnumerable<TSource> nativeAsyncEnumerable) {
        return nativeAsyncEnumerable;
      }

      if (source.Provider is QueryProvider doProvider) {
        return new QueryAsAsyncEnumerable<TSource>(doProvider, source.Expression);
      }

      throw new InvalidOperationException("Query can't be executed asynchronously.");
    }

    // Private methods

    private static MethodInfo NormalizeOperation<TSource, TResult>(MethodInfo operation) =>
      !operation.IsGenericMethod
        ? operation
        : operation.GetGenericArguments().Length == 2 ? operation.CachedMakeGenericMethod(typeof(TSource), typeof(TResult))
        : operation.CachedMakeGenericMethod(typeof(TSource));

    private static async Task<TResult> ExecuteScalarAsync<TResult>(MethodInfo operation,
      IQueryable source, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(source);
      return source.Provider is QueryProvider provider
        ? await provider.ExecuteScalarAsync<TResult>(Expression.Call(operation, source.Expression), cancellationToken)
        : (TResult) operation.Invoke(BoxedZero, [source]);
    }

    private static async Task<TResult> ExecuteScalarAsync<TResult>(MethodInfo operation,
      IQueryable source,
      Expression expression,
      CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(expression);

      return source.Provider is QueryProvider provider
        ? await provider.ExecuteScalarAsync<TResult>(Expression.Call(operation, source.Expression, expression), cancellationToken)
        : (TResult) operation.Invoke(BoxedZero, [source, expression]);
    }
  }
}
