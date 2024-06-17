// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.10.09

using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// A part of a command.
  /// </summary>
  public readonly record struct CommandPart(string Statement, List<DbParameter> Parameters, List<IDisposable> Resources);
}