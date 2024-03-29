// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.09.17

using Xtensive.Tuples.Packed;

namespace Xtensive.Tuples
{
  /// <summary>
  /// Incapsulates <see cref="Tuple.GetValue{T}(int, out Xtensive.Tuples.TupleFieldState)"/> method.
  /// </summary>
  /// <typeparam name="TValue">Type of a value.</typeparam>
  /// <param name="tuple">Tuple to use.</param>
  /// <param name="descriptor">Field descriptor.</param>
  /// <param name="fieldState">State of a field.</param>
  /// <returns></returns>
  internal delegate TValue GetValueDelegate<TValue>(PackedTuple tuple, PackedFieldDescriptor descriptor, out TupleFieldState fieldState);

  /// <summary>
  /// Incapsulates <see cref="Tuple.SetValue{T}"/> method.
  /// </summary>
  /// <typeparam name="TValue">Type of a value.</typeparam>
  /// <param name="tuple">Tuple to use.</param>
  /// <param name="descriptor">Field descriptor.</param>
  /// <param name="value">A value.</param>
  internal delegate void SetValueDelegate<TValue>(PackedTuple tuple, PackedFieldDescriptor descriptor, TValue value);
}