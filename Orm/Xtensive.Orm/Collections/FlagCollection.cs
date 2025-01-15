// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2007.10.01

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using Xtensive.Conversion;
using Xtensive.Core;

namespace Xtensive.Collections
{
  /// <summary>
  /// A sequence of <typeparamref name="TKey"/>-<typeparamref name="TFlag"/> pairs.
  /// </summary>
  /// <remarks>
  /// Item count should be less than 32.
  /// <see cref="Biconverter{TFrom,TTo}"/> is used to convert flag keys from type <typeparamref name="TFlag"/> to <see cref="bool"/>.
  /// </remarks>
  /// <typeparam name="TKey">Type of the key.</typeparam>
  /// <typeparam name="TFlag">Type of the flag.</typeparam>
  [Serializable]
  [DebuggerDisplay("Count = {Count}")]
  public class FlagCollection<TKey, TFlag>: LockableBase,
    IList<KeyValuePair<TKey, TFlag>>,
    IReadOnlyDictionary<TKey, TFlag>,
    IEquatable<FlagCollection<TKey, TFlag>>,
    ISerializable
  {
    private const int MaxItemCount = 32;
    private readonly List<TKey> keys = new();
    private BitVector32 flags;

    /// <summary>
    /// Gets <see cref="Biconverter{TFrom,TTo}"/> instance
    /// used to convert flag value to <see cref="bool"/> and vice versa.
    /// </summary>
    public Biconverter<TFlag, bool> Converter { [DebuggerStepThrough] get; }

    /// <summary>
    /// Gets an <see cref="Collection{T}"/> containing the flags.
    /// </summary>
    public ICollection<TFlag> Flags => Values;

    #region IReadOnlyDictionary<TKey,TFlag> Members

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => keys.Contains(key);

    /// <inheritdoc/>
    public void Add(TKey key, TFlag flag)
    {
      EnsureNotLocked();
      if (keys.Contains(key))
        throw new ArgumentException("key", Strings.ExCollectionAlreadyContainsSpecifiedItem);
      if (keys.Count >= MaxItemCount)
        throw new InvalidOperationException(string.Format(Strings.ExMaxItemCountIsN, MaxItemCount));
      keys.Add(key);
      flags[1 << (keys.Count - 1)] = Converter.ConvertForward(flag);
    }

    /// <inheritdoc/>
    public virtual void Add(TKey key) => Add(key, default);

    public bool TryAdd(TKey key, TFlag flag)
    {
      if (!ContainsKey(key)) {
        Add(key, flag);
        return true;
      }

      return false;
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
      ArgumentValidator.EnsureArgumentIsNotDefault(key, "key");
      EnsureNotLocked();
      int index = keys.IndexOf(key);
      if (index < 0)
        return false;
      keys.RemoveAt(index);
      int data = flags.Data;
      int remainder = data & (0xFFFF << index);
      data ^= remainder;
      data |= (remainder >> 1) & (0xFFFF << index);
      flags = new BitVector32(data);
      return true;
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, out TFlag value)
    {
      value = Converter.ConvertBackward(false);
      int index = keys.IndexOf(key);
      if (index < 0)
        return false;
      value = Converter.ConvertBackward(flags[1 << index]);
      return true;
    }

    /// <inheritdoc/>
    public TFlag this[TKey key]
    {
      get
      {
        ArgumentValidator.EnsureArgumentIsNotDefault(key, "key");
        return TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
      }
      set
      {
        ArgumentValidator.EnsureArgumentIsNotDefault(key, "key");
        EnsureNotLocked();
        int index = keys.IndexOf(key);
        if (index < 0)
          Add(key, value);
        else
          flags[1 << index] = Converter.ConvertForward(value);
      }
    }

    /// <summary>
    /// Gets a list of keys.
    /// </summary>
    /// <returns>A list of keys.</returns>
    public IReadOnlyList<TKey> Keys => keys;

    /// <inheritdoc/>
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TFlag>.Keys => Keys;

    /// <summary>
    /// Gets an array of values.
    /// </summary>
    /// <returns>An array of values.</returns>
    public TFlag[] Values {
      get {
        var n = keys.Count;
        var array = new TFlag[n];
        for (int i = 0; i < n; i++)
          array[i] = Converter.ConvertBackward(flags[1 << i]);
        return array;
      }
    }

    /// <inheritdoc/>
    IEnumerable<TFlag> IReadOnlyDictionary<TKey, TFlag>.Values => Values;

    #endregion

    #region IList<KeyValuePair<TKey,TFlag>> Members

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TFlag> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public void Clear()
    {
      EnsureNotLocked();
      keys.Clear();
      flags = new BitVector32(0);
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TFlag> item) => ContainsKey(item.Key);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TFlag>[] array, int arrayIndex) => this.Copy(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TFlag> item) => Remove(item.Key);

    /// <inheritdoc/>
    public int Count => keys.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => IsLocked;

    /// <inheritdoc/>
    int IList<KeyValuePair<TKey, TFlag>>.IndexOf(KeyValuePair<TKey, TFlag> item) => throw new NotSupportedException();

    /// <inheritdoc/>
    void IList<KeyValuePair<TKey, TFlag>>.Insert(int index, KeyValuePair<TKey, TFlag> item) => throw new NotSupportedException();

    /// <inheritdoc/>
    void IList<KeyValuePair<TKey, TFlag>>.RemoveAt(int index) => throw new NotSupportedException();

    /// <inheritdoc/>
    public KeyValuePair<TKey, TFlag> this[int index] {
      get {
        if (keys.Count <= index)
          throw new ArgumentOutOfRangeException("index");
        return new KeyValuePair<TKey, TFlag>(keys[index], Converter.ConvertBackward(flags[1<<index]));
      }
      set {
        // TODO: implement?
        throw new NotImplementedException();
      }
    }

    #endregion

    #region ICollection<KeyValuePair<TKey, TFlag>> Members

    /// <inheritdoc/>
    void ICollection<KeyValuePair<TKey, TFlag>>.Add(KeyValuePair<TKey, TFlag> key) => Add(key.Key, key.Value);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, TFlag>>.Contains(KeyValuePair<TKey, TFlag> item)
    {
      int index = keys.IndexOf(item.Key);
      if (index < 0)
        return false;
      return flags[1 << index] == Converter.ConvertForward(item.Value);
    }

    /// <inheritdoc/>
    void ICollection<KeyValuePair<TKey, TFlag>>.CopyTo(KeyValuePair<TKey, TFlag>[] array, int arrayIndex) => this.Copy(array, arrayIndex);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, TFlag>>.Remove(KeyValuePair<TKey, TFlag> item) => throw new NotSupportedException();

    #endregion

    #region GetEnumerator methods

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TFlag>> GetEnumerator()
    {
      for (int i = 0; i < keys.Count; i++)
        yield return new KeyValuePair<TKey, TFlag>(keys[i], Converter.ConvertBackward(flags[1 << i]));
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable<KeyValuePair<TKey, TFlag>>)this).GetEnumerator();
    }

    #endregion

    #region Equals, GetHashCode methods

    /// <inheritdoc/>
    public bool Equals(FlagCollection<TKey, TFlag> other)
    {
      if (ReferenceEquals(this, other))
        return true;
      if (other == null)
        return false;
      var count = Count;
      if (count != other.Count)
        return false;
      for (int i = 0; i < count; i++)
        if (!this[i].Equals(other[i]))
          return false;
      return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is FlagCollection<TKey, TFlag> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(keys, flags);

    #endregion


    // Constructors

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="converter"><see cref="Converter"/> property value.</param>
    public FlagCollection(Biconverter<TFlag, bool> converter)
      : this()
    {
      Converter = converter;
    }

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="converter"><see cref="Converter"/> property value.</param>
    /// <param name="enumerable">Initial content of collection.</param>
    public FlagCollection(Biconverter<TFlag, bool> converter, IEnumerable<KeyValuePair<TKey, TFlag>> enumerable)
      : this()
    {
      Converter = converter;
      foreach (KeyValuePair<TKey, TFlag> pair in enumerable)
        Add(pair.Key, pair.Value);
    }

    private FlagCollection()
    {
    }

    #region ISerializable members

    /// <summary>
    /// Deserializes instance of this type.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected FlagCollection(SerializationInfo info, StreamingContext context)
      : base(info.GetBoolean("IsLocked"))
    {
      Converter = (Biconverter<TFlag, bool>)
        info.GetValue("AdvancedConverter", typeof(Biconverter<TFlag, bool>));
      keys = (List<TKey>)info.GetValue("Keys", typeof(List<TKey>));
      flags = new BitVector32(info.GetInt32("Flags"));
    }

    /// <summary>
    /// Serializes instance of this type.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    [SecurityCritical]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("IsLocked", IsLocked);
      info.AddValue("AdvancedConverter", Converter);
      info.AddValue("Keys", keys);
      info.AddValue("Flags", flags.Data);
    }

    #endregion
  }
}
