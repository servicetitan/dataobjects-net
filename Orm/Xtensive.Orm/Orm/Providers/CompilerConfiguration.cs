// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.26

using System.Collections.Generic;

namespace Xtensive.Orm.Providers
{
  public readonly record struct CompilerConfiguration
  (
    bool PrepareRequest,
    bool PreferTypeIdAsParameter,
    IReadOnlyList<string> Tags,
    StorageNode StorageNode
  );
}
