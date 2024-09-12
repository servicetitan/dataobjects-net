// Copyright (C) 2014 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2014.03.13

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Xtensive.Orm.Internals
{
  internal readonly struct StorageNodeRegistry()
  {
    private readonly ConcurrentDictionary<string, StorageNode> nodes = new();

    public bool Add(StorageNode node)
    {
      ArgumentNullException.ThrowIfNull(node);
      return nodes.TryAdd(node.Id, node);
    }

    public bool Remove(string nodeId)
    {
      ArgumentNullException.ThrowIfNull(nodeId);
      return nodeId != WellKnown.DefaultNodeId
        ? nodes.TryRemove(nodeId, out var dummy)
        : throw new InvalidOperationException(Strings.ExDefaultStorageNodeCanNotBeRemoved);
    }

    public StorageNode TryGet(string nodeId)
    {
      ArgumentNullException.ThrowIfNull(nodeId);
      return nodes.GetValueOrDefault(nodeId);
    }

    public StorageNode Get(string nodeId) =>
      TryGet(nodeId) ?? throw new KeyNotFoundException(string.Format(Strings.ExStorageNodeWithIdXIsNotFound, nodeId));
  }
}
