// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.01.29

using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Rse.Providers;

namespace Xtensive.Orm.Rse.Compilation
{
  public class CompositePostCompiler(IReadOnlyList<IPostCompiler> items) : IPostCompiler
  {
    public IReadOnlyList<IPostCompiler> Items { get; } = items;

    public ExecutableProvider Process(ExecutableProvider rootProvider)
    {
      var provider = rootProvider;
      foreach (var item in Items)
        provider = item.Process(provider);
      return provider;
    }
  }
}
