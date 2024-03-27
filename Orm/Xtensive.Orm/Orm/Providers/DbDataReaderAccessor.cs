// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.30

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Xtensive.Tuples;
using Xtensive.Sql;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Provider-level <see cref="DbDataReader"/> accessor.
  /// </summary>
  public readonly struct DbDataReaderAccessor
  {
    private readonly TypeMapping[] mappings;

    public TupleDescriptor Descriptor { get; }

    public Tuple Read(DbDataReader source)
    {
      var target = Tuple.Create(Descriptor);
      for (int i = 0, n = mappings.Length; i < n; i++) {
        var value = !source.IsDBNull(i)
          ? mappings[i].ReadValue(source, i)
          : null;
        target.SetValue(i, value);
      }
      return target;
    }

    // Constructors

    internal DbDataReaderAccessor(in TupleDescriptor descriptor, IEnumerable<TypeMapping> mappings)
    {
      Descriptor = descriptor;
      this.mappings = mappings.ToArray();
    }
  }
}