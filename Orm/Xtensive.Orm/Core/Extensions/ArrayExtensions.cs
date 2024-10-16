// Copyright (C) 2007-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Anton U. Rogozhin
// Reimplemented by: Dmitri Maximov
// Created:    2007.07.04

using System;
using System.Collections.Generic;
using Xtensive.Comparison;
using Xtensive.Core;
using Xtensive.Reflection;


namespace Xtensive.Core
{
  /// <summary>
  /// <see cref="Array"/> related extension methods.
  /// </summary>
  public static class ArrayExtensions
  {
    /// <summary>
    /// Clones <paramref name="source"/> array with type case.
    /// </summary>
    /// <typeparam name="TItem">The type of source array items.</typeparam>
    /// <typeparam name="TNewItem">The type of result array items.</typeparam>
    /// <param name="source">Collection to convert.</param>
    /// <returns>An array containing all the items from the <paramref name="source"/>.</returns>
    public static TNewItem[] Cast<TItem, TNewItem>(this TItem[] source)
      where TNewItem: TItem
    {
      var items = new TNewItem[source.Length];
      int i = 0;
      foreach (TItem item in source)
        items[i++] = (TNewItem)item;
      return items;
    }

    /// <summary>
    /// Clones <paramref name="source"/> array with element conversion.
    /// </summary>
    /// <typeparam name="TItem">The type of item.</typeparam>
    /// <typeparam name="TNewItem">The type of item to convert to.</typeparam>
    /// <param name="source">The array to convert.</param>
    /// <param name="converter">A delegate that converts each element.</param>
    /// <returns>An array of converted elements.</returns>
    public static TNewItem[] Convert<TItem, TNewItem>(this TItem[] source, Converter<TItem, TNewItem> converter)
    {
      ArgumentNullException.ThrowIfNull(converter);
      var items = new TNewItem[source.Length];
      int i = 0;
      foreach (TItem item in source)
        items[i++] = converter(item);
      return items;
    }

    /// <summary>
    /// Gets the index of first occurrence of the <paramref name="item"/>
    /// in the <paramref name="items"/> array, if found;
    /// otherwise returns <see langword="-1"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of item.</typeparam>
    /// <param name="items">Array to search for the item.</param>
    /// <param name="item">Item to locate in the array.</param>
    /// <returns>
    /// Index of first occurrence of the <paramref name="item"/>
    /// in the <paramref name="items"/> array, if found;
    /// otherwise, <see langword="-1"/>.
    /// </returns>
    public static int IndexOf<TItem>(this TItem[] items, TItem item) 
    {
      ArgumentNullException.ThrowIfNull(items);
      for (int i = 0; i < items.Length; i++)
        if (AdvancedComparerStruct<TItem>.System.Equals(item, items[i]))
          return i;
      return -1;
    }

    /// <summary>
    /// Enumerates segment of an array.
    /// </summary>
    /// <typeparam name="TItem">The type of the array item.</typeparam>
    /// <param name="items">The array to enumerate the segment of.</param>
    /// <param name="offset">Segment offset.</param>
    /// <param name="length">Segment length.</param>
    /// <returns>An enumerable iterating through the segment.</returns>
    public static IEnumerable<TItem> Segment<TItem>(this TItem[] items, int offset, int length)
    {
      ArgumentNullException.ThrowIfNull(items);
      int lastIndex = offset + length;
      if (offset<0)
        offset = 0;
      if (lastIndex>items.Length)
        lastIndex = items.Length;
      for (int i = offset; i < lastIndex; i++)
        yield return items[i];
    }

    /// <summary>
    /// Gets the index of first occurrence of the <paramref name="item"/>
    /// in the <paramref name="items"/> array, if found;
    /// otherwise returns <see langword="-1"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of item.</typeparam>
    /// <param name="items">Array to search for the item.</param>
    /// <param name="item">Item to locate in the array.</param>
    /// <param name="byReference">Indicates whether just references
    /// should be compared.</param>
    /// <returns>
    /// Index of first occurrence of the <paramref name="item"/>
    /// in the <paramref name="items"/> array, if found;
    /// otherwise, <see langword="-1"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Value type is passed instead of class.</exception>
    public static int IndexOf<TItem>(this TItem[] items, TItem item, bool byReference) 
    {
      ArgumentNullException.ThrowIfNull(items);
      if (!byReference)
        return IndexOf(items, item);
      if (typeof(TItem).IsValueType)
        throw new InvalidOperationException(string.Format(
          Strings.ExTypeXMustBeReferenceType, 
          typeof(TItem).GetShortName()));
      for (int i = 0; i < items.Length; i++)
        if (ReferenceEquals(item, items[i]))
          return i;
      return -1;
    }

    /// <summary>
    /// Selects the specified item from the ordered sequence of items
    /// produced by ordering the <paramref name="items"/>.
    /// The original sequence will be partially reordered!
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <param name="items">The items to select from.</param>
    /// <param name="comparer">A function that compares two items.</param>
    /// <param name="index">The offset of the item to select from the ordered sequence.</param>
    /// <returns>The specified item from the ordered sequence of items.</returns>
    public static TItem Select<TItem>(this TItem[] items, Func<TItem, TItem, int> comparer, int index)
    {
      var r = new Random();
      int leftIndex = 0;
      int rightIndex = items.Length - 1;
      while (true) {
        int pivotIndex = leftIndex + r.Next(rightIndex - leftIndex + 1);
        pivotIndex = items.Partition(comparer, leftIndex, rightIndex, pivotIndex);
        if (index==pivotIndex)
          return items[index];
        else if (index < pivotIndex)
          rightIndex = pivotIndex - 1;
        else
          leftIndex = pivotIndex + 1;
      }
    }

    /// <summary>
    /// Combines the specified source and target arrays into new one.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    public static TItem[] Combine<TItem>(this TItem[] source, TItem[] target)
    {
      if (source == null || source.Length == 0)
        return target;
      if (target == null || target.Length == 0)
        return source;

      var result = new TItem[source.Length + target.Length];
      Array.Copy(source, result, source.Length);
      Array.Copy(target, 0, result, source.Length, target.Length);

      return result;
    }

    private static int Partition<TItem>(this TItem[] items, Func<TItem, TItem, int> comparer, int leftIndex, int rightIndex, int pivotIndex)
    {
      var pivot = items[pivotIndex];
      // Swap
      var tmp = items[rightIndex];
      items[rightIndex] = pivot;
      items[pivotIndex] = tmp;
      // Loop
      int storeIndex = leftIndex;
      for (int i = leftIndex; i < rightIndex; i++) {
        if (comparer(items[i], pivot) < 0) {
          // Swap
          tmp = items[storeIndex];
          items[storeIndex] = items[i];
          items[i] = tmp;
          storeIndex++;
        }
      }
      // Swap
      tmp = items[rightIndex];
      items[rightIndex] = items[storeIndex];
      items[storeIndex] = tmp;
      return storeIndex;
    }
  }
}
