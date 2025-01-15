// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2010.02.19

using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals;

internal abstract class FieldAccessor(object defaultUntypedValue)
{
  protected ColNum FieldIndex;

  public object DefaultUntypedValue { get; } = defaultUntypedValue;

  public virtual void SetFieldInfo(FieldInfo value)
  {
    FieldIndex = FieldIndex == 0 ? value.MappingInfo.Offset : throw Exceptions.AlreadyInitialized("Field");
  }

  public abstract bool AreSameValues(object oldValue, object newValue);
  public abstract void SetUntypedValue(Persistent obj, object value);
  public abstract object GetUntypedValue(Persistent obj);
}
