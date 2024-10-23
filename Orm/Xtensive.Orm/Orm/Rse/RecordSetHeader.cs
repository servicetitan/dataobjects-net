// Copyright (C) 2007-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2007.09.13

using System.Buffers;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Model;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Tuples;
using IndexInfo = Xtensive.Orm.Model.IndexInfo;

namespace Xtensive.Orm.Rse
{
  /// <summary>
  /// Header of <see cref="Provider"/>.
  /// </summary>
  [Serializable]
  public sealed class RecordSetHeader
  {
    private TupleDescriptor orderTupleDescriptor;

    /// <summary>
    /// Gets the length of this instance.
    /// </summary>
    public ColNum Length => Columns.Count;

    /// <summary>
    /// Gets the <see cref="Provider"/> keys.
    /// </summary>
    /// <value>The keys.</value>
    public IReadOnlyList<ColumnGroup> ColumnGroups { get; }

    /// <summary>
    /// Gets the <see cref="Provider"/> columns.
    /// </summary>
    public ColumnCollection Columns { get; }

    /// <summary>
    /// Gets the <see cref="Provider"/> tuple descriptor.
    /// </summary>
    public TupleDescriptor TupleDescriptor { get; }

    /// <summary>
    /// Gets the indexes of columns <see cref="Provider"/> is ordered by.
    /// </summary>
    public DirectionCollection<ColNum> Order { get; }

    /// <summary>
    /// Gets the tuple descriptor describing
    /// a set of <see cref="Order"/> columns.
    /// </summary>
    public TupleDescriptor OrderTupleDescriptor => Order.Count == 0 ? null : orderTupleDescriptor;

    /// <summary>
    /// Aliases the header.
    /// </summary>
    /// <param name="alias">The alias to apply.</param>
    /// <returns>Aliased header.</returns>
    public RecordSetHeader Alias(string alias)
    {
      ColumnCollection resultColumns = Columns.Alias(alias);

      return new RecordSetHeader(
        TupleDescriptor, resultColumns, ColumnGroups, OrderTupleDescriptor, Order);
    }

    /// <summary>
    /// Adds the specified column to header.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The constructed header.</returns>
    public RecordSetHeader Add(Column column)
    {
      var newColumns = new List<Column>(Columns.Count + 1);
      newColumns.AddRange(Columns);
      newColumns.Add(column);

      var newTupleDescriptor = CreateTupleDescriptor(newColumns);

      return new RecordSetHeader(
        newTupleDescriptor,
        newColumns,
        ColumnGroups,
        OrderTupleDescriptor,
        Order);
    }

    /// <summary>
    /// Adds the specified columns to header.
    /// </summary>
    /// <param name="columns">The columns to add.</param>
    /// <returns>The constructed header.</returns>
    public RecordSetHeader Add(IReadOnlyList<Column> columns)
    {
      var n = Columns.Count + columns.Count;
      var newColumns = Columns.Concat(columns).ToArray(n);

      var newTupleDescriptor = CreateTupleDescriptor(newColumns);

      return new RecordSetHeader(
        newTupleDescriptor,
        newColumns,
        ColumnGroups,
        OrderTupleDescriptor,
        Order);
    }

    /// <summary>
    /// Adds the specified columns to header.
    /// </summary>
    /// <param name="columns">The columns to add.</param>
    /// <returns>The constructed header.</returns>
    public RecordSetHeader Add(IReadOnlyList<Column> columns)
    {
      var newColumns = new List<Column>(Columns.Count + columns.Count);
      newColumns.AddRange(Columns);
      newColumns.AddRange(columns);

      var newTupleDescriptor = CreateTupleDescriptor(newColumns);

      return new RecordSetHeader(
        TupleDescriptor.Create(newFieldTypes),
        newColumns,
        ColumnGroups,
        OrderTupleDescriptor,
        Order);
    }

    /// <summary>
    /// Joins the header with the specified one.
    /// </summary>
    /// <param name="joined">The header to join.</param>
    /// <returns>The joined header.</returns>
    public RecordSetHeader Join(RecordSetHeader joined)
    {
      var columnCount = Columns.Count;
      var newColumns = new Column[columnCount + joined.Columns.Count];
      int j = 0;
      foreach (var c in Columns) {
        newColumns[j++] = c;
      }
      foreach (var c in joined.Columns) {
        newColumns[j++] = c.Clone((ColNum) (columnCount + c.Index));
      }

      var newTupleDescriptor = CreateTupleDescriptor(newColumns);

      var columnGroupCount = ColumnGroups.Count;
      var groups = new ColumnGroup[columnGroupCount + joined.ColumnGroups.Count];
      j = 0;
      foreach (var g in ColumnGroups) {
        groups[j++] = g;
      }
      foreach (var g in joined.ColumnGroups) {
        var keys = new ColNum[g.Keys.Count];
        int k = 0;
        foreach (var i in g.Keys) {
          keys[k++] = (ColNum) (columnCount + i);
        }
        var columns = new ColNum[g.Columns.Count];
        k = 0;
        foreach (var i in g.Columns) {
          columns[k++] = (ColNum) (columnCount + i);
        }
        groups[j++] = new ColumnGroup(g.TypeInfoRef, keys, columns);
      }

      return new RecordSetHeader(
        new(TupleDescriptor, joined.TupleDescriptor),
        newColumns,
        groups,
        OrderTupleDescriptor,
        Order);
    }

    /// <summary>
    /// Selects the specified columns from the header.
    /// </summary>
    /// <param name="selectedColumns">The indexes of columns to select.</param>
    /// <returns>A new header containing only specified columns.</returns>
    public RecordSetHeader Select(IReadOnlyList<ColNum> columns)
    {
      using PooledArray<ColNum> columnsArray = new(Columns.Count);
      ColNum[] columnsMap = columnsArray;
      Array.Fill(columnsMap, (ColNum) (-1));
      for (ColNum newIndex = 0, n = (ColNum) columns.Count; newIndex < n; newIndex++) {
        var oldIndex = columns[newIndex];
        columnsMap[oldIndex] = newIndex;
      }

      var resultOrder = new DirectionCollection<ColNum>(
        Order
          .Select(o => new KeyValuePair<int, Direction>(columnsMap[o.Key], o.Value))
          .TakeWhile(static o => o.Key >= 0));

      var resultGroups = ColumnGroups
        .Where(g => g.Keys.All(k => columnsMap[k] >= 0))
        .Select(g => new ColumnGroup(
            g.TypeInfoRef,
            g.Keys.Select(k => columnsMap[k]),
            g.Columns
              .Select(c => columnsMap[c])
              .Where(static c => c >= 0)));

      return new RecordSetHeader(
        TupleDescriptor.CreateFromNormalized(columns.Select(i => TupleDescriptor[i]).ToArray(columns.Count)),
        columns.Select((oldIndex, newIndex) => Columns[oldIndex].Clone((ColNum) newIndex)).ToArray(columns.Count),
        resultGroups.ToList(),
        null,
        resultOrder);
    }

    /// <summary>
    /// Sorts the header in the specified order.
    /// </summary>
    /// <param name="sortOrder">Order to sort this header in.</param>
    /// <returns>A new sorted header.</returns>
    public RecordSetHeader Sort(DirectionCollection<ColNum> sortOrder)
    {
      return new RecordSetHeader(
        TupleDescriptor,
        Columns,
        ColumnGroups,
        TupleDescriptor,
        sortOrder);
    }


    /// <summary>
    /// Gets the <see cref="RecordSetHeader"/> object for the specified <paramref name="indexInfo"/>.
    /// </summary>
    /// <param name="indexInfo">The index info to get the header for.</param>
    /// <returns>The <see cref="RecordSetHeader"/> object.</returns>
    public static RecordSetHeader GetHeader(IndexInfo indexInfo)
    {
      return CreateHeader(indexInfo);
    }

    private static RecordSetHeader CreateHeader(IndexInfo indexInfo)
    {
      var indexInfoColumns = indexInfo.Columns;
      var indexInfoKeyColumns = indexInfo.KeyColumns;

      var resultFieldTypes = indexInfoColumns.Select(columnInfo => columnInfo.ValueType).ToArray(indexInfoColumns.Count);
      var resultTupleDescriptor = TupleDescriptor.Create(resultFieldTypes);

      var keyOrderEnumerable = indexInfoKeyColumns.Select(static (p, i) => new KeyValuePair<int, Direction>(i, p.Value));
      if (!indexInfo.IsPrimary) {
        var pkKeys = indexInfo.ReflectedType.Indexes.PrimaryIndex.KeyColumns;
        var offset = indexInfoKeyColumns.Count;
        keyOrderEnumerable = keyOrderEnumerable
          .Concat(indexInfo.ValueColumns
            .Select((c, i) => new Pair<ColumnInfo, int>(c, i + offset))
            .Where(static pair => pair.First.IsPrimaryKey)
            .Select(pair => new KeyValuePair<int, Direction>(pair.Second, pkKeys[pair.First])));
      }
      var order = new DirectionCollection<int>(keyOrderEnumerable);

      var keyFieldTypes = indexInfoKeyColumns
        .SelectToArray(static columnInfo => columnInfo.Key.ValueType);
      var keyDescriptor = TupleDescriptor.Create(keyFieldTypes);

      var resultColumns = indexInfoColumns
        .Select(static (c,i) => (Column) new MappedColumn(new ColumnInfoRef(c), i, c.ValueType))
        .ToArray(indexInfoColumns.Count);
      var resultGroups = new[]{indexInfo.Group};

      return new RecordSetHeader(
        resultTupleDescriptor,
        resultColumns,
        resultGroups,
        keyDescriptor,
        order);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static TupleDescriptor CreateTupleDescriptor(List<Column> newColumns)
    {
      var newFieldTypes = new Type[newColumns.Count];
      for (var i = 0; i < newColumns.Count; i++) {
        newFieldTypes[i] = newColumns[i].Type;
      }
      return TupleDescriptor.Create(newFieldTypes);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      return Columns.Select(c => c.ToString()).ToCommaDelimitedString();
    }


    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="tupleDescriptor">Descriptor of the result item.</param>
    /// <param name="columns">Result columns.</param>
    public RecordSetHeader(
      in TupleDescriptor tupleDescriptor,
      IReadOnlyList<Column> columns)
      : this(tupleDescriptor, columns, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="tupleDescriptor">Descriptor of the result item.</param>
    /// <param name="columns">Result columns.</param>
    /// <param name="columnGroups">Column groups.</param>
    public RecordSetHeader(
      TupleDescriptor tupleDescriptor,
      IReadOnlyList<Column> columns,
      IReadOnlyList<ColumnGroup> columnGroups)
      : this(tupleDescriptor, columns, columnGroups, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="tupleDescriptor">Descriptor of the result item.</param>
    /// <param name="columns">Result columns.</param>
    /// <param name="orderKeyDescriptor">Descriptor of ordered columns.</param>
    /// <param name="order">Result sort order.</param>
    public RecordSetHeader(
      TupleDescriptor tupleDescriptor,
      IReadOnlyList<Column> columns,
      TupleDescriptor orderKeyDescriptor,
      DirectionCollection<ColNum> order)
      : this(tupleDescriptor, columns, null, orderKeyDescriptor, order)
    {
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="tupleDescriptor">Descriptor of the result item.</param>
    /// <param name="columns">Result columns.</param>
    /// <param name="columnGroups">Column groups.</param>
    /// <param name="orderKeyDescriptor">Descriptor of ordered columns.</param>
    /// <param name="order">Result sort order.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>columns.Count</c> is out of range.</exception>
    public RecordSetHeader(
      TupleDescriptor tupleDescriptor,
      IReadOnlyList<Column> columns,
      IReadOnlyList<ColumnGroup> columnGroups,
      TupleDescriptor orderKeyDescriptor,
      DirectionCollection<ColNum> order)
    {
      ArgumentNullException.ThrowIfNull(tupleDescriptor);
      ArgumentNullException.ThrowIfNull(columns);

      TupleDescriptor = tupleDescriptor;
      // Unsafe perf. optimization: if you pass a list, it should be immutable!
      Columns = new ColumnCollection(columns);
      if (tupleDescriptor.Count != Columns.Count)
        throw new ArgumentOutOfRangeException("columns.Count");

      ColumnGroups = columnGroups ?? [];
      orderTupleDescriptor = orderKeyDescriptor ?? TupleDescriptor.Empty;
      Order = order ?? new DirectionCollection<ColNum>();
      Order.Lock(true);
    }
  }
}
