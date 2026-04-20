// Copyright (C) 2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xtensive.Core;

namespace Xtensive.Sql.Dml.Collections
{
  /// <summary>
  /// Collection of values of <see cref="SqlInsert"/>.
  /// </summary>
  public sealed class SqlInsertValuesCollection : IReadOnlyList<SqlRow>
  {
    // Typed as List<SqlColumn> (not IReadOnlyList) so CollectionsMarshal.AsSpan can expose
    // the backing array for zero-indirection iteration in the Add and Clone hot paths.
    // All writes to this field already produce a List<SqlColumn>.
    private List<SqlColumn> columns;
    private List<SqlRow> rows = new();

    /// <summary>
    /// The columns collection has values for.
    /// </summary>
    public IReadOnlyList<SqlColumn> Columns => columns ?? [];

    /// <summary>
    /// Count of rows.
    /// </summary>
    public int Count => rows.Count;

    /// <summary>
    /// Gets row by index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public SqlRow this[int index] => rows[index];

    /// <summary>
    /// Adds column-to-value mapped collection of values as row.
    /// </summary>
    /// <param name="row">column-to-value mapped collection of values</param>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> value is null.</exception>
    /// <exception cref="ArgumentException">Count of values between already added rows and <paramref name="row"/>
    /// -or- particular column in <paramref name="row"/> is not presented in already added rows
    /// -or- <paramref name="row"/> is empty.</exception>
    public void Add(Dictionary<SqlColumn, SqlExpression> row)
    {
      ArgumentNullException.ThrowIfNull(row);
      if (row.Count == 0) {
        throw new ArgumentException("Empty row is not allowed.");
      }

      if (rows.Count == 0) {
        // save columns order as header for further rows to match;
        columns = row.Keys.ToList();
        rows.Add(SqlDml.Row(row.Values.ToArray()));
      }
      else {
        if (columns.Count != row.Count)
          throw new ArgumentException("Inconsistent row length.");
        if (row.Keys.SequenceEqual(columns)) {
          //fast addition
          rows.Add(SqlDml.Row(row.Values.ToArray()));
        }
        else {
          //re-arrange values to be the same order
          //and also make sure all columns exist
          // CollectionsMarshal.AsSpan exposes columns' backing array so the loop reads
          // each SqlColumn directly from the span — no boxed enumerator and no per-element
          // indexer call through IReadOnlyList<T>.
          var columnsSpan = CollectionsMarshal.AsSpan(columns);
          var rowList = new List<SqlExpression>(columnsSpan.Length);
          for (int i = 0; i < columnsSpan.Length; i++) {
            var column = columnsSpan[i];
            if (row.TryGetValue(column, out var value)) {
              rowList.Add(value);
            }
            else {
              throw new ArgumentException($"There is no mentioning of column '{column.Name}' in previously added rows.");
            }
          }

          rows.Add(SqlDml.Row(rowList));
        }
      }
    }

    /// <summary>
    /// Removes row by index.
    /// </summary>
    /// <param name="index">The index of row to remove.</param>
    public void RemoveAt(int index)
    {
      rows.RemoveAt(index);
      if (rows.Count == 0) {
        columns = null;
      }
    }

    /// <summary>
    /// Clears rows and columns.
    /// </summary>
    public void Clear()
    {
      rows = new List<SqlRow>();
      columns = null;
    }

    // Returns the concrete List<T>.Enumerator struct so 'foreach (var row in coll)' resolves
    // to this method via the C# foreach pattern and avoids the boxed IEnumerator<T> allocation.
    // Iteration on this collection happens once per row written by an INSERT, so the saving
    // is one heap object per compile of every batched INSERT statement.
    public List<SqlRow>.Enumerator GetEnumerator() => rows.GetEnumerator();

    IEnumerator<SqlRow> IEnumerable<SqlRow>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal SqlInsertValuesCollection Clone(SqlNodeCloneContext ctx)
    {
      var clone = new SqlInsertValuesCollection();

      if (rows.Count == 0) {
        return clone;
      }

      var columnsSpan = CollectionsMarshal.AsSpan(columns);
      var clonedList = new List<SqlColumn>(columnsSpan.Length);
      for (int i = 0; i < columnsSpan.Length; i++) {
        clonedList.Add((SqlColumn) ctx.NodeMapping[columnsSpan[i]]);
      }
      clone.columns = clonedList;

      clone.rows = new List<SqlRow>(rows.Count);
      foreach(var oldRow in rows) {
        clone.rows.Add(oldRow.Clone(ctx));
      }

      return clone;
    }
  }
}
