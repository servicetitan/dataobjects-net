// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.01.24

using System;
using System.Data.Common;
using System.Runtime.Serialization;
using Xtensive.Sql;

namespace Xtensive.Tuples
{
  /// <summary>
  /// Base class for any regular tuple.
  /// </summary>
  [DataContract]
  [Serializable]
  public abstract class RegularTuple : Tuple
  {
    public abstract void SetValueFromDataReader(in MapperReader mr);

    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    protected RegularTuple()
    {
    }
  }
}