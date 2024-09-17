// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.11.07

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using JetBrains.Annotations;
using Xtensive.Core;
using Xtensive.Orm;
using Xtensive.Orm.Model;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Compiler
{
  /// <summary>
  /// <see cref="PostCompiler"/> configuration.
  /// </summary>
  public sealed class SqlPostCompilerConfiguration
  {
    public HashSet<object> AlternativeBranches { get; } = new();

    public TypeIdRegistry TypeIdRegistry { get; }

    public Dictionary<object, string> PlaceholderValues { get; } = new();

    public Dictionary<object, IReadOnlyList<string[]>> DynamicFilterValues { get; } = new();

    /// <summary>
    /// Gets database mapping.
    /// </summary>
    public IReadOnlyDictionary<string, string> SchemaMapping { get; }

    /// <summary>
    /// Gets database mapping.
    /// </summary>
    public IReadOnlyDictionary<string, string> DatabaseMapping { get; }

    internal void AppendPlaceholderValue(StringBuilder sb, PlaceholderNode node)
    {
      switch (node.Id) {
        case TypeInfo typeInfo when TypeIdRegistry != null:
          if (TypeIdRegistry.GetTypeId(typeInfo) is var typeId && typeId != TypeInfo.NoTypeId) {
            sb.Append(typeId);      // We assume typeId > 0 here to avoid using .AppendFormat(CultureInfo.InvariantCulture) with boxing
            return;
          }
          if (typeInfo.IsInterface) {
            sb.Append(0);
            return;
          }
          break;
        case Schema schema:
          sb.Append(schema.GetActualDbName(SchemaMapping));
          return;
      }
      if (!PlaceholderValues.TryGetValue(node.Id, out var value)) {
        throw new InvalidOperationException(string.Format(Strings.ExValueForPlaceholderXIsNotSet, node.Id));
      }
      sb.Append(value);
    }

    // Constructors

    public SqlPostCompilerConfiguration(StorageNode storageNode = null)
    {
      // this prevents wrong query to be executed.
      SchemaMapping = null;
      DatabaseMapping = null;
    }

    public SqlPostCompilerConfiguration([NotNull] IReadOnlyDictionary<string, string> databaseMapping, [NotNull] IReadOnlyDictionary<string, string> schemaMapping)
    {
      ArgumentNullException.ThrowIfNull(databaseMapping);
      ArgumentNullException.ThrowIfNull(schemaMapping);

      DatabaseMapping = databaseMapping;
      SchemaMapping = schemaMapping;
    }
  }
}