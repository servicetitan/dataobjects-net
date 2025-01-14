// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.06.02


namespace Xtensive.Orm.Internals;

internal abstract class FieldAccessor<T>() : FieldAccessor(default(T))
{
  public T DefaultValue { get; }

  public abstract void SetValue(Persistent obj, T value);
  public override void SetUntypedValue(Persistent obj, object value) => SetValue(obj, (T) value);

  public abstract T GetValue(Persistent obj);
  public override object GetUntypedValue(Persistent obj) => GetValue(obj);
}
