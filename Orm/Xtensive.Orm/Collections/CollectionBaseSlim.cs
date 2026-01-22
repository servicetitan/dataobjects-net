// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2007.09.24

using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xtensive.Core;

namespace Xtensive.Collections;

/// <summary>
/// Lightweight base class for any collection.
/// </summary>
[Serializable]
[DebuggerDisplay("Count = {Count}")]
public class CollectionBaseSlim<TItem> : List<TItem>, ILockable
{
  /// <inheritdoc/>
  public bool IsLocked { [DebuggerStepThrough] get; private set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static void ThrowInstanceIsLockedException() => throw new InstanceIsLockedException(Strings.ExInstanceIsLocked);

  /// <summary>
  /// Ensures the object is not locked (see <see cref="ILockable.Lock()"/>) yet.
  /// </summary>
  /// <exception cref="InstanceIsLockedException">The instance is locked.</exception>
  public void EnsureNotLocked()
  {
    if (IsLocked) {
      ThrowInstanceIsLockedException();
    }
  }

  /// <inheritdoc/>
  public virtual void Lock(bool recursive = true) => IsLocked = true;

  /// <inheritdoc/>
  public virtual bool IsReadOnly {
    [DebuggerStepThrough]
    get => IsLocked;
  }

  /// <inheritdoc/>
  [DebuggerStepThrough]
  public new virtual bool Contains(TItem item) => base.Contains(item);

  #region Modification methods: Add, Remove, etc.

  /// <inheritdoc/>
  public new virtual void Add(TItem item)
  {
    EnsureNotLocked();
    base.Add(item);
  }

  /// <summary>
  /// Adds the elements of the specified collection to the end of the <see cref="CollectionBaseSlim{TItem}"/>.
  /// </summary>
  /// <param name="collection">The collection whose elements should be added to the end of the <see cref="CollectionBaseSlim{TItem}"/>. The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
  /// <exception cref="T:System.ArgumentNullException">collection is null.</exception>
  public new virtual void AddRange(IEnumerable<TItem> collection)
  {
    EnsureNotLocked();
    base.AddRange(collection);
  }

  /// <inheritdoc/>
  public new virtual bool Remove(TItem item)
  {
    EnsureNotLocked();
    return base.Remove(item);
  }

  /// <inheritdoc/>
  public new virtual void Clear()
  {
    EnsureNotLocked();
    base.Clear();
  }

  #endregion

  // Constructors

  /// <summary>
  /// Initializes a new instance of this type.
  /// </summary>
  public CollectionBaseSlim()
  {
  }

  /// <summary>
  /// Initializes a new instance of this type.
  /// </summary>
  /// <param name="capacity">The capacity.</param>
  public CollectionBaseSlim(int capacity) : base(capacity)
  {
  }

  /// <summary>
  /// Initializes a new instance of this type.
  /// </summary>
  /// <param name="collection">The collection.</param>
  public CollectionBaseSlim(IEnumerable<TItem> collection) : base(collection)
  {
  }
}
