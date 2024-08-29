// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.03.08

using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals
{
  internal sealed class KeyGeneratorRegistry : LockableBase
  {
    private readonly Dictionary<KeyInfo, KeyGenerator> generators = new();
    private readonly Dictionary<KeyInfo, TemporaryKeyGenerator> temporaryGenerators = new();

    // Compatibility indexer
    public KeyGenerator this[KeyInfo key] => Get(key);

    public KeyGenerator Get(KeyInfo key) => generators.GetValueOrDefault(key);

    public KeyGenerator Get(KeyInfo key, bool isTemporary) => isTemporary ? GetTemporary(key) : Get(key);

    public TemporaryKeyGenerator GetTemporary(KeyInfo key) => temporaryGenerators.GetValueOrDefault(key);

    public void Register(KeyInfo key, KeyGenerator generator)
    {
      EnsureNotLocked();
      generators.Add(key, generator);
    }

    public void RegisterTemporary(KeyInfo key, TemporaryKeyGenerator temporaryGenerator)
    {
      EnsureNotLocked();
      temporaryGenerators.Add(key, temporaryGenerator);
    }
  }
}