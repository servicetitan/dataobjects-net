using Xtensive.Orm.Model;

namespace Xtensive.Orm.Rse.Providers;

/// <summary>
/// Index hint provider
/// </summary>
[Serializable]
public sealed class IndexHintProvider(CompilableProvider source, IndexInfoRef index) : UnaryProvider(ProviderType.IndexHint, source.Header, source)
{
  /// <summary>
  /// Reference to the <see cref="IndexInfo"/> instance within the domain.
  /// </summary>
  public IndexInfoRef Index => index;

  internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitIndexHint(this);

  // Constructors
  public IndexHintProvider(CompilableProvider source, IndexInfo index)
    : this(source, new IndexInfoRef(index))
  {
  }
}
