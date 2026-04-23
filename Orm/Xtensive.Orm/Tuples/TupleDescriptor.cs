// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Nick Svetlov
// Created:    2007.05.30


using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using Xtensive.Core;
using Xtensive.Linq.SerializableExpressions.Internals;
using Xtensive.Reflection;
using Xtensive.Tuples.Packed;

namespace Xtensive.Tuples
{
  /// <summary>
  /// Tuple descriptor.
  /// Provides information about <see cref="Tuple"/> structure.
  /// </summary>
  [Serializable]
  public class TupleDescriptor : IEquatable<TupleDescriptor>, IReadOnlyList<Type>, ISerializable
  {
    public static readonly TupleDescriptor Empty = new TupleDescriptor(Array.Empty<Type>());

    private class LazyData
    {
      public int ValuesLength;
      public int ObjectsLength;
      public PackedFieldDescriptor[] FieldDescriptors;

      public LazyData(Type[] fieldTypes)
      {
        var fieldCount = fieldTypes.Length;
        FieldDescriptors = new PackedFieldDescriptor[fieldCount];

        switch (fieldCount) {
          case 0:
            ValuesLength = 0;
            ObjectsLength = 0;
            return;
          case 1:
            TupleLayout.ConfigureLen1(ref fieldTypes[0],
              ref FieldDescriptors[0],
              out ValuesLength, out ObjectsLength);
            break;
          case 2:
            TupleLayout.ConfigureLen2(fieldTypes,
              ref FieldDescriptors[0], ref FieldDescriptors[1],
              out ValuesLength, out ObjectsLength);
            break;
#if DO_MAX_1000_COLUMNS
        case > 1000:
          throw new NotSupportedException("This DataObjects.NET configuration does not support Recordsets with more than 1000 columns");
#endif
          default:
            TupleLayout.Configure(fieldTypes, FieldDescriptors, out ValuesLength, out ObjectsLength);
            break;
        }
      }

      public LazyData(Type[] fieldTypes, SerializationInfo info)
      {
        ValuesLength = info.GetInt32("ValuesLength");
        ObjectsLength = info.GetInt32("ObjectsLength");

        var typeNames = (string[]) info.GetValue("FieldTypes", typeof(string[]));
        FieldDescriptors = (PackedFieldDescriptor[]) info.GetValue(
          "FieldDescriptors", typeof(PackedFieldDescriptor[]));

        for (var i = 0; i < typeNames.Length; i++) {
          TupleLayout.ConfigureFieldAccessor(ref FieldDescriptors[i], fieldTypes[i]);
        }
      }
    }

    [NonSerialized]
    private LazyData data;

    private LazyData Data => data ??= new LazyData(FieldTypes);

    internal int ValuesLength => Data.ValuesLength;
    internal int ObjectsLength => Data.ObjectsLength;
    internal PackedFieldDescriptor[] FieldDescriptors => Data.FieldDescriptors;

    [field: NonSerialized]
    private Type[] FieldTypes { get; }

    #region IReadOnlyList members

    /// <inheritdoc/>
    public Type this[int fieldIndex]
    {
      get => FieldTypes[fieldIndex];
    }

    /// <inheritdoc/>
    public ColNum Count
    {
      [DebuggerStepThrough]
      get => (ColNum) FieldTypes.Length;
    }

    int IReadOnlyCollection<Type>.Count => Count;

    /// <inheritdoc/>
    public IEnumerator<Type> GetEnumerator()
    {
      for (int index = 0, count = Count; index < count; index++) {
        yield return FieldTypes[index];
      }
    }

    /// <inheritdoc/>
    [DebuggerStepThrough]
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    #endregion

    /// <summary>
    /// Creates tuple descriptor containing head of the current one.
    /// </summary>
    /// <param name="fieldCount">Head field count.</param>
    /// <returns>
    /// New tuple descriptor describing the specified set of fields.
    /// </returns>
    public TupleDescriptor Head(int fieldCount)
    {
      ArgumentOutOfRangeException.ThrowIfLessThan(fieldCount, 1);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(fieldCount, FieldTypes.Length);
      var fieldTypes = new Type[fieldCount];
      Array.Copy(FieldTypes, 0, fieldTypes, 0, fieldCount);
      return new TupleDescriptor(fieldTypes);
    }

    /// <summary>
    /// Creates tuple descriptor containing tail of the current one.
    /// </summary>
    /// <param name="tailFieldCount">Tail field count.</param>
    /// <returns>
    /// New tuple descriptor describing the specified set of fields.
    /// </returns>
    public TupleDescriptor Tail(int tailFieldCount)
    {
      ArgumentOutOfRangeException.ThrowIfLessThan(tailFieldCount, 1);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(tailFieldCount, FieldTypes.Length);
      var fieldTypes = new Type[tailFieldCount];
      Array.Copy(FieldTypes, Count - tailFieldCount, fieldTypes, 0, tailFieldCount);
      return new TupleDescriptor(fieldTypes);
    }

    /// <summary>
    /// Creates tuple descriptor containing segment of the current one
    /// </summary>
    /// <param name="segment">Offset and length of segment in form of Segment</param>
    /// <returns>
    /// New tuple descriptor describing the specified set of fields.
    /// </returns>
    public TupleDescriptor Segment(in Segment<ColNum> segment)
    {
      var fieldTypes = new Type[segment.Length];
      Array.Copy(FieldTypes, segment.Offset, fieldTypes, 0, segment.Length);

      return new TupleDescriptor(fieldTypes);
    }

    /// <summary>
    /// Concats fields of the current and the given descriptors to form new one.
    /// </summary>
    /// <param name="second">Tail fields descriptor.</param>
    /// <returns>New tuple descriptor containing fields of the both given source descriptors.</returns>
    public TupleDescriptor ConcatWith(in TupleDescriptor second)
    {
      var firstLength = FieldTypes.Length;
      var secondLength = second.FieldTypes.Length;
      var fieldTypes = new Type[firstLength + secondLength];
      Array.Copy(FieldTypes, fieldTypes, firstLength);
      Array.Copy(second.FieldTypes, 0, fieldTypes, firstLength, secondLength);

      return new TupleDescriptor(fieldTypes);
    }

    #region IEquatable members, GetHashCode

    /// <inheritdoc/>
    public bool Equals(TupleDescriptor other)
    {
      if (other is null) {
        return false;
      }
      var a = FieldTypes;
      var b = other.FieldTypes;
      if (a == null) {
        return b == null;
      }
      if (a.Length != b?.Length) {
        return false;
      }

      for (int i = a.Length; i-- > 0;) {
        if (a[i] != b[i]) {
          return false;
        }
      }
      return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) =>
      obj is TupleDescriptor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      HashCode hashCode = new();
      for (int i = Count; i-- > 0;)
        hashCode.Add(FieldTypes[i]);
      return hashCode.ToHashCode();
    }

    public static bool operator ==(in TupleDescriptor left, in TupleDescriptor right) =>
      (left is null && right is null) || left?.Equals(right) == true;

    public static bool operator !=(in TupleDescriptor left, in TupleDescriptor right) => !(left == right);

    #endregion

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue(nameof(ValuesLength), ValuesLength);
      info.AddValue(nameof(ObjectsLength), ObjectsLength);

      var typeNames = new string[FieldTypes.Length];
      for (var i = 0; i < typeNames.Length; i++)
        typeNames[i] = FieldTypes[i].ToSerializableForm();

      info.AddValue(nameof(FieldTypes), typeNames);
      info.AddValue(nameof(FieldDescriptors), FieldDescriptors);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      var sb = new ValueStringBuilder(stackalloc char[4096]);
      for (int i = 0, count = Count; i < count; i++) {
        if (i > 0)
          sb.Append(", ");
        sb.Append(FieldTypes[i].GetShortName());
      }
      return string.Format(Strings.TupleDescriptorFormat, sb.ToString());
    }

    #region Create methods (base)

    public static TupleDescriptor Create(Type t1) => new([t1]);
    public static TupleDescriptor Create(Type t1, Type t2) => new([t1, t2]);
    public static TupleDescriptor Create(Type t1, Type t2, Type t3) => new([t1, t2, t3]);
    public static TupleDescriptor Create(Type t1, Type t2, Type t3, Type t4) => new([t1, t2, t3, t4]);

    /// <summary>
    /// Creates or returns already created descriptor
    /// for provided set of types.
    /// </summary>
    /// <param name="fieldTypes">List of tuple field types.</param>
    /// <returns>Either new or existing tuple descriptor
    /// describing the specified set of fields.</returns>
    public static TupleDescriptor Create(Type[] fieldTypes)
    {
      ArgumentNullException.ThrowIfNull(fieldTypes);
      return fieldTypes.Length == 0 ? Empty : new(fieldTypes);
    }

    internal static TupleDescriptor CreateFromNormalized(Type[] normalizedFieldTypes)
    {
      ArgumentNullException.ThrowIfNull(normalizedFieldTypes);
      return normalizedFieldTypes.Length == 0 ? Empty : new(normalizedFieldTypes, true);
    }

    #endregion

    #region Create<...> methods (generic shortcuts)

    /// <summary>
    /// Creates new <see cref="TupleDescriptor"/> by its field type(s).
    /// </summary>
    /// <typeparam name="T">Type of the only tuple field.</typeparam>
    /// <returns>Newly created <see cref="TupleDescriptor"/> object.</returns>
    public static TupleDescriptor Create<T>()
      => Create(typeof(T));

    /// <summary>
    /// Creates new <see cref="TupleDescriptor"/> by its field type(s).
    /// </summary>
    /// <typeparam name="T1">Type of the first tuple field.</typeparam>
    /// <typeparam name="T2">Type of the 2nd tuple field.</typeparam>
    /// <returns>Newly created <see cref="TupleDescriptor"/> object</returns>
    public static TupleDescriptor Create<T1, T2>()
      => Create(typeof(T1), typeof(T2));

    /// <summary>
    /// Creates new <see cref="TupleDescriptor"/> by its field type(s).
    /// </summary>
    /// <typeparam name="T1">Type of the first tuple field.</typeparam>
    /// <typeparam name="T2">Type of the 2nd tuple field.</typeparam>
    /// <typeparam name="T3">Type of the 3rd tuple field.</typeparam>
    /// <returns>Newly created <see cref="TupleDescriptor"/> object</returns>
    public static TupleDescriptor Create<T1, T2, T3>()
      => Create(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    /// Creates new <see cref="TupleDescriptor"/> by its field type(s).
    /// </summary>
    /// <typeparam name="T1">Type of the first tuple field.</typeparam>
    /// <typeparam name="T2">Type of the 2nd tuple field.</typeparam>
    /// <typeparam name="T3">Type of the 3rd tuple field.</typeparam>
    /// <typeparam name="T4">Type of the 4th tuple field.</typeparam>
    /// <returns>Newly created <see cref="TupleDescriptor"/> object</returns>
    public static TupleDescriptor Create<T1, T2, T3, T4>()
      => Create(typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    #endregion

    // Constructors

    private TupleDescriptor(Type[] normalizedFieldTypes, bool _)
    {
      FieldTypes = normalizedFieldTypes;
    }

    internal TupleDescriptor(TupleDescriptor a, TupleDescriptor b)
      : this(a.FieldTypes.Combine(b.FieldTypes), true)
    { }

    private TupleDescriptor(Type[] fieldTypes)
    {
      var fieldCount = fieldTypes.Length;
      FieldTypes = fieldTypes;

      switch (fieldCount) {
        case 0:
          Data.ValuesLength = 0;
          Data.ObjectsLength = 0;
          return;
        case 1:
          TupleLayout.ConfigureLen1(ref FieldTypes[0],
            ref FieldDescriptors[0],
            out Data.ValuesLength, out Data.ObjectsLength);
          break;
        case 2:
          TupleLayout.ConfigureLen2(FieldTypes,
            ref FieldDescriptors[0], ref FieldDescriptors[1],
            out Data.ValuesLength, out Data.ObjectsLength);
          break;
        default:
          TupleLayout.Configure(FieldTypes, FieldDescriptors, out Data.ValuesLength, out Data.ObjectsLength);
          break;
      }
    }

    public TupleDescriptor(SerializationInfo info, StreamingContext context)
    {
      var typeNames = (string[]) info.GetValue(nameof(FieldTypes), typeof(string[]));
      FieldTypes = new Type[typeNames.Length];
      for (var i = 0; i < typeNames.Length; i++) {
        FieldTypes[i] = typeNames[i].GetTypeFromSerializableForm();
      }
      data = new LazyData(FieldTypes, info);
    }
  }
}
