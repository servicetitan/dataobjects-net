// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Xtensive.Tuples.Packed
{
  internal static class PackedFieldDescriptorExtensions
  {
    private const int OffsetBitCount = 6;
    private const int OffsetMask = (1 << OffsetBitCount) - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PackedFieldAccessor GetAccessor(this PackedFieldDescriptor descriptor) => PackedFieldAccessor.All[descriptor.AccessorIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsObjectField(this PackedFieldDescriptor descriptor) => descriptor.GetAccessor().Rank < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetObjectIndex(this PackedFieldDescriptor descriptor) => descriptor.DataPosition;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueIndex(this PackedFieldDescriptor descriptor) => descriptor.DataPosition >> OffsetBitCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueBitOffset(this PackedFieldDescriptor descriptor) => descriptor.DataPosition & OffsetMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStateIndex(this PackedFieldDescriptor descriptor) => descriptor.StatePosition >> OffsetBitCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStateBitOffset(this PackedFieldDescriptor descriptor) => descriptor.StatePosition & OffsetMask;
  }
}
