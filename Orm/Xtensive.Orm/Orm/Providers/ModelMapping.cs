// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.23

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Sql.Model;

namespace Xtensive.Orm.Providers
{
  /// <summary>
  /// Mapping between <see cref="DomainModel"/>
  /// and <see cref="Catalog"/>s, <see cref="Schema"/>s and <see cref="Table"/>s.
  /// </summary>
  public sealed class ModelMapping : LockableBase
  {
    private Table[] tableMap = new Table[1];      // Indexed by TypeInfo.SharedId
    private readonly Dictionary<SequenceInfo, SchemaNode> sequenceMap = new();

    private string temporaryTableDatabase;
    private string temporaryTableSchema;
    private string temporaryTableCollation;

    public string TemporaryTableDatabase
    {
      get => temporaryTableDatabase;
      set {
        EnsureNotLocked();
        temporaryTableDatabase = value;
      }
    }

    public string TemporaryTableSchema
    {
      get => temporaryTableSchema;
      set {
        EnsureNotLocked();
        temporaryTableSchema = value;
      }
    }

    public string TemporaryTableCollation
    {
      get => temporaryTableCollation;
      set {
        EnsureNotLocked();
        temporaryTableCollation = value;
      }
    }

    public Table this[TypeInfo typeInfo] => typeInfo.SharedId < tableMap.Length ? tableMap[typeInfo.SharedId] : null;

    public SchemaNode this[SequenceInfo sequenceInfo] => sequenceMap.GetValueOrDefault(sequenceInfo);

    public void Register(TypeInfo typeInfo, Table table)
    {
      EnsureNotLocked();
      Array.Resize(ref tableMap, Math.Max(tableMap.Length, typeInfo.SharedId + 10));
      tableMap[typeInfo.SharedId] = table;
    }

    public void Register(SequenceInfo sequenceInfo, SchemaNode sequence)
    {
      EnsureNotLocked();
      sequenceMap[sequenceInfo] = sequence;
    }

    internal IEnumerable<SchemaNode> GetAllSchemaNodes() =>
      tableMap.Where(static o => o != null).Union(sequenceMap.Values);
  }
}
