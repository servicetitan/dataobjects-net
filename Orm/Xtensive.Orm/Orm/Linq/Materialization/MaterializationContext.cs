// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.29

using Xtensive.Tuples.Transform;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Linq.Materialization
{
  using EntityMappingCache = (TypeMapping? SingleItem, Dictionary<int, TypeMapping> Items);

  internal class MaterializationContext(Session session, int entityCount)
  {
    private readonly EntityMappingCache[] entityMappings = new EntityMappingCache[entityCount];

    /// <summary>
    /// Shared per-row buffer reused by <see cref="ItemMaterializationContext"/> across rows
    /// of the same query to avoid a per-row <see cref="Entity"/>[] allocation.
    /// Safe because <see cref="ItemMaterializationContext"/> is constructed and fully consumed
    /// on a single logical row before the next row begins.
    /// </summary>
    internal readonly Entity[] EntitiesBuffer = new Entity[entityCount];

    /// <summary>
    /// Gets the session in which materialization is executing.
    /// </summary>
    public Session Session => session;

    /// <summary>
    /// Gets model of current <see cref="DomainModel">domain model.</see>
    /// </summary>
    public DomainModel Model => Session.Domain.Model;

    /// <summary>
    /// Gets count of entities in query row.
    /// </summary>
    public int EntitiesInRow => entityMappings.Length;

    /// <summary>
    /// Gets <see cref="StorageNode">node</see> specific type identifiers registry of current node.
    /// </summary>
    public TypeIdRegistry TypeIdRegistry => Session.StorageNode.TypeIdRegistry;

    /// <summary>
    /// Gets or sets queue of materialization actions.
    /// </summary>
    public Queue<Action> MaterializationQueue { get; set; }

    public TypeMapping GetTypeMapping(int entityIndex, TypeInfo approximateType, int typeId, IEnumerable<(ColNum From, ColNum To)> columns)
    {
      ref var cache = ref entityMappings[entityIndex];
      if (cache.SingleItem is { } result) {
        return typeId == ResolveTypeToNodeSpecificTypeIdentifier(result.Type)
          ? result
          : throw new ArgumentOutOfRangeException("typeId");
      }
      if (cache.Items?.TryGetValue(typeId, out result) == true)
        return result;

      var type = TypeIdRegistry[typeId];
      var keyInfo = type.Key;
      var descriptor = type.TupleDescriptor;

      IEnumerable<(ColNum From, ColNum To)> typeColumnMap = columns;
      if (approximateType.IsInterface) {
        // fixup target index
        var fieldMap = type.FieldMap;
        var approximateTypeColumns = approximateType.Columns;
        typeColumnMap = columns.Select(p => (fieldMap[approximateTypeColumns[p.From].Field].MappingInfo.Offset, p.To));
      }

      var allIndexes = MaterializationHelper.CreateSingleSourceMap(descriptor.Count, typeColumnMap);
      var keyCount = keyInfo.TupleDescriptor.Count;
      var keyIndexes = new ColNum[keyCount];
      Array.Copy(allIndexes, keyIndexes, keyCount);

      var transform    = new MapTransform(true, descriptor, allIndexes);
      var keyTransform = new MapTransform(true, keyInfo.TupleDescriptor, keyIndexes);

      result = new TypeMapping(type, keyTransform, transform, keyIndexes);

      if (type.Hierarchy.Root.IsLeaf && approximateType==type)
        cache.SingleItem = result;
      else
        (cache.Items ??= new()).Add(typeId, result);

      return result;
    }

    private int ResolveTypeToNodeSpecificTypeIdentifier(TypeInfo typeInfo) => TypeIdRegistry[typeInfo];
  }
}
