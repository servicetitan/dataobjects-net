// Copyright (C) 2025 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using NUnit.Framework;
using Xtensive.Orm.Model;
using Xtensive.Orm.Tests.Storage.ClassTableMultiPathInterfaceTestModel;

namespace Xtensive.Orm.Tests.Storage
{
  namespace ClassTableMultiPathInterfaceTestModel
  {
    [HierarchyRoot]
    public class Owner : Entity
    {
      [Field, Key]
      public int Id { get; private set; }
    }

    // Persistent interface that declares an Entity-reference field. Xtensive auto-emits an
    // FK_Owner index for it.
    public interface IOwned : IEntity
    {
      [Field]
      Owner Owner { get; }
    }

    // Sub-interface of IOwned. Its Indexes collection independently carries an inherited
    // copy of the FK_Owner IndexInfo (a different IndexInfo reference whose DeclaringIndex
    // chain bottoms out at IOwned.FK_Owner).
    public interface INamedAndOwned : IOwned
    {
      [Field(Length = 50)]
      string Label { get; set; }
    }

    public abstract class OwnedBase : Entity, IOwned
    {
      [Field, Key]
      public int Id { get; private set; }

      [Field]
      public Owner Owner { get; set; }
    }

    public abstract class NamedOwnedBase : OwnedBase, INamedAndOwned
    {
      public string Label { get; set; }
    }

    // typeof(Root).GetInterfaces() returns both IOwned (directly via OwnedBase) and
    // INamedAndOwned (via NamedOwnedBase). Xtensive's ModelInspector.FindInterfaces uses
    // .NET reflection's full transitive set, so DirectInterfaces on Root contains both.
    [HierarchyRoot(InheritanceSchema.ClassTable)]
    public class Root : NamedOwnedBase
    {
      [Field]
      public int Payload { get; set; }
    }

    // HierarchyInfo.UpdateState() demotes single-type hierarchies to ConcreteTable, which
    // would mask the ClassTable assertion below. A concrete descendant keeps Types.Count > 1.
    public class Leaf : Root
    {
      [Field]
      public int Extra { get; set; }
    }
  }

  /// <summary>
  /// Regression test for the ClassTable inherited-interface dedup bug.
  ///
  /// Before the fix, BuildClassTableIndexes used reference identity on the visited
  /// interfaceIndex:
  ///
  ///   if (type.Indexes.Any(i => i.DeclaringIndex == interfaceIndex)) continue;
  ///
  /// When the same logical interface index reached a ClassTable root through two paths
  /// in DirectInterfaces, each path yielded a distinct IndexInfo. The guard missed on
  /// the second visit; BuildInheritedIndex ran again and produced an index with the
  /// same name; NodeCollection.Add threw:
  ///
  ///   "Item with name 'Root.IOwned.FK_Owner' already exists in 'Root.Indexes'"
  ///
  /// at Domain.Build, before any DB work.
  ///
  /// Sibling BuildConcreteTableIndexes already handles this with a name-based check
  /// after BuildInheritedIndex (disposing the surplus IndexInfo); BuildSingleTableIndexes
  /// handles it with a chain-normalized identity check. ClassTable was the odd one out.
  /// </summary>
  [TestFixture]
  public class ClassTableMultiPathInterfaceTest
  {
    [Test]
    public void Build_ClassTableHierarchy_WithMultiPathInterfaceImpl_Succeeds()
    {
      var configuration = DomainConfigurationFactory.Create();
      configuration.Types.Register(typeof(Owner).Assembly, typeof(Owner).Namespace);
      using (var domain = Domain.Build(configuration)) {
        Assert.That(domain.Model.Types[typeof(Root)], Is.Not.Null);
        Assert.That(domain.Model.Types[typeof(Root)].Hierarchy.InheritanceSchema,
          Is.EqualTo(InheritanceSchema.ClassTable));
      }
    }
  }
}
