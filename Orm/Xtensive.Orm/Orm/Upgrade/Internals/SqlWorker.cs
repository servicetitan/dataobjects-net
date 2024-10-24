// Copyright (C) 2012-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.03.15

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Orm.Providers;
using Xtensive.Sql;
using Xtensive.Sql.Model;

namespace Xtensive.Orm.Upgrade
{
  internal static class SqlWorker
  {
    public static Func<SqlWorkerResult> Create(UpgradeServiceAccessor services, SqlWorkerTask task)
    {
      return () => Run(services, task);
    }

    private static SqlWorkerResult Run(UpgradeServiceAccessor services, SqlWorkerTask task)
    {
      var result = new SqlWorkerResult();
      var executor = new SqlExecutor(services.StorageDriver, services.Connection);
      if ((task & SqlWorkerTask.DropSchema) > 0) {
        DropSchema(services, executor);
      }
      if ((task & SqlWorkerTask.ExtractSchema) > 0) {
        result.Schema = ExtractSchema(services, executor);
      }
      if ((task & (SqlWorkerTask.ExtractMetadataTypes | SqlWorkerTask.ExtractMetadataAssemblies | SqlWorkerTask.ExtractMetadataExtension)) > 0) {
        ExtractMetadata(services, executor, result, task);
      }
      services.StorageDriver.CreateTypesIfNotExistAsync(executor).Wait();

      return result;
    }

    private static void ExtractMetadata(UpgradeServiceAccessor services, SqlExecutor executor, SqlWorkerResult result, SqlWorkerTask task)
    {
      var set = new MetadataSet();
      var mapping = new MetadataMapping(services.StorageDriver, services.NameBuilder);
      var metadataExtractor = new MetadataExtractor(mapping, executor);
      var metadataTasks = services.MappingResolver.GetMetadataTasks()
        .Where(metadataTask => !ShouldSkipMetadataExtraction(mapping, result, metadataTask));
      foreach (var metadataTask in metadataTasks) {
        try {
          if (task.HasFlag(SqlWorkerTask.ExtractMetadataAssemblies)) {
            metadataExtractor.ExtractAssemblies(set, metadataTask);
          }
          if (task.HasFlag(SqlWorkerTask.ExtractMetadataTypes)) {
            metadataExtractor.ExtractTypes(set, metadataTask);
          }
          if (task.HasFlag(SqlWorkerTask.ExtractMetadataExtension)) {
            metadataExtractor.ExtractExtensions(set, metadataTask);
          }
        }
        catch (Exception exception) {
          UpgradeLog.Warning(nameof(Strings.LogFailedToExtractMetadataFromXYZ), metadataTask.Catalog, metadataTask.Schema, exception);
        }
      }
      result.Metadata = set;
    }

    private static bool ShouldSkipMetadataExtraction(MetadataMapping mapping, SqlWorkerResult result, SqlExtractionTask task)
    {
      if (result.Schema == null) {
        return false;
      }

      var tables = GetSchemaTables(result, task);
      return tables[mapping.Assembly] == null && tables[mapping.Type] == null && tables[mapping.Extension] == null;
    }

    private static PairedNodeCollection<Schema, Table> GetSchemaTables(SqlWorkerResult result, SqlExtractionTask task)
    {
      var catalog = GetCatalog(result, task.Catalog);
      var schema = GetSchema(catalog, task.Schema);
      return schema.Tables;
    }

    private static Catalog GetCatalog(SqlWorkerResult result, string catalogName)
    {
      if (catalogName.IsNullOrEmpty())
        return result.Schema.Catalogs.Single(c => c.Name==catalogName);
      return result.Schema.Catalogs[catalogName];
    }

    private static Schema GetSchema(Catalog catalog, string schemaName)
    {
      return schemaName.IsNullOrEmpty()
        ? catalog.Schemas.Single(s => s.Name == schemaName)
        : catalog.Schemas[schemaName];
    }

    private static SchemaExtractionResult ExtractSchema(UpgradeServiceAccessor services, ISqlExecutor executor)
    {
      var schema = new SchemaExtractionResult(executor.Extract(services.MappingResolver.GetSchemaTasks()));
      return new IgnoreRulesHandler(schema, services.Configuration, services.MappingResolver).Handle();
    }

    private static void DropSchema(UpgradeServiceAccessor services, ISqlExecutor executor)
    {
      var driver = services.StorageDriver;
      var extractionResult = ExtractSchema(services, executor);
      var schemas = extractionResult.Catalogs.SelectMany(c => c.Schemas).ToList();
      var tables = schemas.SelectMany(s => s.Tables).ToList();
      var sequences = schemas.SelectMany(s => s.Sequences);

      DropForeignKeys(driver, tables, executor);
      DropTables(driver, tables, executor);
      DropSequences(driver, sequences, executor);
    }

    private static void DropSequences(StorageDriver driver, IEnumerable<Sequence> sequences, ISqlExecutor executor)
    {
      var statements = BreakEvery(sequences
        .Select(s => driver.Compile(SqlDdl.Drop(s)).GetCommandText()), 25).ToList();
      executor.ExecuteMany(statements);
    }

    private static void DropTables(StorageDriver driver, IEnumerable<Table> tables, ISqlExecutor executor)
    {
      var statements = BreakEvery(tables
        .Select(t => driver.Compile(SqlDdl.Drop(t)).GetCommandText()), 25);
      executor.ExecuteMany(statements);
    }

    private static void DropForeignKeys(StorageDriver driver, IEnumerable<Table> tables, ISqlExecutor executor)
    {
      var statements = BreakEvery(tables
        .SelectMany(t => t.TableConstraints.OfType<ForeignKey>())
        .Select(fk => driver.Compile(SqlDdl.Alter(fk.Table, SqlDdl.DropConstraint(fk))).GetCommandText()), 25);
      executor.ExecuteMany(statements);
    }

    private static IEnumerable<TItem> BreakEvery<TItem>(IEnumerable<TItem> source, int breakOnCount)
      where TItem : class
    {
      var count = 0;
      foreach(var item in source) {
        count++;
        if (count == breakOnCount) {
          yield return default(TItem);
          count = 0;
        }
        yield return item;
      }
    }
  }
}
