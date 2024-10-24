// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Ustinov
// Created:    2007.06.01

using System;
using System.Diagnostics;
using Xtensive.Comparison;

namespace Xtensive.Core
{
  /// <summary>
  /// A pair of two values.
  /// </summary>
  /// <typeparam name="TFirst">The <see cref="Type"/> of first value.</typeparam>
  /// <typeparam name="TSecond">The <see cref="Type"/> of second value.</typeparam>
  [Serializable]
  [DebuggerDisplay("{First}, {Second}")]
  public readonly struct Pair<TFirst, TSecond> :
    IComparable<Pair<TFirst, TSecond>>,
    IEquatable<Pair<TFirst, TSecond>>
  {
    private static readonly AdvancedComparerStruct<TFirst> FirstComparer = AdvancedComparerStruct<TFirst>.System;
    private static readonly AdvancedComparerStruct<TSecond> SecondComparer = AdvancedComparerStruct<TSecond>.System;

    /// <summary>
    /// A first value.
    /// </summary>
    public readonly TFirst First;
    /// <summary>
    /// A second value.
    /// </summary>
    public readonly TSecond Second;

    #region IComparable<...>, IEquatable<...> methods

    /// <inheritdoc/>
    public bool Equals(Pair<TFirst, TSecond> other) =>
      FirstComparer.Equals(First, other.First) && SecondComparer.Equals(Second, other.Second);

    /// <inheritdoc/>
    public int CompareTo(Pair<TFirst, TSecond> other)
    {
      int result = FirstComparer.Compare(First, other.First);
      return result != 0
        ? result
        : SecondComparer.Compare(Second, other.Second);
    }

    #endregion

    #region Equals, GetHashCode, ==, !=

    /// <inheritdoc/>
    public override bool Equals(object obj) =>
      obj is Pair<TFirst, TSecond> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(First, Second);

    /// <summary>
    /// Checks specified objects for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Pair<TFirst, TSecond> left, Pair<TFirst, TSecond> right)
    {
      return left.Equals(right);
    }

    /// <summary>
    /// Checks specified objects for inequality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Pair<TFirst, TSecond> left, Pair<TFirst, TSecond> right)
    {
      return !left.Equals(right);
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
      return string.Format(Strings.PairFormat, First, Second);
    }


    // Constructors

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="first">A first value in pair.</param>
    /// <param name="second">A second value in pair.</param>
    public Pair(TFirst first, TSecond second)
    {
      First = first;
      Second = second;
    }
  }
}
