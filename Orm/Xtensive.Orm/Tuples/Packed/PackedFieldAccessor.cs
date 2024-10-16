// Copyright (C) 2013-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2013.01.22

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xtensive.Sql;
using Counters = Xtensive.Tuples.Packed.TupleLayout.Counters;
using CounterIncrementer = Xtensive.Tuples.Packed.TupleLayout.CounterIncrementer;
using PositionUpdater = Xtensive.Tuples.Packed.TupleLayout.PositionUpdater;

namespace Xtensive.Tuples.Packed
{
  internal abstract class PackedFieldAccessor
  {
    public static readonly PackedFieldAccessor[] All = new PackedFieldAccessor[20];

    /// <summary>
    /// Getter delegate.
    /// </summary>
    protected Delegate Getter;

    /// <summary>
    /// Setter delegate.
    /// </summary>
    protected Delegate Setter;

    /// <summary>
    /// Nullable getter delegate.
    /// </summary>
    protected Delegate NullableGetter;

    /// <summary>
    /// Nullable setter delegate.
    /// </summary>
    protected Delegate NullableSetter;

    public readonly int Rank;
    public readonly int ValueBitCount;
    protected readonly long ValueBitMask;
    public readonly byte Index;

    public bool IsObjectAccessor
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Rank < 0;
    }

    public readonly CounterIncrementer CounterIncrementer;
    public readonly PositionUpdater PositionUpdater;

    public void SetValue<T>(PackedTuple tuple, PackedFieldDescriptor descriptor, bool isNullable, T value)
    {
      if ((isNullable ? NullableSetter : Setter) is SetValueDelegate<T> setter) {
        setter.Invoke(tuple, descriptor, value);
      }
      else {
        SetUntypedValue(tuple, descriptor, value);
      }
    }

    public virtual void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      throw new NotSupportedException($"{this} does not support reading from DbDataReader");

    public T GetValue<T>(PackedTuple tuple, PackedFieldDescriptor descriptor, bool isNullable, out TupleFieldState fieldState)
    {
      if ((isNullable ? NullableGetter : Getter) is GetValueDelegate<T> getter) {
        return getter.Invoke(tuple, descriptor, out fieldState);
      }
      var targetType = typeof(T);

      //Dirty hack of nullable enum reading
      if (isNullable) {
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
      }
      if (targetType.IsEnum) {
        return (T) Enum.ToObject(targetType, GetUntypedValue(tuple, descriptor, out fieldState));
      }
      return (T) GetUntypedValue(tuple, descriptor, out fieldState);
    }

    public abstract object GetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState);

    public abstract void SetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, object value);

    public abstract void CopyValue(PackedTuple source, PackedFieldDescriptor sourceDescriptor,
      PackedTuple target, PackedFieldDescriptor targetDescriptor);

    public abstract bool ValueEquals(PackedTuple left, PackedFieldDescriptor leftDescriptor,
      PackedTuple right, PackedFieldDescriptor rightDescriptor);

    public abstract int GetValueHashCode(PackedTuple tuple, PackedFieldDescriptor descriptor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateDescriptorPosition(ref PackedFieldDescriptor descriptor, ref int bitCounter)
    {
      descriptor.DataPosition = bitCounter;
      bitCounter += ValueBitCount;
    }

    protected PackedFieldAccessor(int rank, byte index)
    {
      Rank = rank;
      Index = index;
      if (All[Index] != null) {
        throw new IndexOutOfRangeException($"Duplicated Index {Index} of PackedFieldAccessor instance");
      }
      All[Index] = this;
      ValueBitCount = 1 << Rank;

      // What we want here is to shift 1L by ValueBitCount to left and then subtract 1
      // This gives us a mask. For example if bit count = 4 then
      // 0000_0001 << 4 = 0001_0000
      // 0001_000 - 1 = 0000_1111
      // However in case bit count equal to data type size left shift doesn't work as we want
      // e.g. for Int8 : 0000_0001 << 8 = 0000_0001 but we would like it to be 0000_0000
      // because 0000_0000 - 1 = 1111_1111 and this is exactly what we need.
      // As a workaround we do left shift in two steps. In the example above
      // 0000_0001 << 7 = 1000_0000
      // and then
      // 1000_0000 << 1 = 0000_0000
      ValueBitMask = (1L << (ValueBitCount - 1) << 1) - 1;

      CounterIncrementer = Rank switch {
        0 => (ref Counters counters) => counters.Val001Counter++,
        3 => (ref Counters counters) => counters.Val008Counter++,
        4 => (ref Counters counters) => counters.Val016Counter++,
        5 => (ref Counters counters) => counters.Val032Counter++,
        6 => (ref Counters counters) => counters.Val064Counter++,
        7 => (ref Counters counters) => counters.Val128Counter++,
        _ => (ref Counters counters) => throw new NotSupportedException(),
      };

      PositionUpdater = Rank switch {
        0 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val001Counter),
        3 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val008Counter),
        4 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val016Counter),
        5 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val032Counter),
        6 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val064Counter),
        7 => (ref PackedFieldDescriptor descriptor, ref Counters counters) => UpdateDescriptorPosition(ref descriptor, ref counters.Val128Counter),
        _ => (ref PackedFieldDescriptor descriptor, ref Counters counters) => throw new NotSupportedException()
      };
    }
  }

  internal sealed class ObjectFieldAccessor : PackedFieldAccessor
  {
    public const int FixedIndex = 1;

    public override object GetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState)
    {
      var state = tuple.GetFieldState(descriptor);
      fieldState = state;
      return state == TupleFieldState.Available ? tuple.Objects[descriptor.Index] : null;
    }

    public override void SetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, object value)
    {
      tuple.Objects[descriptor.Index] = value;
      tuple.SetFieldState(descriptor, value != null ? TupleFieldState.Available : (TupleFieldState.Available | TupleFieldState.Null));
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetUntypedValue(tuple, descriptor, mr.Reader(mr.DbDataReader, mr.FieldIndex));

    public override void CopyValue(PackedTuple source, PackedFieldDescriptor sourceDescriptor,
      PackedTuple target, PackedFieldDescriptor targetDescriptor)
    {
      target.Objects[targetDescriptor.Index] = source.Objects[sourceDescriptor.Index];
    }

    public override bool ValueEquals(PackedTuple left, PackedFieldDescriptor leftDescriptor,
      PackedTuple right, PackedFieldDescriptor rightDescriptor)
    {
      return Equals(left.Objects[leftDescriptor.Index], right.Objects[rightDescriptor.Index]);
    }

    public override int GetValueHashCode(PackedTuple tuple, PackedFieldDescriptor descriptor)
    {
      return tuple.Objects[descriptor.Index]?.GetHashCode() ?? 0;
    }

    public ObjectFieldAccessor()
      : base(-1, FixedIndex)
    { }
  }

  internal abstract class ValueFieldAccessor : PackedFieldAccessor
  {
    public Type FieldType { get; protected set; }

    private static int GetRank(int bitSize) =>
      BitOperations.Log2((uint)bitSize);

    protected ValueFieldAccessor(int bitCount, byte index)
      : base(GetRank(bitCount), index)
    {}
  }

  internal abstract class ValueFieldAccessor<T> : ValueFieldAccessor
    where T : struct, IEquatable<T>
  {
    private static readonly T DefaultValue = default;
    private static readonly T? NullValue = null;

    protected virtual long Encode(T value) => throw new NotSupportedException();

    protected virtual T Decode(long value) => throw new NotSupportedException();

    public override object GetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState)
    {
      fieldState = tuple.GetFieldState(descriptor);
      return fieldState == TupleFieldState.Available ? (object) Load(tuple, descriptor) : null;
    }

    public override void SetUntypedValue(PackedTuple tuple, PackedFieldDescriptor descriptor, object value)
    {
      if (value != null) {
        Store(tuple, descriptor, (T) value);
        tuple.SetFieldState(descriptor, TupleFieldState.Available);
      }
      else {
        tuple.SetFieldState(descriptor, TupleFieldState.Available | TupleFieldState.Null);
      }
    }

    public override void CopyValue(PackedTuple source, PackedFieldDescriptor sourceDescriptor,
      PackedTuple target, PackedFieldDescriptor targetDescriptor)
    {
      Store(target, targetDescriptor, Load(source, sourceDescriptor));
    }

    public override bool ValueEquals(PackedTuple left, PackedFieldDescriptor leftDescriptor, PackedTuple right, PackedFieldDescriptor rightDescriptor) =>
      Load(left, leftDescriptor).Equals(Load(right, rightDescriptor));

    public override int GetValueHashCode(PackedTuple tuple, PackedFieldDescriptor descriptor)
    {
      return Load(tuple, descriptor).GetHashCode();
    }

    private T GetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState)
    {
      fieldState = tuple.GetFieldState(descriptor);
      return fieldState == TupleFieldState.Available ? Load(tuple, descriptor) : DefaultValue;
    }

    private T? GetNullableValue(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState)
    {
      fieldState = tuple.GetFieldState(descriptor);
      return fieldState == TupleFieldState.Available ? Load(tuple, descriptor) : NullValue;
    }

    protected void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, T value)
    {
      Store(tuple, descriptor, value);
      tuple.SetFieldState(descriptor, TupleFieldState.Available);
    }

    private void SetNullableValue(PackedTuple tuple, PackedFieldDescriptor descriptor, T? value)
    {
      if (value != null) {
        Store(tuple, descriptor, value.Value);
        tuple.SetFieldState(descriptor, TupleFieldState.Available);
      }
      else {
        tuple.SetFieldState(descriptor, TupleFieldState.Available | TupleFieldState.Null);
      }
    }

    protected virtual void Store(PackedTuple tuple, PackedFieldDescriptor d, T value)
    {
      var encoded = Encode(value);
      ref var block = ref tuple.Values[d.Index];
      var valueBitOffset = d.ValueBitOffset;
      var mask = ValueBitMask << valueBitOffset;
      block = (block & ~mask) | ((encoded << valueBitOffset) & mask);
    }

    protected virtual T Load(PackedTuple tuple, PackedFieldDescriptor d) =>
      Decode((tuple.Values[d.Index] >> d.ValueBitOffset) & ValueBitMask);

    protected ValueFieldAccessor(int bits, byte index)
      : base(bits, index)
    {
      FieldType = typeof(T);
      Getter = (GetValueDelegate<T>) GetValue;
      Setter = (SetValueDelegate<T>) SetValue;

      NullableGetter = (GetValueDelegate<T?>) GetNullableValue;
      NullableSetter = (SetValueDelegate<T?>) SetNullableValue;
    }
  }

  internal sealed class BooleanFieldAccessor : ValueFieldAccessor<bool>
  {
    protected override long Encode(bool value)
    {
      return value ? 1L : 0L;
    }

    protected override bool Decode(long value)
    {
      return value != 0;
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadBoolean(mr.DbDataReader, mr.FieldIndex));

    public BooleanFieldAccessor()
      : base(1, 2)
    {
    }
  }

  internal sealed class FloatFieldAccessor : ValueFieldAccessor<float>
  {
    protected override long Encode(float value)
    {
      unsafe {
        return *(int*) &value;
      }
    }

    protected override float Decode(long value)
    {
      var intValue = unchecked((int) value);
      unsafe {
        return *(float*) &intValue;
      }
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadFloat(mr.DbDataReader, mr.FieldIndex));

    public FloatFieldAccessor()
      : base(sizeof(float) * 8, 3)
    {
    }
  }

  internal sealed class DoubleFieldAccessor : ValueFieldAccessor<double>
  {
    protected override long Encode(double value)
    {
      return BitConverter.DoubleToInt64Bits(value);
    }

    protected override double Decode(long value)
    {
      return BitConverter.Int64BitsToDouble(value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadDouble(mr.DbDataReader, mr.FieldIndex));

    public DoubleFieldAccessor()
      : base(sizeof(double) * 8, 4)
    {
    }
  }

  internal sealed class TimeSpanFieldAccessor : ValueFieldAccessor<TimeSpan>
  {
    protected override long Encode(TimeSpan value)
    {
      return value.Ticks;
    }

    protected override TimeSpan Decode(long value)
    {
      return TimeSpan.FromTicks(value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadTimeSpan(mr.DbDataReader, mr.FieldIndex));

    public TimeSpanFieldAccessor()
      : base(sizeof(long) * 8, 5)
    {
    }
  }

  internal sealed class DateTimeFieldAccessor : ValueFieldAccessor<DateTime>
  {
    protected override long Encode(DateTime value)
    {
      return value.ToBinary();
    }

    protected override DateTime Decode(long value)
    {
      return DateTime.FromBinary(value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadDateTime(mr.DbDataReader, mr.FieldIndex));

    public DateTimeFieldAccessor()
      : base(sizeof(long) * 8, 6)
    {
    }
  }

  internal sealed class ByteFieldAccessor : ValueFieldAccessor<byte>
  {
    protected override long Encode(byte value)
    {
      return value;
    }

    protected override byte Decode(long value)
    {
      return unchecked((byte) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadByte(mr.DbDataReader, mr.FieldIndex));

    public ByteFieldAccessor()
      : base(sizeof(byte) * 8, 7)
    {
    }
  }

  internal sealed class SByteFieldAccessor : ValueFieldAccessor<sbyte>
  {
    protected override long Encode(sbyte value)
    {
      return value;
    }

    protected override sbyte Decode(long value)
    {
      return unchecked((sbyte) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadSByte(mr.DbDataReader, mr.FieldIndex));

    public SByteFieldAccessor()
      : base(sizeof(sbyte) * 8, 8)
    {
    }
  }

  internal sealed class ShortFieldAccessor : ValueFieldAccessor<short>
  {
    protected override long Encode(short value)
    {
      return value;
    }

    protected override short Decode(long value)
    {
      return unchecked((short) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadShort(mr.DbDataReader, mr.FieldIndex));

    public ShortFieldAccessor()
      : base(sizeof(short) * 8, 9)
    {
    }
  }

  internal sealed class UShortFieldAccessor : ValueFieldAccessor<ushort>
  {
    protected override long Encode(ushort value)
    {
      return value;
    }

    protected override ushort Decode(long value)
    {
      return unchecked((ushort) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadUShort(mr.DbDataReader, mr.FieldIndex));

    public UShortFieldAccessor()
      : base(sizeof(ushort) * 8, 10)
    {
    }
  }

  internal sealed class IntFieldAccessor : ValueFieldAccessor<int>
  {
    protected override long Encode(int value)
    {
      return value;
    }

    protected override int Decode(long value)
    {
      return unchecked((int) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadInt(mr.DbDataReader, mr.FieldIndex));

    public IntFieldAccessor()
      : base(sizeof(int) * 8, 11)
    {
    }
  }

  internal sealed class UIntFieldAccessor : ValueFieldAccessor<uint>
  {
    protected override long Encode(uint value)
    {
      return value;
    }

    protected override uint Decode(long value)
    {
      return unchecked((uint) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadUInt(mr.DbDataReader, mr.FieldIndex));

    public UIntFieldAccessor()
      : base(sizeof(uint) * 8, 12)
    {
    }
  }

  internal sealed class LongFieldAccessor : ValueFieldAccessor<long>
  {
    protected override long Encode(long value)
    {
      return value;
    }

    protected override long Decode(long value)
    {
      return value;
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadLong(mr.DbDataReader, mr.FieldIndex));

    public LongFieldAccessor()
      : base(sizeof(long) * 8, 13)
    {
    }
  }

  internal sealed class ULongFieldAccessor : ValueFieldAccessor<ulong>
  {
    protected override long Encode(ulong value)
    {
      return unchecked((long) value);
    }

    protected override ulong Decode(long value)
    {
      return unchecked((ulong) value);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadULong(mr.DbDataReader, mr.FieldIndex));

    public ULongFieldAccessor()
      : base(sizeof(ulong) * 8, 14)
    {
    }
  }

  internal sealed class GuidFieldAccessor : ValueFieldAccessor<Guid>
  {
    protected override Guid Load(PackedTuple tuple, PackedFieldDescriptor d)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          return *(Guid*) valuePtr;
      }
    }

    protected override void Store(PackedTuple tuple, PackedFieldDescriptor d, Guid value)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          *(Guid*) valuePtr = value;
      }
    }

    private static unsafe int GetSize()
    {
      return sizeof(Guid);
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadGuid(mr.DbDataReader, mr.FieldIndex));

    public GuidFieldAccessor()
      : base(GetSize() * 8, 15)
    {
    }
  }

  internal sealed class DecimalFieldAccessor : ValueFieldAccessor<decimal>
  {
    protected override decimal Load(PackedTuple tuple, PackedFieldDescriptor d)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          return *(decimal*) valuePtr;
      }
    }

    protected override void Store(PackedTuple tuple, PackedFieldDescriptor d, decimal value)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          *(decimal*) valuePtr = value;
      }
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadDecimal(mr.DbDataReader, mr.FieldIndex));

    public DecimalFieldAccessor()
      : base(sizeof(decimal) * 8, 16)
    {
    }
  }

  internal sealed class DateTimeOffsetFieldAccessor : ValueFieldAccessor<DateTimeOffset>
  {
    protected override DateTimeOffset Load(PackedTuple tuple, PackedFieldDescriptor d)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          return *(DateTimeOffset*) valuePtr;
      }
    }

    protected override void Store(PackedTuple tuple, PackedFieldDescriptor d, DateTimeOffset value)
    {
      unsafe {
        fixed (long* valuePtr = &tuple.Values[d.Index])
          *(DateTimeOffset*) valuePtr = value;
      }
    }

    private static unsafe int GetSize()
    {
      // Depending on architecture, x86 or x64, the size of DateTimeOffset is either 12 or 16 respectively.
      // Due to the fact that Rank calculation algorithm expects sizes to be equal to one of the power of two
      // it returns wrong rank value for size 12 (bitsize = 96) which causes wrong choice of Encode/Decode methods.
      // Setting it to 16 helps to solve Rank problem.
      return sizeof(long) * 2;
    }

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadDateTimeOffset(mr.DbDataReader, mr.FieldIndex));

    public DateTimeOffsetFieldAccessor()
       : base(GetSize() * 8, 17)
    { }
  }

  internal sealed class DateOnlyFieldAccessor : ValueFieldAccessor<DateOnly>
  {
    protected override DateOnly Decode(long value) =>
      DateOnly.FromDayNumber((int)value);

    protected override long Encode(DateOnly value) =>
      value.DayNumber;

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadDateOnly(mr.DbDataReader, mr.FieldIndex));

    public DateOnlyFieldAccessor()
       : base(sizeof(int) * 8, 18)
    { }
  }

  internal sealed class TimeOnlyFieldAccessor : ValueFieldAccessor<TimeOnly>
  {
    protected override TimeOnly Decode(long value) =>
      new TimeOnly(value);

    protected override long Encode(TimeOnly value) =>
      value.Ticks;

    public override void SetValue(PackedTuple tuple, PackedFieldDescriptor descriptor, MapperReader mr) =>
      SetValue(tuple, descriptor, mr.Mapper.ReadTimeOnly(mr.DbDataReader, mr.FieldIndex));

    public TimeOnlyFieldAccessor()
       : base(sizeof(long) * 8, 19)
    { }
  }
}