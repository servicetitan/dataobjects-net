// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Nick Svetlov
// Created:    2008.06.11

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Issues.CustomerBug1Model;
using Xtensive.Orm.Tests.Storage.ActivatorModel;

namespace Xtensive.Orm.Tests.Storage.ActivatorModel
{
  [Serializable]
  [HierarchyRoot]
  public abstract class Ancestor : Entity
  {
    [Field, Key]
    public int ID { get; private set; }
  }

  [Serializable]
  public class Descendant : Ancestor
  {
    [Field]
    public int Number { get; set; }
  }

  [Serializable]
  [HierarchyRoot]
  public class InitializebleClass : Entity
  {
    public Lock syncRoot = new();

    protected override void OnInitialize()
    {
      base.OnInitialize();
      syncRoot = new();
    }

    public InitializebleClass()
    {
      syncRoot = new();
      // Логика, которая юзает syncRoot.
      Assert.That(syncRoot != null, Is.True);
    }

    [Field, Key]
    public int ID { get; private set; }
  }

  
}

namespace Xtensive.Orm.Tests.Storage
{
  

  public class  ActivatorTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      DomainConfiguration config = base.BuildConfiguration();
      config.Types.RegisterCaching(Assembly.GetExecutingAssembly(), "Xtensive.Orm.Tests.Storage.ActivatorModel");
      return config;
    }

    [Test]
    public void TestFieldInitializer()
    {
      using (var session = Domain.OpenSession())
      {
        using (var t = session.OpenTransaction())
        {
          var obj1 = new  InitializebleClass();
          Assert.That(obj1.syncRoot != null, Is.True);
          t.Complete();
        }
      }
      using (var session = Domain.OpenSession())
      {
        using (var t = session.OpenTransaction())
        {
          var obj1 = session.Query.All<InitializebleClass>().First();
          Assert.IsNotNull(obj1);
          Assert.That(obj1.syncRoot != null, Is.True);
          t.Complete();
        }
      }
    }

    [Test]
    public void Test()
    {
      Key key;
      using (var session = Domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var descendant = new Descendant();
          key = descendant.Key;          
          t.Complete();
        }        
      }
      using (var session = Domain.OpenSession()) {
        using (session.OpenTransaction()) {
          var ancestor = session.Query.SingleOrDefault<Ancestor>(key);
          Assert.IsNotNull(ancestor);

          var descendant = session.Query.SingleOrDefault<Descendant>(key);
          Assert.IsNotNull(descendant);
        }        
      }
    }
  }
}
