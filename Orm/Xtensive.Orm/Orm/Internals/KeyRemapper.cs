﻿// Copyright (C) 2014-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2014.05.06

using System.Linq;
using Xtensive.Tuples.Transform;

namespace Xtensive.Orm.Internals
{
  internal struct KeyRemapper(Session session)
  {
    /// <summary>
    /// Remap temporary (local) keys to real (will be saved to storage) keys.
    /// </summary>
    /// <param name="registry">Registry that contains changed <see cref="EntityState">states of entity.</see></param>
    /// <returns>Mapping temporary keys to real keys.</returns>
    public KeyMapping Remap(EntityChangeRegistry registry)
    {
      var context = new RemapContext(registry);
      RemapEntityKeys(context);
      RemapReferencesToEntities(context);
      session.ReferenceFieldsChangesRegistry.Clear();
      return context.KeyMapping;
    }
    
    private void RemapEntityKeys(RemapContext context)
    {
      var domain = session.Domain;
      foreach (var entityState in context.EntitiesToRemap.Where(el => el.Key.IsTemporary(domain))) {
        var newKey = Key.Generate(session, entityState.Entity.TypeInfo);
        context.RegisterKeyMap(entityState.Key, newKey);
      }
    }

    private void RemapReferencesToEntities(RemapContext context)
    {
      foreach (var setInfo in session.ReferenceFieldsChangesRegistry.GetItems())
        if (setInfo.Field.IsEntitySet)
          RemapEntitySetReference(context, setInfo);
        else
          RemapEntityReference(context, setInfo);
    }

    private void RemapEntitySetReference(RemapContext context, ReferenceFieldChangeInfo info)
    {
      var fieldAssociation = info.Field.GetAssociation(info.FieldValue.TypeInfo);
      if (!fieldAssociation.IsMaster && fieldAssociation.IsPaired)
        return;

      var oldCombinedKey = info.AuxiliaryEntity;

      var fieldOwnerKey = context.TryRemapKey(info.FieldOwner);
      var fieldValueKey = context.TryRemapKey(info.FieldValue);

      var transformer = new CombineTransform(false, fieldOwnerKey.Value.Descriptor, fieldValueKey.Value.Descriptor);
      var combinedTuple = transformer.Apply(TupleTransformType.Tuple, fieldOwnerKey.Value, fieldValueKey.Value);

      var newCombinedKey = Key.Create(session.Domain, session.StorageNodeId, fieldAssociation.AuxiliaryType, TypeReferenceAccuracy.ExactType, combinedTuple);
      context.RegisterKeyMap(oldCombinedKey, newCombinedKey);
    }

    private void RemapEntityReference(RemapContext context, ReferenceFieldChangeInfo info)
    {
      var entity = session.Query.SingleOrDefault(info.FieldOwner);
      if (entity==null)
        return;
      var referencedEntity = (Entity) entity.GetFieldValue(info.Field);
      if (referencedEntity==null)
        return;
      var referencedKey = referencedEntity.Key;
      var realKey = context.TryRemapKey(referencedKey);
      entity.SetReferenceKey(info.Field, realKey);
    }
  }
}
