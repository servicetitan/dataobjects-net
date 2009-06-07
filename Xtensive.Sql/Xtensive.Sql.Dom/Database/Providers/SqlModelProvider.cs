// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Data;
using System.Data.Common;
using Xtensive.Core;
using Xtensive.Sql.Dom.Database.Extractor;

namespace Xtensive.Sql.Dom.Database.Providers
{
  /// <summary>
  /// Represents a database model provider that builds database model from database.
  /// </summary>
  public class SqlModelProvider : IModelProvider
  {
    private SqlConnection connection;
    private DbTransaction transaction;

    /// <summary>
    /// Gets or sets the connection.
    /// </summary>
    /// <value>The connection.</value>
    public SqlConnection Connection
    {
      get { return connection; }
      set
      {
        ArgumentValidator.EnsureArgumentNotNull(value, "value");
        connection = value;
      }
    }

    #region IModelProvider Members

    /// <summary>
    /// Builds the database model.
    /// </summary>
    public Model Build()
    {
      SqlExtractor extractor = connection.Driver.Extractor;
      Model model = new Model();
      bool connectionIsClosed = connection.State==ConnectionState.Closed;
      bool transactionIsAbsent = connectionIsClosed | transaction==null;
      try {
        if (connectionIsClosed)
          connection.Open();
        if (transactionIsAbsent)
          transaction = connection.BeginTransaction();

        SqlExtractorContext context = extractor.CreateContext(model, connection, transaction);
        extractor.Initialize(context);
        extractor.ExtractServers(context, model);
        foreach (Server server in model.Servers) {
          extractor.ExtractUsers(context, server);
          extractor.ExtractCatalogs(context, server);
          foreach (Catalog catalog in server.Catalogs) {
            extractor.ExtractSchemas(context, catalog);
            foreach (Schema schema in catalog.Schemas) {
              extractor.ExtractAssertions(context, schema);
              extractor.ExtractCharacterSets(context, schema);
              extractor.ExtractCollations(context, schema);
              extractor.ExtractTranslations(context, schema);
              extractor.ExtractDomains(context, schema);
              extractor.ExtractSequences(context, schema);
              extractor.ExtractTables(context, schema);
              extractor.ExtractViews(context, schema);
              extractor.ExtractColumns(context, schema);
              foreach (var table in schema.Tables)
                extractor.ExtractDefaultConstraints(context, schema, table);
              extractor.ExtractUniqueConstraints(context, schema);
              extractor.ExtractIndexes(context, schema);
              extractor.ExtractStoredProcedures(context, schema);
            }
            extractor.ExtractForeignKeys(context, catalog);
          }
        }
      }
      finally {
        if (transactionIsAbsent && transaction!=null)
          transaction.Commit();
        if (connectionIsClosed && connection!=null)
          connection.Close();
      }
      return model;
    }

    /// <summary>
    /// Saves the specified model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <remarks>This method is not suppported by this type.</remarks>
    public void Save(Model model)
    {
      throw new NotSupportedException();
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlModelProvider"/> class.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="transaction">The transaction.</param>
    public SqlModelProvider(SqlConnection connection, DbTransaction transaction)
      : this(connection)
    {
      this.transaction = transaction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlModelProvider"/> class.
    /// </summary>
    /// <param name="connection">The connection.</param>
    public SqlModelProvider(SqlConnection connection)
    {
      Connection = connection;
    }
  }
}
