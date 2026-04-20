// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents collection of <see cref="SqlColumn"/>s.
  /// </summary>
  [Serializable]
  public class SqlTableColumnCollection : IReadOnlyList<SqlTableColumn>
  {
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private readonly IReadOnlyList<SqlTableColumn> columnList;
    private Dictionary<string, SqlTableColumn> columnLookup;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="SqlTableColumnCollection"/>.
    /// </summary>
    public int Count => columnList.Count;

    // Public 'GetEnumerator' returning a custom struct enumerator. The C# foreach pattern
    // binds to this method (not the explicit IEnumerable<T> implementation), so iterating
    // this collection costs zero heap allocations even though the backing field is typed
    // as IReadOnlyList<T> (which itself only exposes a boxed enumerator). This collection
    // is hot — every SQL table reference walks its columns during compile / pruning.
    public Enumerator GetEnumerator() => new(columnList);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<SqlTableColumn> IEnumerable<SqlTableColumn>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the column at the specified <paramref name="index"/>.
    /// </summary>
    public SqlTableColumn this[int index] => columnList[index];

    /// <summary>
    /// Gets the column with the specified <paramref name="name"/>
    /// or <see langword="null"/> if collection doesn't contain such a column.
    /// </summary>
    public SqlTableColumn this[string name]
    {
      get {
        if (string.IsNullOrEmpty(name)) {
          return null;
        }

        var count = columnList.Count;
        return count <= 16 ? FindColumnInList(name) : FindColumnInDictionaryLookup(name, count);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SqlTableColumn FindColumnInList(string name)
    {
      foreach (var column in columnList) {
        if (Comparer.Equals(column.Name, name)) {
          return column;
        }
      }

      return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SqlTableColumn FindColumnInDictionaryLookup(string name, int count)
    {
      if (columnLookup != null) {
        return columnLookup.TryGetValue(name, out var column) ? column : null;
      }

      SqlTableColumn result = null;
      columnLookup = new Dictionary<string, SqlTableColumn>(count, Comparer);
      for (var index = count - 1; index >= 0; index--) {
        var column = columnList[index];
        var columnName = column.Name;
        columnLookup[columnName] = column;
        if (Comparer.Equals(columnName, name)) {
          result = column;
        }
      }

      return result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTableColumnCollection"/> class.
    /// </summary>
    /// <param name="columns">A collection of <see cref="SqlTableColumn"/>s to be wrapped.</param>
    public SqlTableColumnCollection(IEnumerable<SqlTableColumn> columns)
    {
      columnList = new List<SqlTableColumn>(columns);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTableColumnCollection"/> class.
    /// This is special version it uses provided list as is.
    /// </summary>
    internal SqlTableColumnCollection(IReadOnlyList<SqlTableColumn> columns)
    {
      columnList = columns;
    }

    /// <summary>
    /// Struct enumerator over <see cref="SqlTableColumnCollection"/>. Lives on the stack
    /// and indexes the underlying read-only list directly, so 'foreach' over the parent
    /// collection performs zero heap allocations.
    /// </summary>
    public struct Enumerator : IEnumerator<SqlTableColumn>
    {
      private readonly IReadOnlyList<SqlTableColumn> list;
      private readonly int count;
      private int index;
      private SqlTableColumn current;

      internal Enumerator(IReadOnlyList<SqlTableColumn> list)
      {
        this.list = list;
        count = list.Count;
        index = 0;
        current = null;
      }

      public readonly SqlTableColumn Current => current;

      readonly object IEnumerator.Current => current;

      public bool MoveNext()
      {
        if (index < count) {
          current = list[index++];
          return true;
        }

        current = null;
        return false;
      }

      void IEnumerator.Reset()
      {
        index = 0;
        current = null;
      }

      public readonly void Dispose() { }
    }
  }
}