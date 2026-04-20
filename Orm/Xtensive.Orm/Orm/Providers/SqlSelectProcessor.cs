// Copyright (C) 2012-2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Sql;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;

namespace Xtensive.Orm.Providers
{
  internal class SqlSelectProcessor : ISqlVisitor
  {
    private readonly SqlSelect rootSelect;
    private readonly ProviderInfo providerInfo;
    private readonly HashSet<SqlExpression> visitedExpressions = new HashSet<SqlExpression>();

    public void Visit(SqlAggregate node)
    {
      VisitNullable(node.Expression);
    }

    public void Visit(SqlAlterDomain node)
    {
    }

    public void Visit(SqlAlterPartitionFunction node)
    {
    }

    public void Visit(SqlAlterPartitionScheme node)
    {
    }

    public void Visit(SqlAlterTable node)
    {
    }

    public void Visit(SqlAlterSequence node)
    {
    }

    public void Visit(SqlArray node)
    {
    }

    public void Visit(SqlAssignment node)
    {
      if (node.Left!=null)
        Visit(node.Left);
      VisitNullable(node.Right);
    }

    public void Visit(SqlBatch node)
    {
    }

    public void Visit(SqlBetween node)
    {
      VisitNullable(node.Left);
      VisitNullable(node.Right);
      VisitNullable(node.Expression);
    }

    public void Visit(SqlBinary node)
    {
      VisitNullable(node.Left);
      VisitNullable(node.Right);
    }

    public void Visit(SqlBreak node)
    {
    }

    public void Visit(SqlCase node)
    {
      VisitNullable(node.Value);
      VisitNullable(node.Else);
      foreach (var pair in (IEnumerable<KeyValuePair<SqlExpression, SqlExpression>>) node) {
        VisitNullable(pair.Key);
        VisitNullable(pair.Value);
      }
    }

    public void Visit(SqlCast node)
    {
      VisitNullable(node.Operand);
    }

    public void Visit(SqlCloseCursor node)
    {
    }

    public void Visit(SqlCollate node)
    {
      VisitNullable(node.Operand);
    }

    public void Visit(SqlColumnRef node)
    {
      VisitNullable(node.SqlColumn);
    }

    public void Visit(SqlConcat node)
    {
    }

    public void Visit(SqlContainsTable node)
    {
      if (node.TargetTable!=null)
        Visit(node.TargetTable);
      foreach (var column in node.Columns)
        Visit(column);
      foreach (var column in node.TargetColumns)
        Visit(column);
    }

    public void Visit(SqlContinue node)
    {
    }

    public void Visit(SqlContainer node)
    {
    }

    public void Visit(SqlCommand node)
    {
    }

    public void Visit(SqlCreateAssertion node)
    {
    }

    public void Visit(SqlCreateCharacterSet node)
    {
    }

    public void Visit(SqlCreateCollation node)
    {
    }

    public void Visit(SqlCreateDomain node)
    {
    }

    public void Visit(SqlCreateIndex node)
    {
    }

    public void Visit(SqlCreatePartitionFunction node)
    {
    }

    public void Visit(SqlCreatePartitionScheme node)
    {
    }

    public void Visit(SqlCreateSchema node)
    {
    }

    public void Visit(SqlCreateSequence node)
    {
    }

    public void Visit(SqlCreateTable node)
    {
    }

    public void Visit(SqlCreateTranslation node)
    {
    }

    public void Visit(SqlCreateView node)
    {
    }

    public void Visit(SqlCursor node)
    {
    }

    public void Visit(SqlDeclareCursor node)
    {
    }

    public void Visit(SqlDefaultValue node)
    {
    }

    public void Visit(SqlDelete node)
    {
      if (node.Delete!=null)
        Visit(node.Delete);
      VisitNullable(node.Where);
    }

    public void Visit(SqlDropAssertion node)
    {
    }

    public void Visit(SqlDropCharacterSet node)
    {
    }

    public void Visit(SqlDropCollation node)
    {
    }

    public void Visit(SqlDropDomain node)
    {
    }

    public void Visit(SqlDropIndex node)
    {
    }

    public void Visit(SqlDropPartitionFunction node)
    {
    }

    public void Visit(SqlDropPartitionScheme node)
    {
    }

    public void Visit(SqlDropSchema node)
    {
    }

    public void Visit(SqlDropSequence node)
    {
    }

    public void Visit(SqlDropTable node)
    {
    }

    public void Visit(SqlDropTranslation node)
    {
    }

    public void Visit(SqlDropView node)
    {
    }

    public void Visit(SqlTruncateTable node)
    {
    }

    public void Visit(SqlDynamicFilter node)
    {
    }

    public void Visit(SqlPlaceholder node)
    {
    }

    public void Visit(SqlExtract node)
    {
      VisitNullable(node.Operand);
    }

    public void Visit(SqlFastFirstRowsHint node)
    {
    }

    public void Visit(SqlFetch node)
    {
    }

    public void Visit(SqlForceJoinOrderHint node)
    {
    }

    public void Visit(SqlFreeTextTable node)
    {
      if (node.TargetTable!=null)
        Visit(node.TargetTable);
      foreach (var column in node.Columns)
        Visit(column);
      foreach (var column in node.TargetColumns)
        Visit(column);
    }

    public void Visit(SqlFunctionCall node)
    {
      // Indexed for over Arguments (typed as IReadOnlyList<SqlExpression>) avoids the boxed
      // IEnumerator<T> allocation that 'foreach' would create on every function-call visit.
      var args = node.Arguments;
      for (int i = 0, n = args.Count; i < n; i++)
        Visit(args[i]);
    }

    public void Visit(SqlCustomFunctionCall node)
    {
      var args = node.Arguments;
      for (int i = 0, n = args.Count; i < n; i++)
        Visit(args[i]);
    }

    public void Visit(SqlIf node)
    {
      if (node.True!=null)
        Visit(node.True);
      if (node.False!=null)
        Visit(node.False);
      VisitNullable(node.Condition);
    }

    public void Visit(SqlInsert node)
    {
      if (node.From != null) {
        Visit(node.From);
      }
      if (node.Into != null) {
        Visit(node.Into);
      }
      foreach (var value in node.ValueRows.SelectMany(row => row)) {
        Visit(value);
      }
    }

    public void Visit(SqlJoinExpression node)
    {
      VisitNullable(node.Expression);
      if (node.Left != null)
        Visit(node.Left);
      if (node.Right != null)
        Visit(node.Right);
    }

    public void Visit(SqlJoinHint node)
    {
    }

    public void Visit(SqlLike node)
    {
      VisitNullable(node.Expression);
      VisitNullable(node.Escape);
      VisitNullable(node.Pattern);
    }

    public void Visit(SqlLiteral node)
    {
    }

    public void Visit(SqlMatch node)
    {
      VisitNullable(node.Value);
      if (node.SubQuery is not null)
        Visit(node.SubQuery);
    }

    public void Visit(SqlNative node)
    {
    }

    public void Visit(SqlNativeHint node)
    {
    }
    
    public void Visit(SqlIndexHint node)
    {
    }

    public void Visit(SqlNextValue value)
    {
    }

    public void Visit(SqlNull node)
    {
    }

    public void Visit(SqlOpenCursor node)
    {
    }

    public void Visit(SqlOrder node)
    {
      VisitNullable(node.Expression);
    }

    public void Visit(SqlParameterRef node)
    {
    }

    public void Visit(SqlRound node)
    {
      VisitNullable(node.Argument);
      VisitNullable(node.Length);
    }

    public void Visit(SqlQueryExpression node)
    {
      if (node.Left!=null)
        Visit(node.Left);
      if (node.Right!=null)
        Visit(node.Right);
    }

    public void Visit(SqlQueryRef node)
    {
      foreach (var column in node.Columns)
        Visit(column);
      if (node.Query!=null)
        Visit(node.Query);
    }

    public void Visit(SqlRow node)
    {
    }

    public void Visit(SqlRowNumber node)
    {
      foreach (var order in node.OrderBy)
        Visit(order);
    }

    public void Visit(SqlRenameTable node)
    {
    }

    public void Visit(SqlStatementBlock node)
    {
    }

    public void Visit(SqlTableColumn node)
    {
    }

    public void Visit(SqlTableRef node)
    {
      if (node.DataTable!=null)
        Visit(node.DataTable);
      foreach (var column in node.Columns)
        Visit(column);
    }

    public void Visit(SqlTrim node)
    {
      VisitNullable(node.Expression);
    }

    public void Visit(SqlSelect node)
    {
      // Prune unused columns from the immediate FROM clause before recursing
      // into it. Doing this at the start of every Visit(SqlSelect) cascades
      // top-down through nested selects via the visitor's own recursion —
      // the inner SqlSelect's pruning sees the already-reduced parent column
      // set when its own Visit(SqlSelect) runs.
      SqlColumnPruner.PruneFromClause(node);

      foreach (var column in node.Columns)
        Visit(column);
      // GroupByReadOnly / OrderByReadOnly / Hints are typed as IReadOnlyList<T>; foreach over
      // those would allocate a boxed enumerator on every Visit(SqlSelect) — a hot per-statement
      // hub. Indexed for keeps the iteration on the stack with zero allocations and routes to
      // Array.Empty<T>'s cheap Count/this[] when the property is empty (the common case).
      var groupBy = node.GroupByReadOnly;
      for (int i = 0, n = groupBy.Count; i < n; i++)
        Visit(groupBy[i]);
      var orderBy = node.OrderByReadOnly;
      for (int i = 0, n = orderBy.Count; i < n; i++)
        Visit(orderBy[i]);
      if (node.From != null)
        Visit(node.From);
      VisitNullable(node.Having);
      VisitNullable(node.Limit);
      VisitNullable(node.Offset);
      VisitNullable(node.Where);
      var hints = node.Hints;
      for (int i = 0, n = hints.Count; i < n; i++)
        Visit(hints[i]);

      if (node.Columns.Count==0)
        node.Columns.Add(SqlDml.Null, "NULL");

      var hasPaging = node.HasLimit || node.HasOffset;

      var isCurrentRoot = ReferenceEquals(node, rootSelect);
      var keepOrderBy = isCurrentRoot || hasPaging;
      if (!keepOrderBy && node.OrderByReadOnly.Count > 0)
        node.OrderBy.Clear();

      if (!isCurrentRoot) {
        rootSelect.Comment = SqlComment.Join(rootSelect.Comment, node.Comment);
        node.Comment = null;
      }

      var addOrderBy = hasPaging
        && node.OrderByReadOnly.Count==0
        && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);

      if (addOrderBy)
        node.OrderBy.Add(1);
    }

    public void Visit(SqlSubQuery node)
    {
      if (node.Query!=null)
        Visit(node.Query);
    }

    public void Visit(SqlUnary node)
    {
      VisitNullable(node.Operand);
    }

    public void Visit(SqlMetadata node)
    {
      Visit(node.Expression);
    }

    public void Visit(SqlUpdate node)
    {
      if (node.From!=null)
        Visit(node.From);
      if (node.Update!=null)
        Visit(node.Update);
      VisitNullable(node.Where);
      foreach (var value in node.Values.Values)
        Visit(value);
      var hints = node.Hints;
      for (int i = 0, n = hints.Count; i < n; i++)
        Visit(hints[i]);
    }

    public void Visit(SqlUserColumn node)
    {
      VisitNullable(node.Expression);
    }

    public void Visit(SqlUserFunctionCall node)
    {
      var args = node.Arguments;
      for (int i = 0, n = args.Count; i < n; i++)
        Visit(args[i]);
    }

    public void Visit(SqlDeclareVariable node)
    {
    }

    public void Visit(SqlVariable node)
    {
    }

    public void Visit(SqlVariant node)
    {
      VisitNullable(node.Main);
      VisitNullable(node.Alternative);
    }

    public void Visit(SqlWhile node)
    {
      VisitNullable(node.Condition);
      if (node.Statement!=null)
        Visit(node.Statement);
    }

    public void Visit(SqlFragment node)
    {
    }

    public void Visit(SqlExpression sqlExpression)
    {
      if (visitedExpressions.Add(sqlExpression)) {
        sqlExpression.AcceptVisitor(this);
      }
    }

    public void Visit(SqlStatement sqlStatement)
    {
      sqlStatement.AcceptVisitor(this);
    }

    public void Visit(SqlTable sqlTable)
    {
      sqlTable.AcceptVisitor(this);
    }

    private void VisitNullable(SqlExpression sqlExpression)
    {
      if (sqlExpression is not null) {
        Visit(sqlExpression);
      }
    }

    private void Visit(ISqlQueryExpression queryExpression)
    {
      queryExpression.AcceptVisitor(this);
    }

    private void Visit(ISqlLValue sqlLValue)
    {
      sqlLValue.AcceptVisitor(this);
    }

    private void Visit(DataTable dataTable)
    {
    }

    private void Visit(SqlHint sqlExpression)
    {
    }

    public void Visit(SqlComment comment)
    {

    }

    public static void Process(SqlSelect select, ProviderInfo providerInfo)
    {
      ArgumentNullException.ThrowIfNull(select);
      ArgumentNullException.ThrowIfNull(providerInfo);
      new SqlSelectProcessor(select, providerInfo).Visit(select);
    }

    // Constructors

    private SqlSelectProcessor(SqlSelect rootSelect, ProviderInfo providerInfo)
    {
      this.rootSelect = rootSelect;
      this.providerInfo = providerInfo;
    }
  }
}
