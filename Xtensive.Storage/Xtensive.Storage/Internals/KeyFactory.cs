// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.10.09

using System;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Diagnostics;
using Xtensive.Core.Reflection;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Model;
using Xtensive.Storage.Resources;

namespace Xtensive.Storage.Internals
{
  [Serializable]
  internal static class KeyFactory
  {
    private const string GenericKeyNameFormat = "{0}.{1}`{2}";

    public static Key CreateNext(TypeInfo type)
    {
      if (!type.IsEntity)
        throw new InvalidOperationException(
          string.Format(Strings.ExCouldNotConstructNewKeyInstanceTypeXIsNotAnEntity, type));
      var domain = Domain.Demand();
      var keyGenerator = domain.KeyGenerators[type.KeyProviderInfo];
      if (keyGenerator==null)
        throw new InvalidOperationException(
          String.Format(Strings.ExUnableToCreateKeyForXHierarchy, type.Hierarchy));
      var keyValue = keyGenerator.Next();
      return Create(type, keyValue, null, TypeReferenceAccuracy.ExactType, false);
    }

    /// <exception cref="ArgumentException">Wrong key structure for the specified <paramref name="type"/>.</exception>
    public static Key Create(TypeInfo type, Tuple value, int[] keyIndexes, TypeReferenceAccuracy accuracy, bool canCache)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      ArgumentValidator.EnsureArgumentNotNull(value, "value");

      var domain = Domain.Demand();
      var hierarchy = type.Hierarchy;
      var keyInfo = type.KeyProviderInfo;
      if (keyIndexes==null) {
        if (value.Descriptor!=keyInfo.TupleDescriptor)
          throw new ArgumentException(Strings.ExWrongKeyStructure);
        if (accuracy == TypeReferenceAccuracy.ExactType) {
          int typeIdColumnIndex = keyInfo.TypeIdColumnIndex;
          if (typeIdColumnIndex >= 0 && !value.GetFieldState(typeIdColumnIndex).IsAvailable())
            // Ensures TypeId is filled in into Keys of newly created Entities
            value.SetValue(typeIdColumnIndex, type.TypeId);
        }
      }
      if (hierarchy != null && hierarchy.Root.IsLeaf) {
        accuracy = TypeReferenceAccuracy.ExactType;
        canCache = false; // No reason to cache
      }

      Key key;
      var isGenericKey = keyInfo.Length <= WellKnown.MaxGenericKeyLength;
      if (isGenericKey)
        key = CreateGenericKey(domain, type, accuracy, value, keyIndexes);
      else {
        if (keyIndexes!=null)
          throw Exceptions.InternalError(Strings.ExKeyIndexesAreSpecifiedForNonGenericKey, Log.Instance);
        key = new LongKey(type, accuracy, value);
      }
      if (!canCache || domain==null)
        return key;
      var keyCache = domain.KeyCache;
      lock (keyCache) {
        Key foundKey;
        if (keyCache.TryGetItem(key, true, out foundKey))
          key = foundKey;
        else {
          if (accuracy == TypeReferenceAccuracy.ExactType)
            keyCache.Add(key);
        }
      }
      return key;
    }

    public static Key Create(TypeInfo type, TypeReferenceAccuracy accuracy, params object[] values)
    {
      ArgumentValidator.EnsureArgumentNotNull(values, "values");
      var keyInfo = type.KeyProviderInfo;
      ArgumentValidator.EnsureArgumentIsInRange(values.Length, 1, keyInfo.TupleDescriptor.Count, "values");

      var tuple = Tuple.Create(keyInfo.TupleDescriptor);
      int typeIdIndex = keyInfo.TypeIdColumnIndex;
      if (typeIdIndex>=0)
        tuple.SetValue(typeIdIndex, type.TypeId);

      int tupleIndex = 0;
      if (tupleIndex==typeIdIndex)
        tupleIndex++;
      for (int valueIndex = 0; valueIndex < values.Length; valueIndex++) {
        var value = values[valueIndex];
        ArgumentValidator.EnsureArgumentNotNull(value, String.Format("values[{0}]", valueIndex));
        var entity = value as Entity;
        if (entity!=null)
          value = entity.Key;
        var key = value as Key;
        if (key!=null) {
          if (key.TypeRef.Type.Hierarchy==type.Hierarchy)
            typeIdIndex = -1; // Key must be fully copied in this case
          for (int keyIndex = 0; keyIndex < key.Value.Count; keyIndex++) {
            tuple.SetValue(tupleIndex++, key.Value.GetValueOrDefault(keyIndex));
            if (tupleIndex==typeIdIndex)
              tupleIndex++;
          }
          continue;
        }
        tuple.SetValue(tupleIndex++, value);
        if (tupleIndex==typeIdIndex)
          tupleIndex++;
      }
      if (tupleIndex != tuple.Count)
        throw new ArgumentException(String.Format(
          Strings.ExSpecifiedValuesArentEnoughToCreateKeyForTypeX, type.Name));

      return Create(type, tuple, null, accuracy, false);
    }

    private static Key CreateGenericKey(Domain domain, TypeInfo type, TypeReferenceAccuracy accuracy,
      Tuple tuple, int[] keyIndexes)
    {
      var keyTypeInfo = domain.genericKeyTypes.GetValue(type.TypeId, BuildGenericKeyTypeInfo, type);
      if (keyIndexes==null)
        return keyTypeInfo.DefaultConstructor(type, tuple, accuracy);
      return keyTypeInfo.KeyIndexBasedConstructor(type, tuple, accuracy, keyIndexes);
    }

    private static GenericKeyTypeInfo BuildGenericKeyTypeInfo(int typeId, TypeInfo typeInfo)
    {
      var descriptor = typeInfo.KeyProviderInfo.TupleDescriptor;
      int length = descriptor.Count;
      var keyType = typeof (Key).Assembly.GetType(
        String.Format(GenericKeyNameFormat, typeof (Key<>).Namespace, typeof(Key).Name, length));
      keyType = keyType.MakeGenericType(descriptor.ToArray());
      return new GenericKeyTypeInfo(keyType,
        DelegateHelper.CreateDelegate<Func<TypeInfo, Tuple, TypeReferenceAccuracy, Key>>(null, keyType, "Create", ArrayUtils<Type>.EmptyArray),
        DelegateHelper.CreateDelegate<Func<TypeInfo, Tuple, TypeReferenceAccuracy, int[], Key>>(null, keyType,
          "Create", ArrayUtils<Type>.EmptyArray));
    }
  }
}