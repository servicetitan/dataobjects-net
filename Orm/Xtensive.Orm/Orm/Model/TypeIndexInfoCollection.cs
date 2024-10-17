// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2007.11.26

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Orm.Model
{
  /// <summary>
  /// A collection of indexes that belongs to a particular <see cref="TypeInfo"/>.
  /// </summary>
  [Serializable]
  public sealed class TypeIndexInfoCollection : IndexInfoCollection
  {
    private IndexInfo primaryIndex;
    private IReadOnlyList<IndexInfo> realPrimaryIndexes;
    private IReadOnlyList<IndexInfo> indexesContainingAllData;

    /// <summary>
    /// Gets the primary index in this instance.
    /// </summary>
    public IndexInfo PrimaryIndex
    {
      [DebuggerStepThrough]
      get { return IsLocked ? primaryIndex : FindPrimaryIndex(); }
    }

    /// <summary>
    /// Gets the list of real primary index in this instance.
    /// </summary>
    public IReadOnlyList<IndexInfo> RealPrimaryIndexes
    {
      [DebuggerStepThrough]
      get {
        return IsLocked
          ? realPrimaryIndexes
          : FindRealPrimaryIndexes(PrimaryIndex).AsSafeWrapper();
      }
    }

    public IndexInfo FindFirst(IndexAttributes indexAttributes) =>
      Find(indexAttributes).FirstOrDefault();

    [DebuggerStepThrough]
    public IndexInfo GetIndex(string fieldName, params string[] fieldNames)
    {
      var names = (fieldNames ?? Array.Empty<string>()).Prepend(fieldName);

      var fields = new List<FieldInfo>();
      var reflectedTypeFields = primaryIndex.ReflectedType.Fields;
      foreach (var name in names) {
        if (reflectedTypeFields.TryGetValue(name, out var field)) {
          fields.Add(field);
        }
      }
      if (fields.Count == 0) {
        return null;
      }

      return GetIndex(fields);
    }

    public IndexInfo GetIndex(FieldInfo field, params FieldInfo[] fields) =>
      GetIndex(fields.Prepend(field));

    /// <inheritdoc/>
    public override void UpdateState()
    {
      base.UpdateState();
      primaryIndex = FindPrimaryIndex();
      realPrimaryIndexes = FindRealPrimaryIndexes(primaryIndex).ToArray().AsSafeWrapper();
      indexesContainingAllData = FindIndexesContainingAllData().ToArray().AsSafeWrapper();
    }

    private IndexInfo GetIndex(IEnumerable<FieldInfo> fields)
    {
      var columns = new List<ColumnInfo>();

      void columnsExtractor(IEnumerable<FieldInfo> fieldsToExtract) {
        foreach (var field in fieldsToExtract) {
          if (field.Column != null) {
            columns.Add(field.Column);
          }
          else if (field.IsEntity || field.IsStructure) {
            columnsExtractor(field.Fields);
          }
        }
      }

      columnsExtractor(fields);
      var columnNumber = columns.Count;

      var candidates = this
        .Where(i => i.KeyColumns.Take(columnNumber)
          .Select((pair, index) => (column: pair.Key, columnIndex: index))
          .All(p => p.column == columns[p.columnIndex]))
        .OrderByDescending(i => i.Attributes).ToList();

      return candidates.Where(c => c.KeyColumns.Count == columnNumber).FirstOrDefault() ?? candidates.FirstOrDefault();
    }

    /// <summary>
    /// Gets the minimal set of indexes containing all data for the type.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<IndexInfo> GetIndexesContainingAllData()
    {
      return IsLocked
        ? indexesContainingAllData
        : FindIndexesContainingAllData().AsSafeWrapper();
    }

    private IReadOnlyList<IndexInfo> FindIndexesContainingAllData()
    {
      var result = new List<IndexInfo>(Count);
      var virtualIndexes = this.Where(index => index.IsVirtual);
      result.AddRange(virtualIndexes);
      var realIndexes = from index in this where !index.IsVirtual
                          && (index.Attributes & IndexAttributes.Abstract) == 0
                          && result.Count(virtualIndex => virtualIndex.UnderlyingIndexes.Contains(index)) == 0
                        select index;
      result.AddRange(realIndexes);
      return result;
    }

    private IndexInfo FindPrimaryIndex()
    {
      var result = this.Where(i => i.IsVirtual && i.IsPrimary).OrderByDescending(i => i.Attributes).FirstOrDefault();
      return result ?? FindFirst(IndexAttributes.Real | IndexAttributes.Primary);
    }

    private IReadOnlyList<IndexInfo> FindRealPrimaryIndexes(IndexInfo index)
    {
      if (index == null) {
        return Array.Empty<IndexInfo>();
      }
      if (index.IsPrimary && !index.IsVirtual) {
        return new[] { index };
      }
      var result = new List<IndexInfo>();
      foreach (IndexInfo underlyingIndex in index.UnderlyingIndexes) {
        if (underlyingIndex.IsPrimary && !underlyingIndex.IsVirtual)
          result.Add(underlyingIndex);
        else
          result.AddRange(FindRealPrimaryIndexes(underlyingIndex));
      }
      return result;
    }


    // Constructors

    /// <inheritdoc/>
    public TypeIndexInfoCollection(Node owner, string name)
      : base(owner, name)
    {
    }
  }
}
