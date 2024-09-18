// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.22

using System;
using System.Diagnostics;

using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Orm.Model;


namespace Xtensive.Orm.Model
{
  /// <summary>
  /// Loosely-coupled reference that describes <see cref="IndexInfo"/> instance.
  /// </summary>
  [Serializable]
  [DebuggerDisplay("IndexName = {IndexName}, TypeName = {TypeName}")]
  public readonly struct IndexInfoRef(IndexInfo indexInfo)
  {
    /// <summary>
    /// Name of the index.
    /// </summary>
    public string IndexName { get; } = indexInfo.Name;

    /// <summary>
    /// Name of the reflecting type.
    /// </summary>
    public string TypeName { get; } = indexInfo.ReflectedType.Name;

    public TupleDescriptor KeyTupleDescriptor { get; } = indexInfo.KeyTupleDescriptor;

    /// <summary>
    /// Resolves this instance to <see cref="IndexInfo"/> object within specified <paramref name="model"/>.
    /// </summary>
    /// <param name="model">Domain model.</param>
    public IndexInfo Resolve(DomainModel model)
    {
      if (!model.Types.TryGetValue(TypeName, out var type))
        throw new InvalidOperationException(string.Format(Strings.ExCouldNotResolveXYWithinDomain, "type", TypeName));
      if (!type.Indexes.TryGetValue(IndexName, out var index)) {
        if (type.Hierarchy is { } hierarchy && hierarchy.InheritanceSchema == InheritanceSchema.SingleTable && hierarchy.Root.Indexes.TryGetValue(IndexName, out index)) 
          return index;
        throw new InvalidOperationException(string.Format(Strings.ExCouldNotResolveXYWithinDomain, "index", IndexName));
      }
      return index;
    }

    /// <summary>
    /// Creates reference for <see cref="IndexInfo"/>.
    /// </summary>
    public static implicit operator IndexInfoRef (IndexInfo indexInfo) => new(indexInfo);

    #region Equality members, ==, !=

    /// <inheritdoc/>
    public bool Equals(IndexInfoRef other) => IndexName == other.IndexName && TypeName == other.TypeName;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is IndexInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(IndexName, TypeName);

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(IndexInfoRef x, IndexInfoRef y) => x.Equals(y);

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(IndexInfoRef x, IndexInfoRef y) => !x.Equals(y);

    #endregion

    /// <inheritdoc/>
    public override string ToString() => $"Index '{IndexName}' @ {TypeName}";
  }
}
