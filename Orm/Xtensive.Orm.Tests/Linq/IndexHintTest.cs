using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.ObjectModel;
using Xtensive.Orm.Tests.ObjectModel.ChinookDO;

namespace Xtensive.Orm.Tests.Linq
{
  [Category("Linq")]
  [TestFixture]
  public class IndexHintTest : ChinookDOModelTest
  {
    [Test]
    public void IndexHintInSubQuery()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("Customer.IX_FirstName")
            .Where(c => c.Invoices.WithIndexHint("PK_Invoice").Any(x => x.Status == InvoiceStatus.Paid))
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Invoice", "PK_Invoice"));
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }
    
    [Test]
    public void IndexHintWithJoin()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("Customer.IX_FirstName")
            .Where(c => c.CustomerId == 1)
            .Join(session.Query.All<Invoice>().WithIndexHint("PK_Invoice"), c => c, i => i.Customer, (c, i) => c)
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Invoice", "PK_Invoice"));
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }

    [Test]
    public void IndexHintInWithLock()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("Customer.IX_FirstName")
            .Where(c => c.CustomerId == 1)
            .Join(session.Query.All<Invoice>().WithIndexHint("PK_Invoice"), c => c, i => i.Customer, (c, i) => c)
            .Lock(LockMode.Update, LockBehavior.Wait)
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Invoice", "PK_Invoice"));
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }

    [Test]
    public void IndexHintInUnion()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var left = session.Query.All<Customer>().WithIndexHint("PK_Customer");
        var right = session.Query.All<Customer>().WithIndexHint("Customer.IX_FirstName");

        var query = left.Union(right);

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "PK_Customer"));
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }
    
    [Test]
    public void NonExistingIndexHint()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("Customer.IX_NotExists")
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsFalse(CheckIndexHint(queryString, "Customer", "Customer.IX_NotExists"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }
    
    [Test]
    public void MultipleIndexHints()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("PK_Customer")
            .WithIndexHint("Customer.IX_FirstName")
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "PK_Customer"));
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }
    
    [Test]
    public void DuplicatingIndexHints()
    {
      var session = Session.Demand();
      using (var innerTx = session.OpenTransaction(TransactionOpenMode.New)) {
        var query = session.Query.All<Customer>()
            .WithIndexHint("Customer.IX_FirstName")
            .WithIndexHint("Customer.IX_FirstName")
          ;

        var queryFormatter = session.Services.Demand<QueryFormatter>();
        var queryString = queryFormatter.ToSqlString(query);
        Console.WriteLine(queryString);
        
        Assert.IsTrue(CheckIndexHint(queryString, "Customer", "Customer.IX_FirstName"));
        Assert.DoesNotThrow(() => query.Run());
      }
    }

    private static bool CheckIndexHint(string query, string table, string index)
    {
      if (StorageProviderInfo.Instance.CheckProviderIs(StorageProvider.SqlServer)) {
        var pattern = $"\\[{table}\\] \\[.\\] WITH \\(.*INDEX=\\[{index}\\].*\\)";
        return Regex.IsMatch(query, pattern);
      }

      return true;
    }
  }
}