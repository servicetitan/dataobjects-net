// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Linq.SubqueryFilterCheckerTestModel;

namespace Xtensive.Orm.Tests.Linq.SubqueryFilterCheckerTestModel
{
  [HierarchyRoot]
  public class Promotion : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field(Length = 64)]
    public string CampainName { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Linq
{
  /// <summary>
  /// Regression test for the always-on correctness fix in
  /// <c>SubqueryFilterRemover.SubqueryFilterChecker</c>.
  /// <para>
  /// The legacy checker unconditionally called <c>Pop()</c> on its internal
  /// <c>meaningfulLefts</c>/<c>meaningfulRights</c> stacks on every null-constant
  /// comparison it saw. When the checker visited a <c>FilterProvider</c> whose
  /// predicate started with a bare null-check (e.g. <c>x.Field == null</c>
  /// inside <c>g.Count(...)</c> after a <c>GroupBy</c> on a scalar key) nothing
  /// had been pushed onto the stacks first, so <c>Pop()</c> threw
  /// <c>InvalidOperationException("Stack empty.")</c> during LINQ translation.
  /// </para>
  /// </summary>
  [TestFixture]
  [Category("Linq")]
  public sealed class SubqueryFilterCheckerTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Types.Register(typeof(Promotion));
      return config;
    }

    /// <summary>
    /// Minimal reproducer for the "Stack empty." crash:
    /// <c>GroupBy(pk).Select(g =&gt; g.Count(x =&gt; x.NullableField == null))</c>.
    /// When the grouping key is the entity Id and the <c>Select</c> projects a
    /// single scalar aggregate, <c>Translator.Queryable.VisitAggregate</c>
    /// takes the "use grouping <c>AggregateProvider</c>" optimization path and
    /// runs <see cref="Xtensive.Orm.Linq.Rewriters.SubqueryFilterRemover"/> over
    /// an origin data source whose top <c>FilterProvider</c> carries the bare
    /// null-check predicate. With nothing pushed onto the correlation stacks
    /// yet, the legacy checker did <c>Pop()</c> on empty and crashed.
    /// </summary>
    [Test]
    public void GroupByCountWithBareNullPredicate_DoesNotThrow()
    {
      using var session = Domain.OpenSession();
      using var tx = session.OpenTransaction();

      Assert.DoesNotThrow(() => session.Query.All<Promotion>()
        .GroupBy(p => p.Id)
        .Select(g => g.Count(x => x.CampainName == null))
        .ToArray());
    }
  }
}
