// Copyright (C) 2008-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Elena Vakhtina
// Created:    2008.09.18

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Collections;
using Xtensive.Core;

using Xtensive.Reflection;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Tuples.Transform;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Compilable provider that applies aggregate functions to grouped columns from <see cref="UnaryProvider.Source"/>.
  /// </summary>
  [Serializable]
  public sealed class AggregateProvider : UnaryProvider
  {
    private const string ToStringFormatGroupOnly = "Group by ({0})";
    private const string ToStringFormatAggregateOnly = "{0}";
    private const string ToStringFormatFull = "{0}, Group by ({1})";

    /// <summary>
    /// Gets the aggregate columns.
    /// </summary>
    public AggregateColumn[] AggregateColumns { get; }

    /// <summary>
    /// Gets column indexes to group by.
    /// </summary>
    public IReadOnlyList<ColNum> GroupColumnIndexes { get; }

    /// <summary>
    /// Gets header resize transform.
    /// </summary>
    public MapTransform Transform { get; private set; }

    /// <inheritdoc/>
    protected override RecordSetHeader BuildHeader()
    {
      return Source.Header
        .Select(GroupColumnIndexes)
        .Add(AggregateColumns);
    }

    /// <inheritdoc/>
    protected override string ParametersToString()
    {
      if (AggregateColumns.Length==0)
        return string.Format(
          ToStringFormatGroupOnly,
          GroupColumnIndexes.ToCommaDelimitedString());

      if (GroupColumnIndexes.Count == 0)
        return string.Format(
          ToStringFormatAggregateOnly,
          AggregateColumns.ToCommaDelimitedString());

      return string.Format(
        ToStringFormatFull,
        AggregateColumns.ToCommaDelimitedString(),
        GroupColumnIndexes.ToCommaDelimitedString());
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
      base.Initialize();
      var fieldTypes = new Type[GroupColumnIndexes.Count];
      var columnIndexes = new ColNum[GroupColumnIndexes.Count];
      var i = 0;
      foreach (var index in GroupColumnIndexes) {
        fieldTypes[i] = Source.Header.Columns[index].Type;
        columnIndexes[i] = index;
        i++;
      }
      Transform = new MapTransform(false, TupleDescriptor.Create(fieldTypes), columnIndexes);
    }

    /// <summary>
    /// Gets the type of the aggregate column according to a <see cref="AggregateType"/> and original column type.
    /// </summary>
    /// <param name="sourceColumnType">Type of the source column.</param>
    /// <param name="aggregateType">Type of the aggregate.</param>
    /// <returns>The type of aggregate column.</returns>
    public static Type GetAggregateColumnType(Type sourceColumnType, AggregateType aggregateType)
    {
      // TODO: very stupid - remove when nullables handing fixed everywhere.
      if (sourceColumnType.IsNullable())
        sourceColumnType = sourceColumnType.GetGenericArguments()[0];
      switch (aggregateType) {
      case AggregateType.Count:
        return WellKnownTypes.Int64;
      case AggregateType.Min:
      case AggregateType.Max:
        return GetMinMaxColumnType(sourceColumnType, aggregateType);
      case AggregateType.Sum:
        return GetSumColumnType(sourceColumnType);
      case AggregateType.Avg:
        return GetAvgColumnType(sourceColumnType);
      default:
        throw AggregateNotSupported(sourceColumnType, aggregateType);
      }
    }

    #region Private / internal methods

    private static Type GetMinMaxColumnType(Type sourceColumnType, AggregateType aggregateType)
    {
      switch (System.Type.GetTypeCode(sourceColumnType)) {
      case TypeCode.Char:
      case TypeCode.SByte:
      case TypeCode.Byte:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
      case TypeCode.Decimal:
      case TypeCode.Single:
      case TypeCode.Double:
      case TypeCode.String:
      case TypeCode.DateTime:
        return sourceColumnType;
      default:
        if (sourceColumnType==WellKnownTypes.TimeSpan || sourceColumnType==WellKnownTypes.DateTimeOffset)
          return sourceColumnType;
        if (sourceColumnType == WellKnownTypes.DateOnly || sourceColumnType == WellKnownTypes.TimeOnly)
          return sourceColumnType;
          throw AggregateNotSupported(sourceColumnType, aggregateType);
      }
    }

    private static Type GetSumColumnType(Type sourceColumnType)
    {
      switch (System.Type.GetTypeCode(sourceColumnType)) {
      case TypeCode.SByte:
      case TypeCode.Byte:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
      case TypeCode.Decimal:
      case TypeCode.Single:
      case TypeCode.Double:
        return sourceColumnType;
      default:
        throw AggregateNotSupported(sourceColumnType, AggregateType.Sum);
      }
    }

    private static Type GetAvgColumnType(Type sourceColumnType)
    {
      switch (System.Type.GetTypeCode(sourceColumnType)) {
      case TypeCode.SByte:
      case TypeCode.Byte:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
        return WellKnownTypes.Double;
      case TypeCode.Decimal:
      case TypeCode.Single:
      case TypeCode.Double:
        return sourceColumnType;
      default:
        throw AggregateNotSupported(sourceColumnType, AggregateType.Avg);
      }
    }

    private static NotSupportedException AggregateNotSupported(Type sourceColumnType, AggregateType aggregateType)
    {
      return new NotSupportedException(string.Format(
        Strings.ExAggregateXIsNotSupportedForTypeY, aggregateType, sourceColumnType));
    }

    internal override Provider Visit(ProviderVisitor visitor) => visitor.VisitAggregate(this);

    #endregion

    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="source">The <see cref="UnaryProvider.Source"/> property value.</param>
    /// <param name="columnDescriptors">The descriptors of <see cref="AggregateColumns"/>.</param>
    /// <param name="groupIndexes">The column indexes to group by.</param>
    public AggregateProvider(CompilableProvider source, IReadOnlyList<ColNum> groupIndexes, IEnumerable<AggregateColumnDescriptor> columnDescriptors)
      : base(ProviderType.Aggregate, source)
    {
      GroupColumnIndexes = groupIndexes ?? Array.Empty<ColNum>();
      var baseIndex = GroupColumnIndexes.Count;
      var columns = Source.Header.Columns;
      AggregateColumns = columnDescriptors
        .Select((d, i) => new AggregateColumn(d, (ColNum)(baseIndex + i), GetAggregateColumnType(columns[d.SourceIndex].Type, d.AggregateType)))
        .ToArray();
      Initialize();
    }
  }
}