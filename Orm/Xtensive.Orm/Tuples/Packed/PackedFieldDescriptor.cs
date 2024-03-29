// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.12.29

using System;
using System.Runtime.CompilerServices;

namespace Xtensive.Tuples.Packed
{
  [Serializable]
  internal struct PackedFieldDescriptor
  {
#if DO_MAX_1000_COLUMNS
    private int bitFields;

    internal int Index
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => bitFields & 0x7FF;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => bitFields = bitFields & ~0x7FF | value;
    }

    internal int StateIndex
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => (bitFields >> 11) & 0x1F;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => bitFields = bitFields & ~(0x1F << 11) | (value << 11);
    }

    internal int ValueBitOffset
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => (bitFields >> 16) & 0x3F;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => bitFields = bitFields & ~0x3F0000 | (value << 16);
    }

    internal int StateBitOffset
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => (bitFields >> 21) & 0x3E;                                // Even always

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => bitFields = bitFields & ~(0x3E << 21) | (value << 21);
    }

    internal int AccessorIndex
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => (bitFields >> 27) & 0x1F;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => bitFields = bitFields & ~(0x1F << 27) | (value << 27);
    }
#else
    internal ushort Index;
    internal ushort StateIndex;
    internal byte ValueBitOffset;
    internal byte StateBitOffset;
    internal byte AccessorIndex;

#endif // DO_MAX_1000_COLUMNS

    internal int DataPosition
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set {
        Index = (ushort) (value >> 6);
        ValueBitOffset = (byte) (value & 0x3F);
      }
    }

    internal int StatePosition
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set {
        StateIndex = (ushort) (value >> 6);
        StateBitOffset = (byte) (value & 0x3F);
      }
    }

    internal PackedFieldAccessor Accessor
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => PackedFieldAccessor.All[AccessorIndex];
    }

    internal bool IsObjectField
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Accessor.Rank < 0;
    }
  }
}