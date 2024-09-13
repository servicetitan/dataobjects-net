// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2011.03.25

using Xtensive.Orm.Rse.Compilation;

namespace Xtensive.Orm.Providers.Sqlite
{
  internal class SqlCompiler(HandlerAccessor handlers, in CompilerConfiguration configuration)
    : Providers.SqlCompiler(handlers, configuration);
}
