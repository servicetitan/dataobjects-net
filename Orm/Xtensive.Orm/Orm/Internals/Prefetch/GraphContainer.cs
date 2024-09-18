// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexander Nikolaev
// Created:    2009.09.17

using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Tuples;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals.Prefetch
{
  internal sealed class GraphContainer
  {
    private Dictionary<FieldInfo, ReferencedEntityContainer> referencedEntityContainers;
    private Dictionary<FieldInfo, EntitySetTask> entitySetTasks;
    private readonly bool exactType;
    private int? cachedHashCode;

    public readonly Key Key;
    public readonly TypeInfo Type;
    public readonly PrefetchManager Manager;

    public RootEntityContainer RootEntityContainer { get; private set; }

    public bool ContainsTask
    {
      get {
        return RootEntityContainer != null
          || (referencedEntityContainers != null && referencedEntityContainers.Count > 0)
          || (entitySetTasks != null && entitySetTasks.Count > 0);
      }
    }

    public IEnumerable<ReferencedEntityContainer> ReferencedEntityContainers
    {
      get { return referencedEntityContainers != null ? referencedEntityContainers.Values : null; }
    }

    public IEnumerable<EntitySetTask> EntitySetTasks
    {
      get { return entitySetTasks != null ? entitySetTasks.Values : null; }
    }

    public void AddEntityColumns(IEnumerable<ColumnInfo> columns)
    {
      if (RootEntityContainer == null)
        RootEntityContainer = new RootEntityContainer(Key, Type, exactType, Manager);
      RootEntityContainer.AddColumns(columns);
    }

    public void CreateRootEntityContainer(
      SortedDictionary<ColNum, ColumnInfo> forcedColumns, IReadOnlyList<ColNum> forcedColumnsToBeLoaded)
    {
      RootEntityContainer = new RootEntityContainer(Key, Type, exactType, Manager);
      RootEntityContainer.SetColumnCollections(forcedColumns, forcedColumnsToBeLoaded);
    }

    public void RegisterReferencedEntityContainer(
      EntityState ownerState, in PrefetchFieldDescriptor referencingFieldDescriptor)
    {
      var field = referencingFieldDescriptor.Field;
      if (referencedEntityContainers?.ContainsKey(field) == true)
        return;
      if (!AreAllForeignKeyColumnsLoaded(ownerState, field))
        RegisterFetchByUnknownForeignKey(referencingFieldDescriptor);
      else
        RegisterFetchByKnownForeignKey(referencingFieldDescriptor, ownerState);
    }

    public void RegisterEntitySetTask(EntityState ownerState, in PrefetchFieldDescriptor referencingFieldDescriptor)
    {
      entitySetTasks ??= new();
      if (RootEntityContainer == null)
        AddEntityColumns(Key.TypeReference.Type.Fields
          .Where(field => field.IsPrimaryKey || field.IsSystem).SelectMany(field => field.Columns));
      EntitySetTask task;
      if (!entitySetTasks.TryGetValue(referencingFieldDescriptor.Field, out task))
        entitySetTasks.Add(referencingFieldDescriptor.Field,
          new EntitySetTask(Key, referencingFieldDescriptor, ownerState != null, Manager));
      else if (task.ItemCountLimit == null)
        return;
      else if (referencingFieldDescriptor.EntitySetItemCountLimit == null
        || task.ItemCountLimit < referencingFieldDescriptor.EntitySetItemCountLimit)
        entitySetTasks[referencingFieldDescriptor.Field] =
          new EntitySetTask(Key, referencingFieldDescriptor, ownerState != null, Manager);
    }

    public void NotifyAboutExtractionOfKeysWithUnknownType()
    {
      if (RootEntityContainer != null)
        RootEntityContainer.NotifyOwnerAboutKeyWithUnknownType();
      if (referencedEntityContainers == null)
        return;
      foreach (var pair in referencedEntityContainers)
        pair.Value.NotifyOwnerAboutKeyWithUnknownType();
    }

    public bool Equals(GraphContainer other)
    {
      if (other is null)
        return false;
      if (ReferenceEquals(this, other))
        return true;
      if (!Type.Equals(other.Type))
        return false;
      return Key.Equals(other.Key);
    }

    public override bool Equals(object obj) =>
      ReferenceEquals(this, obj)
        || obj is GraphContainer other && Equals(other);

    public override int GetHashCode()
    {
      cachedHashCode ??= HashCode.Combine(Key, Type);
      return cachedHashCode.Value;
    }

    #region Private . internal methods

    private static bool AreAllForeignKeyColumnsLoaded(EntityState state, FieldInfo field)
    {
      if (state == null || !state.IsTupleLoaded)
        return false;
      var tuple = state.Tuple;
      var fieldStateMap = tuple.GetFieldStateMap(TupleFieldState.Available);
      var fieldMappingInfo = field.MappingInfo;
      for (var i = 0; i < fieldMappingInfo.Length; i++)
        if (!fieldStateMap[fieldMappingInfo.Offset + i])
          return false;
      return true;
    }

    private void RegisterFetchByKnownForeignKey(in PrefetchFieldDescriptor referencingFieldDescriptor,
      EntityState ownerState)
    {
      var association = referencingFieldDescriptor.Field.Associations.Last();
      var referencedKeyTuple = association
        .ExtractForeignKey(ownerState.Type, ownerState.Tuple);
      var referencedKeyTupleState = referencedKeyTuple.GetFieldStateMap(TupleFieldState.Null);
      for (var i = 0; i < referencedKeyTupleState.Length; i++)
        if (referencedKeyTupleState[i])
          return;
      var session = Manager.Owner.Session;
      var referencedKey = Key.Create(session.Domain, session.StorageNodeId,
        association.TargetType, TypeReferenceAccuracy.BaseType,
        referencedKeyTuple);
      var targetType = association.TargetType;
      var areToNotifyOwner = true;
      TypeInfo exactReferencedType;
      var hasExactTypeBeenGotten = PrefetchHelper.TryGetExactKeyType(referencedKey, Manager,
        out exactReferencedType);
      if (hasExactTypeBeenGotten != null) {
        if (hasExactTypeBeenGotten.Value) {
          targetType = exactReferencedType;
          areToNotifyOwner = false;
        }
      }
      else
        return;
      var fieldsToBeLoaded = PrefetchHelper
        .GetCachedDescriptorsForFieldsLoadedByDefault(session.Domain, targetType);
      var graphContainer = Manager.SetUpContainers(referencedKey, targetType,
        fieldsToBeLoaded, true, null, true);
      if (areToNotifyOwner)
        graphContainer.RootEntityContainer.SetParametersOfReference(referencingFieldDescriptor, referencedKey);
    }

    private void RegisterFetchByUnknownForeignKey(in PrefetchFieldDescriptor referencingFieldDescriptor)
    {
      referencedEntityContainers ??= new();
      referencedEntityContainers.Add(referencingFieldDescriptor.Field, new ReferencedEntityContainer(Key,
        referencingFieldDescriptor, exactType, Manager));
    }

    #endregion


    // Constructors

    public GraphContainer(Key key, TypeInfo type, bool exactType, PrefetchManager manager)
    {
      ArgumentNullException.ThrowIfNull(key);
      ArgumentNullException.ThrowIfNull(type);
      ArgumentNullException.ThrowIfNull(manager, "processor");

      Key = key;
      Type = type;

      Manager = manager;
      this.exactType = exactType;
    }
  }
}
