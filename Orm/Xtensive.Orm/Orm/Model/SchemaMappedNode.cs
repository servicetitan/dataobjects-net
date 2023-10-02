// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.02.10

using System;
using Xtensive.Core;
using Xtensive.Orm.Building.Builders;

namespace Xtensive.Orm.Model
{
  /// <summary>
  /// A <see cref="Node"/> that is mapped to existing database schema node.
  /// </summary>
  [Serializable]
  public abstract class SchemaMappedNode : MappedNode
  {
    private SchemaMapping schemaMapping;

    /// <summary>
    /// Gets or sets database/schema this node is mapped to.
    /// </summary>
    public SchemaMapping SchemaMapping {
      get => schemaMapping;
      set {
        EnsureNotLocked();
        schemaMapping = value;
      }
    }

    /// <summary>
    /// Gets or sets database this node is mapped to.
    /// </summary>
    public string MappingDatabase => SchemaMapping.Database;

    /// <summary>
    /// Gets or sets schema this node is mapped to.
    /// </summary>
    public string MappingSchema => SchemaMapping.Schema;


    // Constructors

    /// <summary>
    /// Creates new instance of this class.
    /// </summary>
    protected SchemaMappedNode()
    {
    }

    /// <summary>
    /// Creates new instance of this class.
    /// </summary>
    /// <param name="name">Node name</param>
    protected SchemaMappedNode(string name)
      : base(name)
    {
    }
  }
}