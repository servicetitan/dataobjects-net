// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2008.09.11

namespace Xtensive.Orm.Rse;

/// <summary>
/// Descriptor of the calculated column.
/// </summary>
[Serializable]
public readonly record struct AggregateColumnDescriptor
(
  string Name,
  ColNum SourceIndex,
  AggregateType AggregateType,
  (sbyte Precision, sbyte Scale)? DecimalParametersHint = null
)
{
  /// <inheritdoc/>
  public override string ToString() => $"{base.ToString()} = {AggregateType} on ({SourceIndex})";
}
