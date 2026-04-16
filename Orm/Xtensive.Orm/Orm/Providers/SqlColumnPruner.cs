// Copyright (C) 2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Xtensive.Sql;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Post-processing pass that walks the <see cref="SqlSelect"/> AST top-down
  /// and removes unused columns from inner <see cref="SqlQueryRef"/> subqueries.
  /// This reduces SQL text size by eliminating column bloat introduced when
  /// the compiler unconditionally projects all columns through subquery layers.
  /// </summary>
  internal sealed class SqlColumnPruner
  {
    private readonly HashSet<SqlExpression> visited = new(ReferenceEqualityComparer.Instance);

    public static void Process(SqlSelect rootSelect)
    {
      ArgumentNullException.ThrowIfNull(rootSelect);
      new SqlColumnPruner().PruneSelect(rootSelect);
    }

    private void PruneSelect(SqlSelect select)
    {
      // Prune this level's FROM source first (top-down enables cascading
      // through multiple nesting levels — the inner select sees the
      // already-reduced column set).
      if (select.From is SqlQueryRef queryRef) {
        TryPruneQueryRef(select, queryRef);
      }

      // When FROM is a join, prune each side that is a SqlQueryRef
      if (select.From is SqlJoinedTable) {
        TryPruneJoinSides(select);
      }

      // Then recurse into FROM sources to prune deeper levels
      if (select.From != null) {
        RecurseIntoTable(select.From);
      }

      // Finally handle subqueries embedded in expressions
      RecurseIntoExpressionSubqueries(select);
    }

    private void RecurseIntoTable(SqlTable table)
    {
      switch (table) {
        case SqlQueryRef queryRef:
          if (queryRef.Query is SqlSelect innerSelect) {
            PruneSelect(innerSelect);
          }
          else if (queryRef.Query is SqlQueryExpression queryExpr) {
            RecurseIntoQueryExpression(queryExpr);
          }
          break;
        case SqlJoinedTable joined:
          RecurseIntoTable(joined.JoinExpression.Left);
          RecurseIntoTable(joined.JoinExpression.Right);
          break;
      }
    }

    private void RecurseIntoQueryExpression(SqlQueryExpression expr)
    {
      RecurseIntoQueryExpressionSide(expr.Left);
      RecurseIntoQueryExpressionSide(expr.Right);
    }

    private void RecurseIntoQueryExpressionSide(ISqlQueryExpression side)
    {
      if (side is SqlSelect select) {
        PruneSelect(select);
      }
      else if (side is SqlQueryExpression nested) {
        RecurseIntoQueryExpression(nested);
      }
    }

    private void RecurseIntoExpressionSubqueries(SqlSelect select)
    {
      foreach (var col in select.Columns) {
        FindAndPruneSubqueries(col);
      }
      FindAndPruneSubqueries(select.Where);
      FindAndPruneSubqueries(select.Having);
      foreach (var col in select.GroupByReadOnly) {
        FindAndPruneSubqueries(col);
      }
      foreach (var order in select.OrderByReadOnly) {
        if (order.Expression != null) {
          FindAndPruneSubqueries(order.Expression);
        }
      }
    }

    private void FindAndPruneSubqueries(SqlExpression expr)
    {
      if (expr == null || !visited.Add(expr)) {
        return;
      }

      switch (expr) {
        case SqlSubQuery sub:
          if (sub.Query is SqlSelect innerSelect) {
            PruneSelect(innerSelect);
          }
          break;
        case SqlBinary bin:
          FindAndPruneSubqueries(bin.Left);
          FindAndPruneSubqueries(bin.Right);
          break;
        case SqlUnary un:
          FindAndPruneSubqueries(un.Operand);
          break;
        case SqlCase cas:
          FindAndPruneSubqueries(cas.Value);
          FindAndPruneSubqueries(cas.Else);
          foreach (var pair in (IEnumerable<KeyValuePair<SqlExpression, SqlExpression>>) cas) {
            FindAndPruneSubqueries(pair.Key);
            FindAndPruneSubqueries(pair.Value);
          }
          break;
        case SqlCast cast:
          FindAndPruneSubqueries(cast.Operand);
          break;
        case SqlUserColumn uc:
          FindAndPruneSubqueries(uc.Expression);
          break;
        case SqlColumnRef cr:
          FindAndPruneSubqueries(cr.SqlColumn);
          break;
        case SqlFunctionCallBase func:
          foreach (var arg in func.Arguments) {
            FindAndPruneSubqueries(arg);
          }
          break;
        case SqlAggregate agg:
          FindAndPruneSubqueries(agg.Expression);
          break;
        case SqlLike like:
          FindAndPruneSubqueries(like.Expression);
          FindAndPruneSubqueries(like.Pattern);
          FindAndPruneSubqueries(like.Escape);
          break;
        case SqlBetween between:
          FindAndPruneSubqueries(between.Expression);
          FindAndPruneSubqueries(between.Left);
          FindAndPruneSubqueries(between.Right);
          break;
        case SqlExpressionList list:
          for (int i = 0; i < list.Count; i++) {
            FindAndPruneSubqueries(list[i]);
          }
          break;
        case SqlTrim trim:
          FindAndPruneSubqueries(trim.Expression);
          break;
        case SqlExtract extract:
          FindAndPruneSubqueries(extract.Operand);
          break;
        case SqlRound round:
          FindAndPruneSubqueries(round.Argument);
          FindAndPruneSubqueries(round.Length);
          break;
        case SqlCollate collate:
          FindAndPruneSubqueries(collate.Operand);
          break;
        case SqlMatch match:
          FindAndPruneSubqueries(match.Value);
          FindAndPruneSubqueries(match.SubQuery);
          break;
        case SqlVariant variant:
          FindAndPruneSubqueries(variant.Main);
          FindAndPruneSubqueries(variant.Alternative);
          break;
        case SqlDynamicFilter filter:
          foreach (var e in filter.Expressions) {
            FindAndPruneSubqueries(e);
          }
          break;
        case SqlRowNumber rn:
          foreach (var order in rn.OrderBy) {
            if (order.Expression != null) {
              FindAndPruneSubqueries(order.Expression);
            }
          }
          break;
      }
    }

    private void TryPruneQueryRef(SqlSelect outerSelect, SqlQueryRef queryRef)
    {
      var queryRefColumnCount = queryRef.Columns.Count;
      if (queryRefColumnCount == 0) {
        return;
      }

      var usedColumns = new HashSet<SqlTableColumn>(ReferenceEqualityComparer.Instance);
      CollectUsedColumnsFromSelect(outerSelect, queryRef, usedColumns);

      if (usedColumns.Count >= queryRefColumnCount) {
        return;
      }

      var indicesToKeep = new List<int>(usedColumns.Count);
      for (int i = 0; i < queryRefColumnCount; i++) {
        if (usedColumns.Contains(queryRef.Columns[i])) {
          indicesToKeep.Add(i);
        }
      }

      if (indicesToKeep.Count == 0 || indicesToKeep.Count >= queryRefColumnCount) {
        return;
      }

      queryRef.PruneColumns(indicesToKeep);
    }

    private void TryPruneJoinSides(SqlSelect select)
    {
      var joinQueryRefs = new List<SqlQueryRef>();
      CollectQueryRefsFromJoinTree(select.From, joinQueryRefs);

      foreach (var sideRef in joinQueryRefs) {
        var columnCount = sideRef.Columns.Count;
        if (columnCount == 0) {
          continue;
        }

        var usedColumns = new HashSet<SqlTableColumn>(ReferenceEqualityComparer.Instance);
        CollectUsedColumnsFromSelect(select, sideRef, usedColumns);
        CollectUsedColumnsFromJoinConditions(select.From, sideRef, usedColumns);

        if (usedColumns.Count >= columnCount) {
          continue;
        }

        var indicesToKeep = new List<int>(usedColumns.Count);
        for (int i = 0; i < columnCount; i++) {
          if (usedColumns.Contains(sideRef.Columns[i])) {
            indicesToKeep.Add(i);
          }
        }

        if (indicesToKeep.Count == 0 || indicesToKeep.Count >= columnCount) {
          continue;
        }

        sideRef.PruneColumns(indicesToKeep);
      }
    }

    private static void CollectQueryRefsFromJoinTree(SqlTable table, List<SqlQueryRef> result)
    {
      if (table is SqlQueryRef qr) {
        result.Add(qr);
      }
      else if (table is SqlJoinedTable jt) {
        CollectQueryRefsFromJoinTree(jt.JoinExpression.Left, result);
        CollectQueryRefsFromJoinTree(jt.JoinExpression.Right, result);
      }
    }

    private void CollectUsedColumnsFromJoinConditions(
      SqlTable table, SqlTable targetTable, HashSet<SqlTableColumn> usedColumns)
    {
      if (table is SqlJoinedTable jt) {
        CollectUsedColumns(jt.JoinExpression.Expression, targetTable, usedColumns);
        CollectUsedColumnsFromJoinConditions(jt.JoinExpression.Left, targetTable, usedColumns);
        CollectUsedColumnsFromJoinConditions(jt.JoinExpression.Right, targetTable, usedColumns);
      }
    }

    private void CollectUsedColumnsFromSelect(
      SqlSelect select, SqlTable targetTable, HashSet<SqlTableColumn> usedColumns)
    {
      foreach (var col in select.Columns) {
        CollectUsedColumns(col, targetTable, usedColumns);
      }
      CollectUsedColumns(select.Where, targetTable, usedColumns);
      CollectUsedColumns(select.Having, targetTable, usedColumns);
      foreach (var col in select.GroupByReadOnly) {
        CollectUsedColumns(col, targetTable, usedColumns);
      }
      foreach (var order in select.OrderByReadOnly) {
        if (order.Expression != null) {
          CollectUsedColumns(order.Expression, targetTable, usedColumns);
        }
      }
    }

    private void CollectUsedColumns(
      SqlExpression expr, SqlTable targetTable, HashSet<SqlTableColumn> usedColumns)
    {
      if (expr == null) {
        return;
      }

      switch (expr) {
        case SqlTableColumn tc:
          if (ReferenceEquals(tc.SqlTable, targetTable)) {
            usedColumns.Add(tc);
          }
          break;
        case SqlColumnRef cr:
          CollectUsedColumns(cr.SqlColumn, targetTable, usedColumns);
          break;
        case SqlColumnStub cs:
          CollectUsedColumns(cs.Column, targetTable, usedColumns);
          break;
        case SqlUserColumn uc:
          CollectUsedColumns(uc.Expression, targetTable, usedColumns);
          break;
        case SqlBinary bin:
          CollectUsedColumns(bin.Left, targetTable, usedColumns);
          CollectUsedColumns(bin.Right, targetTable, usedColumns);
          break;
        case SqlUnary un:
          CollectUsedColumns(un.Operand, targetTable, usedColumns);
          break;
        case SqlCase cas:
          CollectUsedColumns(cas.Value, targetTable, usedColumns);
          CollectUsedColumns(cas.Else, targetTable, usedColumns);
          foreach (var pair in (IEnumerable<KeyValuePair<SqlExpression, SqlExpression>>) cas) {
            CollectUsedColumns(pair.Key, targetTable, usedColumns);
            CollectUsedColumns(pair.Value, targetTable, usedColumns);
          }
          break;
        case SqlCast cast:
          CollectUsedColumns(cast.Operand, targetTable, usedColumns);
          break;
        case SqlFunctionCallBase func:
          foreach (var arg in func.Arguments) {
            CollectUsedColumns(arg, targetTable, usedColumns);
          }
          break;
        case SqlAggregate agg:
          CollectUsedColumns(agg.Expression, targetTable, usedColumns);
          break;
        case SqlLike like:
          CollectUsedColumns(like.Expression, targetTable, usedColumns);
          CollectUsedColumns(like.Pattern, targetTable, usedColumns);
          CollectUsedColumns(like.Escape, targetTable, usedColumns);
          break;
        case SqlBetween between:
          CollectUsedColumns(between.Expression, targetTable, usedColumns);
          CollectUsedColumns(between.Left, targetTable, usedColumns);
          CollectUsedColumns(between.Right, targetTable, usedColumns);
          break;
        case SqlSubQuery sub:
          if (sub.Query is SqlSelect innerSelect) {
            CollectUsedColumnsFromSelect(innerSelect, targetTable, usedColumns);
          }
          break;
        case SqlExpressionList list:
          for (int i = 0; i < list.Count; i++) {
            CollectUsedColumns(list[i], targetTable, usedColumns);
          }
          break;
        case SqlTrim trim:
          CollectUsedColumns(trim.Expression, targetTable, usedColumns);
          break;
        case SqlExtract extract:
          CollectUsedColumns(extract.Operand, targetTable, usedColumns);
          break;
        case SqlRound round:
          CollectUsedColumns(round.Argument, targetTable, usedColumns);
          CollectUsedColumns(round.Length, targetTable, usedColumns);
          break;
        case SqlCollate collate:
          CollectUsedColumns(collate.Operand, targetTable, usedColumns);
          break;
        case SqlMatch match:
          CollectUsedColumns(match.Value, targetTable, usedColumns);
          CollectUsedColumns(match.SubQuery, targetTable, usedColumns);
          break;
        case SqlVariant variant:
          CollectUsedColumns(variant.Main, targetTable, usedColumns);
          CollectUsedColumns(variant.Alternative, targetTable, usedColumns);
          break;
        case SqlDynamicFilter filter:
          foreach (var e in filter.Expressions) {
            CollectUsedColumns(e, targetTable, usedColumns);
          }
          break;
        case SqlRowNumber rn:
          foreach (var order in rn.OrderBy) {
            if (order.Expression != null) {
              CollectUsedColumns(order.Expression, targetTable, usedColumns);
            }
          }
          break;

        // Leaf nodes — no column references to collect
        case SqlNull:
        case SqlLiteral:
        case SqlParameterRef:
        case SqlNative:
        case SqlPlaceholder:
        case SqlDefaultValue:
        case SqlVariable:
        case SqlCursor:
        case SqlContainer:
        case SqlMetadata:
          break;

        default:
          // Unknown expression type — conservatively mark all target columns as used
          // to prevent incorrect pruning when new expression types are introduced.
          var targetColumns = targetTable.Columns;
          for (int i = 0; i < targetColumns.Count; i++) {
            usedColumns.Add(targetColumns[i]);
          }
          break;
      }
    }
  }
}
