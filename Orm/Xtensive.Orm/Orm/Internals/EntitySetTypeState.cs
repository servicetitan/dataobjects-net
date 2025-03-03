// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.08.04

using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Tuples.Transform;

namespace Xtensive.Orm.Internals;

[Serializable]
internal record EntitySetTypeState(
  ExecutableProvider SeekProvider,
  MapTransform SeekTransform,
  Func<Tuple, Entity> ItemCtor,
  Func<QueryEndpoint, long> ItemCountQuery
);
