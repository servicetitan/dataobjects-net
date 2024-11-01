// Copyright (C) 2014-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2014.03.13

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Orm.Model
{
  /// <summary>
  /// Dual-mapping between type identifiers and <see cref="TypeInfo"/>.
  /// </summary>
  [Serializable]
  public sealed class TypeIdRegistry : LockableBase
  {
    private readonly IReadOnlyList<TypeInfo> sharedIdToTypeInfo;

    // Typical case: TypeId is small integer
    private UInt16[] typeIdToSharedId;
    private UInt16[] sharedIdToTypeId;

    // For backward compatibility: TypeId may be >= 65536 because of some DB manipulations
    private IDictionary<TypeInfo, int> mapping;
    private IDictionary<int, TypeInfo> reverseMapping;

    /// <summary>
    /// Gets collection of registered types.
    /// </summary>
    public IEnumerable<TypeInfo> Types =>
      mapping?.Keys ?? typeIdToSharedId.Select(o => sharedIdToTypeInfo[o]).OfType<TypeInfo>();

    /// <summary>
    /// Gets collection of registered type identifiers.
    /// </summary>
    public IEnumerable<int> TypeIdentifiers =>
      reverseMapping?.Keys ?? sharedIdToTypeId.Where(o => o != TypeInfo.NoTypeId).Select(o => (int)o);

    /// <summary>
    /// Gets type identifier for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Type to get type identifier for.</param>
    /// <returns>Type identifier for the specified <paramref name="type"/>.</returns>
    public int this[TypeInfo type]
    {
      get {
        ArgumentNullException.ThrowIfNull(type);

        int result = mapping is null ? sharedIdToTypeId[type.SharedId]
          : mapping.TryGetValue(type, out var r) ? r : 0;
        return result == 0
          ? throw new KeyNotFoundException(string.Format(Strings.ExTypeXIsNotRegistered, type.Name))
          : result;
      }
    }

    /// <summary>
    /// Gets type for the specified <paramref name="typeId"/>.
    /// </summary>
    /// <param name="typeId">Type identifier to get type for.</param>
    /// <returns>Type for the specified <paramref name="typeId"/>.</returns>
    public TypeInfo this[int typeId] =>
        (mapping is null
          ? sharedIdToTypeInfo[typeIdToSharedId[typeId]]
          : reverseMapping.TryGetValue(typeId, out var result) ? result : null) ?? throw new KeyNotFoundException(string.Format(Strings.ExTypeIdXIsNotRegistered, typeId));

    /// <summary>
    /// Checks if specified <paramref name="type"/> is registered.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if <paramref name="type"/> is registered,
    /// otherwise false.</returns>
    public bool Contains(TypeInfo type)
    {
      ArgumentNullException.ThrowIfNull(type);
      return mapping?.ContainsKey(type) ?? sharedIdToTypeId[type.SharedId] != TypeInfo.NoTypeId;
    }

    /// <summary>
    /// Gets type identifier for the specified <paramref name="type"/>.
    /// Unlike <see cref="this[Xtensive.Orm.Model.TypeInfo]"/>
    /// this method does not throw <see cref="KeyNotFoundException"/>
    /// if <paramref name="type"/> is not registered.
    /// </summary>
    /// <param name="type">Type to get type identifier for.</param>
    /// <returns>Type identifier for <paramref name="type"/> if it is registered,
    /// otherwise <see cref="TypeInfo.NoTypeId"/>.</returns>
    public int GetTypeId(TypeInfo type)
    {
      ArgumentNullException.ThrowIfNull(type);
      return mapping is null
        ? sharedIdToTypeId[type.SharedId]
        : mapping.TryGetValue(type, out var result) ? result : TypeInfo.NoTypeId;
    }

    /// <summary>
    /// Resets all mapping information.
    /// </summary>
    public void Clear()
    {
      EnsureNotLocked();

      sharedIdToTypeId = typeIdToSharedId = null;
      mapping = null;
      reverseMapping = null;
    }

    /// <summary>
    /// Registers mapping between <paramref name="typeId"/>
    /// and <paramref name="type"/>.
    /// </summary>
    /// <param name="typeId">Type identifier.</param>
    /// <param name="type">Type.</param>
    public void Register(int typeId, TypeInfo type)
    {
      ArgumentNullException.ThrowIfNull(type);
      EnsureNotLocked();

      if (mapping is null) {
        if ((uint)typeId <= UInt16.MaxValue && type.SharedId <= UInt16.MaxValue) {
          sharedIdToTypeId ??= new UInt16[1];
          typeIdToSharedId ??= new UInt16[1];
          Array.Resize(ref sharedIdToTypeId, Math.Max(type.SharedId + 10, Math.Max(sharedIdToTypeInfo.Count, sharedIdToTypeId.Length)));
          sharedIdToTypeId[type.SharedId] = (UInt16)typeId;
          Array.Resize(ref typeIdToSharedId, Math.Max(typeIdToSharedId.Length, typeId + 10));
          typeIdToSharedId[typeId] = (UInt16) type.SharedId;
          return;
        }
        mapping = new Dictionary<TypeInfo, int>();
        reverseMapping = new Dictionary<int, TypeInfo>();
        if (typeIdToSharedId is not null) {
          for (var sharedId = 1; sharedId < sharedIdToTypeId.Length; ++sharedId) {
            var tid = sharedIdToTypeId[sharedId];
            if (tid != 0) {
              mapping[reverseMapping[tid] = sharedIdToTypeInfo[sharedId]] = tid;
            }
          }
          sharedIdToTypeId = typeIdToSharedId = null;
        }
      }
      mapping[type] = typeId;
      reverseMapping[typeId] = type;
    }

    public override void Lock(bool recursive)
    {
      base.Lock(recursive);
      mapping = mapping?.ToFrozenDictionary();
      reverseMapping = reverseMapping?.ToFrozenDictionary();
    }

    public TypeIdRegistry(IReadOnlyList<TypeInfo> sharedIdToTypeInfo)
    {
      this.sharedIdToTypeInfo = sharedIdToTypeInfo;
    }
  }
}
