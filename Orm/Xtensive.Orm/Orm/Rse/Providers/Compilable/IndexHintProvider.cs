using System;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Index hint provider
  /// </summary>
  [Serializable]
  public sealed class IndexHintProvider : UnaryProvider
  {
    /// <summary>
    /// Reference to the <see cref="IndexInfo"/> instance within the domain.
    /// </summary>
    public IndexInfoRef Index { get; }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitIndexHint(this);

    // Constructors
    public IndexHintProvider(CompilableProvider source, IndexInfo index)
      : base(ProviderType.IndexHint, source)
    {
      ArgumentNullException.ThrowIfNull(index);

      Index = new IndexInfoRef(index);
      Initialize();
    }
    
    public IndexHintProvider(CompilableProvider source, IndexInfoRef index)
      : base(ProviderType.IndexHint, source)
    {
      ArgumentNullException.ThrowIfNull(index);
      Index = index;
      Initialize();
    }
  }
}