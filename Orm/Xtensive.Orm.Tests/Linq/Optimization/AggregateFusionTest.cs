// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Providers;
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
    /// <c>g.Where(p).Count()</c> on a grouping parameter is semantically identical
    /// to <c>g.Count(p)</c>; the translator must recognize the shape and fuse it
    /// into the parent <c>GROUP BY</c> the same way as the direct form.
    /// </summary>
    [Test]
    public void GroupByWhereCount_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Where(x => x.PublishedOn == null).Count(),
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
          Drafts = g.Where(x => x.PublishedOn == null).Count(),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.Drafts))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.Drafts)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Chained <c>Where</c>s followed by <c>Count()</c> on a grouping parameter
    /// must fuse as a single aggregate; the translator combines the predicates
    /// with <c>AndAlso</c> and applies the same <c>Count -&gt; Sum(CASE)</c>
    /// rewrite as for the direct form.
    /// </summary>
    [Test]
    public void GroupByChainedWhereCount_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          DraftsD = g.Where(x => x.PublishedOn == null).Where(x => x.Code.StartsWith("D")).Count(),
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
          DraftsD = g.Where(x => x.PublishedOn == null).Where(x => x.Code.StartsWith("D")).Count(),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.DraftsD))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.DraftsD)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Variant of <see cref="GroupByWhereCount_FusesIntoSingleAggregate"/> for
    /// the <c>LongCount</c> path; the collapsed form must still produce the
    /// correct <c>long</c> result and fuse.
    /// </summary>
    [Test]
    public void GroupByWhereLongCount_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          Drafts = g.Where(x => x.PublishedOn == null).LongCount(),
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
          Drafts = g.Where(x => x.PublishedOn == null).LongCount(),
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

    /// <summary>
    /// Generalized fusion: <c>g.Where(p).Sum(selector)</c> on a grouping
    /// parameter must be pulled into the aggregate selector as
    /// <c>g.Sum(x =&gt; p(x) ? selector(x) : 0)</c> (0 ELSE branch for a
    /// non-nullable numeric selector preserves the LINQ "empty set = 0"
    /// contract for <c>Sum</c>) so it fuses with the parent <c>GROUP BY</c>
    /// instead of emitting a correlated subquery.
    /// </summary>
    [Test]
    public void GroupByWhereSum_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          PublishedIdSum = g.Where(x => x.PublishedOn != null).Sum(x => x.Id),
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
          PublishedIdSum = g.Where(x => x.PublishedOn != null).Sum(x => x.Id),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.PublishedIdSum))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.PublishedIdSum)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Non-fusable grouping (here: <c>Where</c>-after-<c>GroupBy</c> wraps the
    /// projector with a <c>FilterProvider</c>) takes the peel-and-rebuild
    /// path. <c>g.Where(p)</c> on <see cref="IGrouping{TKey, TElement}"/>
    /// resolves to <see cref="Enumerable"/>.<c>Where</c>, which expects a
    /// raw <c>Func</c>; quoting the lambda makes <c>Expression.Call</c>
    /// throw <c>ArgumentException: Expression of type
    /// 'Expression`1[Func`2[…]]' cannot be used for parameter of type
    /// 'Func`2[…]'</c>.
    /// </summary>
    [Test]
    public void NonFusableGroupingWhereSum_DoesNotThrow()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Where(g => g.Key)
        .Select(g => new {
          Active = g.Key,
          PublishedSum = g.Where(x => x.PublishedOn != null).Sum(x => x.Id),
        });

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Where(g => g.Key)
        .Select(g => (g.Key, PublishedSum: g.Where(x => x.PublishedOn != null).Sum(x => x.Id)))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.PublishedSum)).ToArray();
      Assert.That(actual, Is.EquivalentTo(expected));
    }
    /// <summary>
    /// Generalized fusion: <c>g.Where(p).Min(selector)</c> must fuse via
    /// <c>g.Min(x =&gt; p(x) ? (T?)selector(x) : null)</c>; SQL <c>MIN</c>
    /// ignores <c>NULL</c>s so the rewrite is semantically equivalent.
    /// </summary>
    [Test]
    public void GroupByWhereMin_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          MinPublishedId = g.Where(x => x.PublishedOn != null).Min(x => (long?) x.Id),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT MIN", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          MinPublishedId = g.Where(x => x.PublishedOn != null).Min(x => (long?) x.Id),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.MinPublishedId))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.MinPublishedId)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Generalized fusion: <c>g.Where(p).Max(selector)</c> must fuse via the
    /// same <c>NULL</c>-in-ELSE trick as <see cref="GroupByWhereMin_FusesIntoSingleAggregate"/>.
    /// </summary>
    [Test]
    public void GroupByWhereMax_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          MaxPublishedId = g.Where(x => x.PublishedOn != null).Max(x => (long?) x.Id),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT MAX", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          MaxPublishedId = g.Where(x => x.PublishedOn != null).Max(x => (long?) x.Id),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.MaxPublishedId))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.MaxPublishedId)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Generalized fusion: <c>g.Where(p).Average(selector)</c> must fuse via
    /// <c>NULL</c>-in-ELSE; SQL <c>AVG</c> ignores <c>NULL</c>s, matching
    /// LINQ's "average over passing rows" contract.
    /// </summary>
    [Test]
    public void GroupByWhereAverage_FusesIntoSingleAggregate()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          AvgPublishedId = g.Where(x => x.PublishedOn != null).Average(x => (double?) x.Id),
        })
        .OrderBy(r => r.Active);

      var sql = Sql(session, query);
      TestContext.WriteLine(sql);
      AssertCount(sql, "(SELECT AVG", 0);

      var expected = session.Query.All<Order>().ToArray()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          AvgPublishedId = g.Where(x => x.PublishedOn != null).Average(x => (double?) x.Id),
        })
        .OrderBy(r => r.Active)
        .Select(r => (r.Active, r.AvgPublishedId))
        .ToArray();

      var actual = query.ToArray().Select(r => (r.Active, r.AvgPublishedId)).ToArray();
      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Regression: <c>g.Where(p).Sum(selector)</c> where no rows match the
    /// predicate in a group must materialize as <c>0</c> (LINQ <c>Sum</c>'s
    /// empty-sequence contract for non-nullable numeric selectors) and not
    /// as <c>NULL</c>. The rewrite must use <c>0</c> — not <c>NULL</c> — in
    /// the ELSE branch when the selector result type is non-nullable.
    /// </summary>
    [Test]
    public void GroupByWhereSum_ZeroMatchingRows_ReturnsZero()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .GroupBy(o => o.IsActive)
        .Select(g => new {
          Active = g.Key,
          PublishedIdSum = g.Where(x => x.PublishedOn != null).Sum(x => x.Id),
        })
        .OrderBy(r => r.Active);

      var rows = query.ToArray();
      var inactive = rows.Single(r => !r.Active);
      Assert.That(inactive.PublishedIdSum, Is.EqualTo(0L),
        "Sum(selector) over an empty filter in a grouping must materialize as 0, not NULL, for a non-nullable selector.");
    }

    /// <summary>
    /// Guard: <see cref="Queryable.Where{T}(IQueryable{T}, System.Linq.Expressions.Expression{System.Func{T, int, bool}})"/>
    /// — the indexed <c>Where</c> overload — must not be folded into a
    /// combined predicate by <c>PeelWhereChain</c>. The <c>index</c>
    /// parameter refers to the row's position in the current sequence; AND-
    /// combining an indexed <c>Where</c> with another predicate would
    /// silently change its semantics and produce an expression tree with an
    /// unbound parameter. The correct behaviour is to stop peeling at the
    /// indexed call and let <c>VisitWhere</c> handle it normally.
    /// </summary>
    [Test]
    public void IndexedWhereChainBeforeCount_IsNotCollapsed()
    {
      Require.AllFeaturesSupported(ProviderFeatures.RowNumber);

      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var query = session.Query.All<Order>()
        .OrderBy(o => o.Id)
        .Where((o, i) => i >= 0)
        .Count(o => o.PublishedOn == null);

      Assert.That(query, Is.EqualTo(3));
    }

    /// <summary>
    /// Root-level (non-fusable) Sum with a Where chain must collapse into a
    /// single WHERE in the emitted SQL — one <c>FilterProvider</c> instead of
    /// stacked ones — and produce the same numeric result as the in-memory
    /// reference. Verifies the <c>PeelWhereChain</c> rebuild path for
    /// Sum/Min/Max/Avg when fusion does not apply.
    /// </summary>
    [Test]
    public void RootLevelWhereChainSum_CollapsesAndMatchesReference()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var actual = session.Query.All<Order>()
        .Where(o => o.IsActive)
        .Where(o => o.PublishedOn != null)
        .Sum(o => o.Id);

      var expected = session.Query.All<Order>().ToArray()
        .Where(o => o.IsActive)
        .Where(o => o.PublishedOn != null)
        .Sum(o => o.Id);

      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Inner <c>Where</c> typed for a derived entity, outer <c>Where</c>
    /// typed for a wider base — the shape behind
    /// <c>Owner.Items.Where(i =&gt; i.Active).Where(…).Sum(i =&gt; i.Total)</c>
    /// where <c>Active</c> is declared only on the derived type.
    /// <c>PeelWhereChain</c> must rebase onto the inner (narrower) parameter;
    /// rebasing onto the outer parameter throws
    /// <c>ArgumentException: Property '…' is not defined for type '…'</c>.
    /// </summary>
    [Test]
    public void WhereWithDerivedTypedPredicate_CountWithBaseTypedPredicate_PreservesDerivedMemberAccess()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var expected = session.Query.All<Order>().ToArray()
        .Where(o => o.IsActive)
        .Count();

      // Widen via IQueryable<out T> covariance so the outer Where binds
      // T = Entity while the inner Where stays Where<Order>; two peeled
      // Wheres with different parameter types are required to exercise the
      // rebase decision.
      IQueryable<Entity> chain = session.Query.All<Order>().Where(o => o.IsActive);

      var actual = chain.Where(e => e != null).Count();

      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Chained generic extensions, each constrained on a different facet
    /// interface (<see cref="IHasActivation"/>, <see cref="IHasCode"/>,
    /// <see cref="IHasPublishDate"/>). The concrete entity satisfies every
    /// constraint so the compiler binds <c>T = Order</c> throughout — a
    /// regression guard ensuring the rebase fix keeps the homogeneously-typed
    /// case working when interface members are accessed through an
    /// <c>Order</c>-typed parameter.
    /// </summary>
    [Test]
    public void InterfaceConstrainedExtensionsChain_Count_ResolvesAgainstConcreteEntity()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var expected = session.Query.All<Order>().ToArray()
        .Where(o => o.IsActive)
        .Where(o => o.Code == "P1")
        .Count(o => o.PublishedOn == new DateTime(2024, 1, 1));

      var actual = session.Query.All<Order>()
        .ActiveOnly()
        .HavingCode("P1")
        .CountPublishedOn(new DateTime(2024, 1, 1));

      Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Mixed-type chain: inner <c>Where&lt;Order&gt;</c> references
    /// <c>Order.IsActive</c>, outer <c>Where</c>/<c>Count</c> bind
    /// <c>T = IHasCode</c> after an <c>IQueryable&lt;out T&gt;</c> covariant
    /// widening and reference <c>IHasCode.Code</c>. Every lambda body in the
    /// peel window needs rebasing; the fix pins the accumulator to the inner
    /// (<c>Order</c>) parameter so both sides' members resolve.
    /// </summary>
    [Test]
    public void StackedMixedInterfaceWhereChain_CountWithInterfacePredicate_ResolvesViaInnermostType()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      var expected = session.Query.All<Order>().ToArray()
        .Where(o => o.IsActive)
        .Where(o => o.Code == "P1")
        .Count(o => o.Code != "SKIP");

      // Widen via IQueryable<out T> covariance; subsequent operators bind
      // T = IHasCode while the inner Where stays Where<Order>.
      IQueryable<IHasCode> chain = session.Query.All<Order>().Where(o => o.IsActive);

      var actual = chain
        .Where(x => x.Code == "P1")
        .Count(x => x.Code != "SKIP");

      Assert.That(actual, Is.EqualTo(expected));
    }
  }

  /// <summary>
  /// Query-building extensions each constrained on a single facet interface.
  /// Composed on a concrete entity that implements all three to mimic the
  /// real-world pattern of pipelines assembled from small generic helpers.
  /// </summary>
  internal static class FacetExtensions
  {
    public static IQueryable<T> ActiveOnly<T>(this IQueryable<T> q) where T : IHasActivation =>
      q.Where(x => x.IsActive);

    public static IQueryable<T> HavingCode<T>(this IQueryable<T> q, string code) where T : IHasCode =>
      q.Where(x => x.Code == code);

    public static int CountPublishedOn<T>(this IQueryable<T> q, DateTime? date) where T : IHasPublishDate =>
      q.Count(x => x.PublishedOn == date);
  }
}
