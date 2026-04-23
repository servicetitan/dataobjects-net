// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.03

using System;
using System.Linq;
using Xtensive.Core;


namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Produces equality join between <see cref="BinaryProvider.Left"/> and 
  /// <see cref="BinaryProvider.Right"/> sources.
  /// </summary>
  [Serializable]
  public sealed class JoinProvider : BinaryProvider
  {
    private const string ToStringFormat = "{0}, {1}";

    /// <summary>
    /// Join operation type.
    /// </summary>
    public JoinType JoinType { get; }

    /// <summary>
    /// Pairs of equal column indexes.
    /// </summary>
    public IReadOnlyList<(ColNum Left, ColNum Right)> EqualIndexes { get; }

    /// <summary>
    /// Pairs of equal columns.
    /// </summary>
    public IReadOnlyList<(Column Left, Column Right)> EqualColumns { get; }

    /// <inheritdoc/>
    protected override string ParametersToString()
    {
      return string.Format(ToStringFormat,
        JoinType,
        EqualColumns.Select(p => p.Left.Name + " == " + p.Right.Name).ToCommaDelimitedString());
    }


    private static (Column Left, Column Right)[] BuildEqualColumns(ColumnCollection leftHeaderColumns, ColumnCollection rightHeaderColumns, IReadOnlyList<(ColNum Left, ColNum Right)> equalIndexes)
    {
      var count = equalIndexes.Count;
      var equalColumns = new (Column Left, Column Right)[count];
      for (int i = 0; i < count; i++) {
        var (left, right) = equalIndexes[i];
        equalColumns[i] = (leftHeaderColumns[left], rightHeaderColumns[right]);
      }
      return equalColumns;
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitJoin(this);

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="left">The left provider to join.</param>
    /// <param name="right">The right provider to join.</param>
    /// <param name="joinType">The join operation type.</param>
    /// <param name="equalIndexes">The <see cref="EqualIndexes"/> property value.</param>
    /// <exception cref="ArgumentException">Wrong arguments.</exception>
    public JoinProvider(CompilableProvider left, CompilableProvider right, JoinType joinType, IReadOnlyList<(ColNum Left, ColNum Right)> equalIndexes)
      : base(ProviderType.Join, left, right)
    {
      if (equalIndexes == null || equalIndexes.Count == 0) {
        throw new ArgumentException(
          Strings.ExAtLeastOneColumnIndexPairMustBeSpecified, nameof(equalIndexes));
      }
      JoinType = joinType;
      EqualIndexes = equalIndexes;
      EqualColumns = BuildEqualColumns(left.Header.Columns, right.Header.Columns, equalIndexes);
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="left">The left provider to join.</param>
    /// <param name="right">The right provider to join.</param>
    /// <param name="joinType">The join operation type.</param>
    /// <param name="equalIndexes">Transformed to the <see cref="EqualIndexes"/> property value.</param>
    /// <exception cref="ArgumentException">Wrong arguments.</exception>
    public JoinProvider(CompilableProvider left, CompilableProvider right, JoinType joinType, params ColNum[] equalIndexes)
      : base(ProviderType.Join, left, right)
    {
      if (equalIndexes == null || equalIndexes.Length < 2) {
        throw new ArgumentException(
          Strings.ExAtLeastOneColumnIndexPairMustBeSpecified, nameof(equalIndexes));
      }
      var ei = new (ColNum Left, ColNum Right)[equalIndexes.Length / 2];
      for (int i = 0, j = 0; i < ei.Length; i++) {
        ei[i] = (equalIndexes[j++], equalIndexes[j++]);
      }
      JoinType = joinType;
      EqualIndexes = ei;
      EqualColumns = BuildEqualColumns(left.Header.Columns, right.Header.Columns, ei);
    }
  }
}
