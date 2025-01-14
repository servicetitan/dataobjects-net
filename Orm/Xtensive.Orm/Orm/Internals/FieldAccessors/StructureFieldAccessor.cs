// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.05.30

using Xtensive.Tuples;

namespace Xtensive.Orm.Internals.FieldAccessors
{
  internal sealed class StructureFieldAccessor<T> : CachingFieldAccessor<T>
  {
    /// <inheritdoc/>
    public override bool AreSameValues(object oldValue, object newValue) => oldValue.Equals(newValue);

    /// <inheritdoc/>
    public override void SetValue(Persistent obj, T value)
    {
      ArgumentNullException.ThrowIfNull(value);
      var valueType = value.GetType();
      if (Field.ValueType != valueType)
        throw new InvalidOperationException(String.Format(
          Strings.ExResultTypeIncorrect, valueType.Name, Field.ValueType.Name));

      var structure = (Structure) (object) value;
      var adapter = (IFieldValueAdapter)value;
      adapter.Owner?.EnsureIsFetched(adapter.Field);
      structure.Tuple.CopyTo(obj.Tuple, 0, FieldIndex, Field.MappingInfo.Length);
    }

    // Type initializer

    static StructureFieldAccessor()
    {
       Constructor = (obj, field) => Activator.CreateStructure(field.ValueType, obj, field);
    }
  }
}
