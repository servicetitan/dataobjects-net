// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2008.10.14

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xtensive.Caching;
using KeyCache = Xtensive.Caching.ICache<Xtensive.Orm.Key, Xtensive.Orm.Key>;

namespace Xtensive.Orm.Internals
{
  /// <summary>
  /// Describes cached state of <see cref="EntitySetBase"/>
  /// </summary>
  public sealed class EntitySetState : TransactionalStateContainer<KeyCache>,
    IEnumerable<Key>,
    IInvalidatable
  {
    private readonly record struct BackedUpState
    (
      bool IsLoaded,
      long? TotalItemCount,
      IEnumerable<Key> AddedKeys,
      IEnumerable<Key> RemovedKeys
    );

    private readonly bool isDisconnected;

    private Guid lastManualPrefetchId = Guid.Empty;
    private bool isLoaded;
    private long? totalItemCount;
    private volatile int version = int.MinValue;
    private HashSet<Key> addedKeys = new();
    private HashSet<Key> removedKeys = new();

    private BackedUpState? previousState;

    public KeyCache FetchedKeys
    {
      get => State;
      set => State = value;
    }

    /// <summary>
    /// Gets total count of elements which entity set contains.
    /// </summary>
    public long? TotalItemCount
    {
      get {
        EnsureIsActual();
        return totalItemCount;
      }
      internal set => totalItemCount = value;
    }

    /// <summary>
    /// Gets the number of cached items.
    /// </summary>
    public long CachedItemCount => FetchedItemsCount - RemovedItemsCount + AddedItemsCount;

    /// <summary>
    /// Gets the number of fetched keys.
    /// </summary>
    public long FetchedItemsCount => FetchedKeys.Count;

    /// <summary>
    /// Gets count of keys which was added but changes are not applyed.
    /// </summary>
    public int AddedItemsCount => addedKeys.Count;

    /// <summary>
    /// Gets count of keys which was removed but changes are not applied.
    /// </summary>
    public int RemovedItemsCount => removedKeys.Count;

    /// <summary>
    /// Gets a value indicating whether state contains all keys which stored in database.
    /// </summary>
    public bool IsFullyLoaded => TotalItemCount == CachedItemCount;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is loaded.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this instance is preloaded; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsLoaded
    {
      get {
        EnsureIsActual();
        return isLoaded;
      }
      internal set => isLoaded = value;
    }

    /// <summary>
    /// Get value indicating whether state has changes.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this state has changes; otherwise, <see langword="false"/>.
    /// </value>
    public bool HasChanges
      => AddedItemsCount != 0 || RemovedItemsCount != 0;

    /// <summary>
    /// Sets cached keys to <paramref name="keys"/>.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <param name="count">Total item count.</param>
    public void Update(IEnumerable<Key> keys, long? count)
    {
      if (HasChanges) {
        UpdateCachedState(keys, count);
      }
      else {
        UpdateSyncedState(keys, count);
      }
    }

    /// <summary>
    /// Determines whether cached state contains specified item.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Check result.</returns>
    public bool Contains(Key key) =>
      !removedKeys.Contains(key) && (addedKeys.Contains(key) || FetchedKeys.ContainsKey(key));

    /// <summary>
    /// Registers the specified fetched key in cached state.
    /// </summary>
    /// <param name="key">The key to register.</param>
    public void Register(Key key) => FetchedKeys.Add(key);

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key to add.</param>
    public void Add(Key key)
    {
      if (!removedKeys.Remove(key)) {
        addedKeys.Add(key);
      }
      if (TotalItemCount != null) {
        TotalItemCount++;
      }

      _ = Interlocked.Increment(ref version);
      Rebind();
    }

    /// <summary>
    /// Removes the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public void Remove(Key key)
    {
      if (!addedKeys.Remove(key)) {
        removedKeys.Add(key);
      }
      if (TotalItemCount!=null) {
        TotalItemCount--;
      }

      _ = Interlocked.Increment(ref version);
      Rebind();
    }

    /// <summary>
    /// Applies all changes to state.
    /// </summary>
    public bool ApplyChanges()
    {
      if (HasChanges) {
        EnsureFetchedKeysIsNotNull();
        BackupState();
        var currentFetchedKeys = FetchedKeys;
        InitializeFetchedKeys();

        foreach (var currentFetchedKey in currentFetchedKeys) {
          if (!removedKeys.Contains(currentFetchedKey)) {
            FetchedKeys.Add(currentFetchedKey);
          }
        }

        foreach (var addedKey in addedKeys) {
          FetchedKeys.Add(addedKey);
        }
        InitializeDifferenceCollections();
        Rebind();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Clear all changes.
    /// </summary>
    public void CancelChanges()
    {
      InitializeDifferenceCollections();
      _ =  Interlocked.Increment(ref version);
      Rebind();
    }

    internal void RollbackState()
    {
      if (previousState is { } prev) {
        TotalItemCount = prev.TotalItemCount;
        IsLoaded = prev.IsLoaded;
        var fetchedKeys = FetchedKeys;

        InitializeFetchedKeys();
        InitializeDifferenceCollections();

        foreach (var fetchedKey in fetchedKeys) {
          FetchedKeys.Add(fetchedKey);
        }

        foreach (var addedKey in prev.AddedKeys) {
          if (fetchedKeys.ContainsKey(addedKey)) {
            FetchedKeys.Remove(addedKey);
          }
          addedKeys.Add(addedKey);
        }
        foreach (var removedKey in prev.RemovedKeys) {
          if (!FetchedKeys.ContainsKey(removedKey)) {
            FetchedKeys.Add(removedKey);
          }
          removedKeys.Add(removedKey);
        }
      }
    }

    internal void RemapKeys(KeyMapping mapping)
    {
      var remapper = mapping.TryRemapKey;
      addedKeys = addedKeys.Select(remapper).ToHashSet();
      removedKeys = removedKeys.Select(remapper).ToHashSet();
    }

    internal bool ShouldUseForcePrefetch(Guid? currentPrefetchOperation)
    {
      if (currentPrefetchOperation.HasValue) {
        if (currentPrefetchOperation.Value == lastManualPrefetchId) {
          return false;
        }

        lastManualPrefetchId = currentPrefetchOperation.Value;
      }

      if (Session.Transaction != null) {
        switch (Session.Transaction.Outermost.IsolationLevel) {
          case System.Transactions.IsolationLevel.ReadCommitted:
          case System.Transactions.IsolationLevel.ReadUncommitted:
            return true;
          case System.Transactions.IsolationLevel.RepeatableRead:
            return string.Equals(Session.Handlers.ProviderInfo.ProviderName, WellKnown.Provider.SqlServer, StringComparison.Ordinal);
          default:
            return false;
        }
      }

      return isDisconnected;
    }

    internal void SetLastManualPrefetchId(Guid? prefetchOperationId)
    {
      if (prefetchOperationId.HasValue) {
        lastManualPrefetchId = prefetchOperationId.Value;
      }
    }

    /// <inheritdoc/>
    protected override void Invalidate()
    {
      TotalItemCount = null;
      IsLoaded = false;
      base.Invalidate();
    }

    void IInvalidatable.Invalidate() => Invalidate();

    /// <inheritdoc/>
    protected override void Refresh() => InitializeFetchedKeys();

    #region GetEnumerator<...> methods

    /// <inheritdoc/>
    public IEnumerator<Key> GetEnumerator()
    {
      var versionSnapshot = version;
      foreach (var fetchedKey in FetchedKeys) {
        if (versionSnapshot != version) {
          throw new InvalidOperationException(Strings.ExCollectionHasBeenChanged);
        }
        if (!removedKeys.Contains(fetchedKey)) {
          yield return fetchedKey;
        }
      }

      if (versionSnapshot != version) {
        throw new InvalidOperationException(Strings.ExCollectionHasBeenChanged);
      }
      foreach (var addedKey in addedKeys) {
        if (versionSnapshot != version) {
          throw new InvalidOperationException(Strings.ExCollectionHasBeenChanged);
        }
        yield return addedKey;
      }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    private void UpdateSyncedState(IEnumerable<Key> keys, long? count)
    {
      FetchedKeys.Clear();
      TotalItemCount = count;
      foreach (var key in keys) {
        FetchedKeys.Add(key);
      }
      Rebind();
    }

    public void UpdateCachedState(IEnumerable<Key> syncronizedKeys, long? count)
    {
      FetchedKeys.Clear();
      HashSet<Key> becameRemovedOnSever = new(removedKeys);
      foreach (var key in syncronizedKeys) {
        if (!addedKeys.Remove(key)) {
          _ = becameRemovedOnSever.Remove(key);
        }
        FetchedKeys.Add(key);
      }
      removedKeys.ExceptWith(becameRemovedOnSever);

      TotalItemCount = count.HasValue
        ? FetchedKeys.Count - removedKeys.Count + AddedItemsCount
        : count;
    }

    private void EnsureFetchedKeysIsNotNull()
    {
      if (FetchedKeys == null) {
        InitializeFetchedKeys();
      }
    }

    private void BackupState() =>
      previousState = new(IsLoaded, TotalItemCount, addedKeys.ToList(), removedKeys.ToList());

    private void InitializeFetchedKeys()
      => FetchedKeys = new LruCache<Key, Key>(WellKnown.EntitySetCacheSize, cachedKey => cachedKey);

    private void InitializeDifferenceCollections()
    {
      addedKeys = new();
      removedKeys = new();
    }

    // Constructors

    internal EntitySetState(EntitySetBase entitySet)
      : base(entitySet.Session)
    {
      InitializeFetchedKeys();
      isDisconnected = entitySet.Session.IsDisconnected;
    }
  }
}