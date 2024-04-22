// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2008.09.09

using System;
using System.Linq.Expressions;
using Xtensive.Core;

using Xtensive.Reflection;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse
{
  /// <summary>
  /// Descriptor of the calculated column.
  /// </summary>
  [Serializable]
  public readonly record struct CalculatedColumnDescriptor
  (
    string Name,
    Type Type,
    Expression<Func<Tuple, object>> Expression
  )
  {
    public override string ToString() => $"{Type.GetShortName()} {Name} = {Expression.ToString(true)}";
  }
}
