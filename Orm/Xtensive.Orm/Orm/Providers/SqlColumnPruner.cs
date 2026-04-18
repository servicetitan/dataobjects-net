// Copyright (C) 2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xtensive.Sql;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Removes unused columns from <see cref="SqlQueryRef"/> sources in the
  /// immediate <see cref="SqlSelect.From"/> clause of a SELECT. This shrinks
  /// generated SQL by eliminating column bloat introduced when the compiler
  /// unconditionally projects all columns through subquery layers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Pruning cascades top-down through nested SELECTs because
  /// <see cref="SqlSelectProcessor"/>'s visitor calls
  /// <see cref="PruneFromClause"/> at the start of every Visit(SqlSelect) and
  /// then recurses into the FROM clause via the visitor. By the time an inner
  /// SELECT's Visit(SqlSelect) runs, its parent has already been reduced.
  /// </para>
  /// <para>
  /// IMPORTANT INVARIANT: nested pruning correctness relies on
  /// <see cref="SqlSelectProcessor"/> visiting every <see cref="SqlSubQuery"/>,
  /// every <see cref="ISqlQueryExpression"/> child, and every join subtree.
  /// If a new node type is added whose visitor does not recurse into nested
  /// SELECTs, pruning silently stops cascading through that node. The
  /// <c>Visit(SqlVariant)</c> / <c>Visit(SqlCase)</c> regressions fixed in
  /// the visitor are the historical evidence of this coupling.
  /// </para>
  /// <para>
  /// IDENTITY-EQUALITY ASSUMPTION: the dictionaries and hash sets below intentionally
  /// rely on the default <see cref="EqualityComparer{T}"/> for <see cref="SqlTable"/>,
  /// <see cref="SqlExpression"/>, and <see cref="SqlTableColumn"/>. None of these
  /// types override <see cref="object.Equals(object)"/> / <see cref="object.GetHashCode"/>
  /// nor implement <see cref="IEquatable{T}"/>, so the default comparer behaves
  /// as a reference-equality comparer. If equality semantics are ever introduced
  /// on those classes, this pruner must be updated to pass an explicit
  /// <see cref="ReferenceEqualityComparer"/>; otherwise structurally-equal but
  /// distinct DML nodes would alias and pruning would become incorrect.
  /// </para>
  /// </remarks>
  internal static class SqlColumnPruner
  {
    /// <summary>
    /// Prunes unused columns from any <see cref="SqlQueryRef"/>(s) directly in
    /// <paramref name="select"/>'s FROM clause. Does NOT recurse into nested
    /// SELECTs — the caller is expected to drive the rest of the AST walk.
    /// </summary>
    internal static void PruneFromClause(SqlSelect select)
    {
      switch (select.From) {
        case SqlQueryRef queryRef:
          TryPruneSingleQueryRef(select, queryRef);
          break;
        case SqlJoinedTable joinedRoot:
          TryPruneJoinTree(select, joinedRoot);
          break;
      }
    }

    private static void TryPruneSingleQueryRef(SqlSelect outerSelect, SqlQueryRef queryRef)
    {
      var columnCount = queryRef.Columns.Count;
      if (columnCount == 0) {
        return;
      }

      var bucket = new ColumnBucket(queryRef, columnCount);
      var bucketByTable = new Dictionary<SqlTable, ColumnBucket>(1) {
        [queryRef] = bucket
      };
      var collectVisited = new HashSet<SqlExpression>();

      CollectUsedColumnsFromSelect(outerSelect, bucketByTable, collectVisited);
      ApplyPruning(bucket);
    }

    private static void TryPruneJoinTree(SqlSelect outerSelect, SqlJoinedTable joinedRoot)
    {
      // Single-pass collection: enumerate every join-leaf SqlQueryRef into a
      // bucket dictionary keyed by table identity, then walk the outer SELECT
      // and the entire join subtree exactly once. SqlTableColumn references
      // are routed into the appropriate bucket via O(1) dictionary lookup.
      // Replaces the previous N-times-per-query walk (one per join target).
      // Typical join trees have a small, bounded number of leaves (2-5).
      // Pre-sizing avoids the resize/rehash cycles that would otherwise hit
      // every Add. The capacity is a hint — real depth is discovered during
      // the walk and growth still works correctly if exceeded.
      const int initialBucketCapacity = 4;
      var allBuckets = new List<ColumnBucket>(initialBucketCapacity);
      var bucketByTable = new Dictionary<SqlTable, ColumnBucket>(initialBucketCapacity);
      CollectJoinTargets(joinedRoot, allBuckets, bucketByTable);
      if (allBuckets.Count == 0) {
        return;
      }

      var collectVisited = new HashSet<SqlExpression>();
      CollectUsedColumnsFromSelect(outerSelect, bucketByTable, collectVisited);
      if (bucketByTable.Count > 0) {
        CollectUsedColumnsFromJoinSubtrees(joinedRoot, bucketByTable, collectVisited);
      }

      // CollectionsMarshal.AsSpan exposes the list's backing array directly so
      // ApplyPruning can take each element by `in` without an intermediate copy
      // of the (small but not free) ColumnBucket struct.
      var bucketSpan = CollectionsMarshal.AsSpan(allBuckets);
      for (int i = 0; i < bucketSpan.Length; i++) {
        ApplyPruning(in bucketSpan[i]);
      }
    }

    private static void CollectJoinTargets(
      SqlTable table,
      List<ColumnBucket> allBuckets,
      Dictionary<SqlTable, ColumnBucket> bucketByTable)
    {
      switch (table) {
        case SqlQueryRef qr:
          var count = qr.Columns.Count;
          if (count > 0) {
            var bucket = new ColumnBucket(qr, count);
            allBuckets.Add(bucket);
            bucketByTable[qr] = bucket;
          }
          break;
        case SqlJoinedTable jt:
          CollectJoinTargets(jt.JoinExpression.Left, allBuckets, bucketByTable);
          CollectJoinTargets(jt.JoinExpression.Right, allBuckets, bucketByTable);
          break;
      }
    }

    private static void ApplyPruning(in ColumnBucket bucket)
    {
      var totalCount = bucket.ColumnCount;
      var usedCount = bucket.UsedColumns.Count;
      if (usedCount == 0 || usedCount >= totalCount) {
        return;
      }

      var indicesToKeep = new List<int>(usedCount);
      var columns = bucket.QueryRef.Columns;
      for (int i = 0; i < totalCount; i++) {
        if (bucket.UsedColumns.Contains(columns[i])) {
          indicesToKeep.Add(i);
        }
      }

      if (indicesToKeep.Count == 0 || indicesToKeep.Count >= totalCount) {
        return;
      }

      bucket.QueryRef.PruneColumns(indicesToKeep);
    }

    // ===== Collection =====

    private static void CollectUsedColumnsFromJoinSubtrees(
      SqlTable table,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      if (bucketByTable.Count == 0) {
        return;
      }
      switch (table) {
        case SqlQueryRef qr:
          CollectUsedColumnsFromQuerySubtree(qr, bucketByTable, collectVisited);
          break;
        case SqlJoinedTable jt:
          CollectUsedColumns(jt.JoinExpression.Expression, bucketByTable, collectVisited);
          CollectUsedColumnsFromJoinSubtrees(jt.JoinExpression.Left, bucketByTable, collectVisited);
          CollectUsedColumnsFromJoinSubtrees(jt.JoinExpression.Right, bucketByTable, collectVisited);
          break;
      }
    }

    private static void CollectUsedColumnsFromQuerySubtree(
      SqlQueryRef queryRef,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      switch (queryRef.Query) {
        case SqlSelect innerSelect:
          RecurseIntoSelect(innerSelect, bucketByTable, collectVisited);
          break;
        case SqlQueryExpression queryExpr:
          CollectUsedColumnsFromQueryExpression(queryExpr, bucketByTable, collectVisited);
          break;
      }
    }

    /// <summary>
    /// Walks a nested <see cref="SqlSelect"/>: collect references from its
    /// projection/predicate/grouping/ordering, then descend into its FROM clause
    /// (which may itself be a join subtree) so column usage from join predicates
    /// of the inner SELECT is also tracked. The FROM walk is skipped when the
    /// active routing dictionary has already been emptied by saturation.
    /// </summary>
    private static void RecurseIntoSelect(
      SqlSelect select,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      CollectUsedColumnsFromSelect(select, bucketByTable, collectVisited);
      if (select.From != null && bucketByTable.Count > 0) {
        CollectUsedColumnsFromJoinSubtrees(select.From, bucketByTable, collectVisited);
      }
    }

    private static void CollectUsedColumnsFromQueryExpression(
      SqlQueryExpression queryExpr,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      CollectUsedColumnsFromQueryExpressionSide(queryExpr.Left, bucketByTable, collectVisited);
      if (bucketByTable.Count == 0) {
        return;
      }
      CollectUsedColumnsFromQueryExpressionSide(queryExpr.Right, bucketByTable, collectVisited);
    }

    private static void CollectUsedColumnsFromQueryExpressionSide(
      ISqlQueryExpression side,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      switch (side) {
        case SqlSelect sel:
          RecurseIntoSelect(sel, bucketByTable, collectVisited);
          break;
        case SqlQueryExpression nested:
          CollectUsedColumnsFromQueryExpression(nested, bucketByTable, collectVisited);
          break;
      }
    }

    private static void CollectUsedColumnsFromSelect(
      SqlSelect select,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      foreach (var col in select.Columns) {
        if (bucketByTable.Count == 0) {
          return;
        }
        CollectUsedColumns(col, bucketByTable, collectVisited);
      }
      if (bucketByTable.Count == 0) {
        return;
      }
      CollectUsedColumns(select.Where, bucketByTable, collectVisited);
      if (bucketByTable.Count == 0) {
        return;
      }
      CollectUsedColumns(select.Having, bucketByTable, collectVisited);
      var groupBy = select.GroupByReadOnly;
      for (int i = 0, n = groupBy.Count; i < n; i++) {
        if (bucketByTable.Count == 0) {
          return;
        }
        CollectUsedColumns(groupBy[i], bucketByTable, collectVisited);
      }
      var orderBy = select.OrderByReadOnly;
      for (int i = 0, n = orderBy.Count; i < n; i++) {
        if (bucketByTable.Count == 0) {
          return;
        }
        var orderExpr = orderBy[i].Expression;
        if (orderExpr != null) {
          CollectUsedColumns(orderExpr, bucketByTable, collectVisited);
        }
      }
    }

    private static void CollectUsedColumns(
      SqlExpression expr,
      Dictionary<SqlTable, ColumnBucket> bucketByTable,
      HashSet<SqlExpression> collectVisited)
    {
      if (expr is null || bucketByTable.Count == 0 || !collectVisited.Add(expr)) {
        return;
      }

      switch (expr) {
        case SqlTableColumn tc:
          var sqlTable = tc.SqlTable;
          if (sqlTable != null) {
            // GetValueRefOrNullRef returns a ref directly into the dictionary's
            // backing storage instead of copying the 24-byte ColumnBucket struct
            // onto the stack. This is the hottest dispatch in the entire walk —
            // every column reference in the SELECT lands here.
            // Safety: we only Remove() AFTER reading IsSaturated through the
            // ref, so the dictionary is not mutated while the ref is live.
            ref var bucket = ref CollectionsMarshal.GetValueRefOrNullRef(bucketByTable, sqlTable);
            if (!Unsafe.IsNullRef(ref bucket)
                && bucket.UsedColumns.Add(tc)
                && bucket.IsSaturated) {
              bucketByTable.Remove(sqlTable);
            }
          }
          break;
        case SqlColumnRef cr:
          CollectUsedColumns(cr.SqlColumn, bucketByTable, collectVisited);
          break;
        case SqlColumnStub cs:
          CollectUsedColumns(cs.Column, bucketByTable, collectVisited);
          break;
        case SqlUserColumn uc:
          CollectUsedColumns(uc.Expression, bucketByTable, collectVisited);
          break;
        case SqlBinary bin:
          CollectUsedColumns(bin.Left, bucketByTable, collectVisited);
          CollectUsedColumns(bin.Right, bucketByTable, collectVisited);
          break;
        case SqlUnary un:
          CollectUsedColumns(un.Operand, bucketByTable, collectVisited);
          break;
        case SqlCase cas:
          CollectUsedColumns(cas.Value, bucketByTable, collectVisited);
          CollectUsedColumns(cas.Else, bucketByTable, collectVisited);
          foreach (var pair in (IEnumerable<KeyValuePair<SqlExpression, SqlExpression>>) cas) {
            CollectUsedColumns(pair.Key, bucketByTable, collectVisited);
            CollectUsedColumns(pair.Value, bucketByTable, collectVisited);
          }
          break;
        case SqlCast cast:
          CollectUsedColumns(cast.Operand, bucketByTable, collectVisited);
          break;
        case SqlFunctionCallBase func:
          var args = func.Arguments;
          for (int i = 0, n = args.Count; i < n; i++) {
            CollectUsedColumns(args[i], bucketByTable, collectVisited);
          }
          break;
        case SqlAggregate agg:
          CollectUsedColumns(agg.Expression, bucketByTable, collectVisited);
          break;
        case SqlLike like:
          CollectUsedColumns(like.Expression, bucketByTable, collectVisited);
          CollectUsedColumns(like.Pattern, bucketByTable, collectVisited);
          CollectUsedColumns(like.Escape, bucketByTable, collectVisited);
          break;
        case SqlBetween between:
          CollectUsedColumns(between.Expression, bucketByTable, collectVisited);
          CollectUsedColumns(between.Left, bucketByTable, collectVisited);
          CollectUsedColumns(between.Right, bucketByTable, collectVisited);
          break;
        case SqlSubQuery sub:
          // SqlSubQuery.Query is ISqlQueryExpression — both SqlSelect and
          // SqlQueryExpression (UNION / INTERSECT / EXCEPT) are valid bodies.
          // Missing the SqlQueryExpression arm meant correlated references
          // inside set-operation subqueries were invisible to the collector,
          // causing the parent query-ref to be incorrectly pruned. Mirror
          // the symmetric dispatch already used by CollectUsedColumnsFromQuerySubtree.
          switch (sub.Query) {
            case SqlSelect innerSelect:
              RecurseIntoSelect(innerSelect, bucketByTable, collectVisited);
              break;
            case SqlQueryExpression queryExpr:
              CollectUsedColumnsFromQueryExpression(queryExpr, bucketByTable, collectVisited);
              break;
          }
          break;
        case SqlExpressionList list:
          for (int i = 0; i < list.Count; i++) {
            CollectUsedColumns(list[i], bucketByTable, collectVisited);
          }
          break;
        case SqlTrim trim:
          CollectUsedColumns(trim.Expression, bucketByTable, collectVisited);
          break;
        case SqlExtract extract:
          CollectUsedColumns(extract.Operand, bucketByTable, collectVisited);
          break;
        case SqlRound round:
          CollectUsedColumns(round.Argument, bucketByTable, collectVisited);
          CollectUsedColumns(round.Length, bucketByTable, collectVisited);
          break;
        case SqlCollate collate:
          CollectUsedColumns(collate.Operand, bucketByTable, collectVisited);
          break;
        case SqlMatch match:
          CollectUsedColumns(match.Value, bucketByTable, collectVisited);
          CollectUsedColumns(match.SubQuery, bucketByTable, collectVisited);
          break;
        case SqlVariant variant:
          CollectUsedColumns(variant.Main, bucketByTable, collectVisited);
          CollectUsedColumns(variant.Alternative, bucketByTable, collectVisited);
          break;
        case SqlDynamicFilter filter:
          var filterExprs = filter.Expressions;
          for (int i = 0, n = filterExprs.Count; i < n; i++) {
            CollectUsedColumns(filterExprs[i], bucketByTable, collectVisited);
          }
          break;
        case SqlRowNumber rn:
          var rnOrderBy = rn.OrderBy;
          for (int i = 0, n = rnOrderBy.Count; i < n; i++) {
            var rnOrderExpr = rnOrderBy[i].Expression;
            if (rnOrderExpr != null) {
              CollectUsedColumns(rnOrderExpr, bucketByTable, collectVisited);
            }
          }
          break;
        case SqlMetadata metadata:
          CollectUsedColumns(metadata.Expression, bucketByTable, collectVisited);
          break;

        // Leaf nodes — no column references to collect.
        case SqlNull:
        case SqlLiteral:
        case SqlParameterRef:
        case SqlNative:
        case SqlPlaceholder:
        case SqlDefaultValue:
        case SqlVariable:
        case SqlCursor:
        case SqlContainer:
          break;

        default:
          // Unknown expression type — almost certainly a new SqlExpression
          // subclass added without updating this switch. Surface the omission
          // loudly in DEBUG, then conservatively mark every candidate target
          // as fully used so pruning becomes a no-op rather than incorrect.
          Debug.Fail(
            $"SqlColumnPruner: unhandled SqlExpression type '{expr.GetType().FullName}'. " +
            "Pruning is conservatively disabled across this node. " +
            "Add a case to CollectUsedColumns to enable pruning through this expression type.");
          MarkAllTargetsFullyUsed(bucketByTable);
          break;
      }
    }

    private static void MarkAllTargetsFullyUsed(Dictionary<SqlTable, ColumnBucket> bucketByTable)
    {
      // Saturate every bucket, then drop them all from the active routing dict
      // in a single Clear() call. Iterating + a tail Clear avoids the need to
      // mutate the dictionary mid-walk, so no key snapshot is required.
      // Dictionary<,>.Enumerator is a struct, so the foreach is allocation-free.
      if (bucketByTable.Count == 0) {
        return;
      }
      foreach (var bucket in bucketByTable.Values) {
        var cols = bucket.QueryRef.Columns;
        var used = bucket.UsedColumns;
        for (int i = 0, n = cols.Count; i < n; i++) {
          used.Add(cols[i]);
        }
      }
      bucketByTable.Clear();
    }

    /// <summary>
    /// Per-target accumulator. Declared as a <c>readonly struct</c> so the
    /// three fields (a <see cref="SqlQueryRef"/> reference, an int, and a
    /// <see cref="HashSet{T}"/> reference) live inline inside the dictionary
    /// entry and list element slots — no per-bucket heap allocation. Mutations
    /// to the accumulated column set still propagate across every struct copy
    /// (dictionary entry, list entry, <c>out var</c> locals) because
    /// <see cref="UsedColumns"/> is a reference to a shared heap object.
    /// </summary>
    private readonly struct ColumnBucket(SqlQueryRef queryRef, int columnCount)
    {
      public readonly SqlQueryRef QueryRef = queryRef;
      public readonly int ColumnCount = columnCount;
      // UsedColumns is bounded above by ColumnCount (we never add a column
      // not already in QueryRef.Columns). Pre-sizing eliminates the entire
      // HashSet 4/8/16/... resize-and-rehash chain, which matters for wide
      // entity-table buckets where ColumnCount is routinely 20+.
      public readonly HashSet<SqlTableColumn> UsedColumns = new(columnCount);

      public bool IsSaturated => UsedColumns.Count >= ColumnCount;
    }
  }
}
