// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Tests.Linq.Optimization.Model;

namespace Xtensive.Orm.Tests.Linq.Optimization
{
  /// <summary>
  /// Aggregate fusion for <c>Count(predicate)</c> and
  /// <c>Sum(predicate ? 1 : 0)</c> inside a <c>GroupBy</c>: the aggregate must
  /// fuse into a single <c>SELECT ... GROUP BY</c> instead of being emitted as
  /// a per-group correlated subquery.
  /// </summary>
  [TestFixture]
  [Category("Linq")]
  public sealed class AggregateFusionTest : OptimizationTestBase
  {
    protected override void PopulateData()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var active = new Customer { Name = "A", IsActive = true };
      var inactive = new Customer { Name = "B", IsActive = false };
      _ = new Customer { Name = "C", IsActive = true };

      _ = new Order { Code = "P1", IsActive = true, PublishedOn = new DateTime(2024, 1, 1), Customer = active };
      _ = new Order { Code = "D1", IsActive = true, PublishedOn = null,                       Customer = active };
      _ = new Order { Code = "D2", IsActive = false, PublishedOn = null,                      Customer = inactive };
      _ = new Order { Code = "P2", IsActive = true, PublishedOn = new DateTime(2024, 3, 1),   Customer = inactive };
      _ = new Order { Code = "D3", IsActive = false, PublishedOn = null,                      Customer = active };

      tx.Complete();
    }

    /// <summary>
    /// <c>GroupBy(k).Select(g =&gt; g.Count(x =&gt; predicate))</c> on a scalar
    /// key must emit a single <c>SELECT ... GROUP BY</c> with no per-group
    /// correlated subquery.
    /// </summary>
    [Test]
    public void GroupByCountPredicate_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.PublishedOn == null),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.PublishedOn == null),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.Drafts))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.Drafts)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Multiple predicated counts in the same projection all fuse into a single
    /// <c>SELECT ... GROUP BY</c>.
    /// </summary>
    [Test]
    public void GroupByMultipleCountPredicates_FuseIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.PublishedOn == null),
          Published = g.Count(x => x.PublishedOn != null),
          Total = g.Count(),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.PublishedOn == null),
          Published = g.Count(x => x.PublishedOn != null),
          Total = g.Count(),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.Drafts, r.Published, r.Total))
        .ToArray();

      var actual = query.ToArray()
        .Select(r => (r.Active, r.Drafts, r.Published, r.Total))
        .ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Compound predicate (<c>AndAlso</c>) inside <c>Count</c> fuses too.
    /// </summary>
    [Test]
    public void GroupByCountAndAlsoPredicate_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.Code.StartsWith("D") && x.PublishedOn == null),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Count(x => x.Code.StartsWith("D") && x.PublishedOn == null),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.Drafts))
        .ToArray();

      var actual = query.ToArray()
        .Select(r => (r.Active, r.Drafts))
        .ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// <c>Sum(x =&gt; condition ? 1 : 0)</c> must also fuse without any
    /// correlated subquery.
    /// </summary>
    [Test]
    public void GroupBySumOfCondition_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Sum(x => x.PublishedOn == null ? 1 : 0),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);
      AssertCount(sql, "(SELECT SUM", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Sum(x => x.PublishedOn == null ? 1 : 0),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.Drafts))
        .ToArray();

      var actual = query.ToArray()
        .Select(r => (r.Active, r.Drafts))
        .ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Regression: when the Count -> Sum(CASE) rewrite fires and a group
    /// contains zero rows matching the predicate, the materialized value must
    /// be <c>0</c> (Count's contract) — not <c>NULL</c>, and the pipeline
    /// must not throw on the int coercion. The inactive group in the seed
    /// data contains only unpublished orders, so the predicate
    /// <c>PublishedOn != null</c> matches zero rows there.
    /// </summary>
    [Test]
    public void GroupByCountPredicate_ZeroMatchingRows_ReturnsZero()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Published = g.Count(x => x.PublishedOn != null),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);

      // Materialization itself is part of the regression guard: if SUM returns
      // NULL for a group with no matching rows, the int coercion would throw.
      var rows = query.ToArray();

      var inactive = rows.Single(r => !r.Active);
      Assert.That(inactive.Published, Is.EqualTo(0),
        "Count(predicate) in a grouping where no rows match must materialize as 0, not NULL.");

      var active = rows.Single(r => r.Active);
      Assert.That(active.Published, Is.EqualTo(2),
        "Sanity: the non-empty group must still count correctly after the rewrite.");
    }

    /// <summary>
    /// Same regression guard as <see cref="GroupByCountPredicate_ZeroMatchingRows_ReturnsZero"/>
    /// but exercises the <c>LongCount</c> path (rewrite emits <c>long</c>
    /// literals and the result column must coerce <c>SUM</c>-of-zero-rows to
    /// <c>0L</c>).
    /// </summary>
    [Test]
    public void GroupByLongCountPredicate_ZeroMatchingRows_ReturnsZero()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Published = g.LongCount(x => x.PublishedOn != null),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT COUNT", 0);

      // Materialization itself is part of the regression guard: if SUM returns
      // NULL for a group with no matching rows, the long coercion would throw.
      var rows = query.ToArray();

      var inactive = rows.Single(r => !r.Active);
      Assert.That(inactive.Published, Is.EqualTo(0L),
        "LongCount(predicate) in a grouping where no rows match must materialize as 0L, not NULL.");

      var active = rows.Single(r => r.Active);
      Assert.That(active.Published, Is.EqualTo(2L),
        "Sanity: the non-empty group must still count correctly after the rewrite.");
    }

    /// <summary>
    /// Root-level <c>Count(predicate)</c> must still return <c>0</c> on an
    /// empty match set (not <c>NULL</c>); the rewrite must not fire here.
    /// </summary>
    [Test]
    public void RootLevelCountPredicate_StillReturnsZeroOnEmpty()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var count = session.Query.All<Order>().Count(o => o.Code == "no-such-code");

      Assert.That(count, Is.EqualTo(0));
    }
  }
}
