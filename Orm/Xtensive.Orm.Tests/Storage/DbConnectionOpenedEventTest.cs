using System.Data.Common;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Storage.DbConnectionOpenedEventTestModel;

namespace Xtensive.Orm.Tests.Storage.DbConnectionOpenedEventTestModel
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
  public class DbConnectionOpenedEventTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    [Test]
    public void FiresOnceOnGenuineOpenTest()
    {
      using (var session = Domain.OpenSession()) {
        var openedCount = 0;
        DbConnection openedConnection = null;
        session.Events.DbConnectionOpened += (_, args) => {
          openedCount++;
          openedConnection = args.Connection;
        };

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "abc" };
          session.SaveChanges();
          transaction.Complete();
        }

        Assert.That(openedCount, Is.EqualTo(1));
        Assert.That(openedConnection, Is.Not.Null);
      }
    }

    [Test]
    public void DoesNotFireAgainOnAlreadyOpenConnectionTest()
    {
      using (var session = Domain.OpenSession()) {
        var openedCount = 0;
        session.Events.DbConnectionOpened += (_, _) => openedCount++;

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "abc" };
          session.SaveChanges();
          _ = new TestEntity { Text = "def" };
          session.SaveChanges();
          transaction.Complete();
        }

        Assert.That(openedCount, Is.EqualTo(1));
      }
    }

    [Test]
    public void FiresAgainOnReopenAfterCloseTest()
    {
      using (var session = Domain.OpenSession()) {
        var openedCount = 0;
        session.Events.DbConnectionOpened += (_, _) => openedCount++;

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "abc" };
          session.SaveChanges();
          transaction.Complete();
        }

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "def" };
          session.SaveChanges();
          transaction.Complete();
        }

        Assert.That(openedCount, Is.EqualTo(2));
      }
    }

    [Test]
    public async System.Threading.Tasks.Task FiresOnceOnGenuineOpenAsyncTest()
    {
      // Domain.OpenSession() defers the connection open lazily, unlike OpenSessionAsync() which opens
      // eagerly as part of session creation, before a caller has a chance to subscribe.
      using (var session = Domain.OpenSession()) {
        var openedCount = 0;
        session.Events.DbConnectionOpened += (_, _) => openedCount++;

        await using (var transaction = await session.OpenTransactionAsync()) {
          _ = new TestEntity { Text = "abc" };
          await session.SaveChangesAsync();
          transaction.Complete();
        }

        Assert.That(openedCount, Is.EqualTo(1));
      }
    }

    [Test]
    public void FiresForNonTransactionalReadsSessionTest()
    {
      var sessionConfiguration = new SessionConfiguration(SessionOptions.ServerProfile | SessionOptions.NonTransactionalReads);

      using (var session = Domain.OpenSession(sessionConfiguration)) {
        var openedCount = 0;
        session.Events.DbConnectionOpened += (_, _) => openedCount++;

        _ = session.Query.All<TestEntity>().Any();

        Assert.That(openedCount, Is.EqualTo(1));
      }
    }
  }
}
