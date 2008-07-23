// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.06.16

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Storage.Attributes;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.KeyProviders;

namespace Xtensive.Storage.Tests.Model.ReferenceTestsModel
{
  #region SelfReferenced Entity

  [HierarchyRoot(typeof (Int32Generator), "Id")]
  public class A : Entity
  {
    [Field]
    public int Id { get; set; }

    [Field]
    public A Parent { get; set; }
  }

  #endregion

  #region SelfContained Structure

  public class SelfContained : Structure
  {
    [Field]
    public SelfContained Value { get; set; }
  }

  #endregion

  #region Cyclic Referenced Structures

  public class S1 : Structure
  {
    [Field]
    public S2 Value { get; set; }
  }

  public class S2 : Structure
  {
    [Field]
    public S3 Value { get; set; }
  }

  public class S3 : Structure
  {
    [Field]
    public S1 Value { get; set; }
  }

  #endregion

  #region Cyclic Referenced Structures with Inheritance

  public class Parent : Structure
  {
    [Field]
    public Child Value { get; set; }
  }

  public class Child : Parent
  {
  }

  #endregion
}

namespace Xtensive.Storage.Tests.Model
{
  [TestFixture]
  public class ReferenceTests
  {
    [Test]
    public void MainTest()
    {
      DomainConfiguration config = new DomainConfiguration("memory://localhost/Bugs");
      config.Types.Register(Assembly.GetExecutingAssembly(), "Xtensive.Storage.Tests.Model.ReferenceTestsModel");
      try {
        Domain.Build(config);
      }
      catch (AggregateException e) {
        Assert.AreEqual(3, e.Exceptions.Count());
      }
    }
  }
}