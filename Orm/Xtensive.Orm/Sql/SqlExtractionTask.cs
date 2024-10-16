using Xtensive.Core;
using Xtensive.Sql.Model;

namespace Xtensive.Sql
{
  /// <summary>
  /// A task for <see cref="Extractor"/>
  /// </summary>
  public sealed class SqlExtractionTask
  {
    /// <summary>
    /// Gets catalog to extact.
    /// </summary>
    public string Catalog { get; private set; }

    /// <summary>
    /// Gets schema to extract.
    /// </summary>
    public string Schema { get; private set; }

    /// <summary>
    /// Gets value indicating if all schemas in the specified catalog
    /// should be extracted.
    /// </summary>
    public bool AllSchemas { get { return Schema==null; } }

    // Constructors

    public SqlExtractionTask(string catalog)
    {
      ArgumentNullException.ThrowIfNull(catalog);

      Catalog = catalog;
    }

    public SqlExtractionTask(string catalog, string schema)
    {
      ArgumentNullException.ThrowIfNull(catalog);
      ArgumentNullException.ThrowIfNull(schema);

      Catalog = catalog;
      Schema = schema;
    }
  }
}