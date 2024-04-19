// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.03

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Collections;
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
    public JoinType JoinType { get; private set; }

    /// <summary>
    /// Pairs of equal column indexes.
    /// </summary>
    public IReadOnlyList<(ColNum Left, ColNum Right)> EqualIndexes { get; }

    /// <summary>
    /// Pairs of equal columns.
    /// </summary>
    public IReadOnlyList<(Column Left, Column Right)> EqualColumns { get; private set; }

    /// <inheritdoc/>
    protected override string ParametersToString()
    {
      return string.Format(ToStringFormat,
        JoinType,
        EqualColumns.Select(p => p.Left.Name + " == " + p.Right.Name).ToCommaDelimitedString());
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
      base.Initialize();
      var leftColumns = Left.Header.Columns;
      var rightColumns = Right.Header.Columns;
      var n = EqualIndexes.Count;
      var equalColumns = new (Column Left, Column Right)[n];
      for (int i = n; i-- > 0;) {
        var (leftIndex, rightIndex) = EqualIndexes[i];
        equalColumns[i] = (leftColumns[leftIndex], rightColumns[rightIndex]);
      }
      EqualColumns = equalColumns;
    }


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
      if (equalIndexes==null || equalIndexes.Count==0)
        throw new ArgumentException(
          Strings.ExAtLeastOneColumnIndexPairMustBeSpecified, "equalIndexes");
      JoinType = joinType;
      EqualIndexes = equalIndexes;
      Initialize();
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
      if (equalIndexes==null || equalIndexes.Length<2)
        throw new ArgumentException(
          Strings.ExAtLeastOneColumnIndexPairMustBeSpecified, "equalIndexes");
      var ei = new (ColNum Left, ColNum Right)[equalIndexes.Length / 2];
      for (int i = 0, j = 0; i < ei.Length; i++)
        ei[i] = (equalIndexes[j++], equalIndexes[j++]);
      JoinType = joinType;
      EqualIndexes = ei;
      Initialize();
    }
  }
}