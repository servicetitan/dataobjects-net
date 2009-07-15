// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.08.29

using System.Collections.Generic;
using System.Linq;
using Xtensive.Core.Collections;
using Xtensive.Sql.Dml;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Providers.Sql
{
  public sealed class SqlRequestBuilderContext
  {
    public SqlBatch Batch { get; private set; }

    public SqlRequestBuilderTask Task { get; private set; }

    public TypeInfo Type { get; private set; }

    public ReadOnlyList<IndexInfo> AffectedIndexes { get; private set;}

    public IndexInfo PrimaryIndex { get; private set; }

    public Dictionary<ColumnInfo, SqlPersistParameterBinding> ParameterBindings { get; private set; }


    // Constructors

    public SqlRequestBuilderContext(SqlRequestBuilderTask task, SqlBatch batch)
    {
      Task = task;
      Batch = batch;
      Type = task.Type;
      var affectedIndexes = Type.AffectedIndexes.Where(index => index.IsPrimary).ToList();
      affectedIndexes.Sort((left, right)=>{
          if (left.ReflectedType.GetAncestors().Contains(right.ReflectedType))
            return 1;
          if (right.ReflectedType.GetAncestors().Contains(left.ReflectedType))
            return -1;
          return 0;});
      AffectedIndexes = new ReadOnlyList<IndexInfo>(affectedIndexes);
      PrimaryIndex = Task.Type.Indexes.PrimaryIndex;
      ParameterBindings = new Dictionary<ColumnInfo, SqlPersistParameterBinding>();
    }
  }
}