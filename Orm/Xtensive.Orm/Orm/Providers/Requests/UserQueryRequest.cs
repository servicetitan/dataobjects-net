// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.25

using Xtensive.Sql.Compiler;

namespace Xtensive.Orm.Providers
{
  public sealed class UserQueryRequest : IQueryRequest
  {
    private readonly SqlCompilationResult compiledStatement;

    public SqlCompilationResult GetCompiledStatement()
    {
      return compiledStatement;
    }

    public IReadOnlyCollection<QueryParameterBinding> ParameterBindings { get; }

    // Constructors

    public UserQueryRequest(SqlCompilationResult compiledStatement, IReadOnlySet<QueryParameterBinding> parameterBindings)
    {
      ArgumentNullException.ThrowIfNull(compiledStatement);
      ArgumentNullException.ThrowIfNull(parameterBindings);

      this.compiledStatement = compiledStatement;
      ParameterBindings = parameterBindings;
    }
  }
}
