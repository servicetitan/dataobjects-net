// Copyright (C) 2008-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using Xtensive.Sql;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Ddl;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;
using Index = Xtensive.Sql.Model.Index;
using System.Linq;
using Xtensive.Core;
using System.Collections.Generic;

namespace Xtensive.Orm.Tests.Sql.SqlServer
{
  [Explicit]
  public class MSSQLTestsInMemory : MSSQLTests
  {
    protected override bool InMemory => true;

#if DEBUG
    protected override bool PerformanceCheck => false;
#else
    protected override bool PerformanceCheck => true;
#endif
  }


  [TestFixture, Explicit]
  public class MSSQLTests : AdventureWorks
  {
    private SqlDriver sqlDriver;
    private SqlConnection sqlConnection;
    private DbCommand dbCommand;
    private DbCommand sqlCommand;

    protected virtual bool PerformanceCheck => false;

    protected virtual int RunsPerGroup => 50;

    [OneTimeSetUp]
    public override void SetUp()
    {
      base.SetUp();
      sqlDriver = TestSqlDriver.Create(Url);
      sqlConnection = sqlDriver.CreateConnection();

      dbCommand = sqlConnection.CreateCommand();
      sqlCommand = sqlConnection.CreateCommand();
      try {
        sqlConnection.Open();
      }
      catch (SystemException e) {
        Console.WriteLine(e);
      }

      if (InMemory) {
        return;
      }

      var stopWatch = new Stopwatch();
      stopWatch.Start();
      try {
        sqlConnection.BeginTransaction();
        Catalog = sqlDriver.ExtractCatalog(sqlConnection);
        sqlConnection.Commit();
      }
      catch {
        sqlConnection.Rollback();
        throw;
      }
      stopWatch.Stop();
      Console.WriteLine(stopWatch.Elapsed);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
      try {
        if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed) {
          sqlConnection.Close();
        }
      }
      catch (Exception ex) {
        Console.WriteLine(ex.Message);
      }
    }

    [Test]
    public void TestExtractCatalog()
    {
      Assert.GreaterOrEqual(Catalog.Schemas.Count, 1);
    }

    #region Internals

    private bool CompareExecuteDataReader(string commandText, ISqlCompileUnit statement)
    {
      var compiledCommandText = PerformanceCheck
        ? CompileWithPerformanceCheck(statement)
        : CompileRegular(statement);

      Console.WriteLine(compiledCommandText);
      if (InMemory) {
        return true;
      }

      sqlCommand.CommandText = compiledCommandText;
      sqlCommand.Prepare();

      Console.WriteLine(commandText);
      dbCommand.CommandText = commandText;

      var r1 = GetExecuteDataReaderResult(dbCommand);
      var r2 = GetExecuteDataReaderResult(sqlCommand);

      Console.WriteLine();
      Console.WriteLine();
      Console.WriteLine(r1);
      Console.WriteLine(r2);

      if (r1.RowCount != r2.RowCount) {
        return false;
      }
      if (r1.FieldCount != r2.FieldCount) {
        return false;
      }
      for (var i = 0; i < r1.FieldCount; i++) {
        if (r1.FieldNames[i] != r2.FieldNames[i]) {
          return false;
        }
      }
      return true;
    }

    private bool CompareExecuteNonQuery(string commandText, ISqlCompileUnit statement)
    {
      var compiledCommandText = PerformanceCheck
        ? CompileWithPerformanceCheck(statement)
        : CompileRegular(statement);

      Console.WriteLine(compiledCommandText);
      if (InMemory) {
        return true;
      }

      sqlCommand.CommandText = compiledCommandText;
      sqlCommand.Prepare();

      Console.WriteLine(commandText);
      dbCommand.CommandText = commandText;

      var r1 = GetExecuteNonQueryResult(dbCommand);
      var r2 = GetExecuteNonQueryResult(sqlCommand);

      Console.WriteLine();
      Console.WriteLine();
      Console.WriteLine(r1);
      Console.WriteLine(r2);

      return r1.RowCount == r2.RowCount;
    }

    private string Compile(ISqlCompileUnit statement)
    {
      return PerformanceCheck
        ? CompileWithPerformanceCheck(statement)
        : CompileRegular(statement);
    }

    private string CompileWithPerformanceCheck(ISqlCompileUnit statement)
    {
      var runs = new TimeSpan[RunsPerGroup * 5];
      var stopwatch = new Stopwatch();
      var compiledCommandText = sqlDriver.Compile(statement).GetCommandText();
      for (var i = 0; i < RunsPerGroup * 5; i++) {
        stopwatch.Start();
        _ = sqlDriver.Compile(statement).GetCommandText();
        stopwatch.Stop();

        runs[i] = stopwatch.Elapsed;
        stopwatch.Reset();
      }

      WriteDetailedStats(runs);
      return compiledCommandText;
    }

    private string CompileRegular(ISqlCompileUnit statement)
    {
      var stopwatch = new Stopwatch();
      stopwatch.Start();
      var compiledStatement = sqlDriver.Compile(statement);
      stopwatch.Stop();
      WriteDuration(stopwatch);
      return compiledStatement.GetCommandText();
    }

    private void WriteDuration(Stopwatch stopwatch) =>
      Console.WriteLine($"Compilation elapsed ticks: {stopwatch.Elapsed} ({stopwatch.ElapsedTicks} ticks)");

    private void WriteDetailedStats(TimeSpan[] values)
    {
      var runs = RunsPerGroup;
      var part1 = new ArraySegment<TimeSpan>(values, runs * 0, runs);
      var part2 = new ArraySegment<TimeSpan>(values, runs * 1, runs);
      var part3 = new ArraySegment<TimeSpan>(values, runs * 2, runs);
      var part4 = new ArraySegment<TimeSpan>(values, runs * 3, runs);
      var part5 = new ArraySegment<TimeSpan>(values, runs * 4, runs);

      var orderedTicks = values.Select(ts => ts.Ticks).OrderBy(t => t).ToArray(values.Length);
      var min = orderedTicks[0];
      var max = orderedTicks[^1];
      var avg = orderedTicks.Average();

      var p95barier = (int) (0.95 * values.Length);
      var p95Values = new ArraySegment<long>(orderedTicks, 0, p95barier);
      var p95Max = p95Values[^1];
      var p95Avg = p95Values.Average();
      var deltas = values.Select(d => Math.Abs(avg - d.Ticks));

      Console.WriteLine("-------------------------------------");
      Console.WriteLine($"Min: {min} ticks({new TimeSpan(min)})");
      Console.WriteLine($"Max: {max} ticks({new TimeSpan(max)})");
      Console.WriteLine($"P95Max {p95Max} ticks({new TimeSpan(p95Max)})");
      Console.WriteLine($"Avg: {avg} ticks");
      Console.WriteLine($"P95Avg: {p95Avg} ticks");

      Console.WriteLine("-------------------------------------");

      Console.WriteLine("--------------Raw values-------------");
      for (var i = 0; i < part1.Count; i++) {
        Console.WriteLine($"{part1[i].Ticks}    {part2[i].Ticks}    {part3[i].Ticks}    {part4[i].Ticks}    {part5[i].Ticks}");
      }
      Console.WriteLine("-------------------------------------");
    }

    #endregion

    [Test]
    public void Test000()
    {
      if (InMemory) {
        throw new IgnoreException("Execution of query required");
      }

      var p = sqlCommand.CreateParameter();
      p.ParameterName = "p1";
      p.DbType = DbType.Int32;
      p.Value = 40;
      _ = sqlCommand.Parameters.Add(p);
      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"], product["ListPrice"]);
      select.Where = product["ListPrice"]>SqlDml.ParameterRef(p.ParameterName);
      select.OrderBy.Add(product["ListPrice"]);

      sqlCommand.CommandText = Compile(select);
      sqlCommand.Prepare();

      var r = GetExecuteDataReaderResult(sqlCommand);
      Console.WriteLine(r);
    }

    [Test]
    public void Test001()
    {
      var nativeSql = "SELECT ProductID, Name, ListPrice "
        +"FROM Production.Product "
          +"WHERE ListPrice > $40 "
            +"ORDER BY ListPrice ASC";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"], product["ListPrice"]);
      select.Where = product["ListPrice"]>40;
      select.OrderBy.Add(product["ListPrice"]);
      select.OrderBy.Add(1);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));

    }

    [Test]
    public void Test002()
    {
      var nativeSql = "SELECT * "
        +"FROM AdventureWorks.Purchasing.ShipMethod";

      var purchasing = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ShipMethod"]);
      var select = SqlDml.Select(purchasing);
      select.Columns.Add(SqlDml.Asterisk);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test003()
    {
      var nativeSql = "SELECT DISTINCT Sales.Customer.CustomerID, Sales.Store.Name "
        +"FROM Sales.Customer JOIN Sales.Store ON "
          +"(Sales.Customer.CustomerID = Sales.Store.CustomerID) "
            +"WHERE Sales.Customer.TerritoryID = 1";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"]);
      var select = SqlDml.Select(customer);
      select.Distinct = true;
      select.Columns.AddRange(customer["CustomerID"], store["Name"]);
      select.From = select.From.InnerJoin(store, customer["CustomerID"]==store["CustomerID"]);
      select.Where = customer["TerritoryID"]==1;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test004()
    {
      var nativeSql = "SELECT DISTINCT c.CustomerID, s.Name "
        +"FROM Sales.Customer AS c "
          +"JOIN "
            +"Sales.Store AS s "
              +"ON ( c.CustomerID = s.CustomerID) "
                +"WHERE c.TerritoryID = 1";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"], "c");
      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select =
        SqlDml.Select(customer.InnerJoin(store, customer["CustomerID"]==store["CustomerID"]));
      select.Distinct = true;
      select.Columns.AddRange(customer["CustomerID"], store["Name"]);
      select.Where = customer["TerritoryID"]==1;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test005()
    {
      var nativeSql = "SELECT DISTINCT ShipToAddressID, TerritoryID "
        +"FROM Sales.SalesOrderHeader "
          +"ORDER BY TerritoryID";

      var salesOrderHeader = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var select = SqlDml.Select(salesOrderHeader);
      select.Distinct = true;
      select.Columns.AddRange(salesOrderHeader["ShipToAddressID"], salesOrderHeader["TerritoryID"]);
      select.OrderBy.Add(salesOrderHeader["TerritoryID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test006()
    {
      var nativeSql = "SELECT TOP 10 SalesOrderID, OrderDate "
        +"FROM Sales.SalesOrderHeader "
          +"WHERE OrderDate < '2007-01-01T01:01:01.012'"
            +"ORDER BY OrderDate DESC";

      var salesOrderHeader = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var select = SqlDml.Select(salesOrderHeader);
      select.Limit = 10;
      select.Columns.AddRange(salesOrderHeader["SalesOrderID"], salesOrderHeader["OrderDate"]);
      select.Where = salesOrderHeader["OrderDate"]<DateTime.Now;
      select.OrderBy.Add(salesOrderHeader["OrderDate"], false);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test007()
    {
      var nativeSql = "SELECT e.IDENTITYCOL AS \"Employee ID\", "
        +"c.FirstName + ' ' + c.LastName AS \"Employee Name\", "
          +"c.Phone "
            +"FROM AdventureWorks.HumanResources.Employee e "
              +"JOIN AdventureWorks.Person.Contact c "
                +"ON e.ContactID = c.ContactID "
                  +"ORDER BY LastName, FirstName ASC";

      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var select =
        SqlDml.Select(employee.InnerJoin(contact, employee["ContactID"]==contact["ContactID"]));
      select.Columns.Add(employee["EmployeeID"], "Employee ID");
      select.Columns.Add(contact["FirstName"]+'.'+contact["LastName"], "Employee Name");
      select.Columns.Add(contact["Phone"]);
      select.OrderBy.Add(contact["LastName"]);
      select.OrderBy.Add(contact["FirstName"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test008()
    {
      var nativeSql = "SELECT * "
        +"FROM Production.Product "
          +"ORDER BY Name";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(SqlDml.Asterisk);
      select.OrderBy.Add(product["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test009()
    {
      var nativeSql = "SELECT s.UnitPrice, p.* "
        +"FROM Production.Product p "
          +"JOIN "
            +"Sales.SalesOrderDetail s "
              +"ON (p.ProductID = s.ProductID) "
                +"ORDER BY p.ProductID";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var salesOrderDetail =
        SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "s");
      var select = SqlDml.Select(
        product.InnerJoin(salesOrderDetail, product["ProductID"]==salesOrderDetail["ProductID"]));
      select.Columns.Add(salesOrderDetail["UnitPrice"]);
      select.Columns.Add(product.Asterisk);
      select.OrderBy.Add(product["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test010()
    {
      var nativeSql = "SELECT * "
        +"FROM Sales.Customer "
          +"ORDER BY CustomerID ASC";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var select = SqlDml.Select(customer);
      select.Columns.Add(SqlDml.Asterisk);
      select.OrderBy.Add(customer["CustomerID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test011()
    {
      var nativeSql =
        "SELECT CustomerID, TerritoryID, AccountNumber, CustomerType, ModifiedDate "
          +"FROM Sales.Customer "
            +"ORDER BY CustomerID ASC";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var select = SqlDml.Select(customer);
      select.Columns.Add(customer["CustomerID"]);
      select.Columns.Add(customer["TerritoryID"]);
      select.Columns.Add(customer["AccountNumber"]);
      select.Columns.Add(customer["CustomerType"]);
      select.Columns.Add(customer["ModifiedDate"]);
      select.OrderBy.Add(customer["CustomerID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test012()
    {
      var nativeSql = "SELECT c.FirstName, c.Phone "
        +"FROM AdventureWorks.HumanResources.Employee e "
          +"JOIN AdventureWorks.Person.Contact c "
            +"ON e.ContactID = c.ContactID "
              +"ORDER BY FirstName ASC";

      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var select =
        SqlDml.Select(employee.InnerJoin(contact, employee["ContactID"]==contact["ContactID"]));
      select.Columns.AddRange(contact["FirstName"], contact["Phone"]);
      select.OrderBy.Add(contact["FirstName"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test013()
    {
      var nativeSql = "SELECT LastName + ', ' + FirstName AS ContactName "
        +"FROM AdventureWorks.Person.Contact "
          +"ORDER BY LastName, FirstName ASC";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["LastName"]+", "+contact["FirstName"], "ContactName");
      select.OrderBy.Add(contact["LastName"]);
      select.OrderBy.Add(contact["FirstName"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test014()
    {
      var nativeSql = "SELECT ROUND( (ListPrice * .9), 2) AS DiscountPrice "
        +"FROM Production.Product "
          +"WHERE ProductID = 58";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(SqlDml.Round(product["ListPrice"]*0.9, 2), "DiscountPrice");
      select.Where = product["ProductID"]==58;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test015()
    {
      var nativeSql = "SELECT ( CAST(ProductID AS VARCHAR(10)) + ': ' "
        +"+ Name ) AS ProductIDName "
          +"FROM Production.Product";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(
        SqlDml.Cast(product["ProductID"], new SqlValueType("varchar(10)")), "ProductIDName");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test016()
    {
      var nativeSql = "SELECT ProductID, Name, "
        +"CASE Class "
          +"WHEN 'H' THEN ROUND( (ListPrice * .6), 2) "
            +"WHEN 'L' THEN ROUND( (ListPrice * .7), 2) "
              +"WHEN 'M' THEN ROUND( (ListPrice * .8), 2) "
                +"ELSE ROUND( (ListPrice * .9), 2) "
                  +"END AS DiscountPrice "
                    +"FROM Production.Product";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      var discountPrice = SqlDml.Case(product["Class"]);
      discountPrice['H'] = SqlDml.Round(product["ListPrice"]*0.6, 2);
      discountPrice['L'] = SqlDml.Round(product["ListPrice"]*0.7, 2);
      discountPrice['M'] = SqlDml.Round(product["ListPrice"]*0.8, 2);
      discountPrice.Else = SqlDml.Round(product["ListPrice"]*0.6, 2);
      select.Columns.Add(discountPrice, "DiscountPrice");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test017()
    {
      var nativeSql = "SELECT Prd.ProductID, Prd.Name, "
        +"(   SELECT SUM(OD.UnitPrice * OD.OrderQty) "
          +"FROM AdventureWorks.Sales.SalesOrderDetail AS OD "
            +"WHERE OD.ProductID = Prd.ProductID "
              +") AS SumOfSales "
                +"FROM AdventureWorks.Production.Product AS Prd "
                  +"ORDER BY Prd.ProductID";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "Prd");
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      var salesOrderDetail =
        SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OD");
      var sumOfSales = SqlDml.Select(salesOrderDetail);
      sumOfSales.Columns.Add(SqlDml.Sum(salesOrderDetail["UnitPrice"]*salesOrderDetail["OrderQty"]));
      sumOfSales.Where = salesOrderDetail["ProductID"]==product["ProductID"];
      select.Columns.Add(sumOfSales, "SumOfSales");
      select.OrderBy.Add(product["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test018()
    {
      var nativeSql = "SELECT p.ProductID, p.Name, "
        +"SUM (p.ListPrice * i.Quantity) AS InventoryValue "
          +"FROM AdventureWorks.Production.Product p "
            +"JOIN AdventureWorks.Production.ProductInventory i "
              +"ON p.ProductID = i.ProductID "
                +"GROUP BY p.ProductID, p.Name "
                  +"ORDER BY p.ProductID";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var i = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductInventory"], "i");
      var select = SqlDml.Select(p.InnerJoin(i, p["ProductID"]==i["ProductID"]));
      select.Columns.AddRange(p["ProductID"], p["Name"]);
      select.Columns.Add(SqlDml.Sum(p["ListPrice"]*i["Quantity"]), "InventoryValue");
      select.GroupBy.AddRange(p["ProductID"], p["Name"]);
      select.OrderBy.Add(p["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test019()
    {
      var nativeSql = "SELECT SalesOrderID, "
        +"DATEDIFF(dd, ShipDate, GETDATE() ) AS DaysSinceShipped "
          +"FROM AdventureWorks.Sales.SalesOrderHeader "
            +"WHERE ShipDate IS NOT NULL";

      var salesOrderHeader = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var select = SqlDml.Select(salesOrderHeader);
      select.Columns.Add(salesOrderHeader["SalesOrderID"]);
      select.Columns.Add(
        SqlDml.FunctionCall(
          "DATEDIFF", SqlDml.Native("dd"), salesOrderHeader["ShipDate"], SqlDml.CurrentDate()),
        "DaysSinceShipped");
      select.Where = SqlDml.IsNotNull(salesOrderHeader["ShipDate"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test020()
    {
      var nativeSql = "SELECT SalesOrderID, "
        +"DaysSinceShipped = DATEDIFF(dd, ShipDate, GETDATE() ) "
          +"FROM AdventureWorks.Sales.SalesOrderHeader "
            +"WHERE ShipDate IS NOT NULL";

      var salesOrderHeader = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var select = SqlDml.Select(salesOrderHeader);
      select.Columns.Add(salesOrderHeader["SalesOrderID"]);
      select.Columns.Add(
        SqlDml.FunctionCall(
          "DATEDIFF", SqlDml.Native("dd"), salesOrderHeader["ShipDate"], SqlDml.CurrentDate()),
        "DaysSinceShipped");
      select.Where = SqlDml.IsNotNull(salesOrderHeader["ShipDate"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test021()
    {
      var nativeSql = "SELECT Name AS \"Product Name\" "
        +"FROM Production.Product "
          +"ORDER BY Name ASC";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"], "Product Name");
      select.OrderBy.Add(product["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test022()
    {
      var nativeSql = "SELECT SUM(TotalDue) AS \"sum\" "
        +"FROM Sales.SalesOrderHeader";

      var product = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(SqlDml.Sum(product["TotalDue"]), "sum");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test023()
    {
      var nativeSql = "SELECT DISTINCT ProductID "
        +"FROM Production.ProductInventory";

      var productInventory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductInventory"]);
      var select = SqlDml.Select(productInventory);
      select.Distinct = true;
      select.Columns.Add(productInventory["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test024()
    {
      var nativeSql = "SELECT Cst.CustomerID, St.Name, Ord.ShipDate, Ord.Freight "
        +"FROM AdventureWorks.Sales.Store AS St "
          +"JOIN AdventureWorks.Sales.Customer AS Cst "
            +"ON St.CustomerID = Cst.CustomerID "
              +"JOIN AdventureWorks.Sales.SalesOrderHeader AS Ord "
                +"ON Cst.CustomerID = Ord.CustomerID";

      var st = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "St");
      var cst = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"], "Cst");
      var ord = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "Ord");
      var select = SqlDml.Select(st);
      select.From = select.From.InnerJoin(cst, st["CustomerID"]==cst["CustomerID"]);
      select.From = select.From.InnerJoin(ord, cst["CustomerID"]==ord["CustomerID"]);
      select.Columns.AddRange(cst["CustomerID"], st["Name"], ord["ShipDate"], ord["Freight"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    [Ignore("")]
    public void Test025()
    {
      var nativeSql = "Not Valid Sql string "
        +"USE AdventureWorks ; "
          +"GO "
            +"SELECT RTRIM(c.FirstName) + ' ' + LTRIM(c.LastName) AS Name, "
              +"d.City "
                +"FROM Person.Contact c "
                  +
                  "INNER JOIN HumanResources.Employee e ON c.ContactID = e.ContactID "
                    +"INNER JOIN (SELECT AddressID, City FROM Person.Address) AS d "
                      +"ON e.AddressID = d.AddressID "
                        +"ORDER BY c.LastName, c.FirstName ;";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var address = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Address"]);
      var subSelect = SqlDml.Select(address);
      subSelect.Columns.AddRange(address["AddressID"], address["City"]);
      var d = SqlDml.QueryRef(subSelect, "d");
      var select = SqlDml.Select(c);
      select.From = select.From.InnerJoin(e, c["ContactID"]==e["ContactID"]);
      select.From = select.From.InnerJoin(d, e["AddressID"]==d["AddressID"]);
      select.Columns.Add(
        SqlDml.Trim(c["FirstName"], SqlTrimType.Trailing)+' '+
          SqlDml.Trim(c["LastName"], SqlTrimType.Leading), "Name");
      select.Columns.Add(d["City"]);
      select.OrderBy.Add(c["LastName"]);
      select.OrderBy.Add(c["FirstName"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    [Ignore("Test is empty.")]
    public void Test026()
    {
      //      var nativeSql = "SELECT @MyIntVariable "
      //                                   +"SELECT @@VERSION "
      //                                   +"SELECT DB_ID('AdventureWorks')";
      //
      //      SqlVariable myIntVariable = Sql.Variable("MyIntVariable");
      //      SqlSelect select1 = Sql.Select();
      //      select1.Columns.Add(myIntVariable);
      //      SqlSelect select2 = Sql.Select();
      //      select2.Columns.Add(Sql.VariableRef("@VERSION"));
      //      SqlSelect select3 = Sql.Select();
      //      select3.Columns.Add(Sql.FunctionCall("DB_ID", "AdventureWorks"));
      //
      //      Console.Write(driver.Translate(select1));
      //      Console.WriteLine();
      //      Console.Write(driver.Translate(select2));
      //      Console.WriteLine();
      //      Console.Write(driver.Translate(select3));
    }

    [Test]
    public void Test027()
    {
      var nativeSql = "SELECT c.CustomerID, s.Name "
        +"FROM Sales.Customer AS c "
          +"JOIN Sales.Store AS s "
            +"ON c.CustomerID = s.CustomerID";

      var c = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"], "c");
      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select = SqlDml.Select(c.InnerJoin(s, c["CustomerID"]==s["CustomerID"]));
      select.Columns.AddRange(c["CustomerID"], s["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test028()
    {
      var nativeSql = "SELECT c.CustomerID, s.Name "
        +"FROM AdventureWorks.Sales.Customer c "
          +"JOIN AdventureWorks.Sales.Store s "
            +"ON s.CustomerID = c.CustomerID "
              +"WHERE c.TerritoryID = 1";

      var c = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"], "c");
      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select = SqlDml.Select(c.InnerJoin(s, c["CustomerID"]==s["CustomerID"]));
      select.Columns.AddRange(c["CustomerID"], s["Name"]);
      select.Where = c["TerritoryID"]==1;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test029()
    {
      var nativeSql = "SELECT OrdD1.SalesOrderID AS OrderID, "
        +"SUM(OrdD1.OrderQty) AS \"Units Sold\", "
          +"SUM(OrdD1.UnitPrice * OrdD1.OrderQty) AS Revenue "
            +"FROM Sales.SalesOrderDetail AS OrdD1 "
              +"WHERE OrdD1.SalesOrderID in (SELECT OrdD2.SalesOrderID "
                +"FROM Sales.SalesOrderDetail AS OrdD2 "
                  +"WHERE OrdD2.UnitPrice > $100) "
                    +"GROUP BY OrdD1.SalesOrderID "
                      +"HAVING SUM(OrdD1.OrderQty) > 100";

      var ordD1 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OrdD1");
      var ordD2 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OrdD2");
      var subSelect = SqlDml.Select(ordD2);
      subSelect.Columns.Add(ordD2["SalesOrderID"]);
      subSelect.Where = ordD2["SalesOrderID"]>100;
      var select = SqlDml.Select(ordD1);
      select.Columns.Add(ordD1["SalesOrderID"], "OrderID");
      select.Columns.Add(SqlDml.Sum(ordD1["OrderQty"]), "Units Sold");
      select.Columns.Add(SqlDml.Sum(ordD1["UnitPrice"]*ordD1["OrderQty"]), "Revenue");
      select.Where = SqlDml.In(ordD1["SalesOrderID"], subSelect);
      select.GroupBy.Add(ordD1["SalesOrderID"]);
      select.Having = SqlDml.Sum(ordD1["OrderQty"])>100;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test030()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE Class = 'H' "
            +"ORDER BY ProductID";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = product["Class"]=='H';
      select.OrderBy.Add(product["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test031()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice BETWEEN 100 and 500 "
            +"ORDER BY ListPrice";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = SqlDml.Between(product["ListPrice"], 100, 500);
      select.OrderBy.Add(product["ListPrice"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test032()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE Color IN ('Multi', 'Silver') "
            +"ORDER BY ProductID";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = SqlDml.In(product["Color"], SqlDml.Row("Multi", "Silver"));
      select.OrderBy.Add(product["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test033()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE Name LIKE 'Ch%' "
            +"ORDER BY ProductID";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = SqlDml.Like(product["Name"], "Ch%");
      select.OrderBy.Add(product["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test034()
    {
      var nativeSql = "SELECT s.Name "
        +"FROM AdventureWorks.Sales.Customer c "
          +"JOIN AdventureWorks.Sales.Store s "
            +"ON c.CustomerID = s.CustomerID "
              +"WHERE s.SalesPersonID IS NOT NULL "
                +"ORDER BY s.Name";

      var c = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"], "c");
      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select = SqlDml.Select(c.InnerJoin(s, c["CustomerID"]==s["CustomerID"]));
      select.Columns.Add(s["Name"]);
      select.Where = SqlDml.IsNotNull(s["SalesPersonID"]);
      select.OrderBy.Add(s["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test035()
    {
      var nativeSql = "SELECT OrdD1.SalesOrderID, OrdD1.ProductID "
        +"FROM Sales.SalesOrderDetail OrdD1 "
          +"WHERE OrdD1.OrderQty > ALL "
            +"(SELECT OrdD2.OrderQty "
              +
              "       FROM Sales.SalesOrderDetail OrdD2 JOIN Production.Product Prd "
                +"ON OrdD2.ProductID = Prd.ProductID "
                  +"WHERE Prd.Class = 'H')";

      var ordD1 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OrdD1");
      var ordD2 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OrdD2");
      var prd = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "Prd");
      var subSelect = SqlDml.Select(ordD2.InnerJoin(prd, ordD2["ProductID"]==prd["ProductID"]));
      subSelect.Columns.Add(ordD2["OrderQty"]);
      subSelect.Where = prd["Class"]=='H';
      var select = SqlDml.Select(ordD1);
      select.Columns.AddRange(ordD1["SalesOrderID"], ordD1["ProductID"]);
      select.Where = ordD1["OrderQty"] > SqlDml.All(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test036()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice < 500 "
            +"OR (Class = 'L' AND ProductLine = 'S')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = product["ListPrice"] < 500 ||
        (product["Class"] == 'L' && product["ProductLine"] == 'S');

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test037()
    {
      var nativeSql = "SELECT Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice > $50.00";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = product["ListPrice"]>50;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test038()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice BETWEEN 15 AND 25";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = SqlDml.Between(product["ListPrice"], 15, 25);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test039()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice = 15 OR ListPrice = 25";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = product["ListPrice"]==15 || product["ListPrice"]==25;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test040()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice > 15 AND ListPrice < 25";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = product["ListPrice"]>15 && product["ListPrice"]<25;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test041()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice NOT BETWEEN 15 AND 25";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = !SqlDml.Between(product["ListPrice"], 15, 25);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test042()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ProductSubcategoryID = 12 OR ProductSubcategoryID = 14 "
            +"OR ProductSubcategoryID = 16";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = product["ProductSubcategoryID"]==12 || product["ProductSubcategoryID"]==14 ||
        product["ProductSubcategoryID"]==16;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test043()
    {
      var nativeSql = "SELECT ProductID, Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ProductSubcategoryID IN (12, 14, 16)";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"]);
      select.Where = SqlDml.In(product["ProductSubcategoryID"], SqlDml.Row(12, 14, 16));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test044()
    {
      var nativeSql = "SELECT DISTINCT Name "
        +"FROM Production.Product "
          +"WHERE ProductModelID IN "
            +"(SELECT ProductModelID "
              +"FROM Production.ProductModel "
                +"WHERE Name = 'Long-sleeve logo jersey');";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productModel = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductModel"]);
      var subSelect = SqlDml.Select(productModel);
      subSelect.Columns.Add(productModel["ProductModelID"]);
      subSelect.Where = productModel["Name"]=="Long-sleeve logo jersey";
      var select = SqlDml.Select(product);
      select.Distinct = true;
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.In(product["ProductModelID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test045()
    {
      var nativeSql = "SELECT DISTINCT Name "
        +"FROM Production.Product "
          +"WHERE ProductModelID NOT IN "
            +"(SELECT ProductModelID "
              +"FROM Production.ProductModel "
                +"WHERE Name = 'Long-sleeve logo jersey');";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productModel = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductModel"]);
      var subSelect = SqlDml.Select(productModel);
      subSelect.Columns.Add(productModel["ProductModelID"]);
      subSelect.Where = productModel["Name"]=="Long-sleeve logo jersey";
      var select = SqlDml.Select(product);
      select.Distinct = true;
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.NotIn(product["ProductModelID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test046()
    {
      var nativeSql = "SELECT Phone "
        +"FROM AdventureWorks.Person.Contact "
          +"WHERE Phone LIKE '415%'";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["Phone"]);
      select.Where = SqlDml.Like(contact["Phone"], "415%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test047()
    {
      var nativeSql = "SELECT Phone "
        +"FROM AdventureWorks.Person.Contact "
          +"WHERE Phone NOT LIKE '415%'";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["Phone"]);
      select.Where = !SqlDml.Like(contact["Phone"], "415%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test048()
    {
      var nativeSql = "SELECT Phone "
        +"FROM Person.Contact "
          +"WHERE Phone LIKE '415%' and Phone IS NOT NULL";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["Phone"]);
      select.Where = SqlDml.Like(contact["Phone"], "415%") && SqlDml.IsNotNull(contact["Phone"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test049()
    {
      var nativeSql = "SELECT Phone "
        +"FROM Person.Contact "
          +"WHERE Phone LIKE '%5/%%' ESCAPE '/'";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["Phone"]);
      select.Where = SqlDml.Like(contact["Phone"], "%5/%%", '/');

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test050()
    {
      var nativeSql = "SELECT ProductID, Name, Color "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE Color IS NULL";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["Name"], product["Color"]);
      select.Where = SqlDml.IsNull(product["Color"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test051()
    {
      var nativeSql = "SELECT CustomerID, AccountNumber, TerritoryID "
        +"FROM AdventureWorks.Sales.Customer "
          +"WHERE TerritoryID IN (1, 2, 3) "
            +"OR TerritoryID IS NULL";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var select = SqlDml.Select(customer);
      select.Columns.AddRange(
        customer["CustomerID"], customer["AccountNumber"], customer["TerritoryID"]);
      select.Where = SqlDml.In(customer["TerritoryID"], SqlDml.Row(1, 2, 3)) ||
        SqlDml.IsNull(customer["TerritoryID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test052()
    {
      var nativeSql = "SELECT CustomerID, AccountNumber, TerritoryID "
        +"FROM AdventureWorks.Sales.Customer "
          +"WHERE TerritoryID = NULL";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var select = SqlDml.Select(customer);
      select.Columns.AddRange(
        customer["CustomerID"], customer["AccountNumber"], customer["TerritoryID"]);
      select.Where = customer["TerritoryID"]==SqlDml.Null;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test053()
    {
      var nativeSql = "SELECT CustomerID, Name "
        +"FROM AdventureWorks.Sales.Store "
          +"WHERE CustomerID LIKE '1%' AND Name LIKE N'Bicycle%'";

      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"]);
      var select = SqlDml.Select(store);
      select.Columns.AddRange(store["CustomerID"], store["Name"]);
      select.Where = SqlDml.Like(store["CustomerID"], "1%") && SqlDml.Like(store["Name"], "Bicycle%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test054()
    {
      var nativeSql = "SELECT CustomerID, Name "
        +"FROM AdventureWorks.Sales.Store "
          +"WHERE CustomerID LIKE '1%' OR Name LIKE N'Bicycle%'";

      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"]);
      var select = SqlDml.Select(store);
      select.Columns.AddRange(store["CustomerID"], store["Name"]);
      select.Where = SqlDml.Like(store["CustomerID"], "1%") || SqlDml.Like(store["Name"], "Bicycle%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test055()
    {
      var nativeSql = "SELECT ProductID, ProductModelID "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ProductModelID = 20 OR ProductModelID = 21 "
            +"AND Color = 'Red'";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["ProductModelID"]);
      select.Where = product["ProductModelID"] == 20 ||
        (product["ProductModelID"] == 21 && product["Color"] == "RED");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test056()
    {
      var nativeSql = "SELECT ProductID, ProductModelID "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE (ProductModelID = 20 OR ProductModelID = 21) "
            +"AND Color = 'Red'";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["ProductModelID"]);
      select.Where = (product["ProductModelID"] == 20 || product["ProductModelID"] == 21) &&
        product["Color"] == "RED";

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test057()
    {
      var nativeSql = "SELECT ProductID, ProductModelID "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ProductModelID = 20 OR (ProductModelID = 21 "
            +"AND Color = 'Red')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductID"], product["ProductModelID"]);
      select.Where = product["ProductModelID"] == 20 ||
        (product["ProductModelID"] == 21 && product["Color"] == "RED");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test058()
    {
      var nativeSql = "SELECT SalesOrderID, SUM(LineTotal) AS SubTotal "
        +"FROM Sales.SalesOrderDetail sod "
          +"GROUP BY SalesOrderID "
            +"ORDER BY SalesOrderID ;";

      var sod = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "sod");
      var select = SqlDml.Select(sod);
      select.Columns.Add(sod["SalesOrderID"]);
      select.Columns.Add(SqlDml.Sum(sod["LineTotal"]), "SubTotal");
      select.GroupBy.Add(sod["SalesOrderID"]);
      select.OrderBy.Add(sod["SalesOrderID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test059()
    {
      var nativeSql = "SELECT DATEPART(yy, HireDate) AS Year, "
        +"COUNT(*) AS NumberOfHires "
          +"FROM AdventureWorks.HumanResources.Employee "
            +"GROUP BY DATEPART(yy, HireDate)";

      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var select = SqlDml.Select(employee);
      select.Columns.Add(
        SqlDml.Extract(SqlDateTimePart.Year, employee["HireDate"]), "Year");
      select.Columns.Add(SqlDml.Count(), "NumberOfHires");
      select.GroupBy.Add(SqlDml.Extract(SqlDateTimePart.Year, employee["HireDate"]));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test060()
    {
      var nativeSql = ""
        +
        "SELECT ProductID, SpecialOfferID, AVG(UnitPrice) AS 'Average Price', "
          +"SUM(LineTotal) AS SubTotal "
            +"FROM Sales.SalesOrderDetail "
              +"GROUP BY ProductID, SpecialOfferID "
                +"ORDER BY ProductID";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(salesOrderDetail["SpecialOfferID"]);
      select.Columns.Add(SqlDml.Avg(salesOrderDetail["UnitPrice"]), "Average Price");
      select.Columns.Add(SqlDml.Sum(salesOrderDetail["LineTotal"]), "SubTotal");
      select.GroupBy.AddRange(salesOrderDetail["ProductID"], salesOrderDetail["SpecialOfferID"]);
      select.OrderBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test061()
    {
      var nativeSql = ""
        +
        "SELECT ProductID, SpecialOfferID, AVG(UnitPrice) AS 'Average Price', "
          +"SUM(LineTotal) AS SubTotal "
            +"FROM Sales.SalesOrderDetail "
              +"GROUP BY ProductID, SpecialOfferID "
                +"ORDER BY ProductID";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(salesOrderDetail["SpecialOfferID"]);
      select.Columns.Add(SqlDml.Avg(salesOrderDetail["UnitPrice"]), "Average Price");
      select.Columns.Add(SqlDml.Sum(salesOrderDetail["LineTotal"]), "SubTotal");
      select.GroupBy.AddRange(salesOrderDetail["ProductID"], salesOrderDetail["SpecialOfferID"]);
      select.OrderBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test062()
    {
      var nativeSql = "SELECT ProductModelID, AVG(ListPrice) AS 'Average List Price' "
        +"FROM Production.Product "
          +"WHERE ListPrice > $1000 "
            +"GROUP BY ProductModelID "
              +"ORDER BY ProductModelID ;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(product["ProductModelID"]);
      select.Columns.Add(SqlDml.Avg(product["ListPrice"]), "Average List Price");
      select.Where = product["ListPrice"]>1000;
      select.GroupBy.Add(product["ProductModelID"]);
      select.OrderBy.Add(product["ProductModelID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test063()
    {
      var nativeSql =
        "SELECT ProductID, AVG(OrderQty) AS AverageQuantity, SUM(LineTotal) AS Total "
          +"FROM Sales.SalesOrderDetail "
            +"GROUP BY ProductID "
              +"HAVING SUM(LineTotal) > $1000000.00 "
                +"AND AVG(OrderQty) < 3 ;";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(SqlDml.Avg(salesOrderDetail["OrderQty"]), "AverageQuantity");
      select.Columns.Add(SqlDml.Sum(salesOrderDetail["LineTotal"]), "Total");
      select.Having = SqlDml.Sum(salesOrderDetail["LineTotal"]) > 1000000 &&
        SqlDml.Avg(salesOrderDetail["OrderQty"]) < 3;
      select.GroupBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test064()
    {
      var nativeSql = "SELECT ProductID, Total = SUM(LineTotal) "
        +"FROM Sales.SalesOrderDetail "
          +"GROUP BY ProductID "
            +"HAVING SUM(LineTotal) > $2000000.00 ;";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(SqlDml.Sum(salesOrderDetail["LineTotal"]), "Total");
      select.Having = SqlDml.Sum(salesOrderDetail["LineTotal"])>2000000;
      select.GroupBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test065()
    {
      var nativeSql = "SELECT ProductID, SUM(LineTotal) AS Total "
        +"FROM Sales.SalesOrderDetail "
          +"GROUP BY ProductID "
            +"HAVING COUNT(*) > 1500 ;";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(SqlDml.Sum(salesOrderDetail["LineTotal"]), "Total");
      select.Having = SqlDml.Count()>1500;
      select.GroupBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test066()
    {
      var nativeSql = "SELECT ProductID "
        +"FROM Sales.SalesOrderDetail "
          +"GROUP BY ProductID "
            +"HAVING AVG(OrderQty) > 5 "
              +"ORDER BY ProductID ;";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.GroupBy.Add(salesOrderDetail["ProductID"]);
      select.Having = SqlDml.Avg(salesOrderDetail["OrderQty"]) > 5;
      select.OrderBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test067()
    {
      var nativeSql = "SELECT pm.Name, AVG(ListPrice) AS 'Average List Price' "
        +"FROM Production.Product AS p "
          +"JOIN Production.ProductModel AS pm "
            +"ON p.ProductModelID = pm.ProductModelID "
              +"GROUP BY pm.Name "
                +"HAVING pm.Name LIKE 'Mountain%' "
                  +"ORDER BY pm.Name ;";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var pm = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductModel"], "pm");
      var select = SqlDml.Select(p.InnerJoin(pm, p["ProductModelID"]==pm["ProductModelID"]));
      select.Columns.Add(pm["Name"]);
      select.Columns.Add(SqlDml.Avg(p["ListPrice"]), "Average List Price");
      select.GroupBy.Add(pm["Name"]);
      select.Having = SqlDml.Like(pm["Name"], "Mountain%");
      select.OrderBy.Add(pm["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test068()
    {
      var nativeSql = "SELECT ProductID, AVG(UnitPrice) AS 'Average Price' "
        +"FROM Sales.SalesOrderDetail "
          +"WHERE OrderQty > 10 "
            +"GROUP BY ProductID "
              +"ORDER BY ProductID ;";

      var salesOrderDetail = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"]);
      var select = SqlDml.Select(salesOrderDetail);
      select.Columns.Add(salesOrderDetail["ProductID"]);
      select.Columns.Add(SqlDml.Avg(salesOrderDetail["UnitPrice"]), "Average Price");
      select.Where = salesOrderDetail["OrderQty"] > 10;
      select.GroupBy.Add(salesOrderDetail["ProductID"]);
      select.OrderBy.Add(salesOrderDetail["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test069()
    {
      var nativeSql = "SELECT Color, AVG (ListPrice) AS 'average list price' "
        +"FROM Production.Product "
          +"WHERE Color IS NOT NULL "
            +"GROUP BY Color "
              +"ORDER BY Color";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Color"]);
      select.Columns.Add(SqlDml.Avg(product["ListPrice"]), "average list price");
      select.Where = SqlDml.IsNotNull(product["Color"]);
      select.GroupBy.Add(product["Color"]);
      select.OrderBy.Add(product["Color"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test070()
    {
      var nativeSql = "SELECT ProductID, ProductSubcategoryID, ListPrice "
        +"FROM Production.Product "
          +"ORDER BY ProductSubcategoryID DESC, ListPrice";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.AddRange(
        product["ProductID"], product["ProductSubcategoryID"], product["ListPrice"]);
      select.OrderBy.Add(product["ProductSubcategoryID"], false);
      select.OrderBy.Add(product["ListPrice"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    [Ignore("")]
    public void Test071()
    {
      var nativeSql = "SELECT LastName FROM Person.Contact "
        +"ORDER BY LastName "
          +"COLLATE Traditional_Spanish_ci_ai ASC";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(contact);
      select.Columns.Add(contact["LastName"]);
      select.OrderBy.Add(
        SqlDml.Collate(contact["LastName"], Catalog.Schemas["Person"].Collations["Traditional_Spanish_CI_AI"]));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test072()
    {
      var nativeSql = "SELECT Color, AVG (ListPrice) AS 'average list price' "
        +"FROM Production.Product "
          +"GROUP BY Color "
            +"ORDER BY 'average list price'";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Color"]);
      select.Columns.Add(SqlDml.Avg(product["ListPrice"]), "average list price");
      select.GroupBy.Add(product["Color"]);
      select.OrderBy.Add(select.Columns["average list price"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test073()
    {
      var nativeSql =
        "SELECT Ord.SalesOrderID, Ord.OrderDate, "
          +"(SELECT MAX(OrdDet.UnitPrice) "
            +"FROM AdventureWorks.Sales.SalesOrderDetail AS OrdDet "
              +"WHERE Ord.SalesOrderID = OrdDet.SalesOrderID) AS MaxUnitPrice "
          +"FROM AdventureWorks.Sales.SalesOrderHeader AS Ord";

      var ordDet = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "OrdDet");
      var ord = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "Ord");
      var subSelect = SqlDml.Select(ordDet);
      subSelect.Columns.Add(SqlDml.Max(ordDet["UnitPrice"]));
      subSelect.Where = ord["SalesOrderID"]==ordDet["SalesOrderID"];
      var select = SqlDml.Select(ord);
      select.Columns.AddRange(ord["SalesOrderID"], ord["OrderDate"]);
      select.Columns.Add(subSelect, "MaxUnitPrice");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test074()
    {
      var nativeSql = "SELECT Name "
        +"FROM AdventureWorks.Production.Product "
          +"WHERE ListPrice = "
            +"(SELECT ListPrice "
              +"FROM AdventureWorks.Production.Product "
                +"WHERE Name = 'Chainring Bolts' )";

      var product1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(product2["ListPrice"]);
      subSelect.Where = product2["Name"]=="Chainring Bolts";
      var select = SqlDml.Select(product1);
      select.Columns.AddRange(product1["Name"]);
      select.Where = product1["ListPrice"]==subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test075()
    {
      var nativeSql = "SELECT Prd1. Name "
        +"FROM AdventureWorks.Production.Product AS Prd1 "
          +"JOIN AdventureWorks.Production.Product AS Prd2 "
            +"ON (Prd1.ListPrice = Prd2.ListPrice) "
              +"WHERE Prd2. Name = 'Chainring Bolts'";

      var prd1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "Prd1");
      var prd2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "Prd2");
      var select = SqlDml.Select(prd1.InnerJoin(prd2, prd1["ListPrice"]==prd2["ListPrice"]));
      select.Columns.Add(prd1["Name"]);
      select.Where = prd2["Name"]=="Chainring Bolts";

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test076()
    {
      var nativeSql = "SELECT Name "
        +"FROM Sales.Store "
          +"WHERE Sales.Store.CustomerID NOT IN "
            +"(SELECT Sales.Customer.CustomerID "
              +"FROM Sales.Customer "
                +"WHERE TerritoryID = 5)";

      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"]);
      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var subSelect = SqlDml.Select(customer);
      subSelect.Columns.Add(customer["CustomerID"]);
      subSelect.Where = customer["TerritoryID"]==5;
      var select = SqlDml.Select(store);
      select.Columns.Add(store["Name"]);
      select.Where = SqlDml.NotIn(store["CustomerID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test077()
    {
      var nativeSql = "SELECT EmployeeID, ManagerID "
        +"FROM HumanResources.Employee "
          +"WHERE ManagerID IN "
            +"(SELECT ManagerID "
              +"FROM HumanResources.Employee "
                +"WHERE EmployeeID = 12)";

      var employee1 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var employee2 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var subSelect = SqlDml.Select(employee2);
      subSelect.Columns.Add(employee2["ManagerID"]);
      subSelect.Where = employee2["EmployeeID"]==12;
      var select = SqlDml.Select(employee1);
      select.Columns.AddRange(employee1["EmployeeID"], employee1["ManagerID"]);
      select.Where = SqlDml.In(employee1["ManagerID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test078()
    {
      var nativeSql = "SELECT e1.EmployeeID, e1.ManagerID "
        +"FROM HumanResources.Employee AS e1 "
          +"INNER JOIN HumanResources.Employee AS e2 "
            +"ON e1.ManagerID = e2.ManagerID "
              +"AND e2.EmployeeID = 12";

      var e1 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e1");
      var e2 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e2");
      var select =
        SqlDml.Select(e1.InnerJoin(e2, e1["ManagerID"]==e2["ManagerID"] && e2["EmployeeID"]==12));
      select.Columns.AddRange(e1["EmployeeID"], e1["ManagerID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test079()
    {
      var nativeSql = "SELECT e1.EmployeeID, e1.ManagerID "
        +"FROM HumanResources.Employee AS e1 "
          +"WHERE e1.ManagerID IN "
            +"(SELECT e2.ManagerID "
              +"FROM HumanResources.Employee AS e2 "
                +"WHERE e2.EmployeeID = 12)";

      var e1 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e1");
      var e2 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e2");
      var subSelect = SqlDml.Select(e2);
      subSelect.Columns.Add(e2["ManagerID"]);
      subSelect.Where = e2["EmployeeID"] == 12;
      var select = SqlDml.Select(e1);
      select.Columns.AddRange(e1["EmployeeID"], e1["ManagerID"]);
      select.Where = SqlDml.In(e1["ManagerID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test080()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ProductSubcategoryID IN "
            +"(SELECT ProductSubcategoryID "
              +"FROM Production.ProductSubcategory "
                +"WHERE Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory["ProductSubcategoryID"]);
      subSelect.Where = productSubcategory["Name"] == "Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.In(product["ProductSubcategoryID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test081()
    {
      var nativeSql = "SELECT p.Name, s.Name "
        +"FROM Production.Product p "
          +"INNER JOIN Production.ProductSubcategory s "
            +"ON p.ProductSubcategoryID = s.ProductSubcategoryID "
              +"AND s.Name = 'Wheels'";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var s = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"], "s");
      var select = SqlDml.Select(
        p.InnerJoin(
          s, p["ProductSubcategoryID"] == s["ProductSubcategoryID"] && s["Name"] == "Wheels"));
      select.Columns.AddRange(p["Name"], s["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test082()
    {
      var nativeSql = "SELECT Name "
        +"FROM Purchasing.Vendor "
          +"WHERE CreditRating = 1 "
            +"AND VendorID IN "
              +"(SELECT VendorID "
                +"FROM Purchasing.ProductVendor "
                  +"WHERE MinOrderQty >= 20 "
                    +"AND AverageLeadTime < 16)";

      var vendor = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"]);
      var productVendor = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"]);
      var subSelect = SqlDml.Select(productVendor);
      subSelect.Columns.Add(productVendor["VendorID"]);
      subSelect.Where = productVendor["MinOrderQty"]>=20 && productVendor["AverageLeadTime"]<16;
      var select = SqlDml.Select(vendor);
      select.Columns.Add(vendor["Name"]);
      select.Where = vendor["CreditRating"] == 1 &&
        SqlDml.In(vendor["VendorID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test083()
    {
      var nativeSql = "SELECT DISTINCT Name "
        +"FROM Purchasing.Vendor v "
          +"INNER JOIN Purchasing.ProductVendor p "
            +"ON v.VendorID = p.VendorID "
              +"WHERE CreditRating = 1 "
                +"AND MinOrderQty >= 20 "
                  +"AND OnOrderQty IS NULL";

      var v = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"], "v");
      var p = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "p");
      var select = SqlDml.Select(v.InnerJoin(p, v["VendorID"] == p["VendorID"]));
      select.Distinct = true;
      select.Columns.Add(v["Name"]);
      select.Where = v["CreditRating"] == 1 && p["MinOrderQty"] >= 20 && SqlDml.IsNull(p["OnOrderQty"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test084()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ProductSubcategoryID NOT IN "
            +"(SELECT ProductSubcategoryID "
              +"FROM Production.Product "
                +"WHERE Name = 'Mountain Bikes' "
                  +"OR Name = 'Road Bikes' "
                    +"OR Name = 'Touring Bikes')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(product2["ProductSubcategoryID"]);
      subSelect.Where = product2["Name"] == "Mountain Bikes" || product2["Name"] == "Road Bikes" ||
        product2["Name"] == "Touring Bikes";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.NotIn(product["ProductSubcategoryID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test085()
    {
      var nativeSql = "UPDATE Production.Product "
        +"SET ListPrice = ListPrice * 2 "
          +"WHERE ProductID IN "
            +"(SELECT ProductID "
              +"FROM Purchasing.ProductVendor "
                +"WHERE VendorID = 51);";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productVendor = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"]);
      var subSelect = SqlDml.Select(productVendor);
      subSelect.Columns.Add(productVendor["ProductID"]);
      subSelect.Where = productVendor["VendorID"] == 51;
      var update = SqlDml.Update(product);
      update.Values[product["ListPrice"]] = product["ListPrice"] * 2;
      update.Where = SqlDml.In(product["ProductID"], subSelect);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test086()
    {
      var nativeSql = "UPDATE Production.Product "
        +"SET ListPrice = ListPrice * 2 "
          +"FROM Production.Product AS p "
            +"INNER JOIN Purchasing.ProductVendor AS pv "
              +"ON p.ProductID = pv.ProductID AND pv.VendorID = 51;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var pv = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv");

      var update = SqlDml.Update(product);
      update.Values[product["ListPrice"]] = p["ListPrice"] * 2;
      update.From = p.InnerJoin(pv, p["ProductID"] == pv["ProductID"] && pv["VendorID"] == 51);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test087()
    {
      var nativeSql = "SELECT CustomerID "
        +"FROM Sales.Customer "
          +"WHERE TerritoryID = "
            +"(SELECT TerritoryID "
              +"FROM Sales.SalesPerson "
                +"WHERE SalesPersonID = 276)";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var subSelect = SqlDml.Select(salesPerson);
      subSelect.Columns.Add(salesPerson["TerritoryID"]);
      subSelect.Where = salesPerson["SalesPersonID"] == 276;
      var select = SqlDml.Select(customer);
      select.Columns.Add(customer["CustomerID"]);
      select.Where = customer["TerritoryID"] == subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test088()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ListPrice > "
            +"(SELECT AVG (ListPrice) "
              +"FROM Production.Product)";

      var product1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(SqlDml.Avg(product2["ListPrice"]));
      var select = SqlDml.Select(product1);
      select.Columns.Add(product1["Name"]);
      select.Where = product1["ListPrice"]>subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test089()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ListPrice > "
            +"(SELECT MIN (ListPrice) "
              +"FROM Production.Product "
                +"GROUP BY ProductSubcategoryID "
                  +"HAVING ProductSubcategoryID = 14)";

      var product1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(SqlDml.Min(product2["ListPrice"]));
      subSelect.GroupBy.Add(product2["ProductSubcategoryID"]);
      subSelect.Having = product2["ProductSubcategoryID"]==14;
      var select = SqlDml.Select(product1);
      select.Columns.Add(product1["Name"]);
      select.Where = product1["ListPrice"]>subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test090()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ListPrice >= ANY "
            +"(SELECT MAX (ListPrice) "
              +"FROM Production.Product "
                +"GROUP BY ProductSubcategoryID)";

      var product1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(SqlDml.Max(product2["ListPrice"]));
      subSelect.GroupBy.Add(product2["ProductSubcategoryID"]);
      var select = SqlDml.Select(product1);
      select.Columns.Add(product1["Name"]);
      select.Where = product1["ListPrice"]>=SqlDml.Any(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test091()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ProductSubcategoryID=ANY "
            +"(SELECT ProductSubcategoryID "
              +"FROM Production.ProductSubcategory "
                +"WHERE Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory["ProductSubcategoryID"]);
      subSelect.Where = productSubcategory["Name"]=="Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = product["ProductSubcategoryID"]==SqlDml.Any(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test092()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ProductSubcategoryID IN "
            +"(SELECT ProductSubcategoryID "
              +"FROM Production.ProductSubcategory "
                +"WHERE Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory["ProductSubcategoryID"]);
      subSelect.Where = productSubcategory["Name"]=="Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.In(product["ProductSubcategoryID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test093()
    {
      var nativeSql = "SELECT CustomerID "
        +"FROM Sales.Customer "
          +"WHERE TerritoryID <> ANY "
            +"(SELECT TerritoryID "
              +"FROM Sales.SalesPerson)";

      var customer = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Customer"]);
      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var subSelect = SqlDml.Select(salesPerson);
      subSelect.Columns.Add(salesPerson["TerritoryID"]);
      var select = SqlDml.Select(customer);
      select.Columns.Add(customer["CustomerID"]);
      select.Where = customer["TerritoryID"]!=SqlDml.Any(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test094()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE EXISTS "
            +"(SELECT * "
              +"FROM Production.ProductSubcategory "
                +"WHERE ProductSubcategoryID = "
                  +"Production.Product.ProductSubcategoryID "
                    +"AND Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory.Asterisk);
      subSelect.Where = productSubcategory["ProductSubcategoryID"]==product["ProductSubcategoryID"];
      subSelect.Where = subSelect.Where && productSubcategory["Name"]=="Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.Exists(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test095()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE ProductSubcategoryID IN "
            +"(SELECT ProductSubcategoryID "
              +"FROM Production.ProductSubcategory "
                +"WHERE Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory["ProductSubcategoryID"]);
      subSelect.Where = productSubcategory["Name"]=="Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = SqlDml.In(product["ProductSubcategoryID"], subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test096()
    {
      var nativeSql = "SELECT Name "
        +"FROM Production.Product "
          +"WHERE NOT EXISTS "
            +"(SELECT * "
              +"FROM Production.ProductSubcategory "
                +"WHERE ProductSubcategoryID = "
                  +"Production.Product.ProductSubcategoryID "
                    +"AND Name = 'Wheels')";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var productSubcategory =
        SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"]);
      var subSelect = SqlDml.Select(productSubcategory);
      subSelect.Columns.Add(productSubcategory.Asterisk);
      subSelect.Where = productSubcategory["ProductSubcategoryID"]==product["ProductSubcategoryID"];
      subSelect.Where = subSelect.Where && productSubcategory["Name"]=="Wheels";
      var select = SqlDml.Select(product);
      select.Columns.Add(product["Name"]);
      select.Where = !SqlDml.Exists(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test097()
    {
      var nativeSql = "SELECT Name, ListPrice, "
        +"(SELECT AVG(ListPrice) FROM Production.Product) AS Average, "
          +
          "    ListPrice - (SELECT AVG(ListPrice) FROM Production.Product) "
            +"AS Difference "
              +"FROM Production.Product "
                +"WHERE ProductSubcategoryID = 1";

      var product1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var product2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var subSelect = SqlDml.Select(product2);
      subSelect.Columns.Add(SqlDml.Avg(product2["ListPrice"]));
      var select = SqlDml.Select(product1);
      select.Columns.AddRange(product1["Name"], product1["ListPrice"]);
      select.Columns.Add(subSelect, "Average");
      select.Columns.Add(product1["ListPrice"]-subSelect, "Difference");
      select.Where = product1["ProductSubcategoryID"]==1;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test098()
    {
      var nativeSql = "SELECT LastName, FirstName "
        +"FROM Person.Contact "
          +"WHERE ContactID IN "
            +"(SELECT ContactID "
              +"FROM HumanResources.Employee "
                +"WHERE EmployeeID IN "
                  +"(SELECT SalesPersonID "
                    +"FROM Sales.SalesPerson))";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var subSelect2 = SqlDml.Select(salesPerson);
      subSelect2.Columns.Add(salesPerson["SalesPersonID"]);
      var subSelect1 = SqlDml.Select(employee);
      subSelect1.Columns.Add(employee["ContactID"]);
      subSelect1.Where = SqlDml.In(employee["EmployeeID"], subSelect2);
      var select = SqlDml.Select(contact);
      select.Columns.AddRange(contact["LastName"], contact["FirstName"]);
      select.Where = SqlDml.In(contact["ContactID"], subSelect1);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test099()
    {
      var nativeSql = "SELECT LastName, FirstName "
        +"FROM Person.Contact c "
          +"INNER JOIN HumanResources.Employee e "
            +"ON c.ContactID = e.ContactID "
              +"JOIN Sales.SalesPerson s "
                +"ON e.EmployeeID = s.SalesPersonID";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "s");
      var select = SqlDml.Select(c);
      select.From = select.From.InnerJoin(e, c["ContactID"]==e["ContactID"]);
      select.From = select.From.InnerJoin(s, e["EmployeeID"]==s["SalesPersonID"]);
      select.Columns.AddRange(c["LastName"], c["FirstName"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test100()
    {
      var nativeSql = "SELECT DISTINCT c.LastName, c.FirstName "
        +"FROM Person.Contact c JOIN HumanResources.Employee e "
          +"ON e.ContactID = c.ContactID "
            +"WHERE 5000.00 IN "
              +"(SELECT Bonus "
                +"FROM Sales.SalesPerson sp "
                  +"WHERE e.EmployeeID = sp.SalesPersonID);";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var sp = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "sp");
      var subSelect = SqlDml.Select(sp);
      subSelect.Columns.Add(sp["Bonus"]);
      subSelect.Where = e["EmployeeID"] == sp["SalesPersonID"];
      var select = SqlDml.Select(c.InnerJoin(e, c["ContactID"] == e["ContactID"]));
      select.Distinct = true;
      select.Columns.AddRange(c["LastName"], c["FirstName"]);
      select.Where = SqlDml.In(5000.00, subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test101()
    {
      var nativeSql = "SELECT LastName, FirstName "
        +"FROM Person.Contact c JOIN HumanResources.Employee e "
          +"ON e.ContactID = c.ContactID "
            +"WHERE 5000 IN (5000)";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var select = SqlDml.Select(c.InnerJoin(e, c["ContactID"]==e["ContactID"]));
      select.Columns.AddRange(c["LastName"], c["FirstName"]);
      select.Where = SqlDml.In(5000, SqlDml.Row(5000));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test102()
    {
      var nativeSql = "SELECT DISTINCT pv1.ProductID, pv1.VendorID "
        +"FROM Purchasing.ProductVendor pv1 "
          +"WHERE ProductID IN "
            +"(SELECT pv2.ProductID "
              +"FROM Purchasing.ProductVendor pv2 "
                +"WHERE pv1.VendorID <> pv2.VendorID) "
                  +"ORDER  BY pv1.VendorID";

      var pv1 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv1");
      var pv2 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv2");
      var subSelect = SqlDml.Select(pv2);
      subSelect.Columns.Add(pv2["ProductID"]);
      subSelect.Where = pv1["VendorID"]!=pv2["VendorID"];
      var select = SqlDml.Select(pv1);
      select.Distinct = true;
      select.Columns.AddRange(pv1["ProductID"], pv1["VendorID"]);
      select.Where = SqlDml.In(pv1["ProductID"], subSelect);
      select.OrderBy.Add(pv1["VendorID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test103()
    {
      var nativeSql = "SELECT DISTINCT pv1.ProductID, pv1.VendorID "
        +"FROM Purchasing.ProductVendor pv1 "
          +"INNER JOIN Purchasing.ProductVendor pv2 "
            +"ON pv1.ProductID = pv2.ProductID "
              +"AND pv1.VendorID <> pv2.VendorID "
                +"ORDER BY pv1.VendorID";

      var pv1 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv1");
      var pv2 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv2");
      var select = SqlDml.Select(
        pv1.InnerJoin(pv2, pv1["ProductID"]==pv2["ProductID"] && pv1["VendorID"]!=pv2["VendorID"]));
      select.Distinct = true;
      select.Columns.AddRange(pv1["ProductID"], pv1["VendorID"]);
      select.OrderBy.Add(pv1["VendorID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test104()
    {
      var nativeSql = "SELECT ProductID, OrderQty "
        +"FROM Sales.SalesOrderDetail s1 "
          +"WHERE s1.OrderQty < "
            +"(SELECT AVG (s2.OrderQty) "
              +"FROM Sales.SalesOrderDetail s2 "
                +"WHERE s2.ProductID = s1.ProductID)";

      var s1 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "s1");
      var s2 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "s2");
      var subSelect = SqlDml.Select(s2);
      subSelect.Columns.Add(SqlDml.Avg(s2["OrderQty"]));
      subSelect.Where = s2["ProductID"]==s1["ProductID"];
      var select = SqlDml.Select(s1);
      select.Columns.AddRange(s1["ProductID"], s1["OrderQty"]);
      select.Where = s1["OrderQty"]<subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test105()
    {
      var nativeSql = "SELECT p1.ProductSubcategoryID, p1.Name "
        +"FROM Production.Product p1 "
          +"WHERE p1.ListPrice > "
            +"(SELECT AVG (p2.ListPrice) "
              +"FROM Production.Product p2 "
                +"WHERE p1.ProductSubcategoryID = p2.ProductSubcategoryID)";

      var p1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p1");
      var p2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p2");
      var subSelect = SqlDml.Select(p2);
      subSelect.Columns.Add(SqlDml.Avg(p2["ListPrice"]));
      subSelect.Where = p2["ProductSubcategoryID"]==p1["ProductSubcategoryID"];
      var select = SqlDml.Select(p1);
      select.Columns.AddRange(p1["ProductSubcategoryID"], p1["Name"]);
      select.Where = p1["ListPrice"]>subSelect;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test106()
    {
      var nativeSql = "SELECT p1.ProductModelID "
        +"FROM Production.Product p1 "
          +"GROUP BY p1.ProductModelID "
            +"HAVING MAX(p1.ListPrice) >= ALL "
              +"(SELECT 2 * AVG(p2.ListPrice) "
                +"FROM Production.Product p2 "
                  +"WHERE p1.ProductModelID = p2.ProductModelID) ;";

      var p1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p1");
      var p2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p2");
      var subSelect = SqlDml.Select(p2);
      subSelect.Columns.Add(2*SqlDml.Avg(p2["ListPrice"]));
      subSelect.Where = p2["ProductModelID"]==p1["ProductModelID"];
      var select = SqlDml.Select(p1);
      select.Columns.Add(p1["ProductModelID"]);
      select.GroupBy.Add(p1["ProductModelID"]);
      select.Having = SqlDml.Max(p1["ListPrice"])>=SqlDml.All(subSelect);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test107()
    {
      var nativeSql = "SELECT ProductID, Purchasing.Vendor.VendorID, Name "
        +"FROM Purchasing.ProductVendor JOIN Purchasing.Vendor "
          +"ON (Purchasing.ProductVendor.VendorID = Purchasing.Vendor.VendorID) "
            +"WHERE StandardPrice > $10 "
              +"AND Name LIKE N'F%'";

      var productVendor = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"]);
      var vendor = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"]);
      var select =
        SqlDml.Select(productVendor.InnerJoin(vendor, productVendor["VendorID"]==vendor["VendorID"]));
      select.Columns.AddRange(productVendor["ProductID"], vendor["VendorID"], vendor["Name"]);
      select.Where = productVendor["StandardPrice"]>10 && SqlDml.Like(vendor["Name"], "F%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test108()
    {
      var nativeSql = "SELECT pv.ProductID, v.VendorID, v.Name "
        +"FROM Purchasing.ProductVendor pv JOIN Purchasing.Vendor v "
          +"ON (pv.VendorID = v.VendorID) "
            +"WHERE StandardPrice > $10 "
              +"AND Name LIKE N'F%'";

      var pv = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv");
      var v = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"], "v");
      var select = SqlDml.Select(pv.InnerJoin(v, pv["VendorID"]==v["VendorID"]));
      select.Columns.AddRange(pv["ProductID"], v["VendorID"], v["Name"]);
      select.Where = pv["StandardPrice"]>10 && SqlDml.Like(v["Name"], "F%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test109()
    {
      var nativeSql = "SELECT pv.ProductID, v.VendorID, v.Name "
        +"FROM Purchasing.ProductVendor pv, Purchasing.Vendor v "
          +"WHERE pv.VendorID = v.VendorID "
            +"AND StandardPrice > $10 "
              +"AND Name LIKE N'F%'";

      var pv = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv");
      var v = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"], "v");
      var select = SqlDml.Select(pv.CrossJoin(v));
      select.Columns.AddRange(pv["ProductID"], v["VendorID"], v["Name"]);
      select.Where = pv["VendorID"]==v["VendorID"] && pv["StandardPrice"]>10 &&
        SqlDml.Like(v["Name"], "F%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test110()
    {
      var nativeSql = "SELECT e.EmployeeID "
        +"FROM HumanResources.Employee AS e "
          +"INNER JOIN Sales.SalesPerson AS s "
            +"ON e.EmployeeID = s.SalesPersonID";

      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "s");
      var select = SqlDml.Select(e.InnerJoin(s, e["EmployeeID"]==s["SalesPersonID"]));
      select.Columns.Add(e["EmployeeID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test111()
    {
      var nativeSql = "SELECT * "
        +"FROM HumanResources.Employee AS e "
          +"INNER JOIN Person.Contact AS c "
            +"ON e.ContactID = c.ContactID "
              +"ORDER BY c.LastName";

      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"], "c");
      var select = SqlDml.Select(e.InnerJoin(c, e["ContactID"]==c["ContactID"]));
      select.Columns.Add(SqlDml.Asterisk);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test112()
    {
      var nativeSql =
        "SELECT DISTINCT p.ProductID, p.Name, p.ListPrice, sd.UnitPrice AS 'Selling Price' "
          +"FROM Sales.SalesOrderDetail AS sd "
            +"JOIN Production.Product AS p "
              +"ON sd.ProductID = p.ProductID AND sd.UnitPrice < p.ListPrice "
                +"WHERE p.ProductID = 718;";

      var sd = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "sd");
      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var select = SqlDml.Select(
        sd.InnerJoin(p, sd["ProductID"]==p["ProductID"] && sd["UnitPrice"]<p["ListPrice"]));
      select.Distinct = true;
      select.Columns.AddRange(p["ProductID"], p["Name"], p["ListPrice"]);
      select.Columns.Add(sd["UnitPrice"], "Selling Price");
      select.Where = p["ProductID"]==718;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test113()
    {
      var nativeSql = "SELECT DISTINCT p1.ProductSubcategoryID, p1.ListPrice "
        +"FROM Production.Product p1 "
          +"INNER JOIN Production.Product p2 "
            +"ON p1.ProductSubcategoryID = p2.ProductSubcategoryID "
              +"AND p1.ListPrice <> p2.ListPrice "
                +"WHERE p1.ListPrice < $15 AND p2.ListPrice < $15 "
                  +"ORDER BY ProductSubcategoryID;";

      var p1 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p1");
      var p2 = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p2");
      var select = SqlDml.Select(
        p1.InnerJoin(
          p2,
          p1["ProductSubcategoryID"] == p2["ProductSubcategoryID"] &&
            p1["ListPrice"] != p2["ListPrice"]));
      select.Distinct = true;
      select.Columns.AddRange(p1["ProductSubcategoryID"], p1["ListPrice"]);
      select.Where = p1["ListPrice"]<15 && p2["ListPrice"]<15;
      select.OrderBy.Add(p1["ProductSubcategoryID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test114()
    {
      var nativeSql = "SELECT DISTINCT p1.VendorID, p1.ProductID "
        +"FROM Purchasing.ProductVendor p1 "
          +"INNER JOIN Purchasing.ProductVendor p2 "
            +"ON p1.ProductID = p2.ProductID "
              +"WHERE p1.VendorID <> p2.VendorID "
                +"ORDER BY p1.VendorID";

      var p1 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "p1");
      var p2 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "p2");
      var select = SqlDml.Select(p1.InnerJoin(p2, p1["ProductID"]==p2["ProductID"]));
      select.Distinct = true;
      select.Columns.AddRange(p1["VendorID"], p1["ProductID"]);
      select.Where = p1["VendorID"] != p2["VendorID"];
      select.OrderBy.Add(p1["VendorID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test115()
    {
      var nativeSql = "SELECT p.Name, pr.ProductReviewID "
        +"FROM Production.Product p "
          +"LEFT OUTER JOIN Production.ProductReview pr "
            +"ON p.ProductID = pr.ProductID";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var pr = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductReview"], "pr");
      var select = SqlDml.Select(p.LeftOuterJoin(pr, p["ProductID"]==pr["ProductID"]));
      select.Columns.AddRange(p["Name"], pr["ProductReviewID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test116()
    {
      var nativeSql = "SELECT st.Name AS Territory, sp.SalesPersonID "
        +"FROM Sales.SalesTerritory st "
          +"RIGHT OUTER JOIN Sales.SalesPerson sp "
            +"ON st.TerritoryID = sp.TerritoryID ;";

      var st = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesTerritory"], "st");
      var sp = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "sp");
      var select = SqlDml.Select(st.RightOuterJoin(sp, st["TerritoryID"]==sp["TerritoryID"]));
      select.Columns.Add(st["Name"], "Territory");
      select.Columns.Add(sp["SalesPersonID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test117()
    {
      var nativeSql = "SELECT st.Name AS Territory, sp.SalesPersonID "
        +"FROM Sales.SalesTerritory st "
          +"RIGHT OUTER JOIN Sales.SalesPerson sp "
            +"ON st.TerritoryID = sp.TerritoryID "
              +"WHERE st.SalesYTD < $2000000;";

      var st = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesTerritory"], "st");
      var sp = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "sp");
      var select = SqlDml.Select(st.RightOuterJoin(sp, st["TerritoryID"]==sp["TerritoryID"]));
      select.Columns.Add(st["Name"], "Territory");
      select.Columns.Add(sp["SalesPersonID"]);
      select.Where = st["SalesYTD"]<2000000;

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test118()
    {
      var nativeSql = "SELECT p.Name, sod.SalesOrderID "
        +"FROM Production.Product p "
          +"FULL OUTER JOIN Sales.SalesOrderDetail sod "
            +"ON p.ProductID = sod.ProductID "
              +"WHERE p.ProductID IS NULL "
                +"OR sod.ProductID IS NULL "
                  +"ORDER BY p.Name ;";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var sod = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderDetail"], "sod");
      var select = SqlDml.Select(p.FullOuterJoin(sod, p["ProductID"]==sod["ProductID"]));
      select.Columns.AddRange(p["Name"], sod["SalesOrderID"]);
      select.Where = SqlDml.IsNull(p["ProductID"]) || SqlDml.IsNull(sod["ProductID"]);
      select.OrderBy.Add(p["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test119()
    {
      var nativeSql = "SELECT e.EmployeeID, d.Name AS Department "
        +"FROM HumanResources.Employee e "
          +"CROSS JOIN HumanResources.Department d "
            +"ORDER BY e.EmployeeID, d.Name ;";

      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var d = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Department"], "d");
      var select = SqlDml.Select(e.CrossJoin(d));
      select.Columns.Add(e["EmployeeID"]);
      select.Columns.Add(d["Name"], "Department");
      select.OrderBy.Add(e["EmployeeID"]);
      select.OrderBy.Add(d["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    [Ignore("Invalid column")]
    public void Test120()
    {
      var nativeSql = "SELECT e.EmployeeID, d.Name AS Department "
        +"FROM HumanResources.Employee e "
          +"CROSS JOIN HumanResources.Department d "
            +"WHERE e.DepartmentID = d.DepartmentID "
              +"ORDER BY e.EmployeeID, d.Name ;";

      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var d = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Department"], "d");
      var select = SqlDml.Select(e.CrossJoin(d));
      select.Columns.Add(e["EmployeeID"]);
      select.Columns.Add(d["Name"], "Department");
      select.Where = e["DepartmentID"]==d["DepartmentID"];
      select.OrderBy.Add(e["EmployeeID"]);
      select.OrderBy.Add(d["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    [Ignore("Invalid column")]
    public void Test121()
    {
      var nativeSql = "SELECT e.EmployeeID, d.Name AS Department "
        +"FROM HumanResources.Employee e "
          +"INNER JOIN HumanResources.Department d "
            +"ON e.DepartmentID = d.DepartmentID "
              +"ORDER BY e.EmployeeID, d.Name ;";

      var e = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"], "e");
      var d = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Department"], "d");
      var select = SqlDml.Select(e.InnerJoin(d, e["DepartmentID"]==d["DepartmentID"]));
      select.Columns.Add(e["EmployeeID"]);
      select.Columns.Add(d["Name"], "Department");
      select.OrderBy.Add(e["EmployeeID"]);
      select.OrderBy.Add(d["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test122()
    {
      var nativeSql = "SELECT DISTINCT pv1.ProductID, pv1.VendorID "
        +"FROM Purchasing.ProductVendor pv1 "
          +"INNER JOIN Purchasing.ProductVendor pv2 "
            +"ON pv1.ProductID = pv2.ProductID "
              +"AND pv1.VendorID <> pv2.VendorID "
                +"ORDER BY pv1.ProductID";

      var pv1 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv1");
      var pv2 = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv2");
      var select = SqlDml.Select(
        pv1.InnerJoin(pv2, pv1["ProductID"]==pv2["ProductID"] && pv1["VendorID"]!=pv2["VendorID"]));
      select.Distinct = true;
      select.Columns.AddRange(pv1["ProductID"], pv1["VendorID"]);
      select.OrderBy.Add(pv1["ProductID"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test123()
    {
      var nativeSql = "SELECT p.Name, v.Name "
        +"FROM Production.Product p "
          +"JOIN Purchasing.ProductVendor pv "
            +"ON p.ProductID = pv.ProductID "
              +"JOIN Purchasing.Vendor v "
                +"ON pv.VendorID = v.VendorID "
                  +"WHERE ProductSubcategoryID = 15 "
                    +"ORDER BY v.Name";

      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var pv = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv");
      var v = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"], "v");
      var select = SqlDml.Select(
        p.InnerJoin(pv, p["ProductID"]==pv["ProductID"]).InnerJoin(
          v, pv["VendorID"]==v["VendorID"]));
      select.Columns.AddRange(p["Name"], v["Name"]);
      select.Where = p["ProductSubcategoryID"]==15;
      select.OrderBy.Add(v["Name"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test124()
    {
      var nativeSql = "INSERT INTO Production.UnitMeasure "
        +"VALUES (N'F2', N'Square Feet', GETDATE());";

      var unitMeasure = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["UnitMeasure"]);
      var insert = SqlDml.Insert(unitMeasure);
      insert.AddValueRow(
        (unitMeasure[0], "F2"),
        (unitMeasure[1], "Square Feet"),
        (unitMeasure[2], SqlDml.CurrentDate())
      );

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, insert));
    }

    [Test]
    public void Test125()
    {
      var nativeSql = "UPDATE AdventureWorks.Production.Product "
        +"SET ListPrice = ListPrice * 1.1 "
          +"WHERE ProductModelID = 37;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var update = SqlDml.Update(product);
      update.Values[product["ListPrice"]] = product["ListPrice"]*1.1;
      update.Where = product["ProductModelID"]==37;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test126()
    {
      var nativeSql = "UPDATE Person.Address "
        +"SET PostalCode = '98000' "
          +"WHERE City = 'Bothell';";

      var product = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Address"]);
      var update = SqlDml.Update(product);
      update.Values[product["PostalCode"]] = "98000";
      update.Where = product["City"]=="Bothell";

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test127()
    {
      var nativeSql = "UPDATE Sales.SalesPerson "
        +"SET Bonus = 6000, CommissionPct = .10, SalesQuota = NULL;";

      var product = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var update = SqlDml.Update(product);
      update.Values[product["Bonus"]] = 6000;
      update.Values[product["CommissionPct"]] = .10;
      update.Values[product["SalesQuota"]] = SqlDml.Null;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test128()
    {
      var nativeSql = "UPDATE Production.Product "
        +"SET ListPrice = ListPrice * 2;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var update = SqlDml.Update(product);
      update.Values[product["ListPrice"]] = product["ListPrice"]*2;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test129()
    {
      var nativeSql = "UPDATE Sales.SalesPerson "
        +"SET SalesYTD = SalesYTD + "
          +"(SELECT SUM(so.SubTotal) "
            +"FROM Sales.SalesOrderHeader AS so "
              +"WHERE so.OrderDate = (SELECT MAX(OrderDate) "
                +"FROM Sales.SalesOrderHeader AS so2 "
                  +"WHERE so2.SalesPersonID = "
                    +"so.SalesPersonID) "
                      +"AND Sales.SalesPerson.SalesPersonID = so.SalesPersonID "
                        +"GROUP BY so.SalesPersonID);";

      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var so = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "so");
      var so2 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "so2");
      var subSelect = SqlDml.Select(so2);
      subSelect.Columns.Add(SqlDml.Max(so2["OrderDate"]));
      subSelect.Where = so2["SalesPersonID"]==so["SalesPersonID"];
      var select = SqlDml.Select(so);
      select.Columns.Add(SqlDml.Sum(so["SubTotal"]));
      select.Where = so["OrderDate"]==subSelect && salesPerson["SalesPersonID"]==so["SalesPersonID"];
      select.GroupBy.Add(so["SalesPersonID"]);
      var update = SqlDml.Update(salesPerson);
      update.Values[salesPerson["SalesYTD"]] = salesPerson["SalesYTD"]+select;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test130()
    {
      var nativeSql = "UPDATE AdventureWorks.Sales.SalesReason "
        +"SET Name = N'Unknown' "
          +"WHERE Name = N'Other';";

      var salesReason = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesReason"]);
      var update = SqlDml.Update(salesReason);
      update.Values[salesReason["Name"]] = "Unknown";
      update.Where = salesReason["Name"]=="Other";

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test131()
    {
      var nativeSql = "UPDATE Sales.SalesPerson "
        +"SET SalesYTD = SalesYTD + SubTotal "
          +"FROM Sales.SalesPerson AS sp "
            +"JOIN Sales.SalesOrderHeader AS so "
              +"ON sp.SalesPersonID = so.SalesPersonID "
                +"AND so.OrderDate = (SELECT MAX(OrderDate) "
                  +"FROM Sales.SalesOrderHeader "
                    +"WHERE SalesPersonID = "
                      +"sp.SalesPersonID);";

      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var salesOrderHeader = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"]);
      var sp = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"], "sp");
      var so = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "so");
      var subSelect = SqlDml.Select(salesOrderHeader);
      subSelect.Columns.Add(SqlDml.Max(salesOrderHeader["OrderDate"]));
      subSelect.Where = salesOrderHeader["SalesPersonID"]==sp["SalesPersonID"];
      _ = SqlDml.Select(
        sp.InnerJoin(so, sp["SalesPersonID"]==so["SalesPersonID"] && so["OrderDate"]==subSelect));
      var update = SqlDml.Update(salesPerson);
      update.Values[salesPerson["SalesYTD"]] = salesPerson["SalesYTD"];

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test132()
    {
      var nativeSql = "UPDATE Sales.SalesPerson "
        +"SET SalesYTD = SalesYTD + "
          +"(SELECT SUM(so.SubTotal) "
            +"FROM Sales.SalesOrderHeader AS so "
              +"WHERE so.OrderDate = (SELECT MAX(OrderDate) "
                +"FROM Sales.SalesOrderHeader AS so2 "
                  +"WHERE so2.SalesPersonID = "
                    +"so.SalesPersonID) "
                      +"AND Sales.SalesPerson.SalesPersonID = so.SalesPersonID "
                        +"GROUP BY so.SalesPersonID);";

      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var so2 = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "so2");
      var so = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesOrderHeader"], "so");
      var subSelect = SqlDml.Select(so2);
      subSelect.Columns.Add(SqlDml.Max(so2["OrderDate"]));
      subSelect.Where = so2["SalesPersonID"]==so["SalesPersonID"];
      var select = SqlDml.Select(so);
      select.Columns.Add(SqlDml.Sum(so["SubTotal"]));
      select.Where = so["OrderDate"]==subSelect && salesPerson["SalesPersonID"]==so["SalesPersonID"];
      select.GroupBy.Add(so["SalesPersonID"]);
      var update = SqlDml.Update(salesPerson);
      update.Values[salesPerson["SalesYTD"]] = salesPerson["SalesYTD"]+select;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test133()
    {
      var nativeSql = "UPDATE Sales.Store "
        + "SET SalesPersonID = 276 "
        + "WHERE SalesPersonID = 275;";

      var store = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"]);
      var update = SqlDml.Update(store);
      update.Values[store["SalesPersonID"]] = 276;
      update.Where = store["SalesPersonID"]==275;

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test134()
    {
      var nativeSql = "UPDATE HumanResources.Employee "
        +"SET VacationHours = VacationHours + 8 "
          +"FROM (SELECT TOP 10 EmployeeID FROM HumanResources.Employee "
            +"ORDER BY HireDate ASC) AS th "
              +"WHERE HumanResources.Employee.EmployeeID = th.EmployeeID;";

      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var employee2 = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);

      var select = SqlDml.Select(employee);
      select.Limit = 10;
      select.Columns.Add(employee["EmployeeID"]);
      select.OrderBy.Add(employee["HireDate"]);
      var th = SqlDml.QueryRef(select, "th");

      var update = SqlDml.Update(employee2);
      update.Values[employee2["VacationHours"]] = employee2["VacationHours"]+8;
      update.From = th;
      update.Where = employee2["EmployeeID"]==th["EmployeeID"];

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, update));
    }

    [Test]
    public void Test135()
    {
      var nativeSql = "DELETE FROM Sales.SalesPersonQuotaHistory "
        +"WHERE SalesPersonID IN "
          +"(SELECT SalesPersonID "
            +"FROM Sales.SalesPerson "
              +"WHERE SalesYTD > 2500000.00);";

      var salesPersonQuotaHistory =
        SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPersonQuotaHistory"]);
      var salesPerson = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var subSelect = SqlDml.Select(salesPerson);
      subSelect.Columns.Add(salesPerson["SalesPersonID"]);
      subSelect.Where = salesPerson["SalesYTD"]>2500000.00;
      var delete = SqlDml.Delete(salesPersonQuotaHistory);
      delete.Where = SqlDml.In(salesPersonQuotaHistory["SalesPersonID"], subSelect);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, delete));
    }

    [Test]
    public void Test136()
    {
      var nativeSql = "DELETE FROM Sales.SalesPersonQuotaHistory "
        +"WHERE SalesPersonID IN  "
          +"(SELECT SalesPersonID "
            +"FROM Sales.SalesPerson "
              +"WHERE SalesYTD > 2500000.00);";

      var salesPersonQuotaHistory =
        SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPersonQuotaHistory"]);

      var sp = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SalesPerson"]);
      var subSelect = SqlDml.Select(sp);
      subSelect.Columns.Add(sp["SalesPersonID"]);
      subSelect.Where = sp["SalesYTD"]>2500000.00;

      var delete = SqlDml.Delete(salesPersonQuotaHistory);
      delete.Where = SqlDml.In(salesPersonQuotaHistory["SalesPersonID"], subSelect);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, delete));
    }

    [Test]
    public void Test137()
    {
      var nativeSql = "DELETE FROM Purchasing.PurchaseOrderDetail "
        +"WHERE PurchaseOrderDetailID IN "
          +"(SELECT TOP 10 PurchaseOrderDetailID "
            +"FROM Purchasing.PurchaseOrderDetail "
              +"ORDER BY DueDate ASC);";

      var purchaseOrderDetail1 =
        SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["PurchaseOrderDetail"]);
      var purchaseOrderDetail2 =
        SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["PurchaseOrderDetail"]);
      var select = SqlDml.Select(purchaseOrderDetail2);
      select.Limit = 10;
      select.Columns.Add(purchaseOrderDetail2["PurchaseOrderDetailID"]);
      select.OrderBy.Add(purchaseOrderDetail2["DueDate"]);
      var delete = SqlDml.Delete(purchaseOrderDetail1);
      delete.Where = SqlDml.In(purchaseOrderDetail1["PurchaseOrderDetailID"], select);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, delete));
    }

    [Test]
    public void Test138()
    {
      // var nativeSql = "DECLARE @EmpIDVar int; "
      //   +"SET @EmpIDVar = 1234; "
      //     +"SELECT * "
      //       +"FROM HumanResources.Employee "
      //         +"WHERE EmployeeID = @EmpIDVar;";

      var employee = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Employee"]);
      var empIDVar = SqlDml.Variable("EmpIDVar", SqlType.Int32);
      var batch = SqlDml.Batch();
      batch.Add(empIDVar.Declare());
      batch.Add(SqlDml.Assign(empIDVar, 1234));
      var select = SqlDml.Select(employee);
      select.Columns.Add(employee.Asterisk);
      select.Where = employee["EmployeeID"]==empIDVar;
      batch.Add(select);

      Console.Write(Compile(batch));
    }

    [Test]
    public void Test139()
    {
      var nativeSql = "SELECT Name, "
        +"CASE Name "
          +"WHEN 'Human Resources' THEN 'HR' "
            +"WHEN 'Finance' THEN 'FI' "
              +"WHEN 'Information Services' THEN 'IS' "
                +"WHEN 'Executive' THEN 'EX' "
                  +"WHEN 'Facilities and Maintenance' THEN 'FM' "
                    +"END AS Abbreviation "
                      +"FROM AdventureWorks.HumanResources.Department "
                        +"WHERE GroupName = 'Executive General and Administration'";

      var department = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["Department"]);
      var c = SqlDml.Case(department["Name"]);
      _ = c.Add("Human Resources", "HR").Add("Finance", "FI").Add("Information Services", "IS").Add("Executive", "EX");
      c["Facilities and Maintenance"] = "FM";
      var select = SqlDml.Select(department);
      select.Columns.AddRange(department["Name"]);
      select.Columns.Add(c, "Abbreviation");
      select.Where = department["GroupName"]=="Executive General and Administration";

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test140()
    {
      var nativeSql = "SELECT   ProductNumber, Category = "
        +"CASE ProductLine "
          +"WHEN 'R' THEN 'Road' "
            +"WHEN 'M' THEN 'Mountain' "
              +"WHEN 'T' THEN 'Touring' "
                +"WHEN 'S' THEN 'Other sale items' "
                  +"ELSE 'Not for sale' "
                    +"END, "
                      +"Name "
                        +"FROM Production.Product "
                          +"ORDER BY ProductNumber;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var c = SqlDml.Case(product["ProductLine"]);
      c["R"] = "Road";
      c["M"] = "Mountain";
      c["T"] = "Touring";
      c["S"] = "Other sale items";
      c.Else = "Not for sale";
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductNumber"]);
      select.Columns.Add(c, "Category");
      select.Columns.Add(product["Name"]);
      select.OrderBy.Add(product["ProductNumber"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test141()
    {
      var nativeSql = "SELECT   ProductNumber, Name, 'Price Range' = "
        +"CASE "
          +"WHEN ListPrice =  0 THEN 'Mfg item - not for resale' "
            +"WHEN ListPrice < 50 THEN 'Under $50' "
              +"WHEN ListPrice >= 50 and ListPrice < 250 THEN 'Under $250' "
                +"WHEN ListPrice >= 250 and ListPrice < 1000 THEN 'Under $1000' "
                  +"ELSE 'Over $1000' "
                    +"END "
                      +"FROM Production.Product "
                        +"ORDER BY ProductNumber ;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var c = SqlDml.Case();
      c[product["ListPrice"]==0] = "Mfg item - not for resale";
      c[product["ListPrice"]<50] = "Under $50";
      c[product["ListPrice"]>=50 && product["ListPrice"]<250] = "Under $250";
      c[product["ListPrice"]>=250 && product["ListPrice"]<1000] = "Under $1000";
      c.Else = "Over $1000";
      var select = SqlDml.Select(product);
      select.Columns.AddRange(product["ProductNumber"], product["Name"]);
      select.Columns.Add(c, "Price Range");
      select.OrderBy.Add(product["ProductNumber"]);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test142()
    {
      var nativeSql = "DECLARE @find varchar(30); "
        +"SET @find = 'Man%'; "
          +"SELECT LastName, FirstName, Phone "
            +"FROM Person.Contact "
              +"WHERE LastName LIKE @find;";

      var contact = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var find = SqlDml.Variable("find", new SqlValueType("varchar(30)"));
      var batch = SqlDml.Batch();
      batch.Add(find.Declare());
      batch.Add(SqlDml.Assign(find, "Man%"));
      var select = SqlDml.Select(contact);
      select.Columns.AddRange(contact["LastName"], contact["FirstName"], contact["Phone"]);
      select.Where = SqlDml.Like(contact["LastName"], find);
      batch.Add(select);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, batch));
    }

    [Test]
    public void Test143()
    {
      var nativeSql = "SELECT * "
        + "FROM Sales.Store s "
          + "WHERE s.Name IN ('West Side Mart', 'West Wind Distributors', 'Westside IsCyclic Store')";

      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select = SqlDml.Select(s);
      select.Columns.Add(SqlDml.Asterisk);
      select.Where =
        SqlDml.In(
          s["Name"], SqlDml.Array("West Side Mart", "West Wind Distributors", "Westside IsCyclic Store"));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test144()
    {
      var nativeSql = "SELECT * "
        +"FROM Sales.Store s "
          +"WHERE s.CustomerID IN (1, 2, 3)";

      var s = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["Store"], "s");
      var select = SqlDml.Select(s);
      select.Columns.Add(SqlDml.Asterisk);
      select.Where = SqlDml.In(s["CustomerID"], SqlDml.Array(1, 2, 3));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test145()
    {
      var nativeSql = "DECLARE complex_cursor CURSOR FOR "
        +"SELECT a.EmployeeID "
          +"FROM HumanResources.EmployeePayHistory AS a "
            +"WHERE RateChangeDate <> "
              +"(SELECT MAX(RateChangeDate) "
                +"FROM HumanResources.EmployeePayHistory AS b "
                  +"WHERE a.EmployeeID = b.EmployeeID); "
                    +"OPEN complex_cursor; "
                      +"FETCH FROM complex_cursor; "
                        +"UPDATE HumanResources.EmployeePayHistory "
                          +"SET PayFrequency = 2 "
                            +"WHERE CURRENT OF complex_cursor; "
                              +"CLOSE complex_cursor; "
                                +"DEALLOCATE complex_cursor;";

      var batch = SqlDml.Batch();
      var employeePayHistory = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["EmployeePayHistory"]);
      var a = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["EmployeePayHistory"], "a");
      var b = SqlDml.TableRef(Catalog.Schemas["HumanResources"].Tables["EmployeePayHistory"], "b");
      var selectInner = SqlDml.Select(b);
      selectInner.Columns.Add(SqlDml.Max(b["RateChangeDate"]));
      selectInner.Where = a["EmployeeID"]==b["EmployeeID"];
      var select = SqlDml.Select(a);
      select.Columns.Add(a["EmployeeID"]);
      select.Where = a["RateChangeDate"]!=selectInner;
      var cursor = SqlDml.Cursor("complex_cursor", select);
      batch.Add(cursor.Declare());
      batch.Add(cursor.Open());
      batch.Add(cursor.Fetch());
      var update = SqlDml.Update(employeePayHistory);
      update.Values[employeePayHistory["PayFrequency"]] = 2;
      update.Where = cursor;
      batch.Add(update);
      batch.Add(cursor.Close());

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, batch));
    }

    [Test]
    public void Test146()
    {
      var drop = SqlDdl.Drop(Catalog.Schemas["HumanResources"].Tables["EmployeePayHistory"]);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test147()
    {
      var drop = SqlDdl.Drop(Catalog.Schemas["HumanResources"].Tables["EmployeePayHistory"], false);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test148()
    {
      var drop = SqlDdl.Drop(Catalog.Schemas["HumanResources"]);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test149()
    {
      var drop = SqlDdl.Drop(Catalog.Schemas["HumanResources"], false);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test150()
    {
      var create = SqlDdl.Create(Catalog.Schemas["Production"].Tables["Product"]);
      create.Table.Filegroup = "xxx";
      Console.Write(Compile(create));
    }

    [Test]
    public void Test151()
    {
      // var nativeSql = "CREATE VIEW [HumanResources].[vEmployee] "
      //   +"AS SELECT "
      //     +"e.[EmployeeID], "
      //       +"c.[Title], "
      //         +"c.[FirstName], "
      //           +"c.[MiddleName], "
      //             +"c.[LastName], "
      //               +"c.[Suffix], "
      //                 +"e.[Title] AS [JobTitle], "
      //                   +"c.[Phone], "
      //                     +"c.[EmailAddress], "
      //                       +"c.[EmailPromotion], "
      //                         +"a.[AddressLine1], "
      //                           +"a.[AddressLine2], "
      //                             +"a.[City], "
      //                               +"sp.[Name] AS [StateProvinceName], "
      //                                 +"a.[PostalCode], "
      //                                   +"cr.[Name] AS [CountryRegionName], "
      //                                     +"FROM [HumanResources].[Employee] e "
      //                                       +"INNER JOIN [Person].[Contact] c "
      //                                         +"ON c.[ContactID] = e.[ContactID] "
      //                                           +"INNER JOIN [HumanResources].[EmployeeAddress] ea "
      //                                             +"ON e.[EmployeeID] = ea.[EmployeeID] "
      //                                               +"INNER JOIN [Person].[Address] a "
      //                                                 +"ON ea.[AddressID] = a.[AddressID] "
      //                                                   +"INNER JOIN [Person].[StateProvince] sp "
      //                                                     +"ON sp.[StateProvinceID] = a.[StateProvinceID] "
      //                                                       +"INNER JOIN [Person].[CountryRegion] cr "
      //                                                         +"ON cr.[CountryRegionCode] = sp.[CountryRegionCode]";
      var create = SqlDdl.Create(Catalog.Schemas["HumanResources"].Views["vEmployee"]);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test152()
    {
      var assertion = Catalog.Schemas["Production"]
        .CreateAssertion("assertion", SqlDml.Literal(1)==1, false, false);
      var create = SqlDdl.Create(assertion);

      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test153()
    {
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(Catalog.Schemas["Production"], "characterSet", characterSetBase, (ICollation)null);
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test154()
    {
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(Catalog.Schemas["Production"], "characterSet", characterSetBase, new DefaultCollation());
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test155()
    {
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(
//          Catalog.Schemas["Production"], "characterSet", characterSetBase,
//          new DescendingCollation(Catalog.Schemas["HumanResources"].Collations["SQL_Latin1_General_CP1_CI_AS"]));
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test156()
    {
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(
//          Catalog.Schemas["Production"], "characterSet", characterSetBase, new ExternalCollation("Collation.bin"));
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test157()
    {
//      Translation translation = new Translation(Catalog.Schemas["Production"], "tranlsation");
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(
//          Catalog.Schemas["Production"], "characterSet", characterSetBase, new TranslationCollation(translation));
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test158()
    {
//      Translation translation = new Translation(Catalog.Schemas["Production"], "tranlsation");
//      CharacterSet characterSetBase = new CharacterSet(Catalog.Schemas["Production"], "characterSetBase");
//      CharacterSet characterSet =
//        new CharacterSet(
//          Catalog.Schemas["Production"], "characterSet", characterSetBase,
//          new TranslationCollation(
//            translation, Catalog.Schemas["HumanResources"].Collations["SQL_Latin1_General_CP1_CI_AS"]));
//      SqlCreateCharacterSet create = SqlDdl.Create(characterSet);
//
//      Console.Write(Compile(create));
    }

    [Test]
    public void Test159()
    {
      var domain =
        Catalog.Schemas["HumanResources"].CreateDomain("domain", new SqlValueType(SqlType.Decimal, 8, 2), 1);
      domain.Collation = Catalog.Schemas["HumanResources"].Collations["SQL_Latin1_General_CP1_CI_AS"];
      _ = domain.CreateConstraint("domainConstraint", SqlDml.Literal(1)==1);
      var create = SqlDdl.Create(domain);

      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test160()
    {
//      CharacterSet characterSet = new CharacterSet(Catalog.Schemas["Production"], "characterSet");
//      Collation collation =
//        new Collation(
//          Catalog.Schemas["HumanResources"], "collation", characterSet,
//          Catalog.Schemas["HumanResources"].Collations["SQL_Latin1_General_CP1_CI_AS"]);
//      collation.PadSpace = true;
//      SqlCreateCollation create = SqlDdl.Create(collation);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test161()
    {
//      CharacterSet source = new CharacterSet(Catalog.Schemas["Production"], "characterSetSource");
//      CharacterSet target = new CharacterSet(Catalog.Schemas["Production"], "characterSetTarget");
//      IdentityTranslation tranlsationSource = new IdentityTranslation();
//      Translation translation = new Translation(Catalog.Schemas["Production"], "translation");
//      translation.SourceCharacterSet = source;
//      translation.TargetCharacterSet = target;
//      translation.TranslationSource = tranlsationSource;
//      SqlCreateTranslation create = SqlDdl.Create(translation);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is emtpy.")]
    public void Test162()
    {
//      CharacterSet source = new CharacterSet(Catalog.Schemas["Production"], "characterSetSource");
//      CharacterSet target = new CharacterSet(Catalog.Schemas["Production"], "characterSetTarget");
//      ExternalTranslation tranlsationSource = new ExternalTranslation("externalTranlsation");
//      Translation translation = new Translation(Catalog.Schemas["Production"], "translation");
//      translation.SourceCharacterSet = source;
//      translation.TargetCharacterSet = target;
//      translation.TranslationSource = tranlsationSource;
//      SqlCreateTranslation create = SqlDdl.Create(translation);
//
//      Console.Write(Compile(create));
    }

    [Test]
    [Ignore("Test is empty.")]
    public void Test163()
    {
//      CharacterSet source = new CharacterSet(Catalog.Schemas["Production"], "characterSetSource");
//      CharacterSet target = new CharacterSet(Catalog.Schemas["Production"], "characterSetTarget");
//      Translation tranlsationSource = new Translation(Catalog.Schemas["Person"], "tranlsationSource");
//      Translation translation = new Translation(Catalog.Schemas["Production"], "translation");
//      translation.SourceCharacterSet = source;
//      translation.TargetCharacterSet = target;
//      translation.TranslationSource = tranlsationSource;
//      SqlCreateTranslation create = SqlDdl.Create(translation);
//
//      Console.Write(Compile(create));
    }

    [Test]
    public void Test164()
    {
      var create = SqlDdl.Create(Catalog.Schemas["Production"]);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test165()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.AddColumn(Catalog.Schemas["Production"].Tables["Product"].TableColumns["Name"]));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test166()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.DropDefault(Catalog.Schemas["Production"].Tables["Product"].TableColumns["Name"]));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test167()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.SetDefault("Empty", Catalog.Schemas["Production"].Tables["Product"].TableColumns["Name"]));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test168()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.DropColumn(Catalog.Schemas["Production"].Tables["Product"].TableColumns["Name"]));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test169()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.AddConstraint(Catalog.Schemas["Production"].Tables["Product"].TableConstraints[0]));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test170()
    {
      var alter = SqlDdl.Alter(
        Catalog.Schemas["Production"].Tables["Product"],
        SqlDdl.DropConstraint(Catalog.Schemas["Production"].Tables["Product"].TableConstraints[0]));

      Console.Write(Compile(alter));
    }

    [Test]
    [Ignore("ALTER DOMAIN is not supported")]
    public void Test171()
    {
      var domain =
        Catalog.Schemas["HumanResources"].CreateDomain("domain171", new SqlValueType(SqlType.Decimal, 8, 2), 1);
      _ = domain.CreateConstraint("domainConstraint", SqlDml.Literal(1)==1);
      var alter = SqlDdl.Alter(domain, SqlDdl.AddConstraint(domain.DomainConstraints[0]));

      Console.Write(Compile(alter));
    }

    [Test]
    [Ignore("ALTER DOMAIN is not supported")]
    public void Test172()
    {
      var domain = Catalog.Schemas["HumanResources"]
        .CreateDomain("domain172", new SqlValueType(SqlType.Decimal, 8, 2), 1);
      _ = domain.CreateConstraint("domainConstraint", SqlDml.Literal(1)==1);
      var alter = SqlDdl.Alter(domain, SqlDdl.DropConstraint(domain.DomainConstraints[0]));

      Console.Write(Compile(alter));
    }

    [Test]
    [Ignore("ALTER DOMAIN is not supported")]
    public void Test173()
    {
      var domain = Catalog.Schemas["HumanResources"]
        .CreateDomain("domain173", new SqlValueType(SqlType.Decimal, 8, 2), 1);
      var alter = SqlDdl.Alter(domain, SqlDdl.SetDefault(0));

      Console.Write(Compile(alter));
    }

    [Test]
    [Ignore("ALTER DOMAIN is not supported")]
    public void Test174()
    {
      var domain = Catalog.Schemas["HumanResources"]
        .CreateDomain("domain174", new SqlValueType(SqlType.Decimal, 8, 2), 1);
      var alter = SqlDdl.Alter(domain, SqlDdl.DropDefault());

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test175()
    {
      var sequence = Catalog.Schemas["Production"].CreateSequence("Generator175");
      var create = SqlDdl.Create(sequence);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test176()
    {
      var seq = Catalog.Schemas["Production"].CreateSequence("Generator176");
      seq.SequenceDescriptor.IsCyclic = true;
      seq.SequenceDescriptor.StartValue = 1000;
      seq.SequenceDescriptor.MaxValue = 1000;
      seq.SequenceDescriptor.MinValue = -1000;
      seq.SequenceDescriptor.Increment = -1;
      var create = SqlDdl.Create(seq);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test177()
    {
      var sequence = Catalog.Schemas["Production"].CreateSequence("Generator177");
      var drop = SqlDdl.Drop(sequence);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test178()
    {
      var sequence = Catalog.Schemas["Production"].CreateSequence("Generator178");
      var alter = SqlDdl.Alter(sequence, new SequenceDescriptor(sequence, 0, 1, null, null, true));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test179()
    {
      var sequence = Catalog.Schemas["Production"].CreateSequence("Generator179");
      var alter = SqlDdl.Alter(sequence, new SequenceDescriptor(sequence, null, null, 1000, -1000));

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test180()
    {
      var table = Catalog.Schemas["Production"].Tables["Product"];
      var action =
        SqlDdl.Alter(table.TableColumns["ProductID"], new SequenceDescriptor(table.TableColumns["ProductID"], 2, 3, null, null, true));
      var alter = SqlDdl.Alter(table, action);

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test181()
    {
      var create = SqlDdl.Create(Catalog.Schemas["Purchasing"].Tables["PurchaseOrderDetail"]);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test182()
    {
      var pf =
        Catalog.CreatePartitionFunction("pf182", new SqlValueType(SqlType.Decimal, 5, 2), "1", "5", "10");
      pf.BoundaryType = BoundaryType.Right;
      var create = SqlDdl.Create(pf);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test183()
    {
      var pf =
        Catalog.CreatePartitionFunction("pf183", new SqlValueType(SqlType.Decimal, 5, 2), "1", "5", "10");
      var drop = SqlDdl.Drop(pf);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test184()
    {
      var pf =
        Catalog.CreatePartitionFunction("pf184", new SqlValueType(SqlType.Decimal, 5, 2), "1", "5", "10");
      var alter = SqlDdl.Alter(pf, "5", SqlAlterPartitionFunctionOption.Split);

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test185()
    {
      var pf =
        Catalog.CreatePartitionFunction("pf185", new SqlValueType(SqlType.Decimal, 5, 2), "1", "5", "10");
      var ps = Catalog.CreatePartitionSchema("ps1", pf, "[PRIMARY]", "sdf", "sdf1", "sdf2");
      var create = SqlDdl.Create(ps);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test186()
    {
      var pf =
        Catalog.CreatePartitionFunction("pf186", new SqlValueType(SqlType.Decimal, 5, 2), "1", "5", "10");
      var ps = Catalog.CreatePartitionSchema("ps186", pf, "[PRIMARY]");
      var create = SqlDdl.Create(ps);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test187()
    {
      var ps = Catalog.CreatePartitionSchema("ps187", null, "[PRIMARY]");
      var drop = SqlDdl.Drop(ps);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test188()
    {
      var ps = Catalog.CreatePartitionSchema("ps188", null, "[PRIMARY]");
      var alter = SqlDdl.Alter(ps);

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test189()
    {
      var ps = Catalog.CreatePartitionSchema("ps189", null, "[PRIMARY]");
      var alter = SqlDdl.Alter(ps, "sdfg");

      Console.Write(Compile(alter));
    }

    [Test]
    public void Test190()
    {
      var ps = Catalog.CreatePartitionSchema("ps190", null, "[PRIMARY]");
      var t = Catalog.Schemas["Production"].Tables["Product"];
      t.PartitionDescriptor = new PartitionDescriptor(t, t.TableColumns["ProductID"], ps);
      var create = SqlDdl.Create(t);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test191()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      t.PartitionDescriptor = new PartitionDescriptor(t, t.TableColumns["ProductID"], PartitionMethod.Hash, 10);
      var create = SqlDdl.Create(t);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test192()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      t.PartitionDescriptor = new PartitionDescriptor(t, t.TableColumns["ProductID"], PartitionMethod.Hash);
      _ = t.PartitionDescriptor.CreateHashPartition("ts1");
      _ = t.PartitionDescriptor.CreateHashPartition("ts2");
      _ = t.PartitionDescriptor.CreateHashPartition("ts3");
      var create = SqlDdl.Create(t);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test193()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      t.PartitionDescriptor = new PartitionDescriptor(t, t.TableColumns["ProductID"], PartitionMethod.List);
      _ = t.PartitionDescriptor.CreateListPartition("p1", "ts1", "sdf", "sdf1", "sdfg");
      _ = t.PartitionDescriptor.CreateListPartition("p2", "ts2", "sir");
      var create = SqlDdl.Create(t);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test194()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      t.PartitionDescriptor = new PartitionDescriptor(t, t.TableColumns["ProductID"], PartitionMethod.Range);
      _ = t.PartitionDescriptor.CreateRangePartition("ts1", new DateTime(2006, 01, 01).ToString());
      _ = t.PartitionDescriptor.CreateRangePartition("ts2", new DateTime(2007, 01, 01).ToString());
      _ = t.PartitionDescriptor.CreateRangePartition("ts3", "MAXVALUE");
      var create = SqlDdl.Create(t);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test195()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      var index = t.CreateIndex("MegaIndex195");
      _ = index.CreateIndexColumn(t.TableColumns[0]);
      _ = index.CreateIndexColumn(t.TableColumns[1]);
      _ = index.CreateIndexColumn(t.TableColumns[2], false);
      _ = index.CreateIndexColumn(t.TableColumns[3]);
      _ = index.CreateIndexColumn(t.TableColumns[4]);
      _ = index.CreateIndexColumn(t.TableColumns[5]);
      index.IsUnique = true;
      index.IsClustered = true;
      index.FillFactor = 80;
      index.Filegroup = "\"default\"";
      var create = SqlDdl.Create(index);

      Console.Write(Compile(create));
    }

    [Test]
    public void Test196()
    {
      var t = Catalog.Schemas["Production"].Tables["Product"];
      var index = t.CreateIndex("MegaIndex196");
      var drop = SqlDdl.Drop(index);

      Console.Write(Compile(drop));
    }

    [Test]
    public void Test197()
    {
      var nativeSql = "Select Top 10 * "
        +"From (Person.StateProvince a "
          +"inner hash join Person.CountryRegion b on a.StateProvinceCode=b.CountryRegionCode)"
            +"inner loop join "
              +"(Person.StateProvince c "
                +"inner merge join Person.CountryRegion d on c.StateProvinceCode=d.CountryRegionCode)"
                  +" on a.CountryRegionCode=c.CountryRegionCode";

      var a = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["StateProvince"], "a");
      var b = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["CountryRegion"], "b");
      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["StateProvince"], "c");
      var d = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["CountryRegion"], "d");

      var ab = a.InnerJoin(b, a["StateProvinceCode"]==b["CountryRegionCode"]);
      var cd = c.InnerJoin(d, c["StateProvinceCode"]==d["CountryRegionCode"]);
      var abcd = SqlDml.Join(SqlJoinType.InnerJoin, ab, cd, a["CountryRegionCode"]==c["CountryRegionCode"]);

      var select = SqlDml.Select(abcd);
      select.Limit = 10;
      select.Columns.Add(SqlDml.Asterisk);
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Hash, ab));
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Merge, cd));
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Loop, abcd));


      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test198()
    {
      var nativeSql = "Select Top 10 * "
        +"From (Person.StateProvince a "
          +"inner hash join Person.CountryRegion b on a.StateProvinceCode=b.CountryRegionCode)"
            +"inner loop join "
              +"(Person.StateProvince c "
                +"inner merge join Person.CountryRegion d on c.StateProvinceCode=d.CountryRegionCode)"
                  +" on a.CountryRegionCode=c.CountryRegionCode";

      var a = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["StateProvince"], "a");
      var b = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["CountryRegion"], "b");
      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["StateProvince"], "c");
      var d = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["CountryRegion"], "d");

      var ab = a.InnerJoin(b, a["StateProvinceCode"]==b["CountryRegionCode"]);
      var cd = c.InnerJoin(d, c["StateProvinceCode"]==d["CountryRegionCode"]);
      var abcd = SqlDml.Join(SqlJoinType.InnerJoin, ab, cd, a["CountryRegionCode"]==c["CountryRegionCode"]);

      var select = SqlDml.Select(abcd);
      select.Limit = 10;
      select.Columns.Add(SqlDml.Asterisk);
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Hash, b));
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Merge, d));
      select.Hints.Add(SqlDml.JoinHint(SqlJoinMethod.Loop, abcd));

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test199()
    {
      var nativeSql =
        "Select Top 10 EmailAddress "+
        "From Person.Contact a "+
        "Where EmailAddress Like 'a%'";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(c);
      select.Limit = 10;
      select.Columns.Add(c["EmailAddress"]);
      select.Where = SqlDml.Like(c["EmailAddress"], "a%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test200()
    {
      var nativeSql =
        "Select Top 10 EmailAddress " +
        "From Person.Contact a " +
        "Where EmailAddress Like 'a%' " +
        "OPTION (FAST 10, KEEP PLAN, ROBUST PLAN)";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select = SqlDml.Select(c);
      select.Limit = 10;
      select.Columns.Add(c["EmailAddress"]);
      select.Where = SqlDml.Like(c["EmailAddress"], "a%");
      select.Hints.Add(SqlDml.FastFirstRowsHint(10));
      select.Hints.Add(SqlDml.NativeHint("KEEP PLAN, ROBUST PLAN"));
      Assert.IsTrue(CompareExecuteDataReader(nativeSql, select));
    }

    [Test]
    public void Test201()
    {
      var nativeSql = "Select Top 10 EmailAddress "
        +"From Person.Contact a "
          +"Where EmailAddress Like 'a%' "
            + "UNION ALL "
              +"Select Top 10 EmailAddress "
                +"From Person.Contact b "
                  +"Where EmailAddress Like 'b%' ";

      var c = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var select1 = SqlDml.Select(c);
      select1.Limit = 10;
      select1.Columns.Add(c["EmailAddress"]);
      select1.Where = SqlDml.Like(c["EmailAddress"], "a%");
      var select2 = SqlDml.Select(c);
      select2.Limit = 10;
      select2.Columns.Add(c["EmailAddress"]);
      select2.Where = SqlDml.Like(c["EmailAddress"], "a%");

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, SqlDml.UnionAll(select1, select2)));
    }

    [Test]
    public void Test202()
    {
      var nativeSql =
        "DECLARE test202cursor CURSOR FOR SELECT * FROM Purchasing.Vendor;\n" +
        "OPEN test202cursor;\n"  +
        "FETCH NEXT FROM test202cursor;\n" +
        "CLOSE test202cursor;";

      var batch = SqlDml.Batch();
      var vendors = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"]);
      var select = SqlDml.Select(vendors);
      select.Columns.Add(select.Asterisk);
      var cursor = SqlDml.Cursor("test202cursor_", select);
      batch.Add(cursor.Declare());
      batch.Add(cursor.Open());
      batch.Add(cursor.Fetch());
      batch.Add(cursor.Close());

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, batch));
    }

    [Test]
    public void Test203()
    {
      var nativeSql = "INSERT INTO Person.Contact DEFAULT VALUES;";

      var unitMeasure = SqlDml.TableRef(Catalog.Schemas["Person"].Tables["Contact"]);
      var insert = SqlDml.Insert(unitMeasure);

      _ = Assert.Throws<Microsoft.Data.SqlClient.SqlException>(() => Assert.IsTrue(CompareExecuteNonQuery(nativeSql, insert)));
    }

    [Test]
    public void Test204()
    {
      var nativeSql = "SELECT a.f FROM ((SELECT 1 as f UNION SELECT 2) EXCEPT (SELECT 3 UNION SELECT 4)) a";

      var s1 = SqlDml.Select();
      var s2 = SqlDml.Select();
      var s3 = SqlDml.Select();
      var s4 = SqlDml.Select();
      s1.Columns.Add(1, "f");
      s2.Columns.Add(2);
      s3.Columns.Add(3);
      s4.Columns.Add(4);
      var qr = SqlDml.QueryRef(s1.Union(s2).Except(s3.Union(s4)), "a");
      var select = SqlDml.Select(qr);
      select.Columns.Add(qr["f"]);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, select));
    }

    [Test]
    public void Test205()
    {
      var nativeSql =
        "DECLARE test205cursor CURSOR FOR SELECT * FROM Purchasing.Vendor;\n" +
        "OPEN test205cursor;\n" +
        "BEGIN\n" +
        "  FETCH NEXT FROM test205cursor;\n"  +
        "END\n" +
        "CLOSE test205cursor;";

      var batch = SqlDml.Batch();
      var vendors = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["Vendor"]);
      var select = SqlDml.Select(vendors);
      select.Columns.Add(select.Asterisk);
      var cursor = SqlDml.Cursor("test205cursor_", select);
      batch.Add(cursor.Declare());
      batch.Add(cursor.Open());
      var block = SqlDml.StatementBlock();
      block.Add(cursor.Fetch());
      batch.Add(block);
      batch.Add(cursor.Close());

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, batch));
    }

    [Test]
    public void Test206()
    {
      var nativeSql =
        "SELECT c.Name SubcategoryName, p.Name ProductName " +
          "FROM Production.ProductSubcategory c " +
            "CROSS APPLY (SELECT Name FROM Production.Product WHERE ProductSubcategoryID = c.ProductSubcategoryID) p " +
              "ORDER BY c.Name, p.Name";

      var subcategories = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["ProductSubcategory"], "c");
      var products = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);

      var innerSelect = SqlDml.Select(products);
      innerSelect.Columns.Add(products.Columns["Name"]);
      innerSelect.Where = products.Columns["ProductSubcategoryID"] == subcategories.Columns["ProductSubcategoryID"];
      var innerQuery = SqlDml.QueryRef(innerSelect, "p");

      var categoryName = subcategories.Columns["Name"];
      var productName = innerQuery.Columns["Name"];

      var outerSelect = SqlDml.Select(subcategories.CrossApply(innerQuery));
      outerSelect.Columns.Add(categoryName, "SubcategoryName");
      outerSelect.Columns.Add(productName, "ProductName");
      outerSelect.OrderBy.Add(categoryName);
      outerSelect.OrderBy.Add(productName);

      Assert.IsTrue(CompareExecuteDataReader(nativeSql, outerSelect));
    }

    [Test]
    public void Test207()
    {
      var nativeSql =
        "DELETE FROM [Sales].[SpecialOfferProduct] WHERE NOT EXISTS (SELECT [ProductID] FROM [Production].[Product]"
      + " WHERE [Production].[Product].[ProductID] = [Sales].[SpecialOfferProduct].[ProductID])";

      var products = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var specialOfferProduct = SqlDml.TableRef(Catalog.Schemas["Sales"].Tables["SpecialOfferProduct"]);

      var select = SqlDml.Select(products);
      select.Columns.Add(products["ProductID"]);
      select.Where = products["ProductID"]==specialOfferProduct["ProductID"];

      var delete = SqlDml.Delete(specialOfferProduct);
      delete.Where = SqlDml.Not(SqlDml.Exists(select));

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, delete));
    }

    [Test]
    public void RenameTest()
    {
      var schema = Catalog.CreateSchema("S1");
      var table = schema.CreateTable("T1");
      _ = table.CreateColumn("C1", new SqlValueType(SqlType.Int32));

      try {
        sqlConnection.BeginTransaction();

        using (var cmd = sqlConnection.CreateCommand()) {
          var batch = SqlDml.Batch();
          batch.Add(SqlDdl.Create(schema));
          cmd.CommandText = Compile(batch);
          _ = cmd.ExecuteNonQuery();
        }

        var exModel1 = sqlDriver.ExtractCatalog(sqlConnection);
        var exT1 = exModel1.Schemas[schema.DbName].Tables[table.DbName];
        Assert.IsNotNull(exT1);
        var exC1 = exT1.TableColumns["C1"];
        Assert.IsNotNull(exC1);

        using (var cmd = sqlConnection.CreateCommand()) {
          var batch = SqlDml.Batch();
          batch.Add(SqlDdl.Rename(exC1, "C2"));
          batch.Add(SqlDdl.Rename(exT1, "T2"));
          cmd.CommandText = Compile(batch);
          _ = cmd.ExecuteNonQuery();
        }

        var exModel2 = sqlDriver.ExtractCatalog(sqlConnection);
        var exT2 = exModel2.Schemas[schema.DbName].Tables["T2"];
        Assert.IsNotNull(exT2);
        var exC2 = exT2.TableColumns["C2"];
        Assert.IsNotNull(exC2);

      }
      finally {
        sqlConnection.Rollback();
      }
    }

    [Test]
    public void TestDeleteFrom()
    {
      var nativeSql = "DELETE FROM Production.Product "
        +"FROM Production.Product AS p "
          +"INNER JOIN Purchasing.ProductVendor AS pv "
            +"ON p.ProductID = pv.ProductID AND pv.VendorID = 0;";

      var product = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"]);
      var p = SqlDml.TableRef(Catalog.Schemas["Production"].Tables["Product"], "p");
      var pv = SqlDml.TableRef(Catalog.Schemas["Purchasing"].Tables["ProductVendor"], "pv");

      var delete = SqlDml.Delete(product);
      delete.From = p.InnerJoin(pv, p["ProductID"] == pv["ProductID"] && pv["VendorID"] == 0);

      Assert.IsTrue(CompareExecuteNonQuery(nativeSql, delete));
    }

    /*

SELECT EmpSSN AS "Employee Social Security Number "
FROM EmpTable

SELECT VendorID, [164] AS Emp1, [198] AS Emp2, [223] AS Emp3, [231] AS Emp4, [233] AS Emp5
FROM
(SELECT PurchaseOrderID, EmployeeID, VendorID
FROM Purchasing.PurchaseOrderHeader) p
PIVOT
(
COUNT (PurchaseOrderID)
FOR EmployeeID IN
( [164], [198], [223], [231], [233] )
) AS pvt
ORDER BY VendorID

DECLARE complex_cursor CURSOR FOR
    SELECT a.EmployeeID
    FROM HumanResources.EmployeePayHistory AS a
    WHERE RateChangeDate <>
         (SELECT MAX(RateChangeDate)
          FROM HumanResources.EmployeePayHistory AS b
          WHERE a.EmployeeID = b.EmployeeID) ;
OPEN complex_cursor;
FETCH FROM complex_cursor;
DELETE FROM HumanResources.EmployeePayHistory
WHERE CURRENT OF complex_cursor;
CLOSE complex_cursor;
DEALLOCATE complex_cursor;

--batch2
BEGIN TRANSACTION
GO
USE AdventureWorks;
GO
CREATE TABLE dbo.mycompanies
(
 id_num int IDENTITY(100, 5),
 company_name nvarchar(100)
)
GO
INSERT mycompanies (company_name)
   VALUES (N'A Bike Store');
INSERT mycompanies (company_name)
   VALUES (N'Progressive Sports');
INSERT mycompanies (company_name)
   VALUES (N'Modular IsCyclic Systems');
INSERT mycompanies (company_name)
   VALUES (N'Advanced Bike Components');
INSERT mycompanies (company_name)
   VALUES (N'Metropolitan Sports Supply');
INSERT mycompanies (company_name)
   VALUES (N'Aerobic Exercise Company');
INSERT mycompanies (company_name)
   VALUES (N'Associated Bikes');
INSERT mycompanies (company_name)
   VALUES (N'Exemplary Cycles');
GO
SELECT id_num, company_name
FROM dbo.mycompanies
ORDER BY company_name ASC;
GO
COMMIT;
GO

-- Create the table.
CREATE TABLE TestTable (cola int, colb char(3))
GO
SET NOCOUNT ON
GO
-- Declare the variable to be used.
DECLARE @MyCounter int

-- Initialize the variable.
SET @MyCounter = 0

-- Test the variable to see if the loop is finished.
WHILE (@MyCounter < 26)
BEGIN
   -- Insert a row into the table.
   INSERT INTO TestTable VALUES
       -- Use the variable to provide the integer value
       -- for cola. Also use it to generate a unique letter
       -- for each row. Use the ASCII function to get the
       -- integer value of 'a'. Add @MyCounter. Use CHAR to
       -- convert the sum back to the character @MyCounter
       -- characters after 'a'.
       (@MyCounter,
        CHAR( ( @MyCounter + ASCII('a') ) )
       )
   -- Increment the variable to count this iteration
   -- of the loop.
   SET @MyCounter = @MyCounter + 1
END
GO
SET NOCOUNT OFF
GO

-- Create a procedure that takes one input parameter
-- and returns one output parameter and a return code.
CREATE PROCEDURE SampleProcedure @EmployeeIDParm INT,
         @MaxTotal INT OUTPUT
AS
-- Declare and initialize a variable to hold @@ERROR.
DECLARE @ErrorSave INT
SET @ErrorSave = 0

-- Do a SELECT using the input parameter.
SELECT FirstName, LastName, JobTitle
FROM HumanResources.vEmployee
WHERE EmployeeID = @EmployeeIDParm

-- Save any nonzero @@ERROR value.
IF (@@ERROR <> 0)
   SET @ErrorSave = @@ERROR

-- Set a value in the output parameter.
SELECT @MaxTotal = MAX(TotalDue)
FROM Sales.SalesOrderHeader;

IF (@@ERROR <> 0)
   SET @ErrorSave = @@ERROR

-- Returns 0 if neither SELECT statement had
-- an error; otherwise, returns the last error.
RETURN @ErrorSave

-- Returns 0 if neither SELECT statement had
-- an error; otherwise, returns the last error.
RETURN @ErrorSave
GO


-- Declare the variables for the return code and output parameter.
DECLARE @ReturnCode INT
DECLARE @MaxTotalVariable INT

-- Execute the stored procedure and specify which variables
-- are to receive the output parameter and return code values.
EXEC @ReturnCode = SampleProcedure @EmployeeIDParm = 19,
   @MaxTotal = @MaxTotalVariable OUTPUT

-- Show the values returned.
PRINT ' '
PRINT 'Return code = ' + CAST(@ReturnCode AS CHAR(10))
PRINT 'Maximum Quantity = ' + CAST(@MaxTotalVariable AS CHAR(10))
GO

DECLARE @Group nvarchar(50), @Sales money;
SET @Group = N'North America';
SET @Sales = 2000000;
SET NOCOUNT OFF;
SELECT FirstName, LastName, SalesYTD
FROM Sales.vSalesPerson
WHERE TerritoryGroup = @Group and SalesYTD >= @Sales;


DECLARE @MyTableVar table(
    EmpID int NOT NULL,
    OldVacationHours int,
    NewVacationHours int,
    ModifiedDate datetime);
UPDATE TOP (10) HumanResources.Employee
SET VacationHours = VacationHours * 1.25
OUTPUT INSERTED.EmployeeID,
       DELETED.VacationHours,
       INSERTED.VacationHours,
       INSERTED.ModifiedDate
INTO @MyTableVar;
--Display the result set of the table variable.
SELECT EmpID, OldVacationHours, NewVacationHours, ModifiedDate
FROM @MyTableVar;
GO
--Display the result set of the table.
--Note that ModifiedDate reflects the value generated by an
--AFTER UPDATE trigger.
SELECT TOP (10) EmployeeID, VacationHours, ModifiedDate
FROM HumanResources.Employee;
GO
*/
  }
}
