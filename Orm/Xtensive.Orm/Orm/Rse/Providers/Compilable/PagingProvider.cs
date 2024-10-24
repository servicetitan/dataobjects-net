// Copyright (C) 2011-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2011.03.24

using System;
using System.Diagnostics;
using Xtensive.Core;


namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Compilable provider that skips X result records and takes Y result records from <see cref="UnaryProvider.Source"/>.
  /// </summary>
  [Serializable]
  public sealed class PagingProvider : UnaryProvider
  {
    /// <summary>
    /// From number function.
    /// </summary>
    public Func<ParameterContext, int> From { get; private set; }

    /// <summary>
    /// To number function.
    /// </summary>
    public Func<ParameterContext, int> To { get; private set; }

    /// <summary>
    /// Skip amount function.
    /// </summary>
    public Func<ParameterContext, int> Skip { get; private set; }

    /// <summary>
    /// Take amount function.
    /// </summary>
    public Func<ParameterContext, int> Take { get; private set; }

    /// <inheritdoc/>
    protected override string ParametersToString()
    {
      return "[" + Skip + "; " + Take +"]";
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitPaging(this);

    // Constructors

    /// <summary>
    ///   Initializes a new instance of this class.
    /// </summary>
    /// <param name="provider">The <see cref="UnaryProvider.Source"/> property value.</param>
    /// <param name="take">The <see cref="Take"/> property value.</param>
    /// <param name="skip">The <see cref="Skip"/> property value.</param>
    public PagingProvider(CompilableProvider provider, Func<ParameterContext, int> skip, Func<ParameterContext, int> take)
      : base(ProviderType.Paging, provider)
    {
      Skip = skip;
      Take = take;
      From = context => skip(context) + 1;
      To = context => take(context) + skip(context);
      Initialize();
    }

    /// <summary>
    /// 	Initializes a new instance of this class.
    /// </summary>
    /// <param name="provider">The <see cref="UnaryProvider.Source"/> property value.</param>
    /// <param name="skip">The value for <see cref="Skip"/> function property.</param>
    /// <param name="take">The value for <see cref="Take"/> function property.</param>
    public PagingProvider(CompilableProvider provider, int skip, int take)
      : base(ProviderType.Paging, provider)
    {
      Skip = context => skip;
      Take = context => take;
      From = context => skip + 1;
      To = context => take + skip;
      Initialize();
    }

    /// <summary>
    ///   Initializes a new instance of this class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <param name="pagingProvider">The paging provider.</param>
    public PagingProvider(CompilableProvider provider, PagingProvider pagingProvider) 
      : base(ProviderType.Paging, provider)
    {
      Skip = pagingProvider.Skip;
      Take = pagingProvider.Take;
      From = pagingProvider.From;
      To = pagingProvider.To;
      Initialize();
    }
  }
}