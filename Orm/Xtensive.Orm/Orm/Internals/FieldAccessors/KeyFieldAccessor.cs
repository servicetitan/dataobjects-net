// Copyright (C) 2008-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2008.11.21

using Xtensive.Tuples;

namespace Xtensive.Orm.Internals.FieldAccessors;

internal class KeyFieldAccessor<T> : FieldAccessor<T>
{
  /// <inheritdoc/>
  public override bool AreSameValues(object oldValue, object newValue) => Equals(oldValue, newValue);

  /// <inheritdoc/>
  public override T GetValue(Persistent obj)
  {
    var value = obj.Tuple.GetValue<string>(FieldIndex, out var state);
    return !state.IsAvailable()
      ? default
      : (T) (object) Key.Parse(obj.Session.Domain, value);
  }

  /// <inheritdoc/>
  public override void SetValue(Persistent obj, T value)
  {
    obj.Tuple.SetValue(FieldIndex, ((Key) (object) value)?.Format());
  }
}
