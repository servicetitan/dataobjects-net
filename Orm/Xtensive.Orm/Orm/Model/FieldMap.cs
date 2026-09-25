// Copyright (C) 2007-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2007.12.27

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Orm.Model
{
  /// <summary>
  /// Maps type fields to interface fields and vice versa.
  /// </summary>
  public sealed class FieldMap: LockableBase, IEnumerable<KeyValuePair<FieldInfo, FieldInfo>>
  {
    internal static readonly FieldMap Empty = new FieldMap().InitLocked();

    private readonly Dictionary<FieldInfo, FieldInfo> map = new();
    private readonly Dictionary<FieldInfo, HashSet<FieldInfo>> reversedMap = new();

    public FieldInfo this[FieldInfo interfaceField] => map[interfaceField];

    public IEnumerable<FieldInfo> GetImplementedInterfaceFields(FieldInfo typeField)
    {
      if (!reversedMap.TryGetValue(typeField, out var value))
        return Enumerable.Empty<FieldInfo>();
      return value;
    }

    public int Count => map.Count;

    public bool ContainsKey(FieldInfo interfaceField) => map.ContainsKey(interfaceField);

    internal bool TryAdd(FieldInfo interfaceField, FieldInfo typeField)
    {
      EnsureNotLocked();
      if (!map.TryAdd(interfaceField, typeField)) {
        return false;
      }
      if (reversedMap.TryGetValue(typeField, out var interfaceFields)) {
        _ = interfaceFields.Add(interfaceField);
      }
      else
        reversedMap.Add(typeField, new HashSet<FieldInfo> { interfaceField });
      return true;
    }

    public void Add(FieldInfo interfaceField, FieldInfo typeField)
    {
      if (!TryAdd(interfaceField, typeField)) {
        throw new ArgumentException("An element with the same key already exists");
      }
    }

    public void AddOrOverride(FieldInfo interfaceField, FieldInfo typeField)
    {
      if (!TryAdd(interfaceField, typeField)) {
        var oldTypeField = map[interfaceField];
        reversedMap.Remove(oldTypeField, out var interfaceFields);
        map[interfaceField] = typeField;
        reversedMap.Add(typeField, interfaceFields);
      }
    }

    public bool TryGetValue(FieldInfo interfaceField, out FieldInfo typeField)
      => map.TryGetValue(interfaceField, out typeField);

    IEnumerator<KeyValuePair<FieldInfo, FieldInfo>> IEnumerable<KeyValuePair<FieldInfo, FieldInfo>>.GetEnumerator()
      => map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<FieldInfo, FieldInfo>>) this).GetEnumerator();


    // Constructors

    internal FieldMap()
    {
    }
  }
}
