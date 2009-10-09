// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.10.08

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Tests.PrefetchProcessorTest.Model;

namespace Xtensive.Storage.Tests.Storage
{
  [TestFixture]
  public sealed class PrefetchWithSmallCacheTest : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Sessions.Add(new SessionConfiguration(WellKnown.Sessions.Default));
      config.Sessions.Default.CacheType = SessionCacheType.LruWeak;
      config.Sessions.Default.CacheSize = 2;
      config.KeyCacheSize = 2;
      config.UpgradeMode = DomainUpgradeMode.Recreate;
      config.Types.Register(typeof(Supplier).Assembly, typeof(Supplier).Namespace);
      return config;
    }

    [TestFixtureSetUp]
    public override void TestFixtureSetUp()
    {
      base.TestFixtureSetUp();
      using (var session = Session.Open(Domain))
      using (var transactionScope = Transaction.Open()) {
        for (int i = 0; i < 111; i++)
          PrefetchProcessorTest.FillDataBase(session);
        transactionScope.Complete();
      }
    }

    [Test]
    public void SimpleTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Order>.All.Select(p => p.Key).ToList();
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (Transaction.Open()) {
        var prefetcher = keys.Prefetch<Order, Key>(key => key).Prefetch(o => o.Employee);
        var orderType = typeof (Order).GetTypeInfo();
        var employeeType = typeof (Employee).GetTypeInfo();
        var employeeField = orderType.Fields["Employee"];
        foreach (var key in prefetcher) {
          GC.Collect(2, GCCollectionMode.Forced);
          GC.WaitForPendingFinalizers();
          var orderState = session.EntityStateCache[key, true];
            PrefetchProcessorTest.AssertOnlySpecifiedColumnsAreLoaded(key, orderType, session,
              PrefetchProcessorTest.IsFieldToBeLoadedByDefault);
            var employeeKey = Key.Create<Person>(employeeField.Association.ExtractForeignKey(orderState.Tuple));
            PrefetchProcessorTest.AssertOnlySpecifiedColumnsAreLoaded(employeeKey, employeeType, session,
              PrefetchProcessorTest.IsFieldToBeLoadedByDefault);
        }
      }
    }

    [Test]
    public void UsingDelayedElementsTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Person>.All.AsEnumerable().Select(p => Key.Create<Person>(p.Key.Value)).ToList();
        Assert.IsTrue(keys.All(key => !key.IsTypeCached));
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (Transaction.Open()) {
        var prefetcher = keys.Prefetch<Person, Key>(key => key).PrefetchSingle(p => p.Name,
          name => new Customer() {Name = name}, customer => {
            customer.First().Remove();
            CollectGarbadge();
            return customer;
          });
        foreach (var key in prefetcher) {
          //var cachedKey = PrefetcherDelayedElementsTest.GetCachedKey(key, Domain);
          var orderState = session.EntityStateCache[key, true];
            PrefetchProcessorTest.AssertOnlySpecifiedColumnsAreLoaded(orderState.Key, orderState.Key.Type,
              session, PrefetchProcessorTest.IsFieldToBeLoadedByDefault);
        }
      }
    }

    [Test]
    public void PrefetchEntitySetTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Order>.All.AsEnumerable().Select(p => Key.Create<Order>(p.Key.Value)).ToList();
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var prefetcher = keys.Prefetch<Order, Key>(key => key).Prefetch(o => o.Details);
        var orderType = typeof (Order).GetTypeInfo();
        var detailsField = orderType.Fields["Details"];
        foreach (var key in prefetcher) {
          CollectGarbadge();
          //var cachedKey = PrefetcherDelayedElementsTest.GetCachedKey(key, Domain);
          PrefetchProcessorTest.AssertOnlySpecifiedColumnsAreLoaded(key, orderType, session,
            PrefetchProcessorTest.IsFieldToBeLoadedByDefault);
          EntitySetState state;
          session.Handler.TryGetEntitySetState(key, detailsField, out state);
          Assert.IsTrue(state.IsFullyLoaded);
          foreach (var detailKey in state) {
            Assert.IsTrue(detailKey.IsTypeCached);
            var detailState = session.EntityStateCache[detailKey, false];
            PrefetchProcessorTest.AssertOnlySpecifiedColumnsAreLoaded(detailKey, detailKey.Type, session,
              PrefetchProcessorTest.IsFieldToBeLoadedByDefault);
          }
        }
      }
    }

    private static void CollectGarbadge()
    {
      GC.Collect(2, GCCollectionMode.Forced);
      GC.WaitForPendingFinalizers();
    }
  }
}