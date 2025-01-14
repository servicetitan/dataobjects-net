// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.06.07

using System;
using Xtensive.Reflection;
using Xtensive.Tuples;

namespace Xtensive.Orm.Internals.FieldAccessors
{
  internal sealed class EnumFieldAccessor<T> : FieldAccessor<T>
  {
    private static readonly Type type = typeof(T);
    private static readonly object @default =
      type.IsEnum
        ? (type.IsNullable() ? null : Enum.GetValues(type).GetValue(0))
        : default(T);

    /// <inheritdoc/>
    public override bool AreSameValues(object oldValue, object newValue)
    {
      return Equals(oldValue, newValue);
    }

    /// <inheritdoc/>
    public override T GetValue(Persistent obj)
    {
      var value = obj.Tuple.GetValue(FieldIndex, out var state);
      return (T) (!state.HasValue() ? @default
        : type.IsEnum ? Enum.ToObject(type, value)
        : Enum.ToObject(Nullable.GetUnderlyingType(type), value));
    }

    /// <inheritdoc/>
    public override void SetValue(Persistent obj, T value)
    {
      // Biconverter<object, T> converter = GetConverter(field.ValueType);
      obj.Tuple.SetValue(FieldIndex, value==null ? null : Convert.ChangeType(value, type));
    }
  }
}
