// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2008.11.07

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Caching;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Model;
using Activator = Xtensive.Orm.Internals.Activator;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm
{
  public partial class Session
  {
    private static readonly Converter<EntityState, Key> keyExtractor = i => i.Key;

    // EntitySets with cached items that filled their cache
    // within DisableSaveChanges() scope.
    private HashSet<EntitySetBase> entitySetsWithInvalidState;

    internal ICache<Key, EntityState> EntityStateCache { get; private set; }
    internal EntityChangeRegistry EntityChangeRegistry { get; private set; }
    internal EntitySetChangeRegistry EntitySetChangeRegistry = new();

    internal void Invalidate()
    {
      if (IsDebugEventLoggingEnabled) {
        OrmLog.Debug(nameof(Strings.LogSessionXInvalidate), this);
      }

      ClearChangeRegistry();
      InvalidateCachedEntities();
    }

    private void InvalidateCachedEntities()
    {
      foreach (var state in EntityStateCache) {
        var entity = state.TryGetEntity();
        // Invalidate any entity sets
        if (entity != null && state.IsActual) // Don't bother invalidating non-actual entities
          foreach (var field in entity.TypeInfo.Fields.Where(f => f.IsEntitySet)) {
            var entitySet = (EntitySetBase) entity.GetFieldAccessor(field).GetUntypedValue(entity);
            ((IInvalidatable) entitySet.State).Invalidate();
          }
        // Invalidate entity itself
        ((IInvalidatable) state).Invalidate();
      }
    }

    internal void NotifyEntitySetCached(EntitySetBase entitySet)
    {
      if (disableAutoSaveChanges || pinner.RootCount > 0)
        entitySetsWithInvalidState.Add(entitySet);
    }

    internal void RemapEntityKeys(KeyMapping keyMapping) =>
      RemapEntityKeys(keyMapping, false).GetAwaiter().GetResult();

    private async ValueTask RemapEntityKeys(KeyMapping keyMapping, bool isAsync, CancellationToken token = default)
    {
      if (keyMapping.Map.Count == 0)
        return;
      using (Activate()) {
        if (!LazyKeyGenerationIsEnabled) {
          await Persist(PersistReason.RemapEntityKeys, isAsync, token).ConfigureAwaitFalse();
          Invalidate();
        }
        if (IsDebugEventLoggingEnabled) {
          OrmLog.Debug(nameof(Strings.LogSessionXRemappingEntityKeys), this);
        }

        foreach (var entityState in EntityChangeRegistry.GetItems(PersistenceState.New)) {
          var key = entityState.Key;
          var remappedKey = keyMapping.TryRemapKey(key);
          if (remappedKey != key)
            entityState.RemapKey(remappedKey);
          EntityStateCache.Add(entityState);
        }
        ProcessChangesOfEntitySets(entitySetState => entitySetState.RemapKeys(keyMapping));
        EntityEvents.RemapKeys(keyMapping);
      }
    }

    internal void EnforceChangeRegistrySizeLimit()
    {
      if (EntityChangeRegistry.Count >= Configuration.EntityChangeRegistrySize)
        Persist(PersistReason.ChangeRegistrySizeLimit);
    }

    internal Entity CreateEntity(Type type, Key key)
    {
      var state = CreateEntityState(key, true);
      return Activator.CreateEntity(this, type, state);
    }

    internal Entity CreateOrInitializeExistingEntity(Type type, Key key)
    {
      var state = CreateEntityState(key, false);
      var entity = state.TryGetEntity();
      if (entity == null)
        return Activator.CreateEntity(this, type, state);
      else {
        InitializeEntity(entity, false);
        return entity;
      }
    }

    internal void RemoveOrCreateRemovedEntity(Type type, Key key, EntityRemoveReason reason)
    {
      // Checking for deleted entity with the same key
      var result = EntityStateCache[key, false];
      if (result != null) {
        if (result.PersistenceState == PersistenceState.Removed) {
          return;
        }
        result.Entity.RemoveLaterInternal(reason);
        return;
      }

      EnforceChangeRegistrySizeLimit(); // Must be done before new entity registration
      result = new EntityState(this, key, null) {
        PersistenceState = PersistenceState.Removed
      };
      EntityStateCache.Add(result);

      if (IsDebugEventLoggingEnabled) {
        OrmLog.Debug(nameof(Strings.LogSessionXCachingY), this, result);
      }
    }

    internal void InitializeEntity(Entity entity, bool materialize)
    {
      try {
        entity.SystemBeforeInitialize(materialize);
      }
      catch (Exception error) {
        entity.SystemInitializationError(error);
        throw;
      }
      entity.SystemInitialize(materialize);
    }

    internal EntityState CreateEntityState(Key key, bool failIfStateIsAlreadyBound)
    {
      // Checking for deleted entity with the same key
      var result = EntityStateCache[key, false];
      EnforceChangeRegistrySizeLimit(); // Must be done before new entity registration

      // If type is unknown, we consider tuple is null,
      // so its Entity is considered as non-existing
      Tuple tuple = null;
      if (key.HasExactType) {
        // A tuple with all the fields set to default values rather then N/A
        var typeInfo = key.TypeInfo;
        tuple = typeInfo.CreateEntityTuple(key.Value, StorageNode.TypeIdRegistry[typeInfo]);
      }

      if (result == null) {
        result = new EntityState(this, key, tuple) {
          PersistenceState = PersistenceState.New
        };
        EntityStateCache.Add(result);
      }
      else {
        if (result.Entity != null && !result.Entity.IsRemoved && failIfStateIsAlreadyBound)
          throw new UniqueConstraintViolationException(string.Format(Strings.ExEntityWithKeyXAlreadyExists, key));
        result.Key = key;
        result.Tuple = tuple;
        result.PersistenceState = PersistenceState.New;
      }

      if (IsDebugEventLoggingEnabled) {
        OrmLog.Debug(nameof(Strings.LogSessionXCachingY), this, result);
      }

      return result;
    }

    internal bool LookupStateInCache(Key key, out EntityState entityState)
    {
      return EntityStateCache.TryGetItem(key, true, out entityState);
    }

    internal bool LookupStateInCache(Key key, FieldInfo fieldInfo, out EntitySetState entitySetState)
    {
      var entityState = EntityStateCache[key, false];
      if (entityState != null) {
        var entity = entityState.Entity;
        if (entity != null) {
          var entitySet = (EntitySetBase) entity.GetFieldValue(fieldInfo);
          if (entitySet.CheckStateIsLoaded()) {
            entitySetState = entitySet.State;
            return true;
          }
        }
      }
      entitySetState = null;
      return false;
    }

    /// <exception cref="InvalidOperationException">
    /// Attempt to associate non-null <paramref name="tuple"/> with <paramref name="key"/> of unknown type.
    /// </exception>
    internal EntityState UpdateStateInCache(Key key, Tuple tuple, bool isStale)
    {
      var result = EntityStateCache[key, true];
      if (result == null) {
        if (!key.HasExactType && tuple != null)
          throw Exceptions.InternalError(
            Strings.ExCannotAssociateNonEmptyEntityStateWithKeyOfUnknownType,
            OrmLog.Instance);
        result = AddEntityStateToCache(key, tuple, isStale);
        SystemEvents.NotifyEntityMaterialized(result);
        Events.NotifyEntityMaterialized(result);
      }
      else {
        if (!result.Key.HasExactType && key.HasExactType) {
          EntityStateCache.RemoveKey(result.Key);
          result = AddEntityStateToCache(key, tuple, result.IsStale);
        }
        result.Update(tuple);
        result.IsStale = isStale;
        if (IsDebugEventLoggingEnabled) {
          OrmLog.Debug(nameof(Strings.LogSessionXUpdatingCacheY), this, result);
        }
      }
      return result;
    }

    internal EntityState UpdateStateInCache(Key key, Tuple tuple)
    {
      return UpdateStateInCache(key, tuple, false);
    }

    internal EntitySetState UpdateStateInCache(Key key, FieldInfo fieldInfo, IEnumerable<Key> entityKeys,
      bool isFullyLoaded)
    {
      var entityState = EntityStateCache[key, true];
      if (entityState == null)
        return null;
      var entity = entityState.Entity;
      if (entity == null)
        return null;
      var entitySet = (EntitySetBase) entity.GetFieldValue(fieldInfo);
      return entitySet.UpdateState(entityKeys, isFullyLoaded);
    }

    internal void RemoveStateFromCache(Key key, bool removeFromInnerCache = false)
    {
      EntityStateCache.RemoveKey(key, removeFromInnerCache);
    }

    private EntityState AddEntityStateToCache(Key key, Tuple tuple, bool isStale)
    {
      var result = new EntityState(this, key, tuple, isStale) {
        PersistenceState = PersistenceState.Synchronized
      };
      EntityStateCache.Add(result);
      if (IsDebugEventLoggingEnabled) {
        OrmLog.Debug(nameof(Strings.LogSessionXCachingY), this, result);
      }

      return result;
    }

    private void InvalidateEntitySetsWithInvalidState()
    {
      try {
        foreach (var item in entitySetsWithInvalidState)
          ((IInvalidatable) item.State).Invalidate();
      }
      finally {
        entitySetsWithInvalidState.Clear();
      }
    }

    private static ICache<Key, EntityState> CreateSessionCache(SessionConfiguration configuration)
    {
      switch (configuration.CacheType) {
        case SessionCacheType.Infinite:
          return new InfiniteCache<Key, EntityState>(configuration.CacheSize, keyExtractor);
        default:
          return new LruCache<Key, EntityState>(
            configuration.CacheSize, keyExtractor,
            new WeakCache<Key, EntityState>(false, keyExtractor));
      }
    }
  }
}
