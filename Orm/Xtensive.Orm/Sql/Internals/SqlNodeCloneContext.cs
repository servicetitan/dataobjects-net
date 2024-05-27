// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Xtensive.Sql
{
  internal readonly struct SqlNodeCloneContext
  {
    private readonly Dictionary<SqlNode, SqlNode> nodeMapping = new();

    public Dictionary<SqlNode, SqlNode> NodeMapping => nodeMapping;

    public T TryGet<T>(T node) where T : SqlNode =>
      NodeMapping.TryGetValue(node, out var clone)
        ? (T) clone
        : null;

    public SqlNodeCloneContext()
    {
    }
  }

  internal static class SqlNodeCloneContextExtensions
  {
    public static T GetOrAdd<T>(this SqlNodeCloneContext? context, T node, Func<T, SqlNodeCloneContext, T> factory) where T : SqlNode
    {
      var ctx = context ?? new();
      if (ctx.NodeMapping.TryGetValue(node, out var clone)) {
        return (T) clone;
      }
      var result = factory(node, ctx);
      ctx.NodeMapping[node] = result;
      return result;
    }
  }
}
