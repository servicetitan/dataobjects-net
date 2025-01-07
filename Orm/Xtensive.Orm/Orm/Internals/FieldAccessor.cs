// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2010.02.19

using System;
using System.Diagnostics;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals
{
  internal abstract class FieldAccessor
  {
    private FieldInfo fld;

    public FieldInfo Field {
      get { return fld; }
      set {
        if (fld !=null)
          throw Exceptions.AlreadyInitialized("Field");
        fld = value;
      }
    }

    public object DefaultUntypedValue { get; private set; }

    public abstract bool AreSameValues(object oldValue, object newValue);

    public abstract void SetUntypedValue(Persistent obj, object value);

    public abstract object GetUntypedValue(Persistent obj);


    // Constructors
    
    protected FieldAccessor(object defaultUntypedValue)
    {
      DefaultUntypedValue = defaultUntypedValue;
    }
  }
}
