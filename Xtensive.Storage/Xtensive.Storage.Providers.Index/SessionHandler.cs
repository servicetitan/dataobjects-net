// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.01

using System;
using System.Linq;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;
using Xtensive.Indexing;
using Xtensive.Storage.Linq;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers.Index.Resources;

namespace Xtensive.Storage.Providers.Index
{
  public class SessionHandler : Providers.SessionHandler
  {
    /// <inheritdoc/>
    public override void BeginTransaction()
    {
      // TODO: Implement transactions;
    }

    /// <inheritdoc/>
    public override void CommitTransaction()
    {
      // TODO: Implement transactions;
    }

    /// <inheritdoc/>
    public override void RollbackTransaction()
    {
      // TODO: Implement transactions;
    }

    protected override void Insert(EntityState state)
    {
      var handler = (DomainHandler)Handlers.DomainHandler;
      foreach (IndexInfo indexInfo in state.Type.AffectedIndexes) {
        var index = handler.GetRealIndex(indexInfo);
        var transform = handler.GetIndexTransform(indexInfo, state.Type);
        var item = transform.Apply(TupleTransformType.Tuple, state.Tuple);
        index.Add(item);
      }
    }

    protected override void Update(EntityState state)
    {
      var handler = (DomainHandler)Handlers.DomainHandler;
      IndexInfo primaryIndex = state.Type.Indexes.PrimaryIndex;
      var indexProvider = Rse.Providers.Compilable.IndexProvider.Get(primaryIndex);
      SeekResult<Tuple> pkSeekResult;
      using (EnumerationScope.Open()) {
        pkSeekResult = indexProvider
          .GetService<IOrderedEnumerable<Tuple, Tuple>>()
          .Seek(new Ray<Entire<Tuple>>(new Entire<Tuple>(state.Key.Value)));
        if (pkSeekResult.ResultType != SeekResultType.Exact)
          throw new InvalidOperationException(string.Format(Strings.ExInstanceXIsNotFound, 
            state.Key.EntityType.Name));
      }

      var pkItem = Tuple.Create(pkSeekResult.Result.Descriptor);
      pkSeekResult.Result.CopyTo(pkItem);
      pkItem.MergeWith(state.Tuple, MergeBehavior.PreferDifference);

      foreach (IndexInfo indexInfo in state.Type.AffectedIndexes) {
        var index = handler.GetRealIndex(indexInfo);
        var transform = handler.GetIndexTransform(indexInfo, state.Type);
        var oldItem = transform.Apply(TupleTransformType.TransformedTuple, pkSeekResult.Result).ToFastReadOnly();
        var item    = transform.Apply(TupleTransformType.Tuple, pkItem);
        index.Remove(oldItem);
        index.Add(item);
      }
    }

    protected override void Remove(EntityState state)
    {
      var handler = (DomainHandler)Handlers.DomainHandler;
      IndexInfo primaryIndex = state.Type.Indexes.PrimaryIndex;
      var indexProvider = Rse.Providers.Compilable.IndexProvider.Get(primaryIndex);
      SeekResult<Tuple> pkSeekResult;
      using (EnumerationScope.Open()) {
        pkSeekResult = indexProvider
          .GetService<IOrderedEnumerable<Tuple, Tuple>>()
          .Seek(new Ray<Entire<Tuple>>(new Entire<Tuple>(state.Key.Value)));
        if (pkSeekResult.ResultType!=SeekResultType.Exact)
          throw new InvalidOperationException(string.Format(Strings.ExInstanceXIsNotFound, 
            state.Key.EntityType.Name));
      }

      foreach (IndexInfo indexInfo in state.Type.AffectedIndexes) {
        var index = handler.GetRealIndex(indexInfo);
        var transform = handler.GetIndexTransform(indexInfo, state.Type);
        var oldItem = transform.Apply(TupleTransformType.TransformedTuple, pkSeekResult.Result).ToFastReadOnly();
        index.Remove(oldItem);
      }
    }

    public override void Initialize()
    {
      // TODO: Think what should be done here.
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
    }
  }
}
