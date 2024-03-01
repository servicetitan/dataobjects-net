// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexander Nikolaev
// Created:    2009.10.22

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals.Prefetch
{
  internal static class PrefetchHelper
  {
    private static readonly Func<TypeInfo, IReadOnlyList<PrefetchFieldDescriptor>> CreateDescriptorsForFieldsLoadedByDefault = type =>
      type.Fields
        .Where(field => field.Parent == null && IsFieldToBeLoadedByDefault(field))
        .Select(field => new PrefetchFieldDescriptor(field, false, false))
        .ToList()
        .AsSafeWrapper();

    public static bool IsFieldToBeLoadedByDefault(FieldInfo field)
    {
      return field.IsPrimaryKey || field.IsSystem || (!field.IsLazyLoad && !field.IsEntitySet);
    }

    public static IReadOnlyList<PrefetchFieldDescriptor> GetCachedDescriptorsForFieldsLoadedByDefault(Domain domain, TypeInfo type)
    {
      return domain.PrefetchFieldDescriptorCache.GetOrAdd(type, CreateDescriptorsForFieldsLoadedByDefault);
    }

    public static bool? TryGetExactKeyType(Key key, PrefetchManager manager, out TypeInfo type)
    {
      type = null;
      var keyTypeReferenceType = key.TypeReference.Type;
      if (!keyTypeReferenceType.IsLeaf) {
        var cachedKey = key;
        if (!manager.TryGetTupleOfNonRemovedEntity(ref cachedKey, out var state))
          return null;
        if (cachedKey.HasExactType) {
          type = cachedKey.TypeReference.Type;
          return true;
        }
        return false;
      }
      type = keyTypeReferenceType;
      return true;
    }

    public static SortedDictionary<ColNum, ColumnInfo> GetColumns(IEnumerable<ColumnInfo> candidateColumns,
      TypeInfo type)
    {
      var columns = new SortedDictionary<ColNum, ColumnInfo>();
      AddColumns(candidateColumns, columns, type);
      return columns;
    }

    public static bool AddColumns(IEnumerable<ColumnInfo> candidateColumns,
      SortedDictionary<ColNum, ColumnInfo> columns, TypeInfo type)
    {
      var result = false;
      var typeIsInterface = type.IsInterface;
      var typeFields = type.Fields;
      var typeFieldMap = type.FieldMap;
      foreach (var column in candidateColumns) {
        result = true;
        var columnField = column.Field;
        var columnIsInterface = columnField.DeclaringType.IsInterface;
        var fieldInfo = typeIsInterface == columnIsInterface ? typeFields[columnField.Name]
          : columnIsInterface ? typeFieldMap[columnField] : throw new InvalidOperationException();
        columns[fieldInfo.MappingInfo.Offset] = column;
      }
      return result;
    }

    public static List<ColNum> GetColumnsToBeLoaded(SortedDictionary<ColNum, ColumnInfo> userColumnIndexes,
      TypeInfo type)
    {
      var result = new List<ColNum>(userColumnIndexes.Count);
      result.AddRange(type.Indexes.PrimaryIndex.ColumnIndexMap.System);
      result.AddRange(userColumnIndexes.Where(pair => !pair.Value.IsPrimaryKey
        && !pair.Value.IsSystem).Select(pair => pair.Key));
      return result;
    }
  }
}