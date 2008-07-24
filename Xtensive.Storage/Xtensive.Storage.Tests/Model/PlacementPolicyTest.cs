// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2007.11.30

using NUnit.Framework;
using Xtensive.Storage.Attributes;
using Xtensive.Storage.Building;
using Xtensive.Storage.Building.Definitions;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Generators;
using Xtensive.Storage.Model;
using Xtensive.Storage.Tests.Model.DefaultPlacement;

namespace Xtensive.Storage.Tests.Model.DefaultPlacement
{
  [Index("Name")]
  public interface IHasName : IEntity
  {
    [Field]
    string Name { get; set; }
  }

  [Index("Name")]
  public interface IHasName2 : IEntity
  {
    [Field]
    string Name { get; set; }
  }

  [MaterializedView(MappingName = "Creatures")]
  public interface ICreature : IHasName
  {
  }

  [Entity(MappingName = "A-Root")]
  [HierarchyRoot(typeof (IncrementalGenerator), "ID")]
  public class A : Entity
  {
    [Field]
    public long ID { get; set; }
  }

  [Entity]
  [Index("Tag")]
  [Index("Name")]
  // TODO: Alex Kochetov: Log error if duplicate index is specified.
  public class B : A, IHasName, IHasName2
  {
    [Field]
    public string Name { get; set; }

    [Field]
    public int Tag { get; set; }
  }

  [Entity]
  [Index("Age")]
  public class C : A
  {
    [Field(MappingName = "MyAge")]
    public int Age { get; set; }
  }

  [Entity]
  [Index("Tag")]
  public class D : C, ICreature
  {
    [Field]
    public string Name { get; set; }

    [Field]
    public virtual string Tag { get; set; }
  }

  [Entity]
  public class E : D
  {
    [Field]
    public override string Tag
    {
      get { return base.Tag; }
      set { base.Tag = value; }
    }
  }

  [Entity]
  public class F : A, ICreature, IHasName2
  {
    [Field]
    string IHasName.Name
    {
      get { return Name; }
      set { Name = value; }
    }

    [Field]
    public string Name { get; set; }
  }

  [Index("Name")]
  [Entity]
  [HierarchyRoot(typeof (IncrementalGenerator), "ID")]
  public class X : Entity
  {
    [Field]
    public long ID { get; set; }

    [Field]
    public string Name { get; set; }
  }

  [Entity]
  public class Y : X
  {
  }

  [Entity]
  public class Z : Y
  {
  }
}

namespace Xtensive.Storage.Tests.Model
{
  internal class AtRootPlacementBuilder : IDomainBuilder
  {
    protected virtual InheritanceSchema InheritanceSchema
    {
      get { return InheritanceSchema.SingleTableInheritance; }
    }

    public void Build(BuildingContext context, DomainDef domain)
    {
      foreach (HierarchyDef hierarchyDef in domain.Hierarchies) {
        hierarchyDef.Schema = InheritanceSchema;
      }
    }
  }

  internal class AtDescendantsPlacementBuilder : AtRootPlacementBuilder
  {
    protected override InheritanceSchema InheritanceSchema
    {
      get { return InheritanceSchema.ConcreteTableInheritance; }
    }
  }

  [TestFixture]
  public class PlacementPolicyTest
  {
    [Test]
    public void   Default()
    {
      DomainConfiguration configuration = new DomainConfiguration(@"memory://localhost/DefaultPlacement");
      configuration.Types.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.Model.DefaultPlacement");
      configuration.NamingConvention.LetterCasePolicy = LetterCasePolicy.Uppercase;
      configuration.NamingConvention.NamingRules = NamingRules.UnderscoreDots | NamingRules.UnderscoreHyphens;
      configuration.NamingConvention.NamespacePolicy = NamespacePolicy.UseHash;
      configuration.NamingConvention.NamespaceSynonyms.Add("Xtensive.Storage.Tests.Model.DefaultPlacement", "X");
      Domain domain = Domain.Build(configuration);
      VerifyModel(domain, InheritanceSchema.ClassTableInheritance);
    }

    [Test]
    public void AtRoot()
    {
      DomainConfiguration configuration = new DomainConfiguration(@"memory://localhost/DefaultPlacement");
      configuration.Types.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.Model.DefaultPlacement");
      configuration.Builders.Add(typeof (AtRootPlacementBuilder));
      Domain domain = Domain.Build(configuration);
      VerifyModel(domain, InheritanceSchema.SingleTableInheritance);
    }

    [Test]
    public void AtDescendants()
    {
      DomainConfiguration configuration = new DomainConfiguration(@"memory://localhost/DefaultPlacement");
      configuration.Types.Register(typeof (A).Assembly, "Xtensive.Storage.Tests.Model.DefaultPlacement");
      configuration.Builders.Add(typeof (AtDescendantsPlacementBuilder));
      Domain domain = Domain.Build(configuration);
      VerifyModel(domain, InheritanceSchema.ConcreteTableInheritance);
    }

    private void VerifyModel(Domain domain, InheritanceSchema policy)
    {
      domain.Model.Dump();
      domain.Model.Types.Dump();

      foreach (TypeInfo type in domain.Model.Types) {
        foreach (IndexInfo indexInfo in type.Indexes) {
          if (indexInfo.IsPrimary)
            Assert.AreEqual(1, indexInfo.KeyColumns.Count, "Type: {0}; index: {1}", indexInfo.ReflectedType.Name,
              indexInfo.Name, type.Name);
          else
            Assert.AreEqual(2, indexInfo.ValueColumns.Count, "Type: {0}; index: {1}", indexInfo.ReflectedType.Name,
              indexInfo.Name, type.Name);
        }
      }

      if (policy==InheritanceSchema.SingleTableInheritance) {
        TypeInfo typeInfo = domain.Model.Types[typeof (A)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(9, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsFalse(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.AreEqual(8, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (B)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(3, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Filtered) > 0);
        Assert.AreEqual(3, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);
        Assert.IsNotNull(typeInfo.Indexes.GetIndex("Name"));
        Assert.IsTrue(typeInfo.Indexes.GetIndex("Name").IsVirtual);


        typeInfo = domain.Model.Types[typeof (C)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(2, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Filtered) > 0);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (D)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(3, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Filtered) > 0);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);
      }
      else if (policy==InheritanceSchema.ConcreteTableInheritance) {
        TypeInfo typeInfo = domain.Model.Types[typeof (A)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(2, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.AreEqual(8, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);


        typeInfo = domain.Model.Types[typeof (B)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(3, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsFalse(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.AreEqual(3, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (C)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(4, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (D)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(8, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);
      }
      else {
        TypeInfo typeInfo = domain.Model.Types[typeof (A)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(2, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Join) > 0);
        Assert.AreEqual(8, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);


        typeInfo = domain.Model.Types[typeof (B)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(4, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Join) > 0);
        Assert.AreEqual(3, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (C)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(3, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Join) > 0);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (D)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(5, typeInfo.Indexes.Count);
        Assert.IsTrue(typeInfo.AffectedIndexes.Count > 0);
        Assert.IsNotNull(typeInfo.Indexes.PrimaryIndex);
        Assert.IsTrue(typeInfo.Indexes.PrimaryIndex.IsVirtual);
        Assert.IsTrue((typeInfo.Indexes.PrimaryIndex.Attributes & IndexAttributes.Join) > 0);
        Assert.AreEqual(4, typeInfo.Indexes.PrimaryIndex.ValueColumns.Count);

        typeInfo = domain.Model.Types[typeof (IHasName)];
        Assert.IsNotNull(typeInfo);
        Assert.AreEqual(2, typeInfo.Indexes.Count);
      }
    }
  }
}