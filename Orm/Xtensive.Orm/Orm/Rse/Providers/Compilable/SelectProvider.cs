// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.03

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Compilable provider that declares select operator over the <see cref="UnaryProvider.Source"/>.
  /// </summary>
  [Serializable]
  public sealed class SelectProvider : UnaryProvider
  {
    /// <summary>
    /// Indexes of columns that should be selected from the <see cref="UnaryProvider.Source"/>.
    /// </summary>
    public IReadOnlyList<ColNum> ColumnIndexes { [DebuggerStepThrough] get; }

    /// <inheritdoc/>
    protected override RecordSetHeader BuildHeader()
    {
      return base.BuildHeader().Select(ColumnIndexes);
    }

    /// <inheritdoc/>
    protected override string ParametersToString()
    {
      return Header.Columns.Select(c => c.Name).ToCommaDelimitedString();
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitSelect(this);

    // Constructors

    /// <summary>
    ///   Initializes a new instance of this class.
    /// </summary>
    public SelectProvider(CompilableProvider provider, IReadOnlyList<ColNum> columnIndexes)
      : base(ProviderType.Select, provider)
    {
      ColumnIndexes = columnIndexes.AsSafeWrapper();

      Initialize();
    }
  }
}