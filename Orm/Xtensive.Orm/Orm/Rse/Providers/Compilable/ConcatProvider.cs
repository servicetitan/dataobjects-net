// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2009.04.01

using System;
using System;
using System.Collections.Generic;
using Xtensive.Collections;


using System.Linq;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Produces concatenation between <see cref="BinaryProvider.Left"/> and 
  /// <see cref="BinaryProvider.Right"/> sources.
  /// </summary>
  [Serializable]
  public sealed class ConcatProvider : BinaryProvider
  {
    protected override RecordSetHeader BuildHeader()
    {
      EnsureConcatIsPossible();
      HashSet<ColNum> mappedColumnIndexes = [];
      var leftHeader = Left.Header;
      var leftHeaderColumns = leftHeader.Columns;
      var rightHeaderColumns = Right.Header.Columns;
      var columns = new Column[leftHeaderColumns.Count];
      for (ColNum i = 0; i < columns.Length; i++) {
        var leftColumn = leftHeaderColumns[i];
        var rightColumn = rightHeaderColumns[i];
        if (leftColumn is MappedColumn leftMappedColumn
            && rightColumn is MappedColumn rightMappedColumn
            && leftMappedColumn.ColumnInfoRef.Equals(rightMappedColumn.ColumnInfoRef)) {
          columns[i] = leftMappedColumn;
          mappedColumnIndexes.Add(i);
        }
        else
          columns[i] = new SystemColumn(leftColumn.Name, leftColumn.Index, leftColumn.Type);
      }
      var columnGroups = leftHeader.ColumnGroups.Where(cg => mappedColumnIndexes.IsSupersetOf(cg.Keys)).ToArray();

      return new RecordSetHeader(
        leftHeader.TupleDescriptor,
        columns,
        columnGroups,
        null,
        null);
    }

    /// <exception cref="InvalidOperationException">Something went wrong.</exception>
    private void EnsureConcatIsPossible()
    {
      var left = Left.Header.TupleDescriptor;
      var right = Right.Header.TupleDescriptor;
      if (!left.Equals(right))
        throw new InvalidOperationException(String.Format(Strings.ExXCantBeExecuted, "Concatenation"));
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitConcat(this);

    // Constructors

    /// <summary>
    ///  Initializes a new instance of this class.
    /// </summary>
    /// <param name="left">The left provider to intersect.</param>
    /// <param name="right">The right provider to intersect.</param>
    public ConcatProvider(CompilableProvider left, CompilableProvider right)
      : base(ProviderType.Concat, left, right)
    {
      Initialize();
    }
  }
}
