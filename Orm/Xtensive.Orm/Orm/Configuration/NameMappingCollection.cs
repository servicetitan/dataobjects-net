// Copyright (C) 2014 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2014.03.13

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Xtensive.Core;

namespace Xtensive.Orm.Configuration
{
  /// <summary>
  /// Name mapping collection.
  /// </summary>
  [Serializable]
  public class NameMappingCollection : LockableBase, IEnumerable<KeyValuePair<string, string>>, ICloneable
  {
    /// <summary>
    /// Gets empty <see cref="NameMappingCollection"/>.
    /// </summary>
    public static readonly NameMappingCollection Empty = new NameMappingCollection();

    private readonly Dictionary<string, string> items = new Dictionary<string, string>();

    /// <summary>
    /// Gets number of elements in this collection.
    /// </summary>
    public int Count { get { return items.Count; } }

    /// <summary>
    /// Adds mapping between <paramref name="originalName"/>
    /// and <paramref name="mappedName"/>.
    /// </summary>
    /// <param name="originalName"></param>
    /// <param name="mappedName"></param>
    public void Add([NotNull] string originalName, [NotNull] string mappedName)
    {
      ArgumentException.ThrowIfNullOrEmpty(originalName);
      ArgumentException.ThrowIfNullOrEmpty(mappedName);
      EnsureNotLocked();
      items[originalName] = mappedName;
    }

    /// <summary>
    /// Removes mapping for <paramref name="originalName"/>.
    /// </summary>
    /// <param name="originalName"></param>
    public bool Remove([NotNull] string originalName)
    {
      ArgumentException.ThrowIfNullOrEmpty(originalName);
      EnsureNotLocked();
      return items.Remove(originalName);
    }

    /// <summary>
    /// Applies mapping to the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Mapped name for <paramref name="name"/>.</param>
    public string Apply([NotNull] string name)
    {
      ArgumentException.ThrowIfNullOrEmpty(name);
      string result;
      if (items.TryGetValue(name, out result))
        return result;
      return name;
    }

    /// <summary>
    /// Removes all mappings.
    /// </summary>
    public void Clear()
    {
      EnsureNotLocked();
      items.Clear();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>A <see cref="IEnumerator{T}" /> that can be used to iterate through the collection.</returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
      return items.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Creates clone of this instance.
    /// </summary>
    /// <returns>Clone of this instance.</returns>
    public NameMappingCollection Clone() => new(this);
    object ICloneable.Clone() => Clone();

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    public NameMappingCollection()
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="items">Mappings to add to this instance.</param>
    public NameMappingCollection([NotNull] IEnumerable<KeyValuePair<string, string>> items)
    {
      ArgumentNullException.ThrowIfNull(items);

      foreach (var item in items)
        Add(item.Key, item.Value);
    }

    static NameMappingCollection()
    {
      Empty.Lock();
    }
  }
}
