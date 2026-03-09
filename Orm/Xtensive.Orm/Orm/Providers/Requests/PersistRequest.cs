// Copyright (C) 2003-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2008.08.22

using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Sql;
using Xtensive.Sql.Compiler;

namespace Xtensive.Orm.Providers;

public readonly record struct PreparedPersistRequest(
  SqlCompilationResult CompiledStatement,
  IReadOnlyCollection<PersistParameterBinding> ParameterBindings
);

/// <summary>
/// Modification (INSERT, UPDATE, DELETE) request.
/// </summary>
public readonly struct PersistRequest
{
  private static readonly IReadOnlySet<PersistParameterBinding> EmptyBindings = new HashSet<PersistParameterBinding>();

  private readonly StorageDriver driver;

  public SqlStatement Statement { get; }

  public ISqlCompileUnit CompileUnit { get; }

  public IReadOnlyCollection<PersistParameterBinding> ParameterBindings { get; }

  public PreparedPersistRequest Prepare() => new(driver.Compile(CompileUnit), ParameterBindings);

  // Constructors

  public PersistRequest(
    StorageDriver driver, SqlStatement statement, IReadOnlySet<PersistParameterBinding> parameterBindings)
  {
    ArgumentNullException.ThrowIfNull(driver);
    ArgumentNullException.ThrowIfNull(statement);

    var compileUnit = statement as ISqlCompileUnit
      ?? throw new ArgumentException("Statement is not ISqlCompileUnit");

    this.driver = driver;
    Statement = statement;
    CompileUnit = compileUnit;
    ParameterBindings = parameterBindings ?? EmptyBindings;
  }
}
