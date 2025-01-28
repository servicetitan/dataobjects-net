// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.08.29

using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// <see cref="PersistRequestBuilder"/> context.
  /// </summary>
  public readonly struct PersistRequestBuilderContext
  {
    public PersistRequestBuilderTask Task { get; }

    public ModelMapping Mapping { get; }

    public NodeConfiguration NodeConfiguration { get; }

    public TypeInfo Type { get; }

    public IReadOnlyList<IndexInfo> AffectedIndexes { get; }

    public IndexInfo PrimaryIndex { get; }

    public Dictionary<ColumnInfo, PersistParameterBinding> ParameterBindings { get; }

    public Dictionary<ColumnInfo, PersistParameterBinding> VersionParameterBindings { get; }

    // Constructors

    public PersistRequestBuilderContext(PersistRequestBuilderTask task, ModelMapping mapping, NodeConfiguration nodeConfiguration)
    {
      Task = task;
      Type = task.Type;
      Mapping = mapping;
      NodeConfiguration = nodeConfiguration;

      var affectedIndexes = Type.AffectedIndexes.Where(index => index.IsPrimary).ToList();
      affectedIndexes.Sort((left, right) => {
        if (left.ReflectedType.Ancestors.Contains(right.ReflectedType))
          return 1;
        if (right.ReflectedType.Ancestors.Contains(left.ReflectedType))
          return -1;
        return 0;
      });
      AffectedIndexes = affectedIndexes.AsSafeWrapper();

      PrimaryIndex = Task.Type.Indexes.PrimaryIndex;
      ParameterBindings = new Dictionary<ColumnInfo, PersistParameterBinding>();

      if (task.ValidateVersion)
        VersionParameterBindings = new Dictionary<ColumnInfo, PersistParameterBinding>();
    }
  }
}
