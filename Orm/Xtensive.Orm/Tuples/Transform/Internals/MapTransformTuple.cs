// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.05.07

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xtensive.Core;


namespace Xtensive.Tuples.Transform.Internals
{
  /// <summary>
  /// A <see cref="MapTransform"/> result tuple mapping arbitrary count of source tuples to a single one (this).
  /// </summary>
  [Serializable]
  public sealed class MapTransformTuple : TransformedTuple<MapTransform>
  {
    private readonly Tuple[] tuples;

    /// <inheritdoc/>
    public override IReadOnlyList<object> Arguments
    {
      [DebuggerStepThrough]
      get => tuples.AsSafeWrapper();
    }

    #region GetFieldState, GetValue, SetValue methods

    /// <inheritdoc/>
    public override TupleFieldState GetFieldState(int fieldIndex)
    {
      var indexes = TypedTransform.map[fieldIndex];
      return tuples[indexes.Item1].GetFieldState(indexes.Item2);
    }

    protected internal override void SetFieldState(int fieldIndex, TupleFieldState fieldState)
    {
      var indexes = TypedTransform.map[fieldIndex];
      tuples[indexes.Item1].SetFieldState(indexes.Item2, fieldState);
    }

    /// <inheritdoc/>
    public override object GetValue(int fieldIndex, out TupleFieldState fieldState)
    {
      var indexes = TypedTransform.map[fieldIndex];
      return tuples[indexes.Item1].GetValue(indexes.Item2, out fieldState);
    }

    /// <inheritdoc/>
    public override void SetValue(int fieldIndex, object fieldValue)
    {
      if (Transform.IsReadOnly)
        throw Exceptions.ObjectIsReadOnly(null);
      var indexes = TypedTransform.map[fieldIndex];
      tuples[indexes.Item1].SetValue(indexes.Item2, fieldValue);
    }

    #endregion

    protected internal override (Tuple, int) GetMappedContainer(int fieldIndex, bool isWriting)
    {
      if (isWriting && Transform.IsReadOnly)
        throw Exceptions.ObjectIsReadOnly(null);
      var map = TypedTransform.map[fieldIndex];
      return tuples[map.Item1].GetMappedContainer(map.Item2, isWriting);
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <param name="sources">Source tuples.</param>
    public MapTransformTuple(MapTransform transform, params Tuple[] sources)
      : base(transform)
    {
      ArgumentNullException.ThrowIfNull(sources, "tuples");
      // Other checks are omitted: this transform should be fast, so delayed errors are ok
      this.tuples = sources ?? throw new ArgumentNullException(nameof(sources));
    }
  }
}
