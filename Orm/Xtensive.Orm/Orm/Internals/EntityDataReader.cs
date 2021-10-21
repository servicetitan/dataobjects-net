// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2008.11.02

using System.Collections.Generic;
using System.Linq;
using Xtensive.Caching;
using Xtensive.Core;
using Xtensive.Orm.Linq.Materialization;
using Xtensive.Orm.Model;
using Xtensive.Orm.Rse;
using Xtensive.Tuples;
using Xtensive.Tuples.Transform;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals
{
  internal class EntityDataReader : DomainBound
  {
    private readonly struct RecordPartMapping
    {
      public int TypeIdColumnIndex { get; }
      public IReadOnlyList<Pair<int>> Columns { get; }
      public TypeInfo ApproximateType { get; }

      public RecordPartMapping(int typeIdColumnIndex, IReadOnlyList<Pair<int>> columns, TypeInfo approximateType)
      {
        TypeIdColumnIndex = typeIdColumnIndex;
        Columns = columns;
        ApproximateType = approximateType;
      }
    }

    private readonly struct CacheItem
    {
      public RecordSetHeader Header { get; }
      public RecordPartMapping[] Mappings { get; }

      public CacheItem(RecordSetHeader header, RecordPartMapping[] mappings)
      {
        Header = header;
        Mappings = mappings;
      }
    }

    private readonly ICache<RecordSetHeader, CacheItem> cache;

    public IEnumerable<Record> Read(IEnumerable<Tuple> source, RecordSetHeader header, Session session)
    {
      var columns = header.Columns;
      var columnGroups = header.ColumnGroups;
      var recordPartCount = columnGroups.Count;
      var context = new MaterializationContext(session, recordPartCount);
      if (!cache.TryGetItem(header, false, out var cacheItem)) {
        var typeIdColumnName = Domain.Handlers.NameBuilder.TypeIdColumnName;
        var model = context.Model;
        var mappings = new RecordPartMapping[recordPartCount];
        for (int i = 0; i < recordPartCount; i++) {
          var columnGroup = columnGroups[i];
          var approximateType = columnGroup.TypeInfoRef.Resolve(model);
          var columnMapping = new List<Pair<int>>(columnGroup.Columns.Count);
          var typeIdColumnIndex = -1;
          foreach (var columnIndex in columnGroup.Columns) {
            var column = (MappedColumn) columns[columnIndex];
            var columnInfo = column.ColumnInfoRef.Resolve(model);
            if (approximateType.Fields.TryGetValue(columnInfo.Field.Name, out var fieldInfo)) {
              var targetColumnIndex = fieldInfo.MappingInfo.Offset;
              if (columnInfo.Name == typeIdColumnName) {
                typeIdColumnIndex = column.Index;
              }
              columnMapping.Add(new Pair<int>(targetColumnIndex, columnIndex));
            }
          }
          mappings[i] = new RecordPartMapping(typeIdColumnIndex, columnMapping, approximateType);
        }
        cacheItem = new CacheItem(header, mappings);
        cache.Add(cacheItem);
      }
      return source.Select(tuple => ParseRow(tuple, context, cacheItem.Mappings));
    }

    private Record ParseRow(Tuple tuple, MaterializationContext context, RecordPartMapping[] recordPartMappings) =>
      recordPartMappings.Length == 1
        ? new Record(tuple, ParseColumnGroup(tuple, context, 0, recordPartMappings[0]))
        : new Record(tuple, recordPartMappings.Select(
            (recordPartMapping, i) => ParseColumnGroup(tuple, context, i, recordPartMapping))
          );

    private Pair<Key, Tuple> ParseColumnGroup(Tuple tuple, MaterializationContext context, int groupIndex, in RecordPartMapping mapping)
    {
      TypeReferenceAccuracy accuracy;
      int typeId = ExtractTypeId(mapping.ApproximateType, context.TypeIdRegistry, tuple, mapping.TypeIdColumnIndex, out accuracy);
      var typeMapping = typeId == TypeInfo.NoTypeId ? null : context.GetTypeMapping(groupIndex, mapping.ApproximateType, typeId, mapping.Columns);
      if (typeMapping == null)
        return new Pair<Key, Tuple>(null, null);

      bool canCache = accuracy == TypeReferenceAccuracy.ExactType;
      Key key;
      if (typeMapping.KeyTransform.Descriptor.Count <= WellKnown.MaxGenericKeyLength)
        key = KeyFactory.Materialize(Domain, context.Session.StorageNodeId, typeMapping.Type, tuple, accuracy, canCache, typeMapping.KeyIndexes);
      else {
        var keyTuple = typeMapping.KeyTransform.Apply(TupleTransformType.TransformedTuple, tuple);
        key = KeyFactory.Materialize(Domain, context.Session.StorageNodeId, typeMapping.Type, keyTuple, accuracy, canCache, null);
      }
      if (accuracy == TypeReferenceAccuracy.ExactType) {
        var entityTuple = typeMapping.Transform.Apply(TupleTransformType.Tuple, tuple);
        return new Pair<Key, Tuple>(key, entityTuple);
      }
      return new Pair<Key, Tuple>(key, null);
    }

    public static int ExtractTypeId(TypeInfo type, TypeIdRegistry typeIdRegistry, Tuple tuple, int typeIdIndex, out TypeReferenceAccuracy accuracy)
    {
      accuracy = TypeReferenceAccuracy.Hierarchy;
      if (type.IsInterface)
        accuracy = TypeReferenceAccuracy.BaseType;

      if (typeIdIndex < 0) {
        if (type.IsLeaf)
          accuracy = TypeReferenceAccuracy.ExactType;
        return typeIdRegistry.GetTypeId(type);
      }

      accuracy = TypeReferenceAccuracy.ExactType;
      TupleFieldState state;
      var value = tuple.GetValue<int>(typeIdIndex, out state);
      if (type.IsLeaf)
        return state.HasValue() ? typeIdRegistry.GetTypeId(type) : TypeInfo.NoTypeId;
      // Hack here: 0 (default) = TypeInfo.NoTypeId
      return value;
    }


    // Constructors

    internal EntityDataReader(Domain domain)
      : base(domain)
    {
      cache = new FastConcurrentLruCache<RecordSetHeader, CacheItem>(
        domain.Configuration.RecordSetMappingCacheSize,
        m => m.Header
      );
    }
  }
}
