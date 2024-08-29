// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.12.29

using System;
using System.Data.Common;
using Xtensive.Sql;

namespace Xtensive.Tuples.Packed
{
  [Serializable]
  internal sealed class PackedTuple : RegularTuple
  {
    public readonly TupleDescriptor PackedDescriptor;
    public readonly long[] Values;
    public readonly object[] Objects;

    public override TupleDescriptor Descriptor
    {
      get { return PackedDescriptor; }
    }

    public override Tuple Clone()
    {
      return new PackedTuple(this);
    }

    public override Tuple CreateNew()
    {
      return new PackedTuple(PackedDescriptor);
    }

    public override bool Equals(Tuple other)
    {
      if (!(other is PackedTuple packedOther)) {
        return base.Equals(other);
      }

      if (ReferenceEquals(packedOther, this)) {
        return true;
      }
      if (Descriptor != packedOther.Descriptor) {
        return false;
      }

      var fieldDescriptors = PackedDescriptor.FieldDescriptors;
      var count = Count;
      for (int i = 0; i < count; i++) {
        var descriptor = fieldDescriptors[i];
        var thisState = GetFieldState(descriptor);
        var otherState = packedOther.GetFieldState(descriptor);
        if (thisState != otherState) {
          return false;
        }
        if (thisState == TupleFieldState.Available &&
            !descriptor.Accessor.ValueEquals(this, descriptor, packedOther, descriptor)) {
          return false;
        }
      }

      return true;
    }

    // Must be compatible with Tuple.GetHashCode() - return the same result
    public override int GetHashCode()
    {
      var fieldDescriptors = PackedDescriptor.FieldDescriptors;
      HashCode hashCode = new();
      for (int i = Count; i-- > 0;) {
        var descriptor = fieldDescriptors[i];
        hashCode.Add(GetFieldState(descriptor) == TupleFieldState.Available
          ? descriptor.Accessor.GetValueHashCode(this, descriptor)
          : 0);
      }
      return hashCode.ToHashCode();
    }

    public override TupleFieldState GetFieldState(int fieldIndex) =>
      GetFieldState(PackedDescriptor.FieldDescriptors[fieldIndex]);

    protected internal override void SetFieldState(int fieldIndex, TupleFieldState fieldState)
    {
      if (fieldState == TupleFieldState.Null) {
        throw new ArgumentOutOfRangeException(nameof(fieldState));
      }

      SetFieldState(PackedDescriptor.FieldDescriptors[fieldIndex], fieldState);
    }

    public override object GetValue(int fieldIndex, out TupleFieldState fieldState)
    {
      var descriptor = PackedDescriptor.FieldDescriptors[fieldIndex];
      return descriptor.Accessor.GetUntypedValue(this, descriptor, out fieldState);
    }

    public override T GetValue<T>(int fieldIndex, out TupleFieldState fieldState)
    {
      var isNullable = null == default(T); // Is nullable value type or class
      var descriptor = PackedDescriptor.FieldDescriptors[fieldIndex];
      return descriptor.Accessor.GetValue<T>(this, descriptor, isNullable, out fieldState);
    }

    public override void SetValue(int fieldIndex, object fieldValue)
    {
      var descriptor = PackedDescriptor.FieldDescriptors[fieldIndex];
      descriptor.Accessor.SetUntypedValue(this, descriptor, fieldValue);
    }

    public override void SetValue<T>(int fieldIndex, T fieldValue)
    {
      var isNullable = null==default(T); // Is nullable value type or class
      var descriptor = PackedDescriptor.FieldDescriptors[fieldIndex];
      descriptor.Accessor.SetValue(this, descriptor, isNullable, fieldValue);
    }

    public override void SetValueFromDataReader(in MapperReader mr)
    {
      if (mr.DbDataReader.IsDBNull(mr.FieldIndex)) {
        SetValue(mr.FieldIndex, null);
      }
      else {
        var descriptor = PackedDescriptor.FieldDescriptors[mr.FieldIndex];
        descriptor.Accessor.SetValue(this, descriptor, mr);
      }
    }

    public void SetFieldState(PackedFieldDescriptor d, TupleFieldState fieldState)
    {
      var bits = (long) fieldState;
      ref var block = ref Values[d.StateIndex];
      var stateBitOffset = d.StateBitOffset;
      block = (block & ~(3L << stateBitOffset)) | (bits << stateBitOffset);

      if (fieldState != TupleFieldState.Available && d.Accessor.IsObjectAccessor) {
        Objects[d.Index] = null;
      }
    }

    public TupleFieldState GetFieldState(PackedFieldDescriptor d) =>
      (TupleFieldState) ((Values[d.StateIndex] >> d.StateBitOffset) & 3);

    public PackedTuple(in TupleDescriptor descriptor)
    {
      PackedDescriptor = descriptor;

      Values = new long[PackedDescriptor.ValuesLength];
      Objects = PackedDescriptor.ObjectsLength > 0
        ? new object[PackedDescriptor.ObjectsLength]
        : Array.Empty<object>();
    }

    private PackedTuple(PackedTuple origin)
    {
      PackedDescriptor = origin.PackedDescriptor;

      Values = (long[]) origin.Values.Clone();
      Objects = PackedDescriptor.ObjectsLength > 0
        ? (object[]) origin.Objects.Clone()
        : Array.Empty<object>();
    }
  }
}