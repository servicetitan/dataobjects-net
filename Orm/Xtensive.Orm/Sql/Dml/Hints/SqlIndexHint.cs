using System;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public class SqlIndexHint : SqlHint
  {
    /// <summary>
    /// Gets the index name.
    /// </summary>
    /// <value>The index name.</value>
    public string IndexName { get; }

    public SqlTableRef From { get; }

    internal override SqlIndexHint Clone(SqlNodeCloneContext context) => 
      context.GetOrAdd(this, static (t, c) =>
        new SqlIndexHint(t.IndexName, t.From));

    public override void AcceptVisitor(ISqlVisitor visitor)
    {
      visitor.Visit(this);
    }

    internal SqlIndexHint(string indexName, SqlTableRef from)
    {
      IndexName = indexName;
      From = from;
    }
  }
}