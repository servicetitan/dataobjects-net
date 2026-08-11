// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Storage.DbConnectionInitializingEventTestModel;

namespace Xtensive.Orm.Tests.Storage.DbConnectionInitializingEventTestModel
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
  public class DbConnectionInitializingEventTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    [Test]
    public void FiresOnceBeforeFirstCommandTest()
    {
      using (var session = Domain.OpenSession()) {
        var initCount = 0;
        var commandCount = 0;
        var commandRanBeforeInit = false;
        session.Events.DbCommandExecuting += (_, _) => commandCount++;
        session.Events.DbConnectionInitializing += (_, _) => {
          if (commandCount > 0) {
            commandRanBeforeInit = true;
          }
          initCount++;
        };

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "abc" };
          session.SaveChanges();
          transaction.Complete();
        }

        Assert.That(initCount, Is.EqualTo(1));
        Assert.That(commandRanBeforeInit, Is.False);
      }
    }

    [Test]
    public void DoesNotFireAgainOnAlreadyOpenConnectionTest()
    {
      using (var session = Domain.OpenSession()) {
        var initCount = 0;
        session.Events.DbConnectionInitializing += (_, _) => initCount++;

        using (var transaction = session.OpenTransaction()) {
          _ = new TestEntity { Text = "abc" };
          session.SaveChanges();
          _ = new TestEntity { Text = "def" };
          session.SaveChanges();
          transaction.Complete();
        }

        Assert.That(initCount, Is.EqualTo(1));
      }
    }

    [Test]
    public void FiresAgainOnReopenAfterCloseTest()
    {
      using (var session = Domain.OpenSession()) {
        var initCount = 0;
        session.Events.DbConnectionInitializing += (_, _) => initCount++;

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

        Assert.That(initCount, Is.EqualTo(2));
      }
    }

    [Test]
    public async Task FiresOnceOnGenuineOpenAsyncTest()
    {
      // Domain.OpenSession() defers the connection open lazily, unlike OpenSessionAsync() which opens
      // eagerly as part of session creation, before a caller has a chance to subscribe.
      using (var session = Domain.OpenSession()) {
        var initCount = 0;
        session.Events.DbConnectionInitializing += (_, _) => initCount++;

        await using (var transaction = await session.OpenTransactionAsync()) {
          _ = new TestEntity { Text = "abc" };
          await session.SaveChangesAsync();
          transaction.Complete();
        }

        Assert.That(initCount, Is.EqualTo(1));
      }
    }

    [Test]
    public void AppendedSqlRunsInTheInitBatchTest()
    {
      Require.ProviderIs(StorageProvider.SqlServer, "SET CONTEXT_INFO is SQL Server-specific");

      using (var session = Domain.OpenSession()) {
        session.Events.DbConnectionInitializing += (_, e) => e.AppendInitializationSql("SET CONTEXT_INFO 0xDECAFBAD");

        using (var transaction = session.OpenTransaction()) {
          using var command = session.Services.Demand<DirectSqlAccessor>().CreateCommand();
          command.CommandText = "SELECT CONTEXT_INFO()";
          var contextInfo = (byte[]) command.ExecuteScalar();

          Assert.That(contextInfo, Is.Not.Null);
          Assert.That(new[] { contextInfo[0], contextInfo[1], contextInfo[2], contextInfo[3] },
            Is.EqualTo(new byte[] { 0xDE, 0xCA, 0xFB, 0xAD }));
          transaction.Complete();
        }
      }
    }
  }
}
