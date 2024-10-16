// Copyright (C) 2019-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2019.01.28

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Collections;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Storage.Multinode.QueryCachingTestModel;
using Xtensive.Orm.Upgrade;

namespace Xtensive.Orm.Tests.Storage.Multinode.QueryCachingTestModel
{
  [HierarchyRoot]
  public class BaseTestEntity : Entity
  {
    [Field, Key]
    public int Id { get; set; }

    [Field]
    public string BaseName { get; set; }

    [Field]
    public string BaseOwnerNodeId { get; set; }

    public BaseTestEntity(Session session)
      : base(session)
    {
    }
  }

  public class MiddleTestEntity : BaseTestEntity
  {
    [Field]
    public string MiddleName { get; set; }

    [Field]
    public string MiddleOwnerNodeId { get; set; }

    public MiddleTestEntity(Session session)
      : base(session)
    {
    }
  }

  public class LeafTestEntity : MiddleTestEntity
  {
    [Field]
    public string LeafName { get; set; }

    [Field]
    public string LeafOwnerNodeId { get; set; }

    public LeafTestEntity(Session session)
      : base(session)
    {
    }
  }

  public class CustomUpgradeHandler : UpgradeHandler
  {
    public static Dictionary<string, int> TypeIdPerNode = new Dictionary<string, int>();
    public static string CurrentNodeId = WellKnown.DefaultNodeId;

    public override bool CanUpgradeFrom(string oldVersion) => true;

    public override void OnPrepare()
    {
      UpgradeContext.UserDefinedTypeMap.Add(typeof(BaseTestEntity).FullName, GetTypeId());
      UpgradeContext.UserDefinedTypeMap.Add(typeof(MiddleTestEntity).FullName, GetTypeId() + 1);
      UpgradeContext.UserDefinedTypeMap.Add(typeof(LeafTestEntity).FullName, GetTypeId() + 2);
    }

    private int GetTypeId()
    {
      if (CurrentNodeId == WellKnown.DefaultNodeId) {
        return 100;
      }

      return !TypeIdPerNode.TryGetValue(CurrentNodeId, out var result)
        ? throw new Exception(string.Format("No map for node {0}", CurrentNodeId))
        : result;
    }
  }
}

namespace Xtensive.Orm.Tests.Storage.Multinode
{
  public sealed class QueryCachingTest : MultinodeTest
  {
    private const string DefaultSchema = WellKnownSchemas.Schema1;
    private const string Schema1 = WellKnownSchemas.Schema2;
    private const string Schema2 = WellKnownSchemas.Schema3;

    private readonly object SimpleQueryKey = new object();
    private readonly object FilterByIdQueryKey = new object();
    private readonly object FilterByManyIdsQueryKey = new object();

    private int initialQueriesCached = 0;

    protected override void CheckRequirements() =>
      Require.AllFeaturesSupported(Orm.Providers.ProviderFeatures.Multischema);

    protected override DomainConfiguration BuildConfiguration()
    {
      CustomUpgradeHandler.TypeIdPerNode.Clear();
      CustomUpgradeHandler.TypeIdPerNode.Add(TestNodeId2, 200);
      CustomUpgradeHandler.TypeIdPerNode.Add(TestNodeId3, 300);

      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof(BaseTestEntity).Assembly, typeof(BaseTestEntity).Namespace);
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      configuration.DefaultSchema = DefaultSchema;
      return configuration;
    }

    protected override void PopulateNodes()
    {
      CustomUpgradeHandler.CurrentNodeId = TestNodeId2;
      var nodeConfiguration = new NodeConfiguration(TestNodeId2);
      nodeConfiguration.SchemaMapping.Add(DefaultSchema, Schema1);
      nodeConfiguration.UpgradeMode = DomainUpgradeMode.Recreate;
      _ = Domain.StorageNodeManager.AddNode(nodeConfiguration);

      CustomUpgradeHandler.CurrentNodeId = TestNodeId3;
      nodeConfiguration = new NodeConfiguration(TestNodeId3);
      nodeConfiguration.SchemaMapping.Add(DefaultSchema, Schema2);
      nodeConfiguration.UpgradeMode = DomainUpgradeMode.Recreate;
      _ = Domain.StorageNodeManager.AddNode(nodeConfiguration);
    }

    protected override void PopulateData()
    {
      var nodes = new[] { WellKnown.DefaultNodeId, TestNodeId2, TestNodeId3 };

      foreach(var nodeId in nodes) {
        var selectedNode = Domain.StorageNodeManager.GetNode(nodeId);
        using (var session = selectedNode.OpenSession())
        using (var tx = session.OpenTransaction()) {
          #region Entity creation

          var nodeIdName = string.IsNullOrEmpty(nodeId) ? "<default>" : nodeId;

          _ = new BaseTestEntity(session) { BaseName = "A", BaseOwnerNodeId = nodeIdName };
          _ = new MiddleTestEntity(session) {
            BaseName = "AA",
            MiddleName = "AAM",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName
          };
          _ = new LeafTestEntity(session) {
            BaseName = "AAA",
            MiddleName = "AAAM",
            LeafName = "AAAL",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName,
            LeafOwnerNodeId = nodeIdName
          };

          _ = new BaseTestEntity(session) { BaseName = "B", BaseOwnerNodeId = nodeIdName };
          _ = new MiddleTestEntity(session) {
            BaseName = "BB",
            MiddleName = "BBM",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName
          };
          _ = new LeafTestEntity(session) {
            BaseName = "BBB",
            MiddleName = "BBBM",
            LeafName = "BBBL",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName,
            LeafOwnerNodeId = nodeIdName
          };

          _ = new BaseTestEntity(session) { BaseName = "C", BaseOwnerNodeId = nodeIdName };
          _ = new MiddleTestEntity(session) {
            BaseName = "CC",
            MiddleName = "CCM",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName
          };
          _ = new LeafTestEntity(session) {
            BaseName = "CCC",
            MiddleName = "CCCM",
            LeafName = "CCCL",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName,
            LeafOwnerNodeId = nodeIdName
          };

          _ = new BaseTestEntity(session) { BaseName = "D", BaseOwnerNodeId = nodeIdName };
          _ = new MiddleTestEntity(session) {
            BaseName = "DD",
            MiddleName = "DDM",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName
          };
          _ = new LeafTestEntity(session) {
            BaseName = "DDD",
            MiddleName = "DDDM",
            LeafName = "DDDL",
            BaseOwnerNodeId = nodeIdName,
            MiddleOwnerNodeId = nodeIdName,
            LeafOwnerNodeId = nodeIdName
          };

          #endregion

          //puts query to cache
          _ = ExecuteSimpleQueryCaching(session);
          var expectedTypeId = GetExpectedTypeId(nodeId);
          _ = ExecuteFilterByTypeIdCaching(session, expectedTypeId);
          _ = ExecuteFilterBySeveralTypeIdsCaching(session, new[] { expectedTypeId, expectedTypeId });

          tx.Complete();
        }
      }

      initialQueriesCached = Domain.QueryCache.Count;
    }

    [Test]
    public void DefaultNodeTest()
    {
      RunTestSimpleQueryTest(WellKnown.DefaultNodeId);
      RunFilterByTypeIdQueryTest(WellKnown.DefaultNodeId);
      RunFilterBySeveralTypeIdsTest(WellKnown.DefaultNodeId);
      Assert.That(Domain.QueryCache.Count, Is.EqualTo(initialQueriesCached));
    }

    [Test]
    public void TestNode2Test()
    {
      RunTestSimpleQueryTest(TestNodeId2);
      RunFilterByTypeIdQueryTest(TestNodeId2);
      RunFilterBySeveralTypeIdsTest(TestNodeId2);
      Assert.That(Domain.QueryCache.Count, Is.EqualTo(initialQueriesCached));
    }

    [Test]
    public void TestNode3Test()
    {
      RunTestSimpleQueryTest(TestNodeId3);
      RunFilterByTypeIdQueryTest(TestNodeId3);
      RunFilterBySeveralTypeIdsTest(TestNodeId3);
      Assert.That(Domain.QueryCache.Count, Is.EqualTo(initialQueriesCached));
    }

    private void RunTestSimpleQueryTest(string nodeId)
    {
      var selectedNode = Domain.StorageNodeManager.GetNode(nodeId);
      using (var session = selectedNode.OpenSession())
      using (var tx = session.OpenTransaction()) {
        var expectedTypeId = GetExpectedTypeId(nodeId);
        var nodeIdName = string.IsNullOrEmpty(nodeId) ? "<default>" : nodeId;

        var allResults = ExecuteSimpleQueryNoCache(session).OrderBy(e => e.TypeId).ToList();
        Assert.That(allResults.Count, Is.EqualTo(3));
        Assert.That(allResults.All(e => e.BaseOwnerNodeId == nodeIdName), Is.True);
        Assert.That(allResults[0].BaseName, Is.EqualTo("C"));
        Assert.That(allResults[0].TypeId, Is.EqualTo(expectedTypeId));
        Assert.That(allResults[1].BaseName, Is.EqualTo("CC"));
        Assert.That(allResults[1].TypeId, Is.EqualTo(expectedTypeId + 1));
        Assert.That(allResults[2].BaseName, Is.EqualTo("CCC"));
        Assert.That(allResults[2].TypeId, Is.EqualTo(expectedTypeId + 2));

        var middles = allResults.OfType<MiddleTestEntity>().ToList();
        Assert.That(middles.Count, Is.EqualTo(2));
        Assert.That(middles.All(e => e.MiddleOwnerNodeId == nodeIdName), Is.True);
        Assert.That(middles[0].MiddleName, Is.EqualTo("CCM"));
        Assert.That(middles[1].MiddleName, Is.EqualTo("CCCM"));

        var leafs = middles.OfType<LeafTestEntity>().ToList();
        Assert.That(leafs.Count, Is.EqualTo(1));
        Assert.That(leafs[0].LeafOwnerNodeId, Is.EqualTo(nodeIdName));
        Assert.That(leafs[0].LeafName, Is.EqualTo("CCCL"));

        allResults = ExecuteSimpleQueryCaching(session);
        Assert.That(allResults.Count, Is.EqualTo(3));
        Assert.That(allResults.All(e => e.BaseOwnerNodeId == nodeIdName), Is.True);
        Assert.That(allResults[0].BaseName, Is.EqualTo("B"));
        Assert.That(allResults[0].TypeId, Is.EqualTo(expectedTypeId));
        Assert.That(allResults[1].BaseName, Is.EqualTo("BB"));
        Assert.That(allResults[1].TypeId, Is.EqualTo(expectedTypeId + 1));
        Assert.That(allResults[2].BaseName, Is.EqualTo("BBB"));
        Assert.That(allResults[2].TypeId, Is.EqualTo(expectedTypeId + 2));

        middles = allResults.OfType<MiddleTestEntity>().ToList();
        Assert.That(middles.Count, Is.EqualTo(2));
        Assert.That(middles.All(e => e.MiddleOwnerNodeId == nodeIdName), Is.True);
        Assert.That(middles[0].MiddleName, Is.EqualTo("BBM"));
        Assert.That(middles[1].MiddleName, Is.EqualTo("BBBM"));

        leafs = middles.OfType<LeafTestEntity>().ToList();
        Assert.That(leafs.Count, Is.EqualTo(1));
        Assert.That(leafs[0].LeafOwnerNodeId, Is.EqualTo(nodeIdName));
        Assert.That(leafs[0].LeafName, Is.EqualTo("BBBL"));
      }
    }

    private void RunFilterByTypeIdQueryTest(string nodeId)
    {
      var selectedNode = Domain.StorageNodeManager.GetNode(nodeId);
      using (var session = selectedNode.OpenSession())
      using (var tx = session.OpenTransaction()) {
        var expectedTypeId = GetExpectedTypeId(nodeId);
        var nodeIdName = string.IsNullOrEmpty(nodeId) ? "<default>" : nodeId;

        var resultWithoutCache = ExecuteFilterByTypeIdNoCache(session, expectedTypeId);
        Assert.That(resultWithoutCache.Count, Is.EqualTo(4));
        Assert.That(resultWithoutCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithoutCache.All(e => e.TypeId == expectedTypeId), Is.True);
        Assert.That(resultWithoutCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithoutCache.OfType<MiddleTestEntity>().Any(), Is.False);
        Assert.That(resultWithoutCache.OfType<LeafTestEntity>().Any(), Is.False);

        resultWithoutCache = ExecuteFilterByTypeIdNoCache(session, expectedTypeId + 1);
        Assert.That(resultWithoutCache.Count, Is.EqualTo(4));
        Assert.That(resultWithoutCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithoutCache.All(e => e.TypeId == expectedTypeId + 1), Is.True);
        Assert.That(resultWithoutCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithoutCache.OfType<MiddleTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithoutCache.OfType<LeafTestEntity>().Any(), Is.False);

        resultWithoutCache = ExecuteFilterByTypeIdNoCache(session, expectedTypeId + 2);
        Assert.That(resultWithoutCache.Count, Is.EqualTo(4));
        Assert.That(resultWithoutCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithoutCache.All(e => e.TypeId == expectedTypeId + 2), Is.True);
        Assert.That(resultWithoutCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithoutCache.OfType<MiddleTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithoutCache.OfType<LeafTestEntity>().Count(), Is.EqualTo(4));

        var resultWithCache = ExecuteFilterByTypeIdCaching(session, expectedTypeId);
        Assert.That(resultWithCache.Count, Is.EqualTo(4));
        Assert.That(resultWithCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithCache.All(e => e.TypeId == expectedTypeId), Is.True);
        Assert.That(resultWithCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithCache.OfType<MiddleTestEntity>().Any(), Is.False);
        Assert.That(resultWithCache.OfType<LeafTestEntity>().Any(), Is.False);

        resultWithCache = ExecuteFilterByTypeIdCaching(session, expectedTypeId + 1);
        Assert.That(resultWithCache.Count, Is.EqualTo(4));
        Assert.That(resultWithCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithCache.All(e => e.TypeId == expectedTypeId + 1), Is.True);
        Assert.That(resultWithCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithCache.OfType<MiddleTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithCache.OfType<LeafTestEntity>().Any(), Is.False);

        resultWithCache = ExecuteFilterByTypeIdCaching(session, expectedTypeId + 2);
        Assert.That(resultWithCache.Count, Is.EqualTo(4));
        Assert.That(resultWithCache.All(e => e.BaseOwnerNodeId == nodeIdName));
        Assert.That(resultWithCache.All(e => e.TypeId == expectedTypeId + 2), Is.True);
        Assert.That(resultWithCache.OfType<BaseTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithCache.OfType<MiddleTestEntity>().Count(), Is.EqualTo(4));
        Assert.That(resultWithCache.OfType<LeafTestEntity>().Count(), Is.EqualTo(4));

        var unexpectedTypeId = GetUnexpectedTypeId(nodeId);
        resultWithoutCache = ExecuteFilterByTypeIdNoCache(session, unexpectedTypeId);
        Assert.That(resultWithoutCache.Count, Is.EqualTo(0));

        resultWithCache = ExecuteFilterByTypeIdCaching(session, unexpectedTypeId);
        Assert.That(resultWithCache.Count, Is.EqualTo(0));
      }
    }

    private void RunFilterBySeveralTypeIdsTest(string nodeId)
    {
      var selectedNode = Domain.StorageNodeManager.GetNode(nodeId);
      using (var session = selectedNode.OpenSession())
      using (var tx = session.OpenTransaction()) {
        var expectedTypeId = GetExpectedTypeId(nodeId);
        var nodeIdName = string.IsNullOrEmpty(nodeId) ? "<default>" : nodeId;

        var allResults = ExecuteFilterBySeveralTypeIdsNoCache(session, new[] { expectedTypeId, expectedTypeId + 1 })
          .OrderBy(e => e.TypeId)
          .ToList();
        Assert.That(allResults.Count, Is.EqualTo(8));
        Assert.That(allResults.All(e => e.BaseOwnerNodeId == nodeIdName), Is.True);
        Assert.That(allResults.Count(e => e.TypeId == expectedTypeId), Is.EqualTo(4));
        Assert.That(allResults.Count(e => e.TypeId == expectedTypeId + 1), Is.EqualTo(4));

        var middles = allResults.OfType<MiddleTestEntity>().ToList();
        Assert.That(middles.Count, Is.EqualTo(4));
        Assert.That(middles.All(e => e.MiddleOwnerNodeId == nodeIdName), Is.True);

        allResults = ExecuteFilterBySeveralTypeIdsCaching(session, new[] { expectedTypeId, expectedTypeId + 1 });
        Assert.That(allResults.Count, Is.EqualTo(8));
        Assert.That(allResults.All(e => e.BaseOwnerNodeId == nodeIdName), Is.True);
        Assert.That(allResults.Count(e => e.TypeId == expectedTypeId), Is.EqualTo(4));
        Assert.That(allResults.Count(e => e.TypeId == expectedTypeId + 1), Is.EqualTo(4));

        middles = allResults.OfType<MiddleTestEntity>().ToList();
        Assert.That(middles.Count, Is.EqualTo(4));
        Assert.That(middles.All(e => e.MiddleOwnerNodeId == nodeIdName), Is.True);

        var unexpectedTypeIds = GetUnexpectedTypeIds(nodeId);
        allResults = ExecuteFilterBySeveralTypeIdsNoCache(session, unexpectedTypeIds);
        Assert.That(allResults.Count, Is.EqualTo(0));

        allResults = ExecuteFilterBySeveralTypeIdsCaching(session, unexpectedTypeIds);
        Assert.That(allResults.Count, Is.EqualTo(0));
      }
    }

    private int GetExpectedTypeId(string nodeId)
    {
      return (nodeId == WellKnown.DefaultNodeId)
        ? 100
        : CustomUpgradeHandler.TypeIdPerNode[nodeId];
    }

    private int GetUnexpectedTypeId(string nodeId)
    {
      return (nodeId == WellKnown.DefaultNodeId)
        ? CustomUpgradeHandler.TypeIdPerNode.First().Value
        : 100;
    }

    private int[] GetUnexpectedTypeIds(string nodeId)
    {
      return (nodeId == WellKnown.DefaultNodeId)
        ? CustomUpgradeHandler.TypeIdPerNode.Values.ToArray()
        : CustomUpgradeHandler.TypeIdPerNode.Where(i => i.Key != nodeId)
            .Select(i => i.Value).Union([100]).ToArray();
    }

    private List<BaseTestEntity> ExecuteSimpleQueryCaching(Session session) =>
      session.Query.Execute(SimpleQueryKey, q => q.All<BaseTestEntity>().Where(e => e.BaseName.Contains("B"))).ToList();

    private List<BaseTestEntity> ExecuteFilterByTypeIdCaching(Session session, int typeId) =>
      session.Query.Execute(FilterByIdQueryKey, q => q.All<BaseTestEntity>()
        .Where(e => e.TypeId == typeId))
        .ToList();

    private List<BaseTestEntity> ExecuteFilterBySeveralTypeIdsCaching(Session session, int[] typeIds)
    {
      var localTypeIds = typeIds;
      return session.Query.Execute(FilterByManyIdsQueryKey, q => q.All<BaseTestEntity>()
        .Where(e => e.TypeId.In(localTypeIds)))
        .ToList();
    }

    private List<BaseTestEntity> ExecuteSimpleQueryNoCache(Session session) =>
      session.Query.All<BaseTestEntity>().Where(e => e.BaseName.Contains("C")).ToList();

    private List<BaseTestEntity> ExecuteFilterByTypeIdNoCache(Session session, int typeId) =>
      session.Query.All<BaseTestEntity>().Where(e => e.TypeId == typeId).ToList();

    private List<BaseTestEntity> ExecuteFilterBySeveralTypeIdsNoCache(Session session, int[] typeIds) =>
      session.Query.All<BaseTestEntity>().Where(e => e.TypeId.In(typeIds)).ToList();
  }
}
