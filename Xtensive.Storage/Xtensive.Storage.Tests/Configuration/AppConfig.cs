// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.08.06

using System;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using System.Reflection;

namespace Xtensive.Storage.Tests.Configuration
{
  [TestFixture]
  public class AppConfig
  {
    [Test]
    public void DomainConfig()
    {
      var configuration = Xtensive.Storage.Configuration.Configuration.Load("AppConfigTest");
      Assert.AreEqual(2, configuration.Domains.Count);
      var domainConfig = configuration.Domains[0];
      Log.Debug("SessionPoolSize: {0}", domainConfig.SessionPoolSize);
      Log.Debug("ConnectionInfo: {0}", domainConfig.ConnectionInfo);
      foreach (Type builder in domainConfig.Builders) {
        Log.Debug("Builder: {0}", builder.FullName);
      }
      foreach (Type type in domainConfig.Types) {
        Log.Debug("Type: {0}", type.FullName);
      }
      Log.Debug("NamingConvention.LetterCasePolicy: {0}", domainConfig.NamingConvention.LetterCasePolicy);
      Log.Debug("NamingConvention.NamespacePolicy: {0}", domainConfig.NamingConvention.NamespacePolicy);
      Log.Debug("NamingConvention.NamingRules: {0}", domainConfig.NamingConvention.NamingRules);
      foreach (KeyValuePair<string, string> namespaceSynonym in domainConfig.NamingConvention.NamespaceSynonyms) {
        Log.Debug("NamingConvention.NamespaceSynonym (key, value): {0} {1}", namespaceSynonym.Key, namespaceSynonym.Value);
      }
      Log.Debug("Session settings. UserName: {0}, CacheSize: {1}", domainConfig.Session.UserName, domainConfig.Session.CacheSize);
      var manualConfig = new DomainConfiguration("memory://localhost/"){
          SessionPoolSize = 77,
          Name = "TestDomain1"
        };
      manualConfig.Builders.Add(typeof(string));
      manualConfig.Builders.Add(typeof(int));
      manualConfig.Types.Register(Assembly.Load("Xtensive.Storage.Tests"), "Xtensive.Storage.Tests");
      manualConfig.NamingConvention.LetterCasePolicy = LetterCasePolicy.Uppercase;
      manualConfig.NamingConvention.NamespacePolicy = NamespacePolicy.Hash;
      manualConfig.NamingConvention.NamingRules = NamingRules.UnderscoreDots;
      manualConfig.NamingConvention.NamespaceSynonyms.Add("Xtensive.Storage", "XS");
      manualConfig.NamingConvention.NamespaceSynonyms.Add("Xtensive.Messaging", "XM");
      manualConfig.NamingConvention.NamespaceSynonyms.Add("Xtensive.Indexing", "XI");
      manualConfig.Session.CacheSize = 123;
      manualConfig.Session.UserName = "TestUserName";
      Assert.AreEqual(domainConfig, manualConfig);
    }

    
  }
}