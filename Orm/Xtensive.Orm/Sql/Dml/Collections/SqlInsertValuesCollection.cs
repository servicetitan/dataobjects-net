// Copyright (C) 2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;

namespace Xtensive.Sql.Dml.Collections
{
  /// <summary>
  /// Collection of values of <see cref="SqlInsert"/>.
  /// </summary>
  public sealed class SqlInsertValuesCollection : IReadOnlyList<SqlRow>
  {
    private IReadOnlyList<SqlColumn> columns;
    private List<SqlRow> rows = new();

    /// <summary>
    /// The columns collection has values for.
    /// </summary>
    public IReadOnlyList<SqlColumn> Columns => columns ?? Array.Empty<SqlColumn>();

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
          var rowList = new List<SqlExpression>(columns.Count);
          // Indexed for over the IReadOnlyList<SqlColumn> field — foreach would allocate
          // a boxed enumerator on every multi-row INSERT add.
          for (int i = 0, n = columns.Count; i < n; i++) {
            var column = columns[i];
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

      var clonedList = new List<SqlColumn>(columns.Count);
      for (int i = 0, n = columns.Count; i < n; i++) {
        clonedList.Add((SqlColumn) ctx.NodeMapping[columns[i]]);
      }
      clone.columns = clonedList;

      clone.rows = new List<SqlRow>(rows.Count);
      foreach(var oldRow in rows) {
        clone.rows.Add((SqlRow) oldRow.Clone());
      }

      return clone;
    }
  }
}
