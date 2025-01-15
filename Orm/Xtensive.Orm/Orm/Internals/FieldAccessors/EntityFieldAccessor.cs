// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2008.05.26

using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Tuples;

namespace Xtensive.Orm.Internals.FieldAccessors
{
  internal class EntityFieldAccessor<T> : FieldAccessor<T>
  {
    private FieldInfo field;

    public override void SetFieldInfo(FieldInfo value)
    {
      field = field is null ? value : throw Exceptions.AlreadyInitialized("Field");
      base.SetFieldInfo(value);
    }

    /// <inheritdoc/>
    public override bool AreSameValues(object oldValue, object newValue) => ReferenceEquals(oldValue, newValue);
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Invalid arguments.</exception>
    public override void SetValue(Persistent obj, T value)
    {
      var tuple = obj.Tuple;
      if (value is Entity entity) {
        if (entity.Session != obj.Session)
          throw new InvalidOperationException(string.Format(Strings.ExEntityXIsBoundToAnotherSession, entity.Key));

        entity.Key.Value.CopyTo(tuple, 0, FieldIndex, field.MappingInfo.Length);
      }
      else {
        if (!ReferenceEquals(value, null))
          throw new InvalidOperationException(string.Format(Strings.ExValueShouldBeXDescendant, WellKnownOrmTypes.Entity));

        for (int i = FieldIndex, nextFieldIndex = FieldIndex + field.MappingInfo.Length; i < nextFieldIndex; i++)
          tuple.SetValue(i, null);
      }
    }

    /// <inheritdoc/>
    public override T GetValue(Persistent obj) =>
      obj.GetReferenceKey(field) is { } key
        ? (T) (object) obj.Session.Query.SingleOrDefault(key)
        : default;
  }
}
