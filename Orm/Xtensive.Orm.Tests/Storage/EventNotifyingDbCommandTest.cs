using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Storage.EventNotifyingDbCommandTestModel;

namespace Xtensive.Orm.Tests.Storage.EventNotifyingDbCommandTestModel
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
  public class EventNotifyingDbCommandTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    protected override void PopulateData()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        _ = new TestEntity { Text = "abc" };
        session.SaveChanges();
        transaction.Complete();
      }
    }

    [Test]
    public void ForwardsMembersFaithfullyTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT 1";
          command.CommandType = CommandType.Text;
          command.CommandTimeout = 42;
          var parameter = command.CreateParameter();
          parameter.ParameterName = "@p";
          parameter.Value = 1;
          command.Parameters.Add(parameter);

          Assert.That(command.CommandText, Is.EqualTo("SELECT 1"));
          Assert.That(command.CommandType, Is.EqualTo(CommandType.Text));
          Assert.That(command.CommandTimeout, Is.EqualTo(42));
          Assert.That(command.Parameters.Count, Is.EqualTo(1));
          Assert.That(command.Connection, Is.Not.Null);
          Assert.That(command.Transaction, Is.Not.Null);
        }
        transaction.Complete();
      }
    }

    [Test]
    public void ExecuteNonQueryFiresNotificationsOnceTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var executingCount = 0;
        var executedCount = 0;
        session.Events.DbCommandExecuting += (_, _) => executingCount++;
        session.Events.DbCommandExecuted += (_, _) => executedCount++;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT 1";
          _ = command.ExecuteNonQuery();
        }

        Assert.That(executingCount, Is.EqualTo(1));
        Assert.That(executedCount, Is.EqualTo(1));
        transaction.Complete();
      }
    }

    [Test]
    public async Task ExecuteNonQueryAsyncFiresNotificationsOnceTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var executingCount = 0;
        var executedCount = 0;
        session.Events.DbCommandExecuting += (_, _) => executingCount++;
        session.Events.DbCommandExecuted += (_, _) => executedCount++;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT 1";
          _ = await command.ExecuteNonQueryAsync();
        }

        Assert.That(executingCount, Is.EqualTo(1));
        Assert.That(executedCount, Is.EqualTo(1));
        transaction.Complete();
      }
    }

    [Test]
    public void ExecuteScalarFiresNotificationsOnceTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var executingCount = 0;
        var executedCount = 0;
        session.Events.DbCommandExecuting += (_, _) => executingCount++;
        session.Events.DbCommandExecuted += (_, _) => executedCount++;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT 1";
          var result = command.ExecuteScalar();
          Assert.That(Convert.ToInt32(result), Is.EqualTo(1));
        }

        Assert.That(executingCount, Is.EqualTo(1));
        Assert.That(executedCount, Is.EqualTo(1));
        transaction.Complete();
      }
    }

    [Test]
    public void ExecuteReaderFiresNotificationsOnceTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var executingCount = 0;
        var executedCount = 0;
        session.Events.DbCommandExecuting += (_, _) => executingCount++;
        session.Events.DbCommandExecuted += (_, _) => executedCount++;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT 1";
          using (var reader = command.ExecuteReader()) {
            while (reader.Read()) {
            }
          }
        }

        Assert.That(executingCount, Is.EqualTo(1));
        Assert.That(executedCount, Is.EqualTo(1));
        transaction.Complete();
      }
    }

    [Test]
    public void FailingCommandPropagatesExceptionUnwrappedTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        Exception notifiedException = null;
        session.Events.DbCommandExecuted += (_, args) => notifiedException = args.Exception;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        using (var command = directSql.CreateCommand()) {
          command.CommandText = "SELECT * FROM [NoSuchTable_EventNotifyingDbCommandTest]";
          var thrown = Assert.Catch(() => command.ExecuteNonQuery());

          Assert.That(thrown, Is.Not.InstanceOf<StorageException>());
          Assert.That(notifiedException, Is.SameAs(thrown));
        }
      }
    }

    [Test]
    public void CancelForwardsToInnerCommandTest()
    {
      var inner = new FakeDbCommand();
      var wrapper = new EventNotifyingDbCommand(null, inner);

      wrapper.Cancel();

      Assert.That(inner.CancelCalled, Is.True);
    }

    [Test]
    public void DisposeForwardsToInnerCommandTest()
    {
      var inner = new FakeDbCommand();
      var wrapper = new EventNotifyingDbCommand(null, inner);

      wrapper.Dispose();

      Assert.That(inner.DisposeCalled, Is.True);
    }

    private sealed class FakeDbCommand : DbCommand
    {
      public bool CancelCalled;
      public bool DisposeCalled;

      public override string CommandText { get; set; }
      public override int CommandTimeout { get; set; }
      public override CommandType CommandType { get; set; }
      public override UpdateRowSource UpdatedRowSource { get; set; }
      public override bool DesignTimeVisible { get; set; }
      protected override DbConnection DbConnection { get; set; }
      protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
      protected override DbTransaction DbTransaction { get; set; }

      public override void Cancel() => CancelCalled = true;

      protected override DbParameter CreateDbParameter() => throw new NotImplementedException();

      public override void Prepare()
      {
      }

      public override int ExecuteNonQuery() => throw new NotImplementedException();

      public override object ExecuteScalar() => throw new NotImplementedException();

      protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();

      protected override void Dispose(bool disposing)
      {
        if (disposing) {
          DisposeCalled = true;
        }
        base.Dispose(disposing);
      }
    }
  }
}
