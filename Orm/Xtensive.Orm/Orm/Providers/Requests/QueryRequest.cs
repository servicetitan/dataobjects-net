// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.08.22

using Xtensive.Orm.Configuration;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Dml;
using Xtensive.Tuples;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Query (SELECT) request.
  /// </summary>
  public sealed class QueryRequest : IQueryRequest
  {
    private static readonly IReadOnlySet<QueryParameterBinding> EmptyBindings = new HashSet<QueryParameterBinding>();

    private readonly StorageDriver driver;

    private DbDataReaderAccessor? accessor;
    private SqlCompilationResult compiledStatement;

    public SqlSelect Statement { get; private set; }
    public IReadOnlyCollection<QueryParameterBinding> ParameterBindings { get; }

    public TupleDescriptor TupleDescriptor { get; }
    public QueryRequestOptions Options { get; }

    public NodeConfiguration NodeConfiguration { get; private set; }

    public bool CheckOptions(QueryRequestOptions requiredOptions)
    {
      return (Options & requiredOptions)==requiredOptions;
    }

    public void Prepare()
    {
      if (compiledStatement!=null && accessor!=null)
        return;
      compiledStatement = driver.Compile(Statement);
      accessor = driver.GetDataReaderAccessor(TupleDescriptor);
      Statement = null;
    }

    public SqlCompilationResult GetCompiledStatement()
    {
      if (compiledStatement==null)
        throw new InvalidOperationException(Strings.ExRequestIsNotPrepared);
      return compiledStatement;
    }

    public DbDataReaderAccessor GetAccessor() =>
      accessor ?? throw new InvalidOperationException(Strings.ExRequestIsNotPrepared);

    // Constructors

    public QueryRequest(
      StorageDriver driver, SqlSelect statement, IReadOnlySet<QueryParameterBinding> parameterBindings,
      TupleDescriptor tupleDescriptor, QueryRequestOptions options)
    {
      ArgumentNullException.ThrowIfNull(driver);
      ArgumentNullException.ThrowIfNull(statement);
      ArgumentNullException.ThrowIfNull(tupleDescriptor);

      this.driver = driver;
      Statement = statement;
      ParameterBindings = parameterBindings ?? EmptyBindings;
      TupleDescriptor = tupleDescriptor;
      Options = options;
    }

    public QueryRequest(
      StorageDriver driver, SqlSelect statement, IReadOnlySet<QueryParameterBinding> parameterBindings,
      TupleDescriptor tupleDescriptor, QueryRequestOptions options, NodeConfiguration nodeConfiguration)
    {
      ArgumentNullException.ThrowIfNull(driver);
      ArgumentNullException.ThrowIfNull(statement);
      ArgumentNullException.ThrowIfNull(tupleDescriptor);

      this.driver = driver;
      Statement = statement;
      ParameterBindings = parameterBindings ?? EmptyBindings;
      TupleDescriptor = tupleDescriptor;
      Options = options;
      NodeConfiguration = nodeConfiguration;
    }
  }
}
