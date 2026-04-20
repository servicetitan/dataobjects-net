// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.20

using Xtensive.Orm.Rse.Compilation;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Sql.Dml;

namespace Xtensive.Orm.Providers
{
  public class SqlSelectCorrector : IPostCompiler
  {
    private readonly ProviderInfo providerInfo;

    public ExecutableProvider Process(ExecutableProvider rootProvider)
    {
      if (rootProvider is SqlProvider sqlProvider) {
        Process(sqlProvider.Request.Statement);
      }
      return rootProvider;
    }

    /// <summary>
    /// Runs the post-compilation pipeline on a bare <see cref="SqlSelect"/>
    /// statement. This is the same pipeline applied to production
    /// <see cref="SqlProvider"/>s by the <see cref="IPostCompiler"/> entry point
    /// above, factored out so that callers (notably unit tests) can drive the
    /// corrector without constructing an entire <see cref="ExecutableProvider"/>
    /// graph. Internal mechanics (column pruning, comment hoisting, paging
    /// fixups, etc.) are intentionally encapsulated here so tests do not pin
    /// the choice of inner class.
    /// </summary>
    internal void Process(SqlSelect statement) =>
      SqlSelectProcessor.Process(statement, providerInfo);

    // Constructors
    
    public SqlSelectCorrector(ProviderInfo providerInfo)
    {
      this.providerInfo = providerInfo;
    }
  }
}