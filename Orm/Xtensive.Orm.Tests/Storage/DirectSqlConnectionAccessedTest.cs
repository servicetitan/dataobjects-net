using System.Data.Common;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Services;
using Xtensive.Orm.Tests.Storage.DirectSqlConnectionAccessedTestModel;

namespace Xtensive.Orm.Tests.Storage.DirectSqlConnectionAccessedTestModel
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
  public class DirectSqlConnectionAccessedTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(TestEntity));
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    [Test]
    public void AccessingConnectionFiresTheHookTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var hookCallCount = 0;
        DbConnection hookConnection = null;
        session.Events.RawConnectionAccessed += (_, args) => {
          hookCallCount++;
          hookConnection = args.Connection;
        };

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        var connection = directSql.Connection;

        Assert.That(hookCallCount, Is.EqualTo(1));
        Assert.That(hookConnection, Is.SameAs(connection));

        transaction.Complete();
      }
    }

    [Test]
    public void EachAccessFiresTheHookAgainTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var hookCallCount = 0;
        session.Events.RawConnectionAccessed += (_, _) => hookCallCount++;

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        _ = directSql.Connection;
        _ = directSql.Connection;

        Assert.That(hookCallCount, Is.EqualTo(2));

        transaction.Complete();
      }
    }

    [Test]
    public void InvokesEachSubscriberTest()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var callOrder = new List<int>();
        session.Events.RawConnectionAccessed += (_, _) => callOrder.Add(1);
        session.Events.RawConnectionAccessed += (_, _) => callOrder.Add(2);

        var directSql = session.Services.Demand<DirectSqlAccessor>();
        _ = directSql.Connection;

        Assert.That(callOrder, Is.EqualTo(new[] { 1, 2 }));

        transaction.Complete();
      }
    }
  }
}
