// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Orm.Providers;
using Xtensive.Sql;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Tests.Sql
{
  /// <summary>
  /// Characterizes the conditions under which a top-down recursive walk of a
  /// SQL DML tree shaped like the ones <see cref="SqlSelectProcessor"/>
  /// processes will reach the same <see cref="SqlSelect"/> instance from more
  /// than one path, and shows that the production re-entrancy guard
  /// (<c>visitedSelects HashSet&lt;SqlSelect&gt;</c>) collapses every such
  /// repeat reach into a single effective visit.
  /// <para>
  /// Each test pairs:
  /// <list type="bullet">
  /// <item>A counting walker (<see cref="CountingSelectVisitor"/>) that mirrors the
  /// recursion <see cref="SqlSelectProcessor"/> performs for the node types
  /// our test trees use, but deliberately omits any deduplication. This proves
  /// the architectural circumstance: the walker reports a per-node visit count
  /// greater than one for the shared inner SELECT.</item>
  /// <item>A run of the production <see cref="SqlSelectProcessor"/> against the
  /// same tree, with the <c>visitedSelects</c> HashSet inspected via reflection
  /// to confirm the guard collapses the repeat reaches into a single tracked
  /// visit.</item>
  /// </list>
  /// </para>
  /// <para>
  /// The guard is a defense-in-depth measure: every benign side effect inside
  /// <see cref="SqlSelectProcessor.Visit(SqlSelect)"/> (comment merge,
  /// <c>OrderBy.Clear</c>, <c>OrderBy.Add(1)</c>, column pruning) is currently
  /// self-idempotent, so the guard has no observable behavior change in the
  /// steady state. Its real value is preventing wasted re-walks and protecting
  /// future, potentially non-idempotent additions to that method.
  /// </para>
  /// </summary>
  [TestFixture]
  public class SqlSelectProcessorVisitTest
  {
    private ProviderInfo providerInfo;

    [OneTimeSetUp]
    public void SetUp()
    {
      // No Catalog / Schema / Table needed: every test in this fixture
      // characterizes traversal over hand-built SqlSelect trees, and the
      // inner SELECT only needs *some* column for its identity to be
      // observable. We use a literal so there is zero schema metadata, no
      // database, and nothing to share with other fixtures.
      providerInfo = new ProviderInfo(
        providerName: "test",
        storageVersion: new Version(1, 0),
        providerFeatures: ProviderFeatures.None,
        maxIdentifierLength: 128,
        constantPrimaryIndexName: "PK",
        defaultDatabase: "test",
        defaultSchema: "dbo",
        supportedTypes: Enumerable.Empty<Type>(),
        maxQueryParameterCount: 1000);
    }

    #region Circumstance #1: same SqlSelect reached from N SqlSubQuery columns

    [Test]
    public void InnerSelectReachedFromTwoSubQueryColumns()
    {
      // Tree:
      //   outer
      //     Columns:
      //       [0] SqlColumnRef -> SqlUserColumn -> SqlSubQuery #A -> inner   <-- path A
      //       [1] SqlColumnRef -> SqlUserColumn -> SqlSubQuery #B -> inner   <-- path B
      //
      // SqlSubQuery #A and #B are different SqlSubQuery wrappers (each call
      // to SqlDml.SubQuery returns a fresh instance), so the sibling
      // visitedExpressions guard inside Visit(SqlExpression) does not stop
      // the walk at the SqlSubQuery layer. Both wrappers therefore call
      // Visit(node.Query), which dispatches to Visit(SqlSelect inner) for the
      // *same* inner instance — twice in a row.

      var inner = CreateInnerSelect();
      var outer = SqlDml.Select();
      outer.Columns.Add(SqlDml.SubQuery(inner));
      outer.Columns.Add(SqlDml.SubQuery(inner));

      var counts = WalkWithoutGuard(outer);
      Assert.That(counts[inner], Is.EqualTo(2),
        "without a guard the recursive walk hits inner once per SqlSubQuery column");
      Assert.That(counts[outer], Is.EqualTo(1));

      AssertProductionGuardCollapsesToOnce(outer, inner, expectedUniqueSelects: 2);
    }

    [Test]
    public void InnerSelectReachedFromManySubQueryColumns()
    {
      // Same shape as above, scaled up: 6 columns all wrapping the same inner.
      // Mirrors the production pattern the visitedSelects guard was added
      // for, where 1 grouping + 5 Sum + 1 Count aggregates over the same
      // tagged source produced ~7 reach paths to the inner SELECT.
      var inner = CreateInnerSelect();
      var outer = SqlDml.Select();
      const int columnCount = 6;
      for (var i = 0; i < columnCount; i++) {
        outer.Columns.Add(SqlDml.SubQuery(inner));
      }

      var counts = WalkWithoutGuard(outer);
      Assert.That(counts[inner], Is.EqualTo(columnCount),
        "without a guard the recursive walk hits inner once per SqlSubQuery column");

      AssertProductionGuardCollapsesToOnce(outer, inner, expectedUniqueSelects: 2);
    }

    #endregion

    #region Circumstance #2: same SqlSelect appearing in FROM and in a WHERE subquery

    [Test]
    public void InnerSelectReachedFromFromAndWhereSubQuery()
    {
      // Tree:
      //   outer
      //     From:    SqlQueryRef(inner)               <-- path A: outer.From -> queryRef -> inner
      //     Where:   SqlSubQuery(inner) IS NOT NULL    <-- path B: outer.Where -> subQuery -> inner
      //
      // Both paths land on the same inner SqlSelect instance via two
      // structurally distinct positions in the parent. Without a guard the
      // walk processes inner twice: once via Visit(SqlQueryRef) ->
      // Visit(node.Query), once via Visit(SqlSubQuery) -> Visit(node.Query).

      var inner = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(inner, "q");
      var outer = SqlDml.Select(queryRef);
      outer.Columns.Add(queryRef[0]);
      outer.Where = SqlDml.IsNotNull(SqlDml.SubQuery(inner));

      var counts = WalkWithoutGuard(outer);
      Assert.That(counts[inner], Is.EqualTo(2),
        "without a guard the walk reaches inner once via FROM and once via WHERE");

      AssertProductionGuardCollapsesToOnce(outer, inner, expectedUniqueSelects: 2);
    }

    #endregion

    #region Circumstance #3: shared inner across distinct sibling parents (ShallowClone-style)

    [Test]
    public void InnerSelectSharedAcrossSiblingParents()
    {
      // Tree:
      //   root
      //     Columns:
      //       [0] SqlSubQuery -> parentA;  parentA.Columns[0] -> SqlSubQuery -> inner
      //       [1] SqlSubQuery -> parentB;  parentB.Columns[0] -> SqlSubQuery -> inner
      //
      // parentA and parentB are distinct SqlSelect instances. This is the
      // shape SqlSelect.ShallowClone produces in the SQL compiler — fresh
      // outer instances that re-aggregate the same children by reference.
      // They each carry their own SqlSubQuery wrapper, but both wrappers
      // point at the same inner SqlSelect. The walk therefore reaches inner
      // through two separate parent traversals.

      var inner = CreateInnerSelect();

      var parentA = SqlDml.Select();
      parentA.Columns.Add(SqlDml.SubQuery(inner));

      var parentB = SqlDml.Select();
      parentB.Columns.Add(SqlDml.SubQuery(inner));

      var root = SqlDml.Select();
      root.Columns.Add(SqlDml.SubQuery(parentA));
      root.Columns.Add(SqlDml.SubQuery(parentB));

      var counts = WalkWithoutGuard(root);
      Assert.That(counts[inner], Is.EqualTo(2),
        "without a guard the walk reaches inner once via parentA and once via parentB");
      Assert.That(counts[parentA], Is.EqualTo(1));
      Assert.That(counts[parentB], Is.EqualTo(1));

      AssertProductionGuardCollapsesToOnce(root, inner, expectedUniqueSelects: 4);
    }

    #endregion

    #region Counterexample: distinct SqlSelects are NOT collapsed

    [Test]
    public void TwoDistinctInnerSelects_AreVisitedIndependently()
    {
      // Tree:
      //   outer
      //     Columns:
      //       [0] SqlSubQuery -> innerA   (independent SqlSelect)
      //       [1] SqlSubQuery -> innerB   (independent SqlSelect)
      //
      // No sharing. Demonstrates that the guard fires on instance identity,
      // not on structural similarity: two SELECTs that happen to be built the
      // same way are still tracked and processed independently.

      var innerA = CreateInnerSelect();
      var innerB = CreateInnerSelect();
      var outer = SqlDml.Select();
      outer.Columns.Add(SqlDml.SubQuery(innerA));
      outer.Columns.Add(SqlDml.SubQuery(innerB));

      var counts = WalkWithoutGuard(outer);
      Assert.That(counts[innerA], Is.EqualTo(1));
      Assert.That(counts[innerB], Is.EqualTo(1));

      var processor = CreateProcessor(outer);
      InvokeVisitSelect(processor, outer);
      var visitedSelects = GetVisitedSelects(processor);
      Assert.That(visitedSelects, Contains.Item(innerA));
      Assert.That(visitedSelects, Contains.Item(innerB));
      Assert.That(visitedSelects.Count, Is.EqualTo(3),
        "outer + innerA + innerB; nothing is collapsed because there is no shared identity");
    }

    #endregion

    #region Helpers — production processor reflection

    private void AssertProductionGuardCollapsesToOnce(
      SqlSelect rootForProcessor, SqlSelect sharedInner, int expectedUniqueSelects)
    {
      // Build a fresh copy of the tree shape so we never feed a mutated tree
      // (the production processor may rewrite Columns, OrderBy, etc.) into
      // the counting walker above. The shared identity invariant holds within
      // each tree independently.
      var processor = CreateProcessor(rootForProcessor);
      InvokeVisitSelect(processor, rootForProcessor);
      var visitedSelects = GetVisitedSelects(processor);
      Assert.That(visitedSelects, Contains.Item(sharedInner),
        "production processor's recursive walk must reach the shared inner SELECT");
      Assert.That(visitedSelects, Contains.Item(rootForProcessor));
      Assert.That(visitedSelects.Count, Is.EqualTo(expectedUniqueSelects),
        $"production guard must collapse repeat reaches; expected {expectedUniqueSelects} unique SELECTs");
    }

    /// <summary>
    /// Instantiates a fresh <see cref="SqlSelectProcessor"/> with the given
    /// root. Reflection is required because the constructor is private; the
    /// production entry point is the static <c>SqlSelectProcessor.Process</c>
    /// which discards the instance, leaving no way to assert on the
    /// instance's <c>visitedSelects</c> bookkeeping after the fact.
    /// </summary>
    private SqlSelectProcessor CreateProcessor(SqlSelect rootSelect)
    {
      var ctor = typeof(SqlSelectProcessor).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        types: new[] { typeof(SqlSelect), typeof(ProviderInfo) },
        modifiers: null);
      Assert.That(ctor, Is.Not.Null,
        "SqlSelectProcessor private constructor signature changed; update the reflection lookup.");
      return (SqlSelectProcessor) ctor.Invoke(new object[] { rootSelect, providerInfo });
    }

    private static void InvokeVisitSelect(SqlSelectProcessor processor, SqlSelect node)
    {
      var visitMethod = typeof(SqlSelectProcessor).GetMethod(
        nameof(SqlSelectProcessor.Visit),
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        types: new[] { typeof(SqlSelect) },
        modifiers: null);
      Assert.That(visitMethod, Is.Not.Null,
        "SqlSelectProcessor.Visit(SqlSelect) signature changed; update the reflection lookup.");
      visitMethod.Invoke(processor, new object[] { node });
    }

    private static HashSet<SqlSelect> GetVisitedSelects(SqlSelectProcessor processor)
    {
      var field = typeof(SqlSelectProcessor).GetField(
        "visitedSelects",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null,
        "SqlSelectProcessor.visitedSelects field renamed or removed; update the reflection lookup.");
      return (HashSet<SqlSelect>) field.GetValue(processor);
    }

    #endregion

    #region Helpers — tree construction and counting walker

    private static SqlSelect CreateInnerSelect()
    {
      // Minimal SELECT with one literal column. No FROM, no real table —
      // identity of the SqlSelect itself is the only thing the multi-reach
      // tests care about.
      var select = SqlDml.Select();
      select.Columns.Add(SqlDml.Literal(1), "x");
      return select;
    }

    private static Dictionary<SqlSelect, int> WalkWithoutGuard(SqlSelect root)
    {
      var v = new CountingSelectVisitor();
      v.Visit(root);
      return v.SelectVisits;
    }

    /// <summary>
    /// Minimal <see cref="ISqlVisitor"/> that mirrors the recursion
    /// <see cref="SqlSelectProcessor"/> performs for the node types this
    /// fixture's test trees use, with two deliberate differences:
    /// <list type="bullet">
    /// <item>It tallies how many times <see cref="Visit(SqlSelect)"/> is
    /// called per <see cref="SqlSelect"/> instance.</item>
    /// <item>It does NOT short-circuit on repeat visits. This is what allows
    /// the count to exceed 1 for shared <see cref="SqlSelect"/> instances and
    /// thus characterize the multi-reach circumstance the production guard
    /// is there to protect.</item>
    /// </list>
    /// All other <see cref="ISqlVisitor"/> overloads are intentional no-ops:
    /// the test trees only use SqlTableRef, SqlTableColumn, SqlColumnRef,
    /// SqlUserColumn, SqlSubQuery, SqlQueryRef, SqlUnary (for IsNotNull),
    /// SqlSelect and a handful of leaf expressions, so unimplemented
    /// overloads are never reached. If a future test adds a node type whose
    /// recursion needs to descend further, add the corresponding override.
    /// </summary>
    private sealed class CountingSelectVisitor : ISqlVisitor
    {
      public Dictionary<SqlSelect, int> SelectVisits { get; } = new Dictionary<SqlSelect, int>();

      // --- Real recursion: only the node types our test trees use ---

      public void Visit(SqlSelect node)
      {
        SelectVisits[node] = SelectVisits.GetValueOrDefault(node) + 1;

        // Mirror SqlSelectProcessor.Visit(SqlSelect) for the subset of
        // children our trees populate.
        foreach (var column in node.Columns) {
          column.AcceptVisitor(this);
        }
        node.From?.AcceptVisitor(this);
        node.Where?.AcceptVisitor(this);
      }

      public void Visit(SqlColumnRef node)
      {
        node.SqlColumn?.AcceptVisitor(this);
      }

      public void Visit(SqlUserColumn node)
      {
        node.Expression?.AcceptVisitor(this);
      }

      public void Visit(SqlSubQuery node)
      {
        if (node.Query is SqlSelect select) {
          Visit(select);
        }
        else {
          ((SqlExpression) node.Query)?.AcceptVisitor(this);
        }
      }

      public void Visit(SqlQueryRef node)
      {
        if (node.Query is SqlSelect select) {
          Visit(select);
        }
      }

      public void Visit(SqlUnary node)
      {
        node.Operand?.AcceptVisitor(this);
      }

      // --- Leaves the test trees touch but do not need to descend through ---

      public void Visit(SqlTableRef node) { }
      public void Visit(SqlTableColumn node) { }
      public void Visit(SqlLiteral node) { }
      public void Visit(SqlNull node) { }

      // --- All other overloads are no-ops; not reached by current test trees ---

      public void Visit(SqlAggregate node) { }
      public void Visit(SqlAlterDomain node) { }
      public void Visit(SqlAlterPartitionFunction node) { }
      public void Visit(SqlAlterPartitionScheme node) { }
      public void Visit(SqlAlterTable node) { }
      public void Visit(SqlAlterSequence node) { }
      public void Visit(SqlArray node) { }
      public void Visit(SqlAssignment node) { }
      public void Visit(SqlBatch node) { }
      public void Visit(SqlBetween node) { }
      public void Visit(SqlBinary node) { }
      public void Visit(SqlBreak node) { }
      public void Visit(SqlCase node) { }
      public void Visit(SqlCast node) { }
      public void Visit(SqlCloseCursor node) { }
      public void Visit(SqlCollate node) { }
      public void Visit(SqlConcat node) { }
      public void Visit(SqlContainsTable node) { }
      public void Visit(SqlContinue node) { }
      public void Visit(SqlContainer node) { }
      public void Visit(SqlCommand node) { }
      public void Visit(SqlComment node) { }
      public void Visit(SqlCreateAssertion node) { }
      public void Visit(SqlCreateCharacterSet node) { }
      public void Visit(SqlCreateCollation node) { }
      public void Visit(SqlCreateDomain node) { }
      public void Visit(SqlCreateIndex node) { }
      public void Visit(SqlCreatePartitionFunction node) { }
      public void Visit(SqlCreatePartitionScheme node) { }
      public void Visit(SqlCreateSchema node) { }
      public void Visit(SqlCreateSequence node) { }
      public void Visit(SqlCreateTable node) { }
      public void Visit(SqlCreateTranslation node) { }
      public void Visit(SqlCreateView node) { }
      public void Visit(SqlCursor node) { }
      public void Visit(SqlDeclareCursor node) { }
      public void Visit(SqlDeclareVariable node) { }
      public void Visit(SqlDefaultValue node) { }
      public void Visit(SqlDelete node) { }
      public void Visit(SqlDropAssertion node) { }
      public void Visit(SqlDropCharacterSet node) { }
      public void Visit(SqlDropCollation node) { }
      public void Visit(SqlDropDomain node) { }
      public void Visit(SqlDropIndex node) { }
      public void Visit(SqlDropPartitionFunction node) { }
      public void Visit(SqlDropPartitionScheme node) { }
      public void Visit(SqlDropSchema node) { }
      public void Visit(SqlDropSequence node) { }
      public void Visit(SqlDropTable node) { }
      public void Visit(SqlDropTranslation node) { }
      public void Visit(SqlDropView node) { }
      public void Visit(SqlTruncateTable node) { }
      public void Visit(SqlDynamicFilter node) { }
      public void Visit(SqlPlaceholder node) { }
      public void Visit(SqlExtract node) { }
      public void Visit(SqlFastFirstRowsHint node) { }
      public void Visit(SqlFetch node) { }
      public void Visit(SqlForceJoinOrderHint node) { }
      public void Visit(SqlFragment node) { }
      public void Visit(SqlFreeTextTable node) { }
      public void Visit(SqlFunctionCall node) { }
      public void Visit(SqlCustomFunctionCall node) { }
      public void Visit(SqlIf node) { }
      public void Visit(SqlInsert node) { }
      public void Visit(SqlJoinExpression node) { }
      public void Visit(SqlJoinHint node) { }
      public void Visit(SqlLike node) { }
      public void Visit(SqlMatch node) { }
      public void Visit(SqlMetadata node) { }
      public void Visit(SqlNative node) { }
      public void Visit(SqlNativeHint node) { }
      public void Visit(SqlIndexHint node) { }
      public void Visit(SqlNextValue value) { }
      public void Visit(SqlOpenCursor node) { }
      public void Visit(SqlOrder node) { }
      public void Visit(SqlParameterRef node) { }
      public void Visit(SqlRound node) { }
      public void Visit(SqlQueryExpression node) { }
      public void Visit(SqlRow node) { }
      public void Visit(SqlRowNumber node) { }
      public void Visit(SqlRenameTable node) { }
      public void Visit(SqlStatementBlock node) { }
      public void Visit(SqlTrim node) { }
      public void Visit(SqlUpdate node) { }
      public void Visit(SqlUserFunctionCall node) { }
      public void Visit(SqlVariable node) { }
      public void Visit(SqlVariant node) { }
      public void Visit(SqlWhile node) { }
    }

    #endregion
  }
}
