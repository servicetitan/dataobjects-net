// Copyright (C) 2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Storage.AsyncSessionInitializationSqlTestModel;

namespace Xtensive.Orm.Tests.Storage
{
  namespace AsyncSessionInitializationSqlTestModel
  {
    [HierarchyRoot]
    public class TestEntity : Entity
    {
      [Key, Field]
      public long Id { get; private set; }
    }
  }

  [TestFixture]
  public class AsyncSessionInitializationSqlTest : AutoBuildTest
  {
    private const string InitializationSql = "SET CONTEXT_INFO 0xDECAFBAD";
    private const int OuterTransactionCount = 3;

    protected override Xtensive.Orm.Configuration.DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      return configuration;
    }

    [Test]
    public async Task InitializationSqlRunsOncePerOuterTransaction()
    {
      Require.ProviderIs(StorageProvider.SqlServer, "SET CONTEXT_INFO is SQL Server-specific");

      await using var session = await Domain.OpenSessionAsync();

      var initializationSqlExecutions = 0;
      session.Events.DbCommandExecuting += (_, e) => {
        if (e.Command.CommandText == InitializationSql) {
          initializationSqlExecutions++;
        }
      };

      // Re-registering the same script each outer transaction reproduces the monolith's tagging pattern
      // and is what the async prepare must drain rather than accumulate.
      session.Events.TransactionOpened += (_, e) => {
        if (!e.Transaction.IsNested) {
          session.Services.Demand<DirectSqlAccessor>().RegisterInitializationSql(InitializationSql);
        }
      };

      for (var i = 0; i < OuterTransactionCount; i++) {
        await using var tx = await session.OpenTransactionAsync();
        // Forces the deferred connection open, running the queued script on the async prepare path.
        _ = await session.Query.All<TestEntity>().CountAsync();
        tx.Complete();
      }

      // An un-drained queue re-runs every prior script, costing 1+2+3 executions instead of one each.
      Assert.That(initializationSqlExecutions, Is.EqualTo(OuterTransactionCount));
    }
  }
}
