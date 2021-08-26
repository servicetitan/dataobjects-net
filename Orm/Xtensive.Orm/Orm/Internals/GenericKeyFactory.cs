// Copyright (C) 2003-2021 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.07.27

using System;
using System.Collections.Generic;
using Xtensive.Orm.Model;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals
{
  [Serializable]
  internal sealed class GenericKeyFactory
  {
    public readonly Type Type;
    public readonly Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, Key> DefaultConstructor;
    public readonly Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, IReadOnlyList<int>, Key> KeyIndexBasedConstructor;


    // Constructors

    public GenericKeyFactory(Type type,
      Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, Key> defaultConstructor,
      Func<string, TypeInfo, Tuple, TypeReferenceAccuracy, IReadOnlyList<int>, Key> keyIndexBasedConstructor)
    {
      Type = type;
      DefaultConstructor = defaultConstructor;
      KeyIndexBasedConstructor = keyIndexBasedConstructor;
    }
  }
}