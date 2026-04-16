// Copyright (C) 2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Providers;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;

namespace Xtensive.Orm.Tests.Sql
{
  [TestFixture]
  public class SqlColumnPrunerTest
  {
    private Table table1;
    private Table table2;

    [OneTimeSetUp]
    public void SetUp()
    {
      var catalog = new Catalog("test");
      var schema = catalog.CreateSchema("dbo");

      table1 = schema.CreateTable("table1");
      _ = table1.CreateColumn("Col0", new SqlValueType(SqlType.Int32));
      _ = table1.CreateColumn("Col1", new SqlValueType(SqlType.VarChar));
      _ = table1.CreateColumn("Col2", new SqlValueType(SqlType.VarChar));
      _ = table1.CreateColumn("Col3", new SqlValueType(SqlType.VarChar));
      _ = table1.CreateColumn("Col4", new SqlValueType(SqlType.VarChar));

      table2 = schema.CreateTable("table2");
      _ = table2.CreateColumn("Id", new SqlValueType(SqlType.Int32));
      _ = table2.CreateColumn("Name", new SqlValueType(SqlType.VarChar));
      _ = table2.CreateColumn("Value", new SqlValueType(SqlType.VarChar));
    }

    #region Basic pruning

    [Test]
    public void PrunesUnusedColumns()
    {
      // Before: SELECT q.Col0, q.Col2 FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col0, q.Col2 FROM (SELECT t.Col0, t.Col2 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(queryRef[2]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col2");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void DoesNotPruneWhenAllColumnsUsed()
    {
      // SELECT q.Col0, q.Col1, q.Col2, q.Col3, q.Col4
      // FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // All 5 columns used → no pruning expected
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      for (int i = 0; i < queryRef.Columns.Count; i++) {
        outerSelect.Columns.Add(queryRef[i]);
      }

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2", "Col3", "Col4");
      AssertSelectColumnCount(innerSelect, 5);
    }

    [Test]
    public void PrunesSingleColumnToOne()
    {
      // Before: SELECT q.Col3 FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col3 FROM (SELECT t.Col3 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[3]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col3");
      AssertSelectColumnCount(innerSelect, 1);
    }

    [Test]
    public void PreservesColumnIdentityAfterPruning()
    {
      // Before: SELECT q.Col0, q.Col4 FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col0, q.Col4 FROM (SELECT t.Col0, t.Col4 FROM table1 t) q
      // SqlTableColumn object references must survive pruning (identity, not just name)
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);

      var col0Ref = queryRef[0];
      var col4Ref = queryRef[4];
      outerSelect.Columns.Add(col0Ref);
      outerSelect.Columns.Add(col4Ref);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col4");
      Assert.That(queryRef.Columns[0], Is.SameAs(col0Ref));
      Assert.That(queryRef.Columns[1], Is.SameAs(col4Ref));
    }

    #endregion

    #region Column references in clauses

    [Test]
    public void ColumnsReferencedInWhereArePreserved()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q WHERE q.Col3 = 'test'
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col3 …) q WHERE q.Col3 = 'test'
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Where = queryRef[3] == SqlDml.Literal("test");

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void ColumnsReferencedInOrderByArePreserved()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q ORDER BY q.Col4
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col4 …) q ORDER BY q.Col4
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.OrderBy.Add(queryRef[4]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col4");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void ColumnsReferencedInGroupByArePreserved()
    {
      // Before: SELECT COUNT(*) FROM (SELECT …5 cols…) q GROUP BY q.Col1
      // After:  SELECT COUNT(*) FROM (SELECT t.Col1 …) q GROUP BY q.Col1
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(SqlDml.Count());
      outerSelect.GroupBy.Add(queryRef[1]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col1");
      AssertSelectColumnCount(innerSelect, 1);
    }

    [Test]
    public void ColumnsReferencedInHavingArePreserved()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q GROUP BY q.Col0 HAVING COUNT(q.Col2) > 1
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col2 …) q GROUP BY q.Col0 HAVING COUNT(q.Col2) > 1
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.GroupBy.Add(queryRef[0]);
      outerSelect.Having = SqlDml.Count(queryRef[2]) > SqlDml.Literal(1);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col2");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void ComplexWhereWithAndOrPreservesAllReferencedColumns()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q WHERE q.Col1 = 'a' AND q.Col3 IS NOT NULL
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col1, t.Col3 …) q WHERE …
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Where = queryRef[1] == SqlDml.Literal("a")
                           & queryRef[3] != SqlDml.Null;

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col3");
      AssertSelectColumnCount(innerSelect, 3);
    }

    #endregion

    #region Expressions

    [Test]
    public void CaseExpressionPreservesAllBranches()
    {
      // Before: SELECT CASE q.Col1 WHEN 'a' THEN q.Col2 ELSE q.Col3 END
      //         FROM (SELECT …5 cols…) q
      // After:  SELECT CASE q.Col1 WHEN 'a' THEN q.Col2 ELSE q.Col3 END
      //         FROM (SELECT t.Col1, t.Col2, t.Col3 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);

      var caseExpr = SqlDml.Case(queryRef[1]);
      caseExpr[SqlDml.Literal("a")] = queryRef[2];
      caseExpr.Else = queryRef[3];
      outerSelect.Columns.Add(caseExpr);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col1", "Col2", "Col3");
      AssertSelectColumnCount(innerSelect, 3);
    }

    [Test]
    public void CaseExpressionWithMultipleBranches()
    {
      // SELECT CASE WHEN q.Col0 > 0 THEN q.Col1
      //             WHEN q.Col0 < 0 THEN q.Col2
      //             ELSE q.Col3 END
      // FROM (SELECT …5 cols…) q
      // → inner pruned to Col0, Col1, Col2, Col3
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);

      var caseExpr = SqlDml.Case();
      caseExpr[queryRef[0] > SqlDml.Literal(0)] = queryRef[1];
      caseExpr[queryRef[0] < SqlDml.Literal(0)] = queryRef[2];
      caseExpr.Else = queryRef[3];
      outerSelect.Columns.Add(caseExpr);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2", "Col3");
      AssertSelectColumnCount(innerSelect, 4);
    }

    [Test]
    public void CastExpressionPreservesOperand()
    {
      // Before: SELECT CAST(q.Col2 AS INT) FROM (SELECT …5 cols…) q
      // After:  SELECT CAST(q.Col2 AS INT) FROM (SELECT t.Col2 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(SqlDml.Cast(queryRef[2], SqlType.Int32));

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col2");
      AssertSelectColumnCount(innerSelect, 1);
    }

    [Test]
    public void CoalescePreservesAllArguments()
    {
      // Before: SELECT COALESCE(q.Col1, q.Col4) FROM (SELECT …5 cols…) q
      // After:  SELECT COALESCE(q.Col1, q.Col4) FROM (SELECT t.Col1, t.Col4 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(SqlDml.Coalesce(queryRef[1], queryRef[4]));

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col1", "Col4");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void AggregatePreservesInnerExpression()
    {
      // Before: SELECT q.Col1, SUM(q.Col3) FROM (SELECT …5 cols…) q GROUP BY q.Col1
      // After:  SELECT q.Col1, SUM(q.Col3) FROM (SELECT t.Col1, t.Col3 FROM table1 t) q GROUP BY q.Col1
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[1]);
      outerSelect.Columns.Add(SqlDml.Sum(queryRef[3]));
      outerSelect.GroupBy.Add(queryRef[1]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col1", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void LikeExpressionPreservesAllParts()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q WHERE q.Col1 LIKE q.Col2
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col1, t.Col2 …) q WHERE q.Col1 LIKE q.Col2
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Where = SqlDml.Like(queryRef[1], queryRef[2]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2");
      AssertSelectColumnCount(innerSelect, 3);
    }

    [Test]
    public void BetweenExpressionPreservesAllParts()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q WHERE q.Col1 BETWEEN q.Col2 AND q.Col3
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3 …) q WHERE …
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Where = SqlDml.Between(queryRef[1], queryRef[2], queryRef[3]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2", "Col3");
      AssertSelectColumnCount(innerSelect, 4);
    }

    [Test]
    public void ArithmeticExpressionPreservesOperands()
    {
      // Before: SELECT q.Col0 + q.Col3 FROM (SELECT …5 cols…) q
      // After:  SELECT q.Col0 + q.Col3 FROM (SELECT t.Col0, t.Col3 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0] + queryRef[3]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void UnaryExpressionPreservesOperand()
    {
      // Before: SELECT -q.Col0 FROM (SELECT …5 cols…) q
      // After:  SELECT -q.Col0 FROM (SELECT t.Col0 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(-queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0");
      AssertSelectColumnCount(innerSelect, 1);
    }

    [Test]
    public void RowNumberInSelectPreservesOrderByColumns()
    {
      // Before: SELECT q.Col0, ROW_NUMBER() OVER(ORDER BY q.Col2)
      //         FROM (SELECT …5 cols…) q
      // After:  SELECT q.Col0, ROW_NUMBER() OVER(ORDER BY q.Col2)
      //         FROM (SELECT t.Col0, t.Col2 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      var rn = SqlDml.RowNumber();
      rn.OrderBy.Add(queryRef[2]);
      outerSelect.Columns.Add(rn);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col2");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void MixedSelectAndWhereExpressions()
    {
      // Before: SELECT COALESCE(q.Col1, q.Col2), CAST(q.Col0 AS VARCHAR)
      //         FROM (SELECT …5 cols…) q
      //         WHERE q.Col3 > 0
      //         ORDER BY q.Col4
      // After:  SELECT COALESCE(q.Col1, q.Col2), CAST(q.Col0 AS VARCHAR)
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      //         WHERE q.Col3 > 0
      //         ORDER BY q.Col4
      // All 5 columns referenced → no pruning
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(SqlDml.Coalesce(queryRef[1], queryRef[2]));
      outerSelect.Columns.Add(SqlDml.Cast(queryRef[0], SqlType.VarChar));
      outerSelect.Where = queryRef[3] > SqlDml.Literal(0);
      outerSelect.OrderBy.Add(queryRef[4]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2", "Col3", "Col4");
      AssertSelectColumnCount(innerSelect, 5);
    }

    [Test]
    public void MixedExpressionsWithPruning()
    {
      // Before: SELECT COALESCE(q.Col1, 'N/A'), q.Col0 * 2
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      //         WHERE q.Col3 IS NOT NULL
      // After:  SELECT COALESCE(q.Col1, 'N/A'), q.Col0 * 2
      //         FROM (SELECT t.Col0, t.Col1, t.Col3 FROM table1 t) q
      //         WHERE q.Col3 IS NOT NULL
      // Col2, Col4 pruned
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(SqlDml.Coalesce(queryRef[1], SqlDml.Literal("N/A")));
      outerSelect.Columns.Add(queryRef[0] * SqlDml.Literal(2));
      outerSelect.Where = queryRef[3] != SqlDml.Null;

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col3");
      AssertSelectColumnCount(innerSelect, 3);
    }

    [Test]
    public void MetadataWrappedColumnPreservesReference()
    {
      // SqlMetadata wraps an inner Expression (used by BooleanExpressionConverter).
      // The pruner must recurse into it to detect column references.
      // Before: SELECT q.Col0, METADATA(q.Col3 = 1, tag)
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col0, METADATA(q.Col3 = 1, tag)
      //         FROM (SELECT t.Col0, t.Col3 FROM table1 t) q
      // Col3 is only referenced inside the SqlMetadata wrapper — must not be pruned.
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var metadataExpr = SqlDml.Metadata(SqlDml.Equals(queryRef[3], SqlDml.Literal(1)), new object());

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(metadataExpr);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void NestedMetadataInsideCastPreservesReference()
    {
      // Mirrors the BooleanToInt pattern: METADATA(CAST(CASE WHEN q.Col2 THEN 1 ELSE 0 END AS bit), tag)
      // Before: SELECT q.Col0, METADATA(CAST(CASE WHEN q.Col2 ... END AS int), tag)
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col0, METADATA(CAST(CASE WHEN q.Col2 ... END AS int), tag)
      //         FROM (SELECT t.Col0, t.Col2 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var caseExpr = SqlDml.Case();
      caseExpr.Add(queryRef[2], SqlDml.Literal(1));
      caseExpr.Else = SqlDml.Literal(0);
      var castExpr = SqlDml.Cast(caseExpr, new SqlValueType(SqlType.Int32));
      var metadataExpr = SqlDml.Metadata(castExpr, new object());

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(metadataExpr);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col2");
      AssertSelectColumnCount(innerSelect, 2);
    }

    #endregion

    #region Subqueries

    [Test]
    public void CorrelatedSubqueryInSelectList()
    {
      // Before: SELECT q.Col0, (SELECT COUNT(*) FROM table2 t2 WHERE t2.Id = q.Col3)
      //         FROM (SELECT …5 cols…) q
      // After:  SELECT q.Col0, (SELECT COUNT(*) FROM table2 t2 WHERE t2.Id = q.Col3)
      //         FROM (SELECT t.Col0, t.Col3 FROM table1 t) q
      // Col3 referenced only in correlated subquery — must not be pruned
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var t2 = SqlDml.TableRef(table2, "t2");
      var subquerySelect = SqlDml.Select(t2);
      subquerySelect.Columns.Add(SqlDml.Count());
      subquerySelect.Where = t2["Id"] == queryRef[3];
      var subquery = SqlDml.SubQuery(subquerySelect);

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(subquery);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void ExistsSubqueryInWhere()
    {
      // Before: SELECT q.Col0 FROM (SELECT …5 cols…) q
      //         WHERE EXISTS (SELECT 1 FROM table2 t2 WHERE t2.Id = q.Col4)
      // After:  SELECT q.Col0 FROM (SELECT t.Col0, t.Col4 FROM table1 t) q
      //         WHERE EXISTS (…)
      // Col4 referenced in EXISTS — must not be pruned
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var t2 = SqlDml.TableRef(table2, "t2");
      var existsSelect = SqlDml.Select(t2);
      existsSelect.Columns.Add(SqlDml.Literal(1));
      existsSelect.Where = t2["Id"] == queryRef[4];

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Where = SqlDml.Exists(existsSelect);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col4");
      AssertSelectColumnCount(innerSelect, 2);
    }

    [Test]
    public void CorrelatedSubqueryAlsoPrunedInternally()
    {
      // The correlated subquery itself may have a SqlQueryRef FROM that can be pruned.
      // Before: SELECT q.Col0,
      //           (SELECT s.Name FROM (SELECT t2.Id, t2.Name, t2.Value FROM table2 t2) s
      //            WHERE s.Id = q.Col3)
      //         FROM (SELECT …5 cols…) q
      // After:  SELECT q.Col0,
      //           (SELECT s.Name FROM (SELECT t2.Id, t2.Name FROM table2 t2) s
      //            WHERE s.Id = q.Col3)
      //         FROM (SELECT t.Col0, t.Col3 FROM table1 t) q
      // Both outer and inner subquery pruned
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var t2 = SqlDml.TableRef(table2, "t2");
      var subInner = SqlDml.Select(t2);
      subInner.Columns.Add(t2["Id"]);
      subInner.Columns.Add(t2["Name"]);
      subInner.Columns.Add(t2["Value"]);

      var subRef = SqlDml.QueryRef(subInner, "s");
      var subOuter = SqlDml.Select(subRef);
      subOuter.Columns.Add(subRef["Name"]);
      subOuter.Where = subRef["Id"] == queryRef[3];

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(SqlDml.SubQuery(subOuter));

      SqlColumnPruner.Process(outerSelect);

      // Outer query pruned to Col0, Col3
      AssertColumnNames(queryRef, "Col0", "Col3");
      AssertSelectColumnCount(innerSelect, 2);
      // Subquery inner pruned to Id, Name (Value removed)
      AssertColumnNames(subRef, "Id", "Name");
      AssertSelectColumnCount(subInner, 2);
    }

    #endregion

    #region Nesting

    [Test]
    public void NestedSubqueriesCascadePruning()
    {
      // Before: SELECT o.Col0, o.Col4
      //         FROM (SELECT m.Col0, m.Col2, m.Col4
      //               FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) m
      //              ) o
      // After:  SELECT o.Col0, o.Col4
      //         FROM (SELECT m.Col0, m.Col4
      //               FROM (SELECT t.Col0, t.Col4 FROM table1 t) m
      //              ) o
      // Cascading: outer prunes middle to 2 cols, middle prunes inner to 2 cols
      var innermostSelect = CreateInnerSelect();
      var innerRef = SqlDml.QueryRef(innermostSelect, "m");

      var middleSelect = SqlDml.Select(innerRef);
      middleSelect.Columns.Add(innerRef[0]);
      middleSelect.Columns.Add(innerRef[2]);
      middleSelect.Columns.Add(innerRef[4]);

      var outerRef = SqlDml.QueryRef(middleSelect, "o");
      var outerSelect = SqlDml.Select(outerRef);
      outerSelect.Columns.Add(outerRef[0]);
      outerSelect.Columns.Add(outerRef[2]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(outerRef, "Col0", "Col4");
      AssertSelectColumnCount(middleSelect, 2);

      AssertColumnNames(innerRef, "Col0", "Col4");
      AssertSelectColumnCount(innermostSelect, 2);
    }

    [Test]
    public void ThreeLevelNestingWithFilterAtMiddle()
    {
      // Before: SELECT o.Col0
      //         FROM (SELECT m.Col0, m.Col1, m.Col2, m.Col3, m.Col4
      //               FROM (SELECT …5 cols…) m
      //               WHERE m.Col2 = 'x'
      //              ) o
      // After:  SELECT o.Col0
      //         FROM (SELECT m.Col0
      //               FROM (SELECT t.Col0, t.Col2 FROM table1 t) m
      //               WHERE m.Col2 = 'x'
      //              ) o
      // Outer prunes middle to Col0; middle still references Col2 in WHERE → inner keeps Col0, Col2
      var innermostSelect = CreateInnerSelect();
      var innerRef = SqlDml.QueryRef(innermostSelect, "m");

      var middleSelect = SqlDml.Select(innerRef);
      for (int i = 0; i < innerRef.Columns.Count; i++) {
        middleSelect.Columns.Add(innerRef[i]);
      }
      middleSelect.Where = innerRef["Col2"] == SqlDml.Literal("x");

      var outerRef = SqlDml.QueryRef(middleSelect, "o");
      var outerSelect = SqlDml.Select(outerRef);
      outerSelect.Columns.Add(outerRef[0]);

      SqlColumnPruner.Process(outerSelect);

      // Outer prunes middle to just Col0
      AssertColumnNames(outerRef, "Col0");
      AssertSelectColumnCount(middleSelect, 1);

      // But middle still references Col2 in its WHERE, so inner keeps Col0 + Col2
      AssertColumnNames(innerRef, "Col0", "Col2");
      AssertSelectColumnCount(innermostSelect, 2);
    }

    #endregion

    #region Set operations and edge cases

    [Test]
    public void PrunesUnionWrappedInQueryRef()
    {
      // Before: SELECT u.Col0
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) u
      // After:  SELECT u.Col0
      //         FROM (SELECT t.Col0 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0 FROM table1 t) u
      // Both sides of the UNION are pruned to the same column indices.
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.AddRange(t1.Columns.ToArray<SqlColumn>());

      var t2 = SqlDml.TableRef(table1, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.AddRange(t2.Columns.ToArray<SqlColumn>());

      var union = selectA.UnionAll(selectB);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(1));
      Assert.That(selectA.Columns.Count, Is.EqualTo(1));
      Assert.That(selectB.Columns.Count, Is.EqualTo(1));
    }

    [Test]
    public void DoesNotPruneSelectDistinct()
    {
      // SELECT DISTINCT uses all projected columns for deduplication.
      // Removing columns can change the result set — no pruning allowed.
      // SELECT q.Col0 FROM (SELECT DISTINCT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // The inner SELECT DISTINCT must keep all 5 columns.
      var innerSelect = CreateInnerSelect();
      innerSelect.Distinct = true;
      var queryRef = SqlDml.QueryRef(innerSelect, "q");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(5));
      AssertSelectColumnCount(innerSelect, 5);
    }

    [Test]
    public void SelectDistinctInnerSubqueriesStillPruned()
    {
      // The DISTINCT select itself is not pruned, but its inner subqueries are.
      // Before: SELECT q.Col0
      //         FROM (SELECT DISTINCT r.Col0, r.Col1, r.Col2
      //               FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) r
      //              ) q
      // After:  SELECT q.Col0
      //         FROM (SELECT DISTINCT r.Col0, r.Col1, r.Col2
      //               FROM (SELECT t.Col0, t.Col1, t.Col2 FROM table1 t) r
      //              ) q
      // The DISTINCT keeps 3 columns, but its inner subquery is pruned from 5 to 3.
      var baseSelect = CreateInnerSelect();
      var baseRef = SqlDml.QueryRef(baseSelect, "r");

      var distinctSelect = SqlDml.Select(baseRef);
      distinctSelect.Columns.Add(baseRef["Col0"]);
      distinctSelect.Columns.Add(baseRef["Col1"]);
      distinctSelect.Columns.Add(baseRef["Col2"]);
      distinctSelect.Distinct = true;

      var queryRef = SqlDml.QueryRef(distinctSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      // DISTINCT select is NOT pruned (still 3 columns)
      Assert.That(queryRef.Columns.Count, Is.EqualTo(3));
      AssertSelectColumnCount(distinctSelect, 3);

      // But the inner subquery IS pruned (from 5 to 3 — only columns used by DISTINCT)
      AssertColumnNames(baseRef, "Col0", "Col1", "Col2");
      AssertSelectColumnCount(baseSelect, 3);
    }

    [Test]
    public void DoesNotPruneUnionDistinct()
    {
      // UNION (without ALL) deduplicates rows using all projected columns,
      // so removing columns can change the result set. No pruning allowed.
      // SELECT u.Col0
      // FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t
      //       UNION
      //       SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) u
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.AddRange(t1.Columns.ToArray<SqlColumn>());

      var t2 = SqlDml.TableRef(table1, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.AddRange(t2.Columns.ToArray<SqlColumn>());

      var union = selectA.Union(selectB);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      var originalColumnCount = queryRef.Columns.Count;
      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(originalColumnCount));
      Assert.That(selectA.Columns.Count, Is.EqualTo(5));
      Assert.That(selectB.Columns.Count, Is.EqualTo(5));
    }

    [Test]
    public void HandlesNoFromSource()
    {
      // SELECT 1  — no FROM clause, nothing to prune, should not throw
      var select = SqlDml.Select();
      select.Columns.Add(SqlDml.Literal(1));

      Assert.DoesNotThrow(() => SqlColumnPruner.Process(select));
    }

    [Test]
    public void HandlesDirectTableRef()
    {
      // SELECT t.Col0, t.Col2 FROM table1 t
      // FROM is a physical table, not a subquery — nothing to prune
      var t = SqlDml.TableRef(table1);
      var select = SqlDml.Select(t);
      select.Columns.Add(t[0]);
      select.Columns.Add(t[2]);

      Assert.DoesNotThrow(() => SqlColumnPruner.Process(select));
      Assert.That(select.Columns.Count, Is.EqualTo(2));
    }

    #endregion

    #region ORM-like patterns (typical compiler output)

    [Test]
    public void PagingPatternWithRowNumber()
    {
      // Simulates the typical paging wrapper produced by the ORM compiler:
      // Before: SELECT p.Col0, p.Col1 FROM (
      //           SELECT q.Col0, q.Col1, q.Col2, q.Col3, q.Col4,
      //                  ROW_NUMBER() OVER(ORDER BY q.Col0) AS rn
      //           FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      //         ) p WHERE p.rn > 10 AND p.rn <= 20
      // After:  SELECT p.Col0, p.Col1 FROM (
      //           SELECT q.Col0, q.Col1, ROW_NUMBER() OVER(ORDER BY q.Col0) AS rn
      //           FROM (SELECT t.Col0, t.Col1 FROM table1 t) q
      //         ) p WHERE p.rn > 10 AND p.rn <= 20
      var baseSelect = CreateInnerSelect();
      var baseRef = SqlDml.QueryRef(baseSelect, "q");

      // Middle: wraps in subquery, adds all columns + ROW_NUMBER
      var middleSelect = SqlDml.Select(baseRef);
      for (int i = 0; i < baseRef.Columns.Count; i++) {
        middleSelect.Columns.Add(baseRef[i]);
      }
      var rn = SqlDml.RowNumber();
      rn.OrderBy.Add(baseRef[0]);
      middleSelect.Columns.Add(rn, "rn");

      // Outer: selects only Col0, Col1 and filters on rn
      var middleRef = SqlDml.QueryRef(middleSelect, "p");
      var outerSelect = SqlDml.Select(middleRef);
      outerSelect.Columns.Add(middleRef["Col0"]);
      outerSelect.Columns.Add(middleRef["Col1"]);
      outerSelect.Where = middleRef["rn"] > SqlDml.Literal(10)
                           & middleRef["rn"] <= SqlDml.Literal(20);

      SqlColumnPruner.Process(outerSelect);

      // Middle should have Col0, Col1, rn (3 cols)
      AssertColumnNames(middleRef, "Col0", "Col1", "rn");
      AssertSelectColumnCount(middleSelect, 3);

      // Base should have Col0, Col1 (2 cols) — Col2-Col4 pruned
      AssertColumnNames(baseRef, "Col0", "Col1");
      AssertSelectColumnCount(baseSelect, 2);
    }

    [Test]
    public void SelectWithComputedColumnsAndAlias()
    {
      // Before: SELECT q.Col0, q.Col1 || ' ' || q.Col2 AS FullName
      //         FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1 t) q
      // After:  SELECT q.Col0, q.Col1 || ' ' || q.Col2 AS FullName
      //         FROM (SELECT t.Col0, t.Col1, t.Col2 FROM table1 t) q
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(
        SqlDml.Concat(queryRef[1], SqlDml.Literal(" "), queryRef[2]),
        "FullName");

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col2");
      AssertSelectColumnCount(innerSelect, 3);
    }

    [Test]
    public void GroupByAggregateMultipleColumns()
    {
      // Before: SELECT q.Col1, COUNT(q.Col0), MAX(q.Col3)
      //         FROM (SELECT …5 cols…) q GROUP BY q.Col1
      // After:  SELECT q.Col1, COUNT(q.Col0), MAX(q.Col3)
      //         FROM (SELECT t.Col0, t.Col1, t.Col3 FROM table1 t) q GROUP BY q.Col1
      var innerSelect = CreateInnerSelect();
      var queryRef = SqlDml.QueryRef(innerSelect, "q");
      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[1]);
      outerSelect.Columns.Add(SqlDml.Count(queryRef[0]));
      outerSelect.Columns.Add(SqlDml.Max(queryRef[3]));
      outerSelect.GroupBy.Add(queryRef[1]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(queryRef, "Col0", "Col1", "Col3");
      AssertSelectColumnCount(innerSelect, 3);
    }

    [Test]
    public void JoinFromSidesRecurseIndependently()
    {
      // When FROM is a join with SqlQueryRef sides, the pruner prunes each side independently.
      // Before: SELECT l.Col0, r.Name
      //         FROM (SELECT t1.Col0, t1.Col1, t1.Col2 FROM table1 t1) l
      //         JOIN (SELECT t2.Id, t2.Name, t2.Value FROM table2 t2) r ON l.Col0 = r.Id
      // After:  SELECT l.Col0, r.Name
      //         FROM (SELECT t1.Col0 FROM table1 t1) l
      //         JOIN (SELECT t2.Id, t2.Name FROM table2 t2) r ON l.Col0 = r.Id
      // Left side pruned from 3 to 1 (only Col0 used in select + join condition).
      // Right side pruned from 3 to 2 (Id used in join condition, Name in select).
      var t1 = SqlDml.TableRef(table1, "t1");
      var leftInner = SqlDml.Select(t1);
      leftInner.Columns.Add(t1["Col0"]);
      leftInner.Columns.Add(t1["Col1"]);
      leftInner.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table2, "t2");
      var rightInner = SqlDml.Select(t2);
      rightInner.Columns.Add(t2["Id"]);
      rightInner.Columns.Add(t2["Name"]);
      rightInner.Columns.Add(t2["Value"]);

      var leftRef = SqlDml.QueryRef(leftInner, "l");
      var rightRef = SqlDml.QueryRef(rightInner, "r");

      var joined = leftRef.InnerJoin(rightRef, leftRef["Col0"] == rightRef["Id"]);
      var outerSelect = SqlDml.Select(joined);
      outerSelect.Columns.Add(leftRef["Col0"]);
      outerSelect.Columns.Add(rightRef["Name"]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(leftRef, "Col0");
      AssertSelectColumnCount(leftInner, 1);
      AssertColumnNames(rightRef, "Id", "Name");
      AssertSelectColumnCount(rightInner, 2);
    }

    [Test]
    public void WrappedJoinResultPrunedThroughQueryRef()
    {
      // When the join result is wrapped in a SqlQueryRef (common ORM pattern):
      // Before: SELECT w.Col0, w.Name
      //         FROM (SELECT l.Col0, l.Col1, l.Col2, r.Id, r.Name, r.Value
      //               FROM l JOIN r ON l.Col0 = r.Id) w
      // After:  SELECT w.Col0, w.Name
      //         FROM (SELECT l.Col0, r.Name
      //               FROM l JOIN r ON l.Col0 = r.Id) w
      // The wrapping subquery is pruned from 6 to 2 columns.
      var t1 = SqlDml.TableRef(table1, "t1");
      var t2 = SqlDml.TableRef(table2, "t2");

      var joined = t1.InnerJoin(t2, t1["Col0"] == t2["Id"]);
      var joinSelect = SqlDml.Select(joined);
      joinSelect.Columns.Add(t1["Col0"]);
      joinSelect.Columns.Add(t1["Col1"]);
      joinSelect.Columns.Add(t1["Col2"]);
      joinSelect.Columns.Add(t2["Id"]);
      joinSelect.Columns.Add(t2["Name"]);
      joinSelect.Columns.Add(t2["Value"]);

      var wrapRef = SqlDml.QueryRef(joinSelect, "w");
      var outerSelect = SqlDml.Select(wrapRef);
      outerSelect.Columns.Add(wrapRef["Col0"]);
      outerSelect.Columns.Add(wrapRef["Name"]);

      SqlColumnPruner.Process(outerSelect);

      AssertColumnNames(wrapRef, "Col0", "Name");
      AssertSelectColumnCount(joinSelect, 2);
    }

    [Test]
    public void JoinSidesWithQueryRefsPrunedThroughWrapper()
    {
      // Pruning cascades through a wrapping SqlQueryRef into inner join sides.
      // Before: SELECT x.Col0, x.Name
      //         FROM (SELECT l.Col0, l.Col1, l.Col2, r.Id, r.Name, r.Value
      //               FROM (SELECT t1.Col0, t1.Col1, t1.Col2 FROM table1 t1) l
      //               JOIN (SELECT t2.Id, t2.Name, t2.Value FROM table2 t2) r ON l.Col0 = r.Id) x
      // After:  SELECT x.Col0, x.Name
      //         FROM (SELECT l.Col0, r.Name
      //               FROM (SELECT t1.Col0 FROM table1 t1) l
      //               JOIN (SELECT t2.Id, t2.Name FROM table2 t2) r ON l.Col0 = r.Id) x
      // Three-level pruning: outer wrapper → join wrapper → join sides.
      var t1 = SqlDml.TableRef(table1, "t1");
      var leftInner = SqlDml.Select(t1);
      leftInner.Columns.Add(t1["Col0"]);
      leftInner.Columns.Add(t1["Col1"]);
      leftInner.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table2, "t2");
      var rightInner = SqlDml.Select(t2);
      rightInner.Columns.Add(t2["Id"]);
      rightInner.Columns.Add(t2["Name"]);
      rightInner.Columns.Add(t2["Value"]);

      var leftRef = SqlDml.QueryRef(leftInner, "l");
      var rightRef = SqlDml.QueryRef(rightInner, "r");

      var joined = leftRef.InnerJoin(rightRef, leftRef["Col0"] == rightRef["Id"]);
      var joinSelect = SqlDml.Select(joined);
      joinSelect.Columns.Add(leftRef["Col0"]);
      joinSelect.Columns.Add(leftRef["Col1"]);
      joinSelect.Columns.Add(leftRef["Col2"]);
      joinSelect.Columns.Add(rightRef["Id"]);
      joinSelect.Columns.Add(rightRef["Name"]);
      joinSelect.Columns.Add(rightRef["Value"]);

      var wrapRef = SqlDml.QueryRef(joinSelect, "x");
      var outerSelect = SqlDml.Select(wrapRef);
      outerSelect.Columns.Add(wrapRef["Col0"]);
      outerSelect.Columns.Add(wrapRef["Name"]);

      SqlColumnPruner.Process(outerSelect);

      // Outer wrapper pruned from 6 to 2 columns
      AssertColumnNames(wrapRef, "Col0", "Name");
      AssertSelectColumnCount(joinSelect, 2);

      // Left join side pruned from 3 to 1 (only Col0 used in select + join condition)
      AssertColumnNames(leftRef, "Col0");
      AssertSelectColumnCount(leftInner, 1);

      // Right join side pruned from 3 to 2 (Id for join condition, Name for select)
      AssertColumnNames(rightRef, "Id", "Name");
      AssertSelectColumnCount(rightInner, 2);
    }

    [Test]
    public void OuterApplyCorrelatedReferencePreservesColumns()
    {
      // OUTER APPLY subqueries reference columns from a sibling join side.
      // The pruner must not remove columns referenced by correlated siblings.
      // Before: SELECT a.Col0, a.Col1, b.Name
      //         FROM (SELECT t1.Col0, t1.Col1, t1.Col2, t1.Col3, t1.Col4 FROM table1 t1) a
      //         OUTER APPLY (SELECT t2.Name FROM table2 t2 WHERE t2.Id = a.Col3) b
      // After:  SELECT a.Col0, a.Col1, b.Name
      //         FROM (SELECT t1.Col0, t1.Col1, t1.Col3 FROM table1 t1) a
      //         OUTER APPLY (SELECT t2.Name FROM table2 t2 WHERE t2.Id = a.Col3) b
      // Col3 is not in the outer SELECT but IS referenced by the APPLY's WHERE → must be kept.
      var t1 = SqlDml.TableRef(table1, "t1");
      var innerA = SqlDml.Select(t1);
      for (int i = 0; i < t1.Columns.Count; i++) {
        innerA.Columns.Add(t1[i]);
      }
      var aRef = SqlDml.QueryRef(innerA, "a");

      var t2 = SqlDml.TableRef(table2, "t2");
      var innerB = SqlDml.Select(t2);
      innerB.Columns.Add(t2["Name"]);
      innerB.Where = t2["Id"] == aRef["Col3"];
      var bRef = SqlDml.QueryRef(innerB, "b");

      var applied = aRef.LeftOuterApply(bRef);
      var outerSelect = SqlDml.Select(applied);
      outerSelect.Columns.Add(aRef["Col0"]);
      outerSelect.Columns.Add(aRef["Col1"]);
      outerSelect.Columns.Add(bRef["Name"]);

      SqlColumnPruner.Process(outerSelect);

      // Col3 preserved because the APPLY subquery references it
      AssertColumnNames(aRef, "Col0", "Col1", "Col3");
      AssertSelectColumnCount(innerA, 3);
    }

    [Test]
    public void MultipleOuterApplyCorrelatedReferences()
    {
      // Multiple OUTER APPLY subqueries each reference different columns from [a].
      // Before: SELECT a.Col0, b.Name, c.Value
      //         FROM (SELECT t1.Col0, t1.Col1, t1.Col2, t1.Col3, t1.Col4 FROM table1 t1) a
      //         OUTER APPLY (SELECT t2.Name FROM table2 t2 WHERE t2.Id = a.Col2) b
      //         OUTER APPLY (SELECT t2.Value FROM table2 t2 WHERE t2.Id = a.Col4) c
      // After:  SELECT a.Col0, b.Name, c.Value
      //         FROM (SELECT t1.Col0, t1.Col2, t1.Col4 FROM table1 t1) a
      //         OUTER APPLY (SELECT t2.Name FROM table2 t2 WHERE t2.Id = a.Col2) b
      //         OUTER APPLY (SELECT t2.Value FROM table2 t2 WHERE t2.Id = a.Col4) c
      // Col2 and Col4 are each referenced by one APPLY sibling — both must be kept.
      var t1 = SqlDml.TableRef(table1, "t1");
      var innerA = SqlDml.Select(t1);
      for (int i = 0; i < t1.Columns.Count; i++) {
        innerA.Columns.Add(t1[i]);
      }
      var aRef = SqlDml.QueryRef(innerA, "a");

      var t2b = SqlDml.TableRef(table2, "t2b");
      var innerB = SqlDml.Select(t2b);
      innerB.Columns.Add(t2b["Name"]);
      innerB.Where = t2b["Id"] == aRef["Col2"];
      var bRef = SqlDml.QueryRef(innerB, "b");

      var t2c = SqlDml.TableRef(table2, "t2c");
      var innerC = SqlDml.Select(t2c);
      innerC.Columns.Add(t2c["Value"]);
      innerC.Where = t2c["Id"] == aRef["Col4"];
      var cRef = SqlDml.QueryRef(innerC, "c");

      var applied = aRef.LeftOuterApply(bRef).LeftOuterApply(cRef);
      var outerSelect = SqlDml.Select(applied);
      outerSelect.Columns.Add(aRef["Col0"]);
      outerSelect.Columns.Add(bRef["Name"]);
      outerSelect.Columns.Add(cRef["Value"]);

      SqlColumnPruner.Process(outerSelect);

      // Col0 from outer SELECT, Col2 from [b]'s WHERE, Col4 from [c]'s WHERE
      AssertColumnNames(aRef, "Col0", "Col2", "Col4");
      AssertSelectColumnCount(innerA, 3);
    }

    [Test]
    public void UnionFromDifferentTablesPrunedThroughQueryRef()
    {
      // UNION sides with different source tables are pruned by index.
      // Before: SELECT u.Col0, u.Col2
      //         FROM (SELECT t1.Col0, t1.Col1, t1.Col2 FROM table1 t1
      //               UNION ALL
      //               SELECT t2.Id, t2.Name, t2.Value FROM table2 t2) u
      // After:  SELECT u.Col0, u.Col2
      //         FROM (SELECT t1.Col0, t1.Col2 FROM table1 t1
      //               UNION ALL
      //               SELECT t2.Id, t2.Value FROM table2 t2) u
      // Column index 1 removed from both sides.
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.Add(t1["Col0"]);
      selectA.Columns.Add(t1["Col1"]);
      selectA.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table2, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.Add(t2["Id"]);
      selectB.Columns.Add(t2["Name"]);
      selectB.Columns.Add(t2["Value"]);

      var union = selectA.UnionAll(selectB);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);
      outerSelect.Columns.Add(queryRef[2]);

      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(2));
      Assert.That(selectA.Columns.Count, Is.EqualTo(2));
      Assert.That(selectB.Columns.Count, Is.EqualTo(2));
    }

    [Test]
    public void DoesNotPruneExceptOrIntersect()
    {
      // EXCEPT and INTERSECT use all projected columns for comparison,
      // so removing columns can change the result set. No pruning allowed.
      // SELECT u.Col0 FROM (SELECT ... EXCEPT SELECT ...) u
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.Add(t1["Col0"]);
      selectA.Columns.Add(t1["Col1"]);
      selectA.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table2, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.Add(t2["Id"]);
      selectB.Columns.Add(t2["Name"]);
      selectB.Columns.Add(t2["Value"]);

      var except = selectA.Except(selectB);
      var queryRef = SqlDml.QueryRef(except, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      var originalColumnCount = queryRef.Columns.Count;
      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(originalColumnCount));
      Assert.That(selectA.Columns.Count, Is.EqualTo(3));
      Assert.That(selectB.Columns.Count, Is.EqualTo(3));
    }

    [Test]
    public void UnionDistinctSidesStillPrunedInternally()
    {
      // The UNION itself is not pruned (column list stays intact),
      // but each side's inner subqueries ARE pruned independently.
      // Before: SELECT u.Col0 FROM (
      //           SELECT t.Col0, t.Col1 FROM (SELECT t.Col0, t.Col1, t.Col2, t.Col3, t.Col4 FROM table1) t
      //           UNION
      //           SELECT t2.Id, t2.Name FROM (SELECT t2.Id, t2.Name, t2.Value FROM table2) t2) u
      // After:  SELECT u.Col0 FROM (
      //           SELECT t.Col0, t.Col1 FROM (SELECT t.Col0, t.Col1 FROM table1) t
      //           UNION
      //           SELECT t2.Id, t2.Name FROM (SELECT t2.Id, t2.Name FROM table2) t2) u
      // The UNION keeps 2 columns per side, but inner subqueries are pruned.
      var t1 = SqlDml.TableRef(table1, "t1");
      var innerA = SqlDml.Select(t1);
      for (int i = 0; i < t1.Columns.Count; i++) {
        innerA.Columns.Add(t1[i]);
      }
      var aRef = SqlDml.QueryRef(innerA, "t");
      var selectA = SqlDml.Select(aRef);
      selectA.Columns.Add(aRef["Col0"]);
      selectA.Columns.Add(aRef["Col1"]);

      var t2 = SqlDml.TableRef(table2, "t2");
      var innerB = SqlDml.Select(t2);
      innerB.Columns.Add(t2["Id"]);
      innerB.Columns.Add(t2["Name"]);
      innerB.Columns.Add(t2["Value"]);
      var bRef = SqlDml.QueryRef(innerB, "t2");
      var selectB = SqlDml.Select(bRef);
      selectB.Columns.Add(bRef["Id"]);
      selectB.Columns.Add(bRef["Name"]);

      var union = selectA.Union(selectB);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      // UNION column list is NOT pruned (still 2 per side)
      Assert.That(queryRef.Columns.Count, Is.EqualTo(2));
      Assert.That(selectA.Columns.Count, Is.EqualTo(2));
      Assert.That(selectB.Columns.Count, Is.EqualTo(2));

      // But inner subqueries ARE pruned: table1 side from 5 to 2, table2 side from 3 to 2
      AssertColumnNames(aRef, "Col0", "Col1");
      AssertSelectColumnCount(innerA, 2);
      AssertColumnNames(bRef, "Id", "Name");
      AssertSelectColumnCount(innerB, 2);
    }

    [Test]
    public void ChainedUnionAllIsPruned()
    {
      // A UNION ALL B UNION ALL C — the entire tree is UNION ALL, so pruning is allowed.
      // Before: SELECT u.Col0
      //         FROM (SELECT t.Col0, t.Col1, t.Col2 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0, t.Col1, t.Col2 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0, t.Col1, t.Col2 FROM table1 t) u
      // After:  SELECT u.Col0
      //         FROM (SELECT t.Col0 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0 FROM table1 t
      //               UNION ALL
      //               SELECT t.Col0 FROM table1 t) u
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.Add(t1["Col0"]);
      selectA.Columns.Add(t1["Col1"]);
      selectA.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table1, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.Add(t2["Col0"]);
      selectB.Columns.Add(t2["Col1"]);
      selectB.Columns.Add(t2["Col2"]);

      var t3 = SqlDml.TableRef(table1, "t3");
      var selectC = SqlDml.Select(t3);
      selectC.Columns.Add(t3["Col0"]);
      selectC.Columns.Add(t3["Col1"]);
      selectC.Columns.Add(t3["Col2"]);

      var union = selectA.UnionAll(selectB).UnionAll(selectC);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(1));
      Assert.That(selectA.Columns.Count, Is.EqualTo(1));
      Assert.That(selectB.Columns.Count, Is.EqualTo(1));
      Assert.That(selectC.Columns.Count, Is.EqualTo(1));
    }

    [Test]
    public void DoesNotPruneUnionAllWithNestedUnionDistinctOnLeft()
    {
      // (A UNION B) UNION ALL C — left subtree is UNION (not ALL), so no pruning.
      // SELECT u.Col0
      // FROM ((SELECT ... UNION SELECT ...) UNION ALL SELECT ...) u
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.Add(t1["Col0"]);
      selectA.Columns.Add(t1["Col1"]);
      selectA.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table1, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.Add(t2["Col0"]);
      selectB.Columns.Add(t2["Col1"]);
      selectB.Columns.Add(t2["Col2"]);

      var t3 = SqlDml.TableRef(table1, "t3");
      var selectC = SqlDml.Select(t3);
      selectC.Columns.Add(t3["Col0"]);
      selectC.Columns.Add(t3["Col1"]);
      selectC.Columns.Add(t3["Col2"]);

      var union = selectA.Union(selectB).UnionAll(selectC);
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      var originalColumnCount = queryRef.Columns.Count;
      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(originalColumnCount));
      Assert.That(selectA.Columns.Count, Is.EqualTo(3));
      Assert.That(selectB.Columns.Count, Is.EqualTo(3));
      Assert.That(selectC.Columns.Count, Is.EqualTo(3));
    }

    [Test]
    public void DoesNotPruneUnionAllWithNestedExceptOnRight()
    {
      // A UNION ALL (B EXCEPT C) — right subtree is EXCEPT, so no pruning.
      // SELECT u.Col0
      // FROM (SELECT ... UNION ALL (SELECT ... EXCEPT SELECT ...)) u
      var t1 = SqlDml.TableRef(table1, "t1");
      var selectA = SqlDml.Select(t1);
      selectA.Columns.Add(t1["Col0"]);
      selectA.Columns.Add(t1["Col1"]);
      selectA.Columns.Add(t1["Col2"]);

      var t2 = SqlDml.TableRef(table1, "t2");
      var selectB = SqlDml.Select(t2);
      selectB.Columns.Add(t2["Col0"]);
      selectB.Columns.Add(t2["Col1"]);
      selectB.Columns.Add(t2["Col2"]);

      var t3 = SqlDml.TableRef(table1, "t3");
      var selectC = SqlDml.Select(t3);
      selectC.Columns.Add(t3["Col0"]);
      selectC.Columns.Add(t3["Col1"]);
      selectC.Columns.Add(t3["Col2"]);

      var union = selectA.UnionAll(selectB.Except(selectC));
      var queryRef = SqlDml.QueryRef(union, "u");

      var outerSelect = SqlDml.Select(queryRef);
      outerSelect.Columns.Add(queryRef[0]);

      var originalColumnCount = queryRef.Columns.Count;
      SqlColumnPruner.Process(outerSelect);

      Assert.That(queryRef.Columns.Count, Is.EqualTo(originalColumnCount));
      Assert.That(selectA.Columns.Count, Is.EqualTo(3));
      Assert.That(selectB.Columns.Count, Is.EqualTo(3));
      Assert.That(selectC.Columns.Count, Is.EqualTo(3));
    }

    #endregion

    #region Helpers

    private SqlSelect CreateInnerSelect()
    {
      var t = SqlDml.TableRef(table1);
      var select = SqlDml.Select(t);
      for (int i = 0; i < t.Columns.Count; i++) {
        select.Columns.Add(t[i]);
      }
      return select;
    }

    private static void AssertColumnNames(SqlQueryRef queryRef, params string[] expectedNames)
    {
      var actualNames = Enumerable.Range(0, queryRef.Columns.Count)
        .Select(i => queryRef.Columns[i].Name)
        .ToArray();
      Assert.That(actualNames, Is.EqualTo(expectedNames),
        $"QueryRef columns: expected [{string.Join(", ", expectedNames)}] " +
        $"but was [{string.Join(", ", actualNames)}]");
    }

    private static void AssertSelectColumnCount(SqlSelect select, int expectedCount)
    {
      Assert.That(select.Columns.Count, Is.EqualTo(expectedCount),
        $"Inner SELECT column count: expected {expectedCount} but was {select.Columns.Count}");
    }

    #endregion
  }
}
