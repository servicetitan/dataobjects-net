// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2009.10.09

using System;
using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Reflection;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Orm.Model;


namespace Xtensive.Orm.Internals
{
  internal static class KeyFactory
  {
    private const string GenericKeyNameFormat = "{0}.{1}`{2}";

    public static Key Generate(Session session, TypeInfo typeInfo)
    {
      if (!typeInfo.IsEntity)
        throw new InvalidOperationException(String.Format(Strings.ExCouldNotConstructNewKeyInstanceTypeXIsNotAnEntity, typeInfo));
      var domain = session.Domain;
      var isTemporary = session.LazyKeyGenerationIsEnabled && !session.IsPersisting;
      var keyGenerator = domain.KeyGenerators.Get(typeInfo.Key, isTemporary);
      if (keyGenerator==null)
        throw new InvalidOperationException(String.Format(Strings.ExUnableToCreateKeyForXHierarchy, typeInfo.Hierarchy));
      var keyValue = keyGenerator.GenerateKey(typeInfo.Key, session);
      var key = Materialize(domain, session.StorageNodeId, typeInfo, keyValue, TypeReferenceAccuracy.ExactType, null);

      return key;
    }

    public static Key Materialize(Domain domain, string nodeId,
      TypeInfo type, Tuple value, TypeReferenceAccuracy accuracy, IReadOnlyList<ColNum> keyIndexes)
    {
      var hierarchy = type.Hierarchy;
      var keyInfo = type.Key;
      if (keyIndexes==null) {
        if (value.Descriptor!=keyInfo.TupleDescriptor)
          throw new ArgumentException(Strings.ExWrongKeyStructure);
        if (accuracy==TypeReferenceAccuracy.ExactType) {
          int typeIdColumnIndex = keyInfo.TypeIdColumnIndex;
          if (typeIdColumnIndex >= 0 && !value.GetFieldState(typeIdColumnIndex).IsAvailable())
            // Ensures TypeId is filled in into Keys of newly created Entities
            value.SetValue(typeIdColumnIndex, type.TypeId);
        }
      }
      if (hierarchy?.Root.IsLeaf == true) {
        accuracy = TypeReferenceAccuracy.ExactType;
      }

      var isGenericKey = keyInfo.TupleDescriptor.Count <= WellKnown.MaxGenericKeyLength;
      return isGenericKey
        ? CreateGenericKey(domain, nodeId, type, accuracy, value, keyIndexes)
        : keyIndexes == null ? new LongKey(nodeId, type, accuracy, value)
        : throw Exceptions.InternalError(Strings.ExKeyIndexesAreSpecifiedForNonGenericKey, OrmLog.Instance);
    }

    public static Key Materialize(Domain domain, string nodeId,
      TypeInfo type, TypeReferenceAccuracy accuracy, params object[] values)
    {
      var keyInfo = type.Key;
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
        ArgumentNullException.ThrowIfNull(value, $"values[{valueIndex}]");
        var entity = value as Entity;
        if (entity!=null) {
          entity.EnsureNotRemoved();
          value = entity.Key;
        }
        var key = value as Key;
        if (key!=null) {
          if (key.TypeReference.Type.Hierarchy==type.Hierarchy)
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

      return Materialize(domain, nodeId, type, tuple, accuracy, null);
    }

    public static bool IsValidKeyTuple(Tuple tuple)
    {
      var limit = tuple.Descriptor.Count;
      for (int i = 0; i < limit; i++)
        if (tuple.GetFieldState(i).IsNull())
          return false;
      return true;
    }

    public static bool IsValidKeyTuple(Tuple tuple, IReadOnlyList<ColNum> keyIndexes)
    {
      if (keyIndexes==null)
        return IsValidKeyTuple(tuple);
      var limit = keyIndexes.Count;
      for (int i = 0; i < limit; i++)
        if (tuple.GetFieldState(keyIndexes[i]).IsNull())
          return false;
      return true;
    }

    private static Key CreateGenericKey(Domain domain, string nodeId, TypeInfo type, TypeReferenceAccuracy accuracy, Tuple tuple, IReadOnlyList<ColNum> keyIndexes)
    {
      var keyTypeInfo = domain.GenericKeyFactories.GetOrAdd(type, BuildGenericKeyFactory);
      if (keyIndexes==null)
        return keyTypeInfo.DefaultConstructor(nodeId, type, tuple, accuracy);
      return keyTypeInfo.KeyIndexBasedConstructor(nodeId, type, tuple, accuracy, keyIndexes);
    }

    private static GenericKeyFactory BuildGenericKeyFactory(TypeInfo typeInfo)
    {
      var descriptor = typeInfo.Key.TupleDescriptor;
      var keyTypeName = string.Format(GenericKeyNameFormat, WellKnownOrmTypes.KeyOfT.Namespace, WellKnownOrmTypes.Key.Name, descriptor.Count);
      var keyType = WellKnownOrmTypes.Key.Assembly.GetType(keyTypeName);
      keyType = keyType.MakeGenericType(descriptor.ToArray(descriptor.Count));
      var defaultConstructor = DelegateHelper.CreateDelegate<Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, Key>>(
        null, keyType, "Create", Array.Empty<Type>());
      var keyIndexBasedConstructor = DelegateHelper.CreateDelegate<Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, IReadOnlyList<ColNum>, Key>>(
        null, keyType, "Create", Array.Empty<Type>());
      return new GenericKeyFactory(keyType, defaultConstructor, keyIndexBasedConstructor);
    }
  }
}