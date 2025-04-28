// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Xtensive.Sql
{
  internal readonly struct SqlNodeCloneContext()
  {
    public Dictionary<SqlNode, SqlNode> NodeMapping { get; } = new();

    public T TryGet<T>(T node) where T : SqlNode =>
      NodeMapping.TryGetValue(node, out var clone)
        ? (T) clone
        : null;
  }

  internal static class SqlNodeCloneContextExtensions
  {
    public static T GetOrAdd<T>(this SqlNodeCloneContext ctx, T node, Func<T, SqlNodeCloneContext, T> factory) where T : SqlNode
    {
      if (ctx.NodeMapping.TryGetValue(node, out var clone)) {
        return (T) clone;
      }
      var result = factory(node, ctx);
      ctx.NodeMapping[node] = result;
      return result;
    }
  }
}
