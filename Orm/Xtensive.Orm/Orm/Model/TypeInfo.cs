// Copyright (C) 2007-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2007.08.27

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Xtensive.Core;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Validation;
using Xtensive.Orm.Upgrade;
using Xtensive.Reflection;
using Xtensive.Tuples;
using Xtensive.Tuples.Transform;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Model
{
  /// <summary>
  /// Represents an object describing any persistent type.
  /// </summary>
  [DebuggerDisplay("{underlyingType}")]
  [Serializable]
  public sealed class TypeInfo : SchemaMappedNode
  {
    /// <summary>
    /// "No <see cref="TypeId"/>" value (<see cref="TypeId"/> is unknown or undefined).
    /// Value is <see langword="0" />.
    /// </summary>
    public const int NoTypeId = 0;

    /// <summary>
    /// Minimal possible <see cref="TypeId"/> value.
    /// Value is <see langword="100" />.
    /// </summary>
    public const int MinTypeId = 100;

    private static readonly Type
      TypeEntity = typeof(Entity)
      , TypeStructure = typeof(Structure);

    private static volatile int CurrentSharedId = 0;
    private static readonly ConcurrentDictionary<Type, int> TypeToSharedId = new();

    private static readonly IReadOnlySet<TypeInfo> EmptyTypes = ImmutableHashSet.Create<TypeInfo>();

    private readonly ColumnInfoCollection columns;
    private readonly FieldMap fieldMap;
    private readonly FieldInfoCollection fields;
    private readonly TypeIndexInfoCollection indexes;
    private readonly NodeCollection<IndexInfo> affectedIndexes;
    private readonly DomainModel model;
    private TypeAttributes attributes;
    private IReadOnlyList<AssociationInfo> targetAssociations;
    private IReadOnlyList<AssociationInfo> ownerAssociations;
    private IReadOnlyList<AssociationInfo> removalSequence;
    private IReadOnlyList<FieldInfo> versionFields;
    private IReadOnlyList<ColumnInfo> versionColumns;
    private HierarchyInfo hierarchy;
    private int typeId = NoTypeId;
    private object typeDiscriminatorValue;
    private MapTransform primaryKeyInjector;
    private bool isLeaf;
    private bool isOutboundOnly;
    private bool isInboundOnly;
    private KeyInfo key;
    private bool hasVersionRoots;
    private IReadOnlyDictionary<(FieldInfo, FieldInfo), FieldInfo> structureFieldMapping;
    private List<AssociationInfo> overriddenAssociations;
    private FieldInfo typeIdField;


    private TypeInfo ancestor;
    private IReadOnlySet<TypeInfo> ancestors;

    private IReadOnlySet<TypeInfo> directDescendants;
    private IReadOnlySet<TypeInfo> allDescendants;
    private IReadOnlySet<TypeInfo> directInterfaces;
    private IReadOnlySet<TypeInfo> allInterfaces;
    private IReadOnlySet<TypeInfo> directImplementors;
    private IReadOnlySet<TypeInfo> allImplementors;
    private IReadOnlySet<TypeInfo> typeWithAncestorsAndInterfaces;

    #region Hierarchical structure properties

    /// <summary>
    /// Gets the ancestor.
    /// </summary>
    public TypeInfo Ancestor {
      get { return ancestor; }
      internal set {
        if (ancestor != null)
          throw Exceptions.AlreadyInitialized(nameof(Ancestor));
        ancestor = value;
      }
    }

    /// <summary>
    /// Gets the root of the hierarchy.
    /// </summary>
    [CanBeNull]
    public TypeInfo Root =>
      IsInterface || IsStructure
        ? null
        : IsLocked
          ? Hierarchy.Root
          : Ancestors.FirstOrDefault() ?? this;

    /// <summary>
    /// Gets the ancestors recursively. Inheritor-to-root order.
    /// </summary>
    public IEnumerable<TypeInfo> AncestorChain
    {
      get {
        for (var ancestor = Ancestor; ancestor != null; ancestor = ancestor.Ancestor) {
          yield return ancestor;
        }
      }
    }

    /// <summary>
    /// Gets the ancestors recursively. Root-to-inheritor order. Reverse of AncestorChain.
    /// </summary>
    /// <returns>The ancestor</returns>
    public IReadOnlySet<TypeInfo> Ancestors =>
      ancestors ??= AncestorChain.Reverse().ToFrozenSet();

    /// <summary>
    /// Gets direct descendants of this instance.
    /// </summary>
    public IReadOnlySet<TypeInfo> DirectDescendants => directDescendants ?? EmptyTypes;


    /// <summary>
    /// Gets all descendants (both direct and nested) of this instance.
    /// </summary>
    public IReadOnlySet<TypeInfo> AllDescendants =>
      allDescendants ??= DirectDescendants.Count == 0
        ? DirectDescendants
        : DirectDescendants.Union(DirectDescendants.SelectMany(static o => o.AllDescendants)).ToFrozenSet();

    /// <summary>
    /// Gets the persistent interfaces this instance implements directly.
    /// </summary>
    public IReadOnlySet<TypeInfo> DirectInterfaces => directInterfaces ?? EmptyTypes;

    /// <summary>
    /// Gets all the persistent interfaces (both direct and non-direct) this instance implements.
    /// </summary>
    public IReadOnlySet<TypeInfo> AllInterfaces =>
      allInterfaces ??= IsInterface
        ? DirectInterfaces
        : DirectInterfaces.Concat(AncestorChain.SelectMany(static o => o.DirectInterfaces)).ToFrozenSet();

    /// <summary>
    /// Gets the direct implementors of this instance.
    /// </summary>
    public IReadOnlySet<TypeInfo> DirectImplementors => directImplementors ?? EmptyTypes;

    /// <summary>
    /// Gets both direct and non-direct implementors of this instance.
    /// </summary>
    public IReadOnlySet<TypeInfo> AllImplementors
    {
      get {
        if (allImplementors == null) {
          if (DirectImplementors.Count == 0) {
            allImplementors = EmptyTypes;
          }
          else {
            var allSet = new HashSet<TypeInfo>(DirectImplementors.Count);
            foreach (var item in DirectImplementors) {
              _ = allSet.Add(item);
              if (!item.IsInterface) {
                foreach (var descendant in item.AllDescendants)
                  _ = allSet.Add(descendant);
              }
            }
            allImplementors = allSet.ToFrozenSet();
          }
        }
        return allImplementors;
      }
    }

    /// <summary>
    /// Gets all ancestors, all interfaces with this instance included.
    /// </summary>
    internal IReadOnlySet<TypeInfo> TypeWithAncestorsAndInterfaces =>
      typeWithAncestorsAndInterfaces ??= Ancestors.Concat(AllInterfaces).Append(this).ToFrozenSet();

    #endregion

    #region IsXxx properties

    /// <summary>
    /// Gets a value indicating whether this instance is entity.
    /// </summary>
    public bool IsEntity
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Entity) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is abstract entity.
    /// </summary>
    public bool IsAbstract
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Abstract) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is interface.
    /// </summary>
    public bool IsInterface
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Interface) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is materialized interface.
    /// </summary>
    public bool IsMaterialized
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Materialized) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is structure.
    /// </summary>
    public bool IsStructure
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Structure) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is system type.
    /// </summary>
    public bool IsSystem
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.System) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is a leaf type,
    /// i.e. its <see cref="DirectDescendants"/> method returns empty collection.
    /// </summary>
    public bool IsLeaf
    {
      [DebuggerStepThrough]
      get { return IsLocked ? isLeaf : GetIsLeaf(); }
    }

    ///<summary>
    /// Gets or sets a value indicating whether this instance is outbound only
    /// i.e. it's has only outgoing references
    /// </summary>
    public bool IsOutboundOnly
    {
      get { return isOutboundOnly; }
      set {
        EnsureNotLocked();
        isOutboundOnly = value;
      }
    }

    ///<summary>
    /// Gets or sets a value indicating whether this instance is inbound only
    /// i.e. it's has only incoming references
    /// </summary>
    public bool IsInboundOnly
    {
      get { return isInboundOnly; }
      set {
        EnsureNotLocked();
        isInboundOnly = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is auxiliary type.
    /// </summary>
    public bool IsAuxiliary
    {
      [DebuggerStepThrough]
      get { return (attributes & TypeAttributes.Auxiliary) == TypeAttributes.Auxiliary; }
      set {
        EnsureNotLocked();
        attributes = value
          ? attributes | TypeAttributes.Auxiliary
          : attributes & ~TypeAttributes.Auxiliary;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is generic type definition.
    /// </summary>
    public bool IsGenericTypeDefinition
    {
      get { return (attributes & TypeAttributes.GenericTypeDefinition) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is automatically registered generic type instance.
    /// </summary>
    public bool IsAutoGenericInstance
    {
      get { return (attributes & TypeAttributes.AutoGenericInstance) > 0; }
    }

    #endregion

    /// <summary>
    /// Gets or sets the type identifier uniquely identifying the type in the domain model.
    /// </summary>
    /// <exception cref="NotSupportedException">Property is already initialized.</exception>
    public int TypeId
    {
      [DebuggerStepThrough]
      get { return typeId; }
      set {
        if (typeId != NoTypeId)
          throw Exceptions.AlreadyInitialized("TypeId");
        typeId = value;
      }
    }

    /// <summary>
    /// Gets or sets the underlying system type.
    /// </summary>
    public Type UnderlyingType { [DebuggerStepThrough] get; }

    internal int SharedId { get; }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public TypeAttributes Attributes
    {
      [DebuggerStepThrough]
      get { return attributes; }
    }

    /// <summary>
    /// Gets the columns contained in this instance.
    /// </summary>
    public ColumnInfoCollection Columns
    {
      [DebuggerStepThrough]
      get { return columns; }
    }

    /// <summary>
    /// Gets the indexes for this instance.
    /// </summary>
    public TypeIndexInfoCollection Indexes
    {
      [DebuggerStepThrough]
      get { return indexes; }
    }

    public NodeCollection<IndexInfo> AffectedIndexes
    {
      [DebuggerStepThrough]
      get { return affectedIndexes; }
    }

    /// <summary>
    /// Gets full-text index if any, otherwise gets <see langword="null"/>.
    /// </summary>
    public FullTextIndexInfo FullTextIndex
    {
      [DebuggerStepThrough]
      get {
        FullTextIndexInfo fullTextIndexInfo;
        model.FullTextIndexes.TryGetValue(this, out fullTextIndexInfo);
        return fullTextIndexInfo;
      }
    }

    /// <summary>
    /// Gets the fields contained in this instance.
    /// </summary>
    public FieldInfoCollection Fields
    {
      [DebuggerStepThrough]
      get { return fields; }
    }

    private FieldInfo[] persistentFields;
    internal FieldInfo[] PersistentFields => persistentFields ??= BuildPersistentFields();

    /// <summary>
    /// Gets the field map for implemented interfaces.
    /// </summary>
    public FieldMap FieldMap
    {
      [DebuggerStepThrough]
      get { return fieldMap; }
    }

    /// <summary>
    /// Gets the <see cref="DomainModel"/> this instance belongs to.
    /// </summary>
    public DomainModel Model
    {
      [DebuggerStepThrough]
      get { return model; }
    }

    /// <summary>
    /// Gets or sets the hierarchy.
    /// </summary>
    public HierarchyInfo Hierarchy
    {
      [DebuggerStepThrough]
      get { return hierarchy; }
      set {
        EnsureNotLocked();
        hierarchy = value;
      }
    }

    /// <summary>
    /// Gets <see cref="KeyInfo"/> for this type.
    /// </summary>
    public KeyInfo Key
    {
      get { return IsLocked ? key : GetKey(); }
    }

    /// <summary>
    /// Gets or sets the type discriminator value.
    /// </summary>
    public object TypeDiscriminatorValue
    {
      get { return typeDiscriminatorValue; }
      set {
        EnsureNotLocked();
        typeDiscriminatorValue = value;
      }
    }

    /// <summary>
    /// Gets the tuple descriptor.
    /// </summary>
    public TupleDescriptor TupleDescriptor { get; private set; }

    /// <summary>
    /// Gets the persistent type prototype.
    /// </summary>
    public Tuple TuplePrototype { get; private set; }

    /// <summary>
    /// Gets the version tuple extractor.
    /// </summary>
    public MapTransform VersionExtractor { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance has version fields.
    /// </summary>
    public bool HasVersionFields { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance has explicit version fields.
    /// </summary>
    public bool HasExplicitVersionFields { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance has version roots.
    /// </summary>
    public bool HasVersionRoots
    {
      [DebuggerStepThrough]
      get { return hasVersionRoots; }
      [DebuggerStepThrough]
      set {
        EnsureNotLocked();
        hasVersionRoots = value;
      }
    }

    /// <summary>
    /// Gets the structure field mapping.
    /// </summary>
    /// <value>The structure field mapping.</value>
    public IReadOnlyDictionary<(FieldInfo, FieldInfo), FieldInfo> StructureFieldMapping =>
        structureFieldMapping ?? BuildStructureFieldMapping();

    /// <summary>
    /// Gets <see cref="IObjectValidator"/> instances
    /// associated with this type.
    /// </summary>
    public IReadOnlyList<IObjectValidator> Validators { get; internal init; }

    /// <summary>
    /// Gets value indicating if this type has validators (including field validators).
    /// </summary>
    public bool HasValidators { get; private set; }

    /// <summary>
    /// Creates the tuple prototype with specified <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="primaryKey">The primary key to use.</param>
    /// <param name="typeIdValue">Identifier of <see cref="Entity"/> type.</param>
    /// <returns>
    /// The <see cref="TuplePrototype"/> with "injected" <paramref name="primaryKey"/>.
    /// </returns>
    public Tuple CreateEntityTuple(Tuple primaryKey, int typeIdValue)
    {
      var result = primaryKeyInjector.Apply(TupleTransformType.Tuple, primaryKey, TuplePrototype);
      if (typeIdField != null)
        result.SetValue(typeIdField.MappingInfo.Offset, typeIdValue);
      return result;
    }

    /// <summary>
    /// Injects the primary key into specified <paramref name="entityTuple"/>
    /// </summary>
    /// <param name="entityTuple">A <see cref="Tuple"/> instance where to inject
    /// the specified <paramref name="primaryKey"/></param>
    /// <param name="primaryKey">The primary key to inject.</param>
    /// <returns>
    /// The <paramref name="entityTuple"/> with "injected" <paramref name="primaryKey"/>.
    /// </returns>
    public Tuple InjectPrimaryKey(Tuple entityTuple, Tuple primaryKey)
    {
      return primaryKeyInjector.Apply(TupleTransformType.Tuple, primaryKey, entityTuple);
    }

    public IEnumerable<AssociationInfo> GetTargetAssociations()
    {
      if (targetAssociations == null) {
        var result = model.Associations.Find(this, true);
        if (!IsLocked) {
          return result;
        }
        targetAssociations = result.ToList().AsSafeWrapper();
      }
      return targetAssociations;
    }

    /// <summary>
    /// Gets the associations this instance is participating in as owner (it has references to other entities).
    /// </summary>
    public IEnumerable<AssociationInfo> GetOwnerAssociations()
    {
      if (ownerAssociations == null) {
        var result = model.Associations.Find(this, false);
        if (!IsLocked) {
          return result;
        }
        ownerAssociations = result.ToList().AsSafeWrapper();
      }
      return ownerAssociations;
    }

    /// <summary>
    /// Gets the association sequence for entity removal.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<AssociationInfo> GetRemovalAssociationSequence()
    {
      return removalSequence;
    }

    /// <summary>
    /// Gets the version field sequence.
    /// </summary>
    /// <returns>The version field sequence.</returns>
    public IReadOnlyList<FieldInfo> GetVersionFields()
    {
      if (versionFields == null) {
        var result = InnerGetVersionFields().ToList();
        if (!IsLocked) {
          return result;
        }
        versionFields = result.AsSafeWrapper();
      }
      return versionFields;
    }

    private IEnumerable<FieldInfo> InnerGetVersionFields()
    {
      var fields = Fields
        .Where(field => field.IsPrimitive && (field.AutoVersion || field.ManualVersion))
        .ToList();
      return fields.Count > 0
        ? fields
        : Fields.Where(f => f.IsPrimitive
          && !f.IsSystem
          && !f.IsPrimaryKey
          && !f.IsLazyLoad
          && !f.IsTypeId
          && !f.IsTypeDiscriminator
          && !f.ValueType.IsArray
          && !f.SkipVersion);
    }

    /// <summary>
    /// Gets the version columns.
    /// </summary>
    /// <returns>The version columns.</returns>
    public IReadOnlyList<ColumnInfo> GetVersionColumns()
    {
      if (versionColumns == null) {
        var result = InnerGetVersionFields()
          .SelectMany(f => f.Columns)
          .OrderBy(c => c.Field.MappingInfo.Offset)
          .ToList();
        if (!IsLocked) {
          return result;
        }
        versionColumns = result.AsSafeWrapper();
      }
      return versionColumns;
    }

    /// <inheritdoc/>
    public override void UpdateState()
    {
      base.UpdateState();

      if (Fields.Count > FieldInfo.MaxFieldId)
        throw new NotSupportedException($"Max number of supported fields is {FieldInfo.MaxFieldId}");
      byte adapterIndex = 0;
      foreach (var field in Fields) {
        if (field.IsStructure || field.IsEntitySet) {
          field.AdapterIndex = adapterIndex++;
        }
      }

      affectedIndexes.UpdateState();
      indexes.UpdateState();
      columns.UpdateState();

      CreateTupleDescriptor();

      columns.UpdateState();
      fields.UpdateState();

      structureFieldMapping = BuildStructureFieldMapping().ToFrozenDictionary();

      if (IsEntity) {
        if (HasVersionRoots) {
          versionFields = Array.Empty<FieldInfo>();
          versionColumns = Array.Empty<ColumnInfo>();
        }
        else {
          versionFields = InnerGetVersionFields().ToList();
        }
        HasVersionFields = versionFields.Count > 0;
        HasExplicitVersionFields = versionFields.Any(f => f.ManualVersion || f.AutoVersion);
      }

      if (IsInterface) {
        // Collect mapping information from the first implementor (if any)
        // We'll check that all implementors are mapped to the same database later.
        // MappingSchema is not important: it's copied for consistency.
        var firstImplementor = DirectImplementors.FirstOrDefault();
        if (firstImplementor != null) {
          MappingDatabase = firstImplementor.MappingDatabase;
          MappingSchema = firstImplementor.MappingSchema;
        }
      }

      HasValidators = Validators.Count > 0 || fields.Any(f => f.HasValidators);

      // Selecting master parts from paired associations & single associations
      var associations = model.Associations.Find(this)
        .Where(a => a.IsMaster)
        .ToList();

      typeIdField = Fields.FirstOrDefault(f => f.IsTypeId && f.IsSystem);

      BuildTuplePrototype();
      BuildVersionExtractor();

      if (associations.Count == 0) {
        removalSequence = Array.Empty<AssociationInfo>();
        return;
      }

      overriddenAssociations = associations
        .Where(a =>
          (a.Ancestors.Count > 0 && ((a.OwnerType == this && a.Ancestors.All(an => an.OwnerType != this) || (a.TargetType == this && a.Ancestors.All(an => an.TargetType != this))))) ||
          (a.Reversed != null && (a.Reversed.Ancestors.Count > 0 && ((a.Reversed.OwnerType == this && a.Reversed.Ancestors.All(an => an.OwnerType != this) || (a.Reversed.TargetType == this && a.Reversed.Ancestors.All(an => an.TargetType != this)))))))
        .SelectMany(a => a.Ancestors.Concat(a.Reversed == null ? Enumerable.Empty<AssociationInfo>() : a.Reversed.Ancestors))
        .ToList();
      var ancestor = Ancestor;
      if (ancestor != null && ancestor.overriddenAssociations != null) {
        overriddenAssociations.AddRange(ancestor.overriddenAssociations);
      }

      foreach (var ancestorAssociation in overriddenAssociations) {
        associations.Remove(ancestorAssociation);
      }

      //
      //Commented action sequence bellow may add dublicates to "sequence".
      //Besides, it takes 6 times enumeration of "associations"
      //

      //var sequence = new List<AssociationInfo>(associations.Count);
      //sequence.AddRange(associations.Where(a => a.OnOwnerRemove == OnRemoveAction.Deny && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      //sequence.AddRange(associations.Where(a => a.OnTargetRemove == OnRemoveAction.Deny && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      //sequence.AddRange(associations.Where(a => a.OnOwnerRemove == OnRemoveAction.Clear && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      //sequence.AddRange(associations.Where(a => a.OnTargetRemove == OnRemoveAction.Clear && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      //sequence.AddRange(associations.Where(a => a.OnOwnerRemove == OnRemoveAction.Cascade && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      //sequence.AddRange(associations.Where(a => a.OnTargetRemove == OnRemoveAction.Cascade && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)));

      //
      // Code bellow adds the same associations, but without dublicates.
      // Also it takes only one enumeration of associations sequence.
      //
      var sequence = new List<AssociationInfo>(associations.Count);
      var b = associations.Where(
        a => (a.OnOwnerRemove == OnRemoveAction.Deny && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)) ||
          (a.OnTargetRemove == OnRemoveAction.Deny && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)) ||
          (a.OnOwnerRemove == OnRemoveAction.Clear && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)) ||
          (a.OnTargetRemove == OnRemoveAction.Clear && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)) ||
          (a.OnOwnerRemove == OnRemoveAction.Cascade && a.OwnerType.UnderlyingType.IsAssignableFrom(UnderlyingType)) ||
          (a.OnTargetRemove == OnRemoveAction.Cascade && a.TargetType.UnderlyingType.IsAssignableFrom(UnderlyingType)));
      sequence.AddRange(b);

      var sortedRemovalSequence = sequence.Where(a => a.Ancestors.Count > 0).ToList();
      if (sortedRemovalSequence.Count == 0) {
        removalSequence = sequence.AsSafeWrapper();
      }
      else {
        var sequenceSize = sequence.Count;
        if (sortedRemovalSequence.Capacity < sequenceSize) {
          sortedRemovalSequence.Capacity = sequenceSize;
        }
        sortedRemovalSequence.AddRange(sequence.Where(a => a.Ancestors.Count == 0));
        removalSequence = sortedRemovalSequence.AsSafeWrapper();
      }
    }

    /// <inheritdoc/>
    public override void Lock(bool recursive)
    {
      base.Lock(recursive);

      if (fields.Count > FieldInfo.MaxFieldId)
        throw new NotSupportedException($"Max number of supported fields is {FieldInfo.MaxFieldId}");
      byte currentFieldId = FieldInfo.MinFieldId;
      foreach (var field in fields)
        field.FieldId = currentFieldId++;
      isLeaf = GetIsLeaf();
      key = GetKey();

      if (IsEntity || IsStructure) {
        foreach (var field in Fields.Where(f => f.FieldId != FieldInfo.NoFieldId)) {
          field.Accessor = FieldAccessorProvider.CreateFieldAccessor(field);
        }
      }

      if (!recursive)
        return;

      directDescendants = directDescendants?.ToFrozenSet() ?? EmptyTypes;
      directInterfaces = directInterfaces?.ToFrozenSet() ?? EmptyTypes;
      directImplementors = directImplementors?.ToFrozenSet() ?? EmptyTypes;

      affectedIndexes.Lock(true);
      indexes.Lock(true);
      columns.Lock(true);
      fieldMap.Lock(true);
      fields.Lock(true);
    }

    #region Private / internal methods

    internal void AddDescendant(TypeInfo descendant) =>
      ((ISet<TypeInfo>) (directDescendants ??= new HashSet<TypeInfo>())).Add(descendant);

    internal void AddInterface(TypeInfo iface) =>
      ((ISet<TypeInfo>) (directInterfaces ??= new HashSet<TypeInfo>())).Add(iface);

    internal void AddImplementor(TypeInfo implementor) =>
      ((ISet<TypeInfo>) (directImplementors ??= new HashSet<TypeInfo>())).Add(implementor);

    private KeyInfo GetKey() =>
      Hierarchy != null ? Hierarchy.Key
        : IsInterface ? DirectImplementors.First().Hierarchy.Key
        : null;

    private bool GetIsLeaf() =>
      IsEntity && DirectDescendants.Count == 0;

    private void CreateTupleDescriptor()
    {
      var orderedColumns = columns.OrderBy(c => c.Field.MappingInfo.Offset).ToList(columns.Count);
      columns.Clear();                    // To prevent event handler leak
      columns.AddRange(orderedColumns);
      TupleDescriptor = TupleDescriptor.Create(
        Columns.Select(c => c.ValueType).ToArray(Columns.Count));
    }

    private void BuildTuplePrototype()
    {
      // Building nullable map
      var nullabilityMap = new BitArray(TupleDescriptor.Count);
      ColNum i = 0;
      foreach (var column in Columns)
        nullabilityMap[i++] = column.IsNullable;

      // fixing reference fields that are marked as not nullable
      foreach (var field in Fields.Where(f => f.IsEntity && !f.IsPrimaryKey && f.IsNullable == false)) {
        var segment = field.MappingInfo;
        for (int j = segment.Offset; j < segment.EndOffset; j++) {
          nullabilityMap[j] = true;
        }
      }

      // Building TuplePrototype
      var tuple = Tuple.Create(TupleDescriptor);
      tuple.Initialize(nullabilityMap);

      // Initializing defaults
      i = 0;
      foreach (var column in Columns) {
        if (column.DefaultValue != null) {
          try {
            tuple.SetValue(i, column.DefaultValue);
          }
          catch (Exception e) {
            OrmLog.Error(e, nameof(Strings.LogExErrorSettingDefaultValueXForColumnYInTypeZ),
              column.DefaultValue, column.Name, Name);
          }
        }
        i++;
      }

      // Aditional initialization for entities
      if (IsEntity) {
        // Setting type discriminator column
        if (Hierarchy.TypeDiscriminatorMap != null)
          tuple.SetValue(Hierarchy.TypeDiscriminatorMap.Field.MappingInfo.Offset, typeDiscriminatorValue);

        // Building primary key injector
        var fieldCount = TupleDescriptor.Count;
        ColNum keyFieldCount = (ColNum) Key.TupleDescriptor.Count;
        var keyFieldMap = new Pair<ColNum, ColNum>[fieldCount];
        for (i = 0; i < fieldCount; i++)
          keyFieldMap[i] = new Pair<ColNum, ColNum>((ColNum) ((i < keyFieldCount) ? 0 : 1), i);
        primaryKeyInjector = new MapTransform(false, TupleDescriptor, keyFieldMap);
      }
      TuplePrototype = IsEntity ? tuple.ToFastReadOnly() : tuple;
    }

    private void BuildVersionExtractor()
    {
      // Building version tuple extractor
      var versionColumns = GetVersionColumns().ToList();
      var versionColumnsCount = versionColumns?.Count ?? 0;
      if (versionColumns == null || versionColumnsCount == 0) {
        VersionExtractor = null;
        return;
      }
      var types = versionColumns.Select(c => c.ValueType).ToArray(versionColumnsCount);
      var map = versionColumns.Select(c => c.Field.MappingInfo.Offset).ToArray(versionColumnsCount);
      var versionTupleDescriptor = TupleDescriptor.Create(types);
      VersionExtractor = new MapTransform(true, versionTupleDescriptor, map);
    }

    private IReadOnlyDictionary<(FieldInfo, FieldInfo), FieldInfo> BuildStructureFieldMapping()
    {
      var result = new Dictionary<(FieldInfo, FieldInfo), FieldInfo>();
      var structureFields = Fields.Where(f => f.IsStructure && f.Parent == null);
      foreach (var structureField in structureFields) {
        var structureTypeInfo = Model.Types[structureField.ValueType];
        foreach (var pair in structureTypeInfo.Fields.Zip(structureField.Fields))
          result.Add((structureField, pair.First), pair.Second);
      }
      return result.AsSafeWrapper();
    }

    private static IEnumerable<FieldInfo> GetBaseFields(Type type, IEnumerable<FieldInfo> fields, bool bRoot)
    {
      if (type == TypeEntity || type == TypeStructure) {
        return Array.Empty<FieldInfo>();
      }
      var declared = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .ToDictionary(p => p.Name, p => p.MetadataToken);

      return GetBaseFields(type.BaseType, fields, bRoot)
        .Concat(
          fields.Select(p => (p, declared.TryGetValue(p.UnderlyingProperty.Name, out var token) ? token : 0))
            .Where(t => bRoot ? t.Item2 != 0 : t.Item1.UnderlyingProperty.MetadataToken == t.Item2)
            .OrderBy(t => t.Item2)
            .Select(t => t.Item1)
        );
    }

    private static bool IsOverrideOfVirtual(FieldInfo a, FieldInfo p) =>
      a.Name == p.Name
        && a.Name != "TypeId"
        && p.IsInherited
        && a.UnderlyingProperty.GetMethod?.IsVirtual == true
        && p.UnderlyingProperty.GetMethod?.IsVirtual == true;

    private FieldInfo[] BuildPersistentFields()
    {
      var propTypeId = IsEntity ? Fields[nameof(Entity.TypeId)] : null;
      bool isRoot = Hierarchy?.Root == this;
      var potentialFields = Fields.Where(p => !p.IsDynamicallyDefined && p.Parent == null && p != propTypeId).ToArray();
      var recycled = UnderlyingType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(p => p.GetAttribute<RecycledAttribute>() != null && !potentialFields.Any(pf => pf.UnderlyingProperty == p));

      FieldInfo[] baseFields;
      FieldInfo[] ancestorFields = Array.Empty<FieldInfo>();
      if (Ancestor != null && Ancestor.UnderlyingType != TypeStructure) {
        ancestorFields = Ancestor.PersistentFields;
        baseFields = ancestorFields.Select(p => p != null && Fields.TryGetValue(p.Name, out var f) ? f : null).ToArray();
      }
      else {
        baseFields = !(IsEntity || IsStructure)
          ? Array.Empty<FieldInfo>()
          : GetBaseFields(UnderlyingType.BaseType, potentialFields, isRoot).ToArray();
      }
      var baseFieldsSet = baseFields.ToHashSet();
      var props = baseFields.Concat(
        potentialFields.Where(p => p.UnderlyingProperty.DeclaringType == UnderlyingType
          && (isRoot
            || p.IsExplicit
            || !baseFieldsSet.Contains(p)
            || ancestorFields.Any(a => IsOverrideOfVirtual(a, p))))
          .Select(p => (p, p.UnderlyingProperty.MetadataToken))
          .Concat(recycled.Select(p => ((FieldInfo)null, p.MetadataToken)))
          .OrderBy(t => t.Item2)
          .Select(t => t.Item1)
      ).Select(p => p?.ReflectedType.IsInterface == true ? FieldMap[p] : p);
      if (IsEntity && Ancestor == null) {
        props = props.Prepend(propTypeId);
      }
      return props.ToArray();
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
      return Name;
    }

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="typeAttributes">The type attributes.</param>
    public TypeInfo(DomainModel model, Type underlyingType, TypeAttributes typeAttributes)
    {
      this.model = model;
      UnderlyingType = underlyingType;
      SharedId = TypeToSharedId.GetOrAdd(underlyingType, static _ => Interlocked.Increment(ref CurrentSharedId));
      attributes = typeAttributes;
      columns = new ColumnInfoCollection(this, "Columns");
      fields = new FieldInfoCollection(this, "Fields");
      fieldMap = IsEntity ? new FieldMap() : FieldMap.Empty;
      indexes = new TypeIndexInfoCollection(this, "Indexes");
      affectedIndexes = new NodeCollection<IndexInfo>(this, "AffectedIndexes");
    }
  }
}
