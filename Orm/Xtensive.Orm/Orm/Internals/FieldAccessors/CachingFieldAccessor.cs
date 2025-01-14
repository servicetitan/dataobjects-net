// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.07.08

using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals.FieldAccessors;

internal abstract class CachingFieldAccessor<T> : FieldAccessor<T>
{
  public static Func<Persistent, FieldInfo, IFieldValueAdapter> Constructor;

  protected FieldInfo Field;

  public override void SetFieldInfo(FieldInfo value) {
    Field = Field is null ? value : throw Exceptions.AlreadyInitialized("Field");
    base.SetFieldInfo(value);
  }

  public override T GetValue(Persistent obj) => (T) obj.GetFieldValueAdapter(Field, Constructor);
}
