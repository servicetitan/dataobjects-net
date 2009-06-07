// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.04.09

using System;
using System.Collections.Generic;
using System.Data.Common;
using Xtensive.Core.Collections;
using Xtensive.Modelling.Actions;
using Xtensive.Sql.Common;
using Xtensive.Sql.Dom;
using Xtensive.Sql.Dom.Database;
using Xtensive.Storage.Building;
using Xtensive.Storage.Indexing.Model;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers.Sql;
using TypeInfo=Xtensive.Storage.Indexing.Model.TypeInfo;

namespace Xtensive.Storage.Providers.PgSql
{
  /// <summary>
  /// Upgrades storage schema.
  /// </summary>
  public class SchemaUpgradeHandler : Sql.SchemaUpgradeHandler
  {
    private SqlConnection Connection
    {
      get { return ((SessionHandler) Handlers.SessionHandler).Connection; }
    }

    private DbTransaction Transaction
    {
      get { return ((SessionHandler) Handlers.SessionHandler).Transaction; }
    }
    
    /// <inheritdoc/>
    public override StorageInfo GetExtractedSchema()
    {
      var schema = ExtractStorageSchema();
      var sessionHandeler = (SessionHandler) BuildingContext.Demand().SystemSessionHandler;
      var converter = new PgSqlModelConverter(schema, sessionHandeler.ExecuteScalar, ConvertType);
      return converter.GetConversionResult();
    }

    /// <inheritdoc/>
    public override void UpgradeSchema(ActionSequence upgradeActions, StorageInfo sourceSchema, StorageInfo targetSchema)
    {
      var upgradeScript = GenerateUpgradeScript(upgradeActions, sourceSchema, targetSchema);
      foreach (var batch in upgradeScript) {
        if (string.IsNullOrEmpty(batch))
          continue;
        using (var command = new SqlCommand(Connection)) {
          command.CommandText = batch;
          command.Prepare();
          command.Transaction = Transaction;
          command.ExecuteNonQuery();
        }
      }
    }

    private List<string> GenerateUpgradeScript(ActionSequence actions, StorageInfo sourceSchema, StorageInfo targetSchema)
    {
      var valueTypeMapper = ((DomainHandler) Handlers.DomainHandler).ValueTypeMapper;
      var translator = new SqlActionTranslator(
        actions,
        ExtractStorageSchema(),
        Connection.Driver,
        valueTypeMapper,
        sourceSchema, targetSchema, false);

      var delimiter = Connection.Driver.Translator.BatchStatementDelimiter;
      var batch = new List<string>();
      batch.Add(string.Join(delimiter, translator.PreUpgradeCommands.ToArray()));
      batch.Add(string.Join(delimiter, translator.UpgradeCommands.ToArray()));
      batch.Add(string.Join(delimiter, translator.DataManipulateCommands.ToArray()));
      batch.Add(string.Join(delimiter, translator.PostUpgradeCommands.ToArray()));

      WriteToLog(delimiter, translator);

      return batch;
    }

    private void WriteToLog(string delimiter, SqlActionTranslator translator)
    {
      var logDelimiter = delimiter + Environment.NewLine;
      var logBatch = new List<string>();
      translator.PreUpgradeCommands.Apply(logBatch.Add);
      translator.UpgradeCommands.Apply(logBatch.Add);
      translator.DataManipulateCommands.Apply(logBatch.Add);
      translator.PostUpgradeCommands.Apply(logBatch.Add);
      if (logBatch.Count > 0)
        Log.Info("Upgrade DDL: {0}", 
          Environment.NewLine + string.Join(logDelimiter, logBatch.ToArray()));
    }

    private static TypeInfo ConvertType(SqlValueType valueType)
    {
      var sessionHandeler = (SessionHandler) BuildingContext.Demand().SystemSessionHandler;
      var dataTypes = sessionHandeler.Connection.Driver.ServerInfo.DataTypes;
      var nativeType = sessionHandeler.Connection.Driver.Translator.Translate(valueType);

      var dataType = dataTypes[nativeType] ?? dataTypes[valueType.DataType];

      int? length = 0;
      var streamType = dataType as StreamDataTypeInfo;
      if (streamType!=null
        && (streamType.SqlType==SqlDataType.VarBinaryMax
          || streamType.SqlType==SqlDataType.VarCharMax
            || streamType.SqlType==SqlDataType.AnsiVarCharMax))
        length = null;
      else
        length = valueType.Size;

      var type = dataType!=null ? dataType.Type : typeof (object);
      return new TypeInfo(type, false, length);
    }
  }
}