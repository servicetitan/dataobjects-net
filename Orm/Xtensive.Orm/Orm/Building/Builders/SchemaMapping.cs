using BitFaster.Caching.Lru;

namespace Xtensive.Orm.Building.Builders
{
  public class SchemaMapping
  {
      private static readonly FastConcurrentLru<(string, string), SchemaMapping> cache = new(20_000);

      public string Database { get; private init; }
      public string Schema { get; private init; }

      public static SchemaMapping Get(string database, string schema) =>
        cache.GetOrAdd((database, schema), static p => new SchemaMapping { Database = p.Item1, Schema = p.Item2 });
  }
}
