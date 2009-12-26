// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.06.23

using System;

namespace Xtensive.Storage.Linq.Expressions
{
  [Flags]
  internal enum MarkerType
  {
    None = 0x0,
    First = 0x1,
    Single = 0x2,
    Default = 0x4
  }
}