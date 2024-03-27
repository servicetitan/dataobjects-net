// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.30

using System;
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
    private readonly Func<DbDataReader, int, object>[] readers;

    public TupleDescriptor Descriptor { get; }

    public Tuple Read(DbDataReader source)
    {
      var target = Tuple.Create(Descriptor);
      int i = 0;
      foreach (var reader in readers) {
        var value = source.IsDBNull(i) ? null : reader(source, i);
        target.SetValue(i, value);
        i++;
      }
      return target;
    }

    // Constructors

    internal DbDataReaderAccessor(in TupleDescriptor descriptor, IEnumerable<Func<DbDataReader, int, object>> readers)
    {
      Descriptor = descriptor;
      this.readers = readers.ToArray();
    }
  }
}