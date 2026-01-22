// Copyright (C) 2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created to test fix for SQL Server batch separation when CREATE INDEX with WHERE clause
// references a column added in the same batch.

using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Orm.Upgrade;
using V1 = Xtensive.Orm.Tests.Issues.Issue_SqlServerFilteredIndexWithNewColumn.Model.Version1;
using V2 = Xtensive.Orm.Tests.Issues.Issue_SqlServerFilteredIndexWithNewColumn.Model.Version2;

namespace Xtensive.Orm.Tests.Issues.Issue_SqlServerFilteredIndexWithNewColumn
{
  namespace Model
  {
    namespace Version1
    {
      [HierarchyRoot]
      public class TestEntity : Entity
      {
        [Key, Field]
        public int Id { get; private set; }

        [Field]
        public string Name { get; set; }
      }

      public class Upgrader : UpgradeHandler
      {
        protected override string DetectAssemblyVersion() => "1";
      }
    }

    namespace Version2
    {
      // Filtered index that references the newly added Z column
      // This would fail in SQL Server if ALTER TABLE and CREATE INDEX are in the same batch
      [HierarchyRoot]
      [Index("Name", Filter = nameof(FilterExpression))]
      public class TestEntity : Entity
      {
        [Key, Field]
        public int Id { get; private set; }

        [Field]
        public string Name { get; set; }

        // New field added in version 2
        [Field]
        public int? Z { get; set; }

        public static Expression<Func<TestEntity, bool>> FilterExpression() =>
          entity => entity.Z != null;
      }

      public class Upgrader : UpgradeHandler
      {
        public override bool CanUpgradeFrom(string oldVersion) => true;

        protected override string DetectAssemblyVersion() => "2";

        protected override void AddUpgradeHints(ISet<UpgradeHint> hints)
        {
          _ = hints.Add(new RenameTypeHint(typeof(V1.TestEntity).FullName, typeof(TestEntity)));
        }
      }
    }
  }

  [TestFixture]
  public class UpgradeTest
  {
    [Test]
    public void UpgradeWithFilteredIndexOnNewColumnTest()
    {
      // Build initial domain with version 1
      using (var domain = BuildDomain("1", DomainUpgradeMode.Recreate))
      using (var session = domain.OpenSession())
      using (var tx = session.OpenTransaction())
      {
        var entity = new V1.TestEntity { Name = "Test" };
        tx.Complete();
      }

      // Upgrade to version 2 - this should succeed with the fix
      // Without the fix, this would fail with "Invalid column name 'Z'"
      using (var domain = BuildDomain("2", DomainUpgradeMode.Perform))
      using (var session = domain.OpenSession())
      using (var tx = session.OpenTransaction())
      {
        var entity = session.Query.All<V2.TestEntity>().FirstOrDefault();
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Name, Is.EqualTo("Test"));
        
        // Verify the new field exists
        var newEntity = new V2.TestEntity { Name = "Test2", Z = 42 };
        Assert.That(newEntity.Z, Is.EqualTo(42));
        tx.Complete();
      }
    }

    [Test]
    public async Task UpgradeWithFilteredIndexOnNewColumnAsyncTest()
    {
      // Build initial domain with version 1
      using (var domain = BuildDomain("1", DomainUpgradeMode.Recreate))
      using (var session = domain.OpenSession())
      using (var tx = session.OpenTransaction())
      {
        var entity = new V1.TestEntity { Name = "Test" };
        tx.Complete();
      }

      // Upgrade to version 2 - this should succeed with the fix
      using (var domain = await BuildDomainAsync("2", DomainUpgradeMode.Perform))
      using (var session = domain.OpenSession())
      using (var tx = session.OpenTransaction())
      {
        var entity = session.Query.All<V2.TestEntity>().FirstOrDefault();
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Name, Is.EqualTo("Test"));
        tx.Complete();
      }
    }

    private Domain BuildDomain(string version, DomainUpgradeMode upgradeMode)
    {
      var ns = typeof(V1.TestEntity).Namespace;
      var nsPrefix = ns.Substring(0, ns.Length - 1);

      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = upgradeMode;
      configuration.Types.RegisterCaching(Assembly.GetExecutingAssembly(), nsPrefix + version);
      return Domain.Build(configuration);
    }

    private async Task<Domain> BuildDomainAsync(string version, DomainUpgradeMode upgradeMode)
    {
      var ns = typeof(V1.TestEntity).Namespace;
      var nsPrefix = ns.Substring(0, ns.Length - 1);

      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = upgradeMode;
      configuration.Types.RegisterCaching(Assembly.GetExecutingAssembly(), nsPrefix + version);
      return await Domain.BuildAsync(configuration);
    }
  }
}
