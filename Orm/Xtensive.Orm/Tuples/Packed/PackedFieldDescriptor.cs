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
    internal ushort Index;
    internal ushort StateIndex;
    internal byte ValueBitOffset;
    internal byte StateBitOffset;
    internal byte AccessorIndex;

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