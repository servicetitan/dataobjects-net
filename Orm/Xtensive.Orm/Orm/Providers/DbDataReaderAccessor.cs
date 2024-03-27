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
  public sealed class DbDataReaderAccessor
  {
    private readonly TypeMapper mapper;
    private readonly Func<DbDataReader, int, object>[] readers;

    public TupleDescriptor Descriptor { get; private set; }

    public Tuple Read(DbDataReader source)
    {
      var target = Tuple.Create(Descriptor);
      for (int i = 0, n = Descriptor.Count; i < n; ++i) {
        target.SetValueFromDataReader(new(mapper, readers[i], source, i));
      }
      return target;
    }

    // Constructors

    internal DbDataReaderAccessor(in TupleDescriptor descriptor, TypeMapper mapper, Func<DbDataReader, int, object>[] readers)
    {
      Descriptor = descriptor;
      this.mapper = mapper;
      this.readers = readers;
    }
  }
}