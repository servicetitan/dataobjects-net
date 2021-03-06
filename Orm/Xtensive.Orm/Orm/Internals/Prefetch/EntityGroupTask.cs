// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexander Nikolaev
// Created:    2009.10.20

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals.Prefetch
{
  [Serializable]
  internal sealed class EntityGroupTask : IEquatable<EntityGroupTask>
  {
    #region Nested classes

    private struct CacheKey : IEquatable<CacheKey>
    {
      public readonly int[] ColumnIndexes;
      public readonly TypeInfo Type;
      private readonly int cachedHashCode;

      public bool Equals(CacheKey other)
      {
        if (!Type.Equals(other.Type)) {
          return false;
        }

        if (ColumnIndexes.Length != other.ColumnIndexes.Length) {
          return false;
        }

        for (var i = ColumnIndexes.Length - 1; i >= 0; i--) {
          if (ColumnIndexes[i] != other.ColumnIndexes[i]) {
            return false;
          }
        }

        return true;
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(null, obj)) {
          return false;
        }

        if (obj.GetType() != typeof(CacheKey)) {
          return false;
        }

        return Equals((CacheKey) obj);
      }

      public override int GetHashCode() => cachedHashCode;


      // Constructors

      public CacheKey(int[] columnIndexes, TypeInfo type, int cachedHashCode)
      {
        ColumnIndexes = columnIndexes;
        Type = type;
        this.cachedHashCode = cachedHashCode;
      }
    }

    #endregion

    private const int MaxKeyCountInOneStatement = 40;
    private static readonly object recordSetCachingRegion = new object();
    private static readonly Parameter<IEnumerable<Tuple>> includeParameter =
      new Parameter<IEnumerable<Tuple>>("Keys");

    private Dictionary<Key, bool> keys;
    private readonly TypeInfo type;
    private readonly PrefetchManager manager;
    private List<QueryTask> queryTasks;
    private readonly CacheKey cacheKey;

    public CompilableProvider Provider { get; private set; }

    public void AddKey(Key key, bool exactType)
    {
      if (keys == null) {
        keys = new Dictionary<Key, bool>();
      }

      if (keys.ContainsKey(key)) {
        return;
      }

      keys.Add(key, exactType);
    }

    public void RegisterQueryTasks()
    {
      queryTasks = new List<QueryTask>(keys.Count);
      var count = 0;
      var keyCount = keys.Count;
      var totalCount = 0;
      List<Tuple> currentKeySet = null;
      foreach (var pair in keys) {
        if (count == 0) {
          currentKeySet = new List<Tuple>(MaxKeyCountInOneStatement);
        }

        currentKeySet.Add(pair.Key.Value);
        count++;
        totalCount++;
        if (count == MaxKeyCountInOneStatement || totalCount == keyCount) {
          count = 0;
          var queryTask = CreateQueryTask(currentKeySet);
          queryTasks.Add(queryTask);
          manager.Owner.Session.RegisterInternalDelayedQuery(queryTask);
        }
      }
    }

    public void UpdateCache(HashSet<Key> foundKeys)
    {
      var reader = manager.Owner.Session.Domain.EntityDataReader;
      foreach (var queryTask in queryTasks) {
        PutLoadedStatesInCache(queryTask.Result, reader, foundKeys);
      }

      HandleMissedKeys(foundKeys);
    }

    public bool Equals(EntityGroupTask other)
    {
      if (ReferenceEquals(null, other)) {
        return false;
      }

      return ReferenceEquals(this, other) || other.cacheKey.Equals(cacheKey);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) {
        return false;
      }

      if (ReferenceEquals(this, obj)) {
        return true;
      }

      return obj is EntityGroupTask entityGroupTask && Equals(entityGroupTask);
    }

    public override int GetHashCode() => cacheKey.GetHashCode();

    private QueryTask CreateQueryTask(List<Tuple> currentKeySet)
    {
      var parameterContext = new ParameterContext();
      parameterContext.SetValue(includeParameter, currentKeySet);
      object key = new Pair<object, CacheKey>(recordSetCachingRegion, cacheKey);
      Func<object, object> generator = CreateRecordSet;
      var session = manager.Owner.Session;
      Provider = (CompilableProvider) session.StorageNode.InternalQueryCache.GetOrAdd(key, generator);
      var executableProvider = session.Compile(Provider);
      return new QueryTask(executableProvider, session.GetLifetimeToken(), parameterContext);
    }

    private static CompilableProvider CreateRecordSet(object cachingKey)
    {
      var pair = (Pair<object, CacheKey>) cachingKey;
      var selectedColumnIndexes = pair.Second.ColumnIndexes;
      var keyColumnsCount = pair.Second.Type.Indexes.PrimaryIndex.KeyColumns.Count;
      var keyColumnIndexes = new int[keyColumnsCount];
      foreach (var index in Enumerable.Range(0, keyColumnsCount)) {
        keyColumnIndexes[index] = index;
      }

      var columnCollectionLength = pair.Second.Type.Indexes.PrimaryIndex.Columns.Count;
      return pair.Second.Type.Indexes.PrimaryIndex.GetQuery().Include(IncludeAlgorithm.ComplexCondition,
        true, context => context.GetValue(includeParameter), $"includeColumnName-{Guid.NewGuid()}",
        keyColumnIndexes).Filter(t => t.GetValue<bool>(columnCollectionLength)).Select(selectedColumnIndexes);
    }

    private void PutLoadedStatesInCache(IEnumerable<Tuple> queryResult, EntityDataReader reader,
      HashSet<Key> foundedKeys)
    {
      var entityRecords = reader.Read(queryResult, Provider.Header, manager.Owner.Session);
      foreach (var entityRecord in entityRecords) {
        if (entityRecord != null) {
          var fetchedKey = entityRecord.GetKey();
          var tuple = entityRecord.GetTuple();
          if (tuple != null) {
            manager.SaveStrongReference(manager.Owner.UpdateState(fetchedKey, tuple));
            foundedKeys.Add(fetchedKey);
          }
        }
      }
    }

    private void HandleMissedKeys(HashSet<Key> foundKeys)
    {
      if (foundKeys.Count == keys.Count) {
        return;
      }

      var countOfHandledKeys = foundKeys.Count;
      var totalCount = keys.Count;
      foreach (var pair in keys) {
        if (!foundKeys.Contains(pair.Key)) {
          MarkMissedEntityState(pair.Key, pair.Value);
          countOfHandledKeys++;
        }

        if (countOfHandledKeys == totalCount) {
          break;
        }
      }
    }

    private void MarkMissedEntityState(Key key, bool exactType)
    {
      var cachedEntityState = manager.GetCachedEntityState(ref key, out var isRemoved);
      if (exactType && !isRemoved
        && (cachedEntityState==null || cachedEntityState.Key.HasExactType && cachedEntityState.Key
          .TypeReference.Type==type)) {
        // Ensures there will be "removed" EntityState associated with this key
        manager.SaveStrongReference(manager.Owner.UpdateState(key, null));
      }
    }


    // Constructors

    public EntityGroupTask(TypeInfo type, int[] columnIndexes, PrefetchManager manager)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, nameof(type));
      ArgumentValidator.EnsureArgumentNotNull(columnIndexes, nameof(columnIndexes));
      ArgumentValidator.EnsureArgumentIsGreaterThan(columnIndexes.Length, 0, "columnIndexes.Length");
      ArgumentValidator.EnsureArgumentNotNull(manager, nameof(manager));

      this.type = type;
      this.manager = manager;
      var cachedHashCode = 0;
      foreach (var columnIndex in columnIndexes) {
        cachedHashCode = unchecked (379 * cachedHashCode + columnIndex);
      }

      cachedHashCode ^= type.GetHashCode();
      cacheKey = new CacheKey(columnIndexes, type, cachedHashCode);
    }
  }
}
