// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexander Nikolaev
// Created:    2009.07.13

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Xtensive.Core;
using Xtensive.Orm.Model;
using ComparerProvider = Xtensive.Comparison.ComparerProvider;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals
{
  [Serializable]
  internal sealed class Key<T1, T2, T3, T4> : Key
  {
    private static readonly Predicate<T1, T1> EqualityComparer1 =
      ComparerProvider.Default.GetComparer<T1>().Equals;
    private static readonly Predicate<T2, T2> EqualityComparer2 =
      ComparerProvider.Default.GetComparer<T2>().Equals;
    private static readonly Predicate<T3, T3> EqualityComparer3 =
      ComparerProvider.Default.GetComparer<T3>().Equals;
    private static readonly Predicate<T4, T4> EqualityComparer4 =
      ComparerProvider.Default.GetComparer<T4>().Equals;

    private readonly T1 value1;
    private readonly T2 value2;
    private readonly T3 value3;
    private readonly T4 value4;

    protected override Tuple GetValue()
    {
      var result = CreateTuple();
      result.SetValue(0, value1);
      result.SetValue(1, value2);
      result.SetValue(2, value3);
      result.SetValue(3, value4);
      return result;
    }

    protected override bool ValueEquals(Key other) =>
      other is Key<T1, T2, T3, T4> otherKey
        && EqualityComparer4(value4, otherKey.value4)
        && EqualityComparer3(value3, otherKey.value3)
        && EqualityComparer2(value2, otherKey.value2)
        && EqualityComparer1(value1, otherKey.value1);

    protected override int CalculateHashCode() => HashCode.Combine(value1, value2, value3, value4);

    [UsedImplicitly]
    public static Key Create(string nodeId, TypeInfo type, Tuple tuple, TypeReferenceAccuracy accuracy, IReadOnlyList<ColNum> keyIndexes)
    {
      return new Key<T1, T2, T3, T4>(nodeId, type, accuracy,
        tuple.GetValueOrDefault<T1>(keyIndexes[0]),
        tuple.GetValueOrDefault<T2>(keyIndexes[1]),
        tuple.GetValueOrDefault<T3>(keyIndexes[2]),
        tuple.GetValueOrDefault<T4>(keyIndexes[3]));
    }

    [UsedImplicitly]
    public static Key Create(string nodeId, TypeInfo type, Tuple tuple, TypeReferenceAccuracy accuracy)
    {
      return new Key<T1, T2, T3, T4>(nodeId, type, accuracy,
        tuple.GetValueOrDefault<T1>(0),
        tuple.GetValueOrDefault<T2>(1),
        tuple.GetValueOrDefault<T3>(2),
        tuple.GetValueOrDefault<T4>(3));
    }


    // Constructors

    private Key(string nodeId, TypeInfo type, TypeReferenceAccuracy accuracy, T1 value1, T2 value2, T3 value3, T4 value4)
      : base(nodeId, type, accuracy, null)
    {
      this.value1 = value1;
      this.value2 = value2;
      this.value3 = value3;
      this.value4 = value4;
    }
  }
}