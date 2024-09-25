// Copyright (C) 2014 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2014.03.13

using System;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Configuration
{
  /// <summary>
  /// Storage node configuration.
  /// </summary>
  [Serializable]
  public class NodeConfiguration : LockableBase, ICloneable
  {
    private string nodeId;
    private string connectionInitializationSql;
    private ConnectionInfo connectionInfo;
    private DomainUpgradeMode upgradeMode = DomainUpgradeMode.Default;
    public TypeIdRegistry TypeIdRegistry { get; set; }

    /// <summary>
    /// Gets or sets node identifier.
    /// </summary>
    public string NodeId
    {
      get { return nodeId; }
      set
      {
        EnsureNotLocked();
        nodeId = value;
      }
    }

    /// <summary>
    /// Gets or sets <see cref="DomainUpgradeMode"/>.
    /// </summary>
    public DomainUpgradeMode UpgradeMode
    {
      get { return upgradeMode; }
      set
      {
        EnsureNotLocked();
        upgradeMode = value;
      }
    }

    /// <summary>
    /// Gets or sets connection information.
    /// </summary>
    public ConnectionInfo ConnectionInfo
    {
      get { return connectionInfo; }
      set
      {
        EnsureNotLocked();
        connectionInfo = value;
      }
    }

    /// <summary>
    /// Gets or sets connection initialization SQL code.
    /// </summary>
    public string ConnectionInitializationSql
    {
      get { return connectionInitializationSql; }
      set
      {
        EnsureNotLocked();
        connectionInitializationSql = value;
      }
    }

    /// <summary>
    /// Gets schema mapping.
    /// </summary>
    public NameMappingCollection SchemaMapping { get; init; }  = new();

    /// <summary>
    /// Gets database mapping.
    /// </summary>
    public NameMappingCollection DatabaseMapping { get; init; }  = new();

    public override void Lock(bool recursive)
    {
      base.Lock(recursive);

      SchemaMapping.Lock();
      DatabaseMapping.Lock();
    }

    /// <summary>
    /// Creates clone of this instance.
    /// </summary>
    /// <returns>Clone of this instance.</returns>
    public NodeConfiguration Clone() =>
      new() {
        nodeId = nodeId,
        connectionInfo = connectionInfo,
        connectionInitializationSql = connectionInitializationSql,
        DatabaseMapping = DatabaseMapping.Clone(),
        SchemaMapping = SchemaMapping.Clone(),
        TypeIdRegistry = TypeIdRegistry,
      };

    object ICloneable.Clone() => Clone();

    internal void Validate(DomainConfiguration configuration)
    {
      if (string.IsNullOrEmpty(nodeId))
        throw new InvalidOperationException(Strings.ExInvalidNodeIdentifier);

      if (SchemaMapping.Count > 0 && !configuration.IsMultischema)
        throw new InvalidOperationException(Strings.ExSchemaMappingRequiresMultischemaDomainConfiguration);

      if (DatabaseMapping.Count > 0 && !configuration.IsMultidatabase)
        throw new InvalidOperationException(Strings.ExDatabaseMappingRequiresMultidatabaseDomainConfiguration);
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    public NodeConfiguration()
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    public NodeConfiguration(string nodeId)
    {
      NodeId = nodeId;
    }
  }
}
