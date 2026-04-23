// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Linq.Optimization.Model;

namespace Xtensive.Orm.Tests.Linq.Optimization
{
  /// <summary>
  /// Shared harness for every translator-optimization test. Subclasses get:
  /// <list type="bullet">
  ///   <item>A registered <see cref="Customer"/>/<see cref="Order"/>/<see cref="OrderItem"/>/<see cref="Workflow"/> model.</item>
  ///   <item><see cref="Sql{T}(Session, IQueryable{T})"/> that returns the SQL a query will actually produce.</item>
  ///   <item>Shape assertions (<see cref="AssertNotContains"/>, <see cref="AssertCount"/>) for the generated SQL string.</item>
  ///   <item><see cref="AssertResultsEqual{T}"/> to pair every shape assertion with a correctness check.</item>
  /// </list>
  /// Subclasses that need to opt into a specific <see cref="TranslatorOptimizations"/>
  /// flag override <see cref="Optimizations"/>.
  /// </summary>
  public abstract class OptimizationTestBase : AutoBuildTest
  {
    /// <summary>
    /// Flags to apply to <see cref="DomainConfiguration.TranslatorOptimizations"/>.
    /// Default keeps the legacy translator behavior so this base class is a no-op
    /// for tests that only care about correctness.
    /// </summary>
    protected virtual TranslatorOptimizations Optimizations => TranslatorOptimizations.Default;

    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(Customer));
      configuration.Types.Register(typeof(Workflow));
      configuration.Types.Register(typeof(Order));
      configuration.Types.Register(typeof(OrderItem));
      configuration.TranslatorOptimizations = Optimizations;
      return configuration;
    }

    /// <summary>
    /// Returns the SQL string that <paramref name="query"/> would execute against
    /// the configured storage provider. Mirrors the pattern used by
    /// <see cref="TagTest"/>: <c>session.Services.Demand&lt;QueryFormatter&gt;().ToSqlString(q)</c>.
    /// </summary>
    protected static string Sql<T>(Session session, IQueryable<T> query)
    {
      var formatter = session.Services.Demand<QueryFormatter>();
      return formatter.ToSqlString(query);
    }

    /// <summary>
    /// Asserts that none of <paramref name="fragments"/> appears in <paramref name="sql"/>
    /// (case-insensitive). Useful for "must not emit OUTER APPLY / LEFT JOIN / ..." shape tests.
    /// </summary>
    protected static void AssertNotContains(string sql, params string[] fragments)
    {
      ArgumentNullException.ThrowIfNull(sql);
      ArgumentNullException.ThrowIfNull(fragments);
      foreach (var fragment in fragments) {
        Assert.That(
          sql.IndexOf(fragment, StringComparison.OrdinalIgnoreCase),
          Is.LessThan(0),
          () => $"SQL unexpectedly contains '{fragment}':\n{sql}");
      }
    }

    /// <summary>
    /// Asserts that <paramref name="fragment"/> appears exactly <paramref name="expected"/>
    /// times in <paramref name="sql"/> (case-insensitive). Use for counting subqueries,
    /// joins, OR-branches, etc.
    /// </summary>
    protected static void AssertCount(string sql, string fragment, int expected)
    {
      ArgumentNullException.ThrowIfNull(sql);
      ArgumentException.ThrowIfNullOrEmpty(fragment);
      var actual = CountOccurrences(sql, fragment);
      Assert.That(
        actual,
        Is.EqualTo(expected),
        () => $"Expected '{fragment}' to occur {expected} times, but it occurred {actual} times:\n{sql}");
    }

    /// <summary>
    /// Runs both queries to materialized lists and compares them element-wise. Every
    /// shape assertion in a test should be paired with a call to this method so
    /// the optimization never silently changes semantics.
    /// </summary>
    protected static void AssertResultsEqual<T>(IQueryable<T> actual, IQueryable<T> expected)
    {
      var actualList = actual.ToList();
      var expectedList = expected.ToList();
      Assert.That(actualList, Is.EquivalentTo(expectedList));
    }

    /// <summary>
    /// Runs both sequences and compares them element-wise. Useful when the
    /// "baseline" side is an in-memory <see cref="IEnumerable{T}"/> computed by
    /// the test (e.g. LINQ-to-objects fallback).
    /// </summary>
    protected static void AssertResultsEqual<T>(IQueryable<T> actual, IEnumerable<T> expected)
    {
      Assert.That(actual.ToList(), Is.EquivalentTo(expected.ToList()));
    }

    private static int CountOccurrences(string source, string fragment)
    {
      var count = 0;
      var index = 0;
      while ((index = source.IndexOf(fragment, index, StringComparison.OrdinalIgnoreCase)) >= 0) {
        count++;
        index += fragment.Length;
      }
      return count;
    }
  }
}
