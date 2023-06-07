// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.05.17

using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;

namespace Xtensive.Orm.Internals.KeyGenerators
{
  internal sealed class DomainCachingSequenceProvider<TValue> : ICachingSequenceProvider<TValue>
  {
    private readonly IStorageSequenceAccessor accessor;

    public CachingSequence<TValue> GetSequence(SequenceInfo sequenceInfo, Session session) =>
      (CachingSequence<TValue>) session.StorageNode.KeySequencesCache.GetOrAdd(
        sequenceInfo,
        static (_, a) => new CachingSequence<TValue>(a, true),
        accessor
   );

    // Constructor

    public DomainCachingSequenceProvider(IStorageSequenceAccessor accessor)
    {
      ArgumentValidator.EnsureArgumentNotNull(accessor, "accessor");

      this.accessor = accessor;
    }
  }
}
