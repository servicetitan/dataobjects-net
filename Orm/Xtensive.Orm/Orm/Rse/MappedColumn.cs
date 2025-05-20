// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2007.09.21

using Xtensive.Orm.Model;

namespace Xtensive.Orm.Rse;

/// <summary>
/// Mapped column of the <see cref="RecordSetHeader"/>.
/// </summary>
[Serializable]
public class MappedColumn(ColumnInfoRef columnInfoRef, string name, ColNum index, Type type)
  : Column(name, index, type)
{
  /// <summary>
  /// Gets the reference that describes a column.
  /// </summary>
  public ColumnInfoRef ColumnInfoRef { get; } = columnInfoRef;

  /// <inheritdoc/>
  public override string ToString() => $"{base.ToString()} = {ColumnInfoRef}";

  /// <inheritdoc/>
  public override Column Clone(ColNum newIndex) => new MappedColumn(ColumnInfoRef, Name, newIndex, Type);

  /// <inheritdoc/>
  public override Column Clone(string newName) => new DerivedMappedColumn(newName, Index, Type, Origin, ColumnInfoRef);

  // Constructors

  #region Basic constructors

  /// <summary>
  /// Initializes a new instance of this class.
  /// </summary>
  /// <param name="name"><see cref="Column.Name"/> property value.</param>
  /// <param name="index"><see cref="Column.Index"/> property value.</param>
  /// <param name="type"><see cref="Column.Type"/> property value.</param>
  public MappedColumn(string name, ColNum index, Type type)
    : this(default, name, index, type)
  {
  }

  /// <summary>
  /// Initializes a new instance of this class.
  /// </summary>
  /// <param name="columnInfoRef"><see cref="ColumnInfoRef"/> property value.</param>
  /// <param name="index"><see cref="Column.Index"/> property value.</param>
  /// <param name="type"><see cref="Column.Type"/> property value.</param>
  public MappedColumn(ColumnInfoRef columnInfoRef, ColNum index, Type type)
    : this(columnInfoRef, columnInfoRef.ColumnName, index, type)
  {
  }

  #endregion

}

// The purpose of this class is minimize allocation size of `MappedColumn`
// Non self-referencing `Origin` property is a rare case
internal sealed class DerivedMappedColumn(string name, ColNum index, Type type, Column origin, ColumnInfoRef columnInfoRef)
  : MappedColumn(columnInfoRef, name, index, type)
{
  public override Column Origin => origin ?? this;
}
