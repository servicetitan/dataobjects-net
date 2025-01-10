// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2009.04.01

using Xtensive.Collections;

namespace Xtensive.Orm.Rse.Providers;

public abstract class ConcatUnionBaseProvider : BinaryProvider
{
  protected abstract void EnsureOperationIsPossible();

  protected override RecordSetHeader BuildHeader()
  {
    EnsureOperationIsPossible();
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

  protected ConcatUnionBaseProvider(ProviderType type, CompilableProvider left, CompilableProvider right)
    : base(type, left, right)
  {
    Initialize();
  }
}

/// <summary>
/// Produces concatenation between <see cref="BinaryProvider.Left"/> and 
/// <see cref="BinaryProvider.Right"/> sources.
/// </summary>
[Serializable]
public sealed class ConcatProvider(CompilableProvider left, CompilableProvider right)
  : ConcatUnionBaseProvider(ProviderType.Concat, left, right)
{
  /// <exception cref="InvalidOperationException">Something went wrong.</exception>
  protected override void EnsureOperationIsPossible()
  {
    if (!Left.Header.TupleDescriptor.Equals(Right.Header.TupleDescriptor))
      throw new InvalidOperationException(String.Format(Strings.ExXCantBeExecuted, "Concatenation"));
  }

  internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitConcat(this);
}
