// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.04

using System;
using Xtensive.Core;
using Xtensive.Orm.Rse.Compilation;
using Xtensive.Orm.Rse.Providers;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// <see cref="CompilableProvider"/> compilation service.
  /// </summary>
  public readonly struct CompilationService
  {
    private readonly Func<CompilerConfiguration, ICompiler> compilerProvider;
    private readonly Func<CompilerConfiguration, IPreCompiler> preCompilerProvider;
    private readonly Func<CompilerConfiguration, ICompiler, IPostCompiler> postCompilerProvider;

    public CompilerConfiguration CreateConfiguration(Session session) =>
      new(
        PrepareRequest: true,
        StorageNode: session.StorageNode,
        Tags: session.Tags,
        // prefer constants during upgrade process
        PreferTypeIdAsParameter: !session.Name.Equals(WellKnown.Sessions.System) && session.Domain.Configuration.PreferTypeIdsAsQueryParameters
      );

    public ExecutableProvider Compile(CompilableProvider provider, CompilerConfiguration configuration)
    {
      ArgumentNullException.ThrowIfNull(provider);
      ArgumentNullException.ThrowIfNull(configuration);

      var preCompiler = preCompilerProvider.Invoke(configuration);
      var compiler = compilerProvider.Invoke(configuration);
      var postCompiler = postCompilerProvider.Invoke(configuration, compiler);

      if (compiler==null)
        throw new InvalidOperationException(Strings.ExCanNotCompileNoCompiler);

      var preprocessed = preCompiler.Process(provider);
      var compiled = compiler.Compile(preprocessed);
      compiled = postCompiler.Process(compiled);

      if (compiled==null)
        throw new InvalidOperationException(string.Format(Strings.ExCantCompileProviderX, provider));

      return compiled;
    }

    // Constructors

    public CompilationService(
      Func<CompilerConfiguration, ICompiler> compilerProvider,
      Func<CompilerConfiguration, IPreCompiler> preCompilerProvider,
      Func<CompilerConfiguration, ICompiler, IPostCompiler> postCompilerProvider)
    {
      ArgumentNullException.ThrowIfNull(compilerProvider);
      ArgumentNullException.ThrowIfNull(preCompilerProvider);
      ArgumentNullException.ThrowIfNull(postCompilerProvider);

      this.compilerProvider = compilerProvider;
      this.preCompilerProvider = preCompilerProvider;
      this.postCompilerProvider = postCompilerProvider;
    }
  }
}
