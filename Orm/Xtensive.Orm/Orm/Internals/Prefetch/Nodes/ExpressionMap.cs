// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.24

using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Xtensive.Orm.Internals.Prefetch
{
  internal sealed class ExpressionMap
  {
    private readonly Dictionary<Expression, HashSet<Expression>> childrenMap
      = new Dictionary<Expression, HashSet<Expression>>();

    public IEnumerable<Expression> GetChildren(Expression parent)
    {
      HashSet<Expression> children;
      return childrenMap.TryGetValue(parent, out children)
        ? children
        : Enumerable.Empty<Expression>();
    }

    public void RegisterChild(Expression parent, Expression child)
    {
      ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenMap, parent, out var exists);
      if (exists) {
        children.Add(child);
      }
      else {
        children = [child];
      }
    }
  }
}
