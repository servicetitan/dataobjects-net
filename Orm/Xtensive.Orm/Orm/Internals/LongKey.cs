// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.10.20

using System;
using Xtensive.Orm.Model;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals
{
  [Serializable]
  internal sealed class LongKey(string nodeId, TypeInfo type, TypeReferenceAccuracy accuracy, Tuple value) : Key(nodeId, type, accuracy, value)
  {
    /// <inheritdoc/>
    protected override Tuple GetValue() => value;

    /// <inheritdoc/>
    protected override int CalculateHashCode() => value.GetHashCode();

    /// <inheritdoc/>
    protected override bool ValueEquals(Key other) =>
      other is LongKey otherKey && value.Equals(otherKey.value);
  }
}