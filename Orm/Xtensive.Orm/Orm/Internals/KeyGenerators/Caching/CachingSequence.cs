// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.05.17

using System;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;

namespace Xtensive.Orm.Internals.KeyGenerators
{
  internal class CachingSequence
  {
    protected readonly IStorageSequenceAccessor accessor;
    protected readonly object syncRoot;

    protected long nextValue;
    protected long nextValueBound;

    public void Reset()
    {
      if (syncRoot == null) {
        ResetUnsafe();
        return;
      }

      lock (syncRoot)
        ResetUnsafe();
    }

    private void ResetUnsafe()
    {
      nextValue = 0;
      nextValueBound = 0;
    }

    public CachingSequence(IStorageSequenceAccessor accessor, bool threadSafe)
    {
      ArgumentNullException.ThrowIfNull(accessor, "accessor");

      this.accessor = accessor;
      syncRoot = threadSafe ? new object() : null;
    }
  }

  internal sealed class CachingSequence<TValue> : CachingSequence
  {
    public TValue GetNextValue(SequenceInfo sequenceInfo, Session session)
    {
      if (syncRoot == null) {
        return GetNextValueUnsafe(sequenceInfo, session);
      }

      lock (syncRoot) {
        return GetNextValueUnsafe(sequenceInfo, session);
      }
    }

    private TValue GetNextValueUnsafe(SequenceInfo sequenceInfo, Session session)
    {
      if (nextValue == nextValueBound) {
        var values = accessor.NextBulk(sequenceInfo, session);
        nextValue = values.Offset;
        nextValueBound = values.EndOffset;
      }
      return (TValue) Convert.ChangeType(nextValue++, typeof(TValue));
    }

    // Constructors

    public CachingSequence(IStorageSequenceAccessor accessor, bool threadSafe)
      : base(accessor, threadSafe) { }
  }
}
