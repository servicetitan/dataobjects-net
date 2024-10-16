// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.03.05

using System;
using Xtensive.Collections;
using Xtensive.Reflection;
using Xtensive.Tuples.Transform;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Compilable provider that adds row number to <see cref="UnaryProvider.Source"/>.
  /// </summary>
  [Serializable]
  public sealed class RowNumberProvider : UnaryProvider
  {
    /// <summary>
    /// Gets the row number column.
    /// </summary>
    public SystemColumn SystemColumn { get; private set; }

    /// <summary>
    /// Gets header resize transform.
    /// </summary>
    public MapTransform ResizeTransform { get; private set; }

    /// <inheritdoc/>
    protected override void Initialize()
    {
      base.Initialize();
      var columnIndexes = new ColNum[Header.Length];
      for (ColNum i = 0; i < columnIndexes.Length; i++)
        columnIndexes[i] = (i < Source.Header.Length) ? i : MapTransform.NoMapping;
      ResizeTransform = new MapTransform(false, Header.TupleDescriptor, columnIndexes);
    }

    /// <inheritdoc/>
    protected override RecordSetHeader BuildHeader()
    {
      return Source.Header.Add(SystemColumn);
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitRowNumber(this);

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="source">The <see cref="UnaryProvider.Source"/> property value.</param>
    /// <param name="columnName">The name of <see cref="SystemColumn"/>.</param>
    public RowNumberProvider(CompilableProvider source, string columnName)
      : base(ProviderType.RowNumber, source)
    {
      SystemColumn = new SystemColumn(columnName, Source.Header.Length, WellKnownTypes.Int64);
      Initialize();
    }
  }
}