// Copyright (C) 2016 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kulakov
// Created:    2016.06.21

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals
{
  using Identifier = (EntityState EntityState, AssociationInfo Association);

  internal struct NonPairedReferenceChangesRegistry
  {
    private readonly Dictionary<Identifier, HashSet<EntityState>> removedReferences = new();
    private readonly Dictionary<Identifier, HashSet<EntityState>> addedReferences = new();
    private readonly object accessGuard = new();

    internal Session Session { get; }
    public int RemovedReferencesCount => removedReferences.Values.Sum(el => el.Count);
    public int AddedReferencesCount => addedReferences.Values.Sum(el => el.Count);

    public IEnumerable<EntityState> GetRemovedReferencesTo(EntityState target, AssociationInfo association)
    {
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(association);

      if (association.IsPaired)
        return Enumerable.Empty<EntityState>();

      var key = MakeKey(target, association);
      return removedReferences.TryGetValue(key, out var removedMap)
        ? removedMap
        : Enumerable.Empty<EntityState>();
    }

    public IEnumerable<EntityState> GetAddedReferenceTo(EntityState target, AssociationInfo association)
    {
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(association);

      if (association.IsPaired)
        return Enumerable.Empty<EntityState>();

      var key = MakeKey(target, association);
      return addedReferences.TryGetValue(key, out var removedMap)
        ? removedMap
        : Enumerable.Empty<EntityState>();
    }

    public void Invalidate() => Clear();

    public void Clear()
    {
      if (accessGuard is not null) {
        lock (accessGuard) {
          removedReferences.Clear();
          addedReferences.Clear();
        }
      }
    }

    private void RegisterChange(EntityState referencedState, EntityState referencingState, EntityState noLongerReferencedState, AssociationInfo association)
    {
      ArgumentNullException.ThrowIfNull(association);
      ArgumentNullException.ThrowIfNull(referencingState);
      if (!Session.DisableAutoSaveChanges)
        return;

      if (association.IsPaired)
        return;

      if (referencedState==null && noLongerReferencedState==null)
        return;
      if (referencedState!=null && noLongerReferencedState!=null) {
        var oldKey = MakeKey(noLongerReferencedState, association);
        var newKey = MakeKey(referencedState, association);
        RegisterRemoveInternal(oldKey, referencingState);
        RegisterAddInternal(newKey, referencingState);
      }
      else if (noLongerReferencedState!=null) {
        var oldKey = MakeKey(noLongerReferencedState, association);
        RegisterRemoveInternal(oldKey, referencingState);
      }
      else {
        var newKey = MakeKey(referencedState, association);
        RegisterAddInternal(newKey, referencingState);
      }
    }

    private void RegisterRemoveInternal(Identifier oldKey, EntityState referencingState)
    {
      HashSet<EntityState> references;
      if (addedReferences.TryGetValue(oldKey, out references)) {
        if (references.Remove(referencingState)) {
          if (references.Count==0)
            addedReferences.Remove(oldKey);
          return;
        }
      }
      if (removedReferences.TryGetValue(oldKey, out references)) {
        if (!references.Add(referencingState))
          throw new InvalidOperationException(Strings.ExReferenceRregistrationErrorReferenceRemovalIsAlreadyRegistered);
        return;
      }
      removedReferences.Add(oldKey, new HashSet<EntityState>{referencingState});
    }

    private void RegisterAddInternal(Identifier newKey, EntityState referencingState)
    {
      HashSet<EntityState> references;
      if (removedReferences.TryGetValue(newKey, out references)) {
        if (references.Remove(referencingState))
          if (references.Count==0)
            removedReferences.Remove(newKey);
        return;
      }
      if (addedReferences.TryGetValue(newKey, out references)) {
        if (!references.Add(referencingState))
          throw new InvalidOperationException(Strings.ExReferenceRegistrationErrorReferenceAdditionIsAlreadyRegistered);
        return;
      }
      addedReferences.Add(newKey, new HashSet<EntityState>{referencingState});
    }

    private Identifier MakeKey(EntityState state, AssociationInfo association) => (state, association);

    private void OnSessionPersisted(object sender, EventArgs e) => Invalidate();

    private void OnEntitySetItemRemoveCompleted(object sender, EntitySetItemActionCompletedEventArgs e)
    {
      if (ShouldSkipRegistration(e))
        return;
      RegisterChange(null, e.Entity.State, e.Item.State, e.Field.Associations.First());
    }

    private void OnEntitySetItemAddCompleted(object sender, EntitySetItemActionCompletedEventArgs e)
    {
      if (ShouldSkipRegistration(e))
        return;
      RegisterChange(e.Item.State, e.Entity.State, null, e.Field.Associations.First());
    }

    private void OnEntityFieldValueSetCompleted(object sender, EntityFieldValueSetCompletedEventArgs e)
    {
      if (ShouldSkipRegistration(e))
        return;
      if (e.Field.IsStructure)
        HandleStructureValues(e.Entity, e.Field, (Structure) e.OldValue, (Structure) e.NewValue);
      else
        HandleEntityValues(e.Entity, e.Field, (Entity) e.OldValue, (Entity) e.NewValue);
    }

    private void HandleStructureValues(Entity owner, FieldInfo fieldOfOwner, Structure oldValue, Structure newValue)
    {
      var referenceFields = oldValue.TypeInfo.Fields.Where(f => f.IsEntity).Union(oldValue.TypeInfo.StructureFieldMapping.Values.Where(f => f.IsEntity));

      foreach (var referenceFieldOfStructure in referenceFields) {
        var realField = owner.TypeInfo.Fields[BuildNameOfEntityField(fieldOfOwner, referenceFieldOfStructure)];
        if (realField.Associations.Count>1)
          continue;
        var realAssociation = realField.Associations.First();
        var oldFieldValue = GetStructureFieldValue(referenceFieldOfStructure, oldValue);
        var newFieldValue = GetStructureFieldValue(referenceFieldOfStructure, newValue);
        RegisterChange(newFieldValue, owner.State, oldFieldValue, realAssociation);
      }
    }

    private void HandleEntityValues(Entity owner, FieldInfo field, Entity oldValue, Entity newValue)
    {
      var oldEntityState = oldValue?.State;
      var newEntityState = newValue?.State;
      var association = field.GetAssociation((oldValue ?? newValue).TypeInfo);
      RegisterChange(newEntityState, owner.State, oldEntityState, association);
    }

    private bool ShouldSkipRegistration(EntityFieldValueSetCompletedEventArgs e)
    {
      if (!Session.DisableAutoSaveChanges)
        return true;
      if (Session.IsPersisting)
        return true;
      if (e.Exception!=null)
        return true;
      if (!e.Field.IsEntity && !e.Field.IsStructure)
        return true;
      if (e.Field.IsEntity && e.Field.Associations.First().IsPaired)
        return true;
      if (e.NewValue==null && e.OldValue==null)
        return true;
      return false;
    }

    private bool ShouldSkipRegistration(EntitySetItemActionCompletedEventArgs e) =>
      !Session.DisableAutoSaveChanges || Session.IsPersisting || e.Exception != null || e.Field.Associations.First().IsPaired;

    private EntityState GetStructureFieldValue(FieldInfo fieldOfStructure, Structure structure) =>
      ((Entity) structure?.GetFieldAccessor(fieldOfStructure).GetUntypedValue(structure))?.State;

    private string BuildNameOfEntityField(FieldInfo fieldOfOwner, FieldInfo referenceFieldOfStructure) =>
      $"{fieldOfOwner.Name}.{referenceFieldOfStructure.Name}";

    internal NonPairedReferenceChangesRegistry(Session session)
    {
      Session = session;

      var systemEvents = session.SystemEvents;
      systemEvents.EntityFieldValueSetCompleted += OnEntityFieldValueSetCompleted;
      systemEvents.EntitySetItemAddCompleted += OnEntitySetItemAddCompleted;
      systemEvents.EntitySetItemRemoveCompleted += OnEntitySetItemRemoveCompleted;
      systemEvents.Persisted += OnSessionPersisted;
    }
  }
}
