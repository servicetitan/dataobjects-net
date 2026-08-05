using System.Data;
using System.Data.Common;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Storage.DirectSqlGetConnectionAsyncTestModel;

namespace Xtensive.Orm.Tests.Storage.DirectSqlGetConnectionAsyncTestModel
{
  [HierarchyRoot]
  public class TestEntity : Entity
  {
    [Key, Field]
    public int Id { get; set; }

    [Field]
    public string Text { get; set; }
  }
}

namespace Xtensive.Orm.Tests.Storage
{
  public class DirectSqlGetConnectionAsyncTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    [Test]
    public async Task OpensConnectionAndFiresHookOnceTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var hookCallCount = 0;
        DbConnection hookConnection = null;
        session.Events.RawConnectionAccessedAsync += (connection, _) => {
          hookCallCount++;
          hookConnection = connection;
          return Task.CompletedTask;
        };

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        var connection = await directSql.GetConnectionAsync();

        Assert.That(connection, Is.Not.Null);
        Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(hookCallCount, Is.EqualTo(1));
        Assert.That(hookConnection, Is.SameAs(connection));

        transaction.Complete();
      }
    }

    [Test]
    public async Task HookAwaitsItsWorkTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var hookCompleted = false;
        session.Events.RawConnectionAccessedAsync += async (_, cancellationToken) => {
          await Task.Delay(10, cancellationToken);
          hookCompleted = true;
        };

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        _ = await directSql.GetConnectionAsync();

        // GetConnectionAsync must not return until the hook's own await completes.
        Assert.That(hookCompleted, Is.True);

        transaction.Complete();
      }
    }

    [Test]
    public async Task TransactionReadAfterIsCheapNoOpTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var directSql = session.Services.Demand<DirectSqlAccessor>();
        _ = await directSql.GetConnectionAsync();

        Assert.That(directSql.Transaction, Is.Not.Null);

        transaction.Complete();
      }
    }

    [Test]
    public async Task InvokesEachSubscriberSequentiallyTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var callOrder = new List<int>();
        session.Events.RawConnectionAccessedAsync += async (_, _) => {
          await Task.Delay(20);
          callOrder.Add(1);
        };
        session.Events.RawConnectionAccessedAsync += async (_, _) => {
          await Task.Delay(1);
          callOrder.Add(2);
        };

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        _ = await directSql.GetConnectionAsync();

        Assert.That(callOrder, Is.EqualTo(new[] { 1, 2 }));

        transaction.Complete();
      }
    }
  }
}
