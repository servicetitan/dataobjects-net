// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information

using System;
using System.Collections.Generic;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents collection of <see cref="SqlColumn"/>s.
  /// </summary>
  [Serializable]
  public class SqlColumnCollection(IEnumerable<SqlColumn> columns) : List<SqlColumn>(columns)
  {
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets the column with the specified <paramref name="name"/>
    /// or <see langword="null"/> if collection doesn't contain such a column.
    /// </summary>
    public SqlColumn this[string name] =>
      string.IsNullOrEmpty(name) ? null : Find(column => Comparer.Equals(column.Name, name));

    /// <summary>
    /// Builds a <see cref="SqlColumnRef"/> to the specified <paramref name="column"/> using
    /// the provided <paramref name="alias"/> and then adds it to the end of the <see cref="SqlColumnCollection"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="alias"/> is null.</exception>
    public void Add(SqlColumn column, string alias) => Add(SqlDml.ColumnRef(column, alias));

    /// <summary>
    /// Builds a <see cref="SqlColumnRef"/> by the specified <paramref name="expression"/> and
    /// then adds it to the end of the <see cref="SqlColumnCollection"/>.
    /// </summary>
    public void Add(SqlExpression expression) =>
      base.Add(expression as SqlColumn ?? SqlDml.ColumnRef(SqlDml.Column(expression)));

    /// <summary>
    /// Builds a <see cref="SqlColumnRef"/> by the specified <paramref name="expression"/> and
    /// <paramref name="alias"/>; then adds it to the end of the <see cref="SqlColumnCollection"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
    public void Add(SqlExpression expression, string alias) => Add(SqlDml.ColumnRef(SqlDml.Column(expression), alias));

    /// <summary>
    /// Builds a <see cref="SqlColumnRef"/> by the specified <paramref name="expression"/> and <paramref name="alias"/>
    /// then inserts it into <see cref="SqlColumnCollection"/> at the specified <paramref name="index"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than <c>0</c>.
    /// -or- <paramref name="index"/> is greater than <see cref="Count"/>.</exception>
    public void Insert(int index, SqlExpression expression, string alias) =>
      Insert(index, SqlDml.ColumnRef(SqlDml.Column(expression), alias));

    /// <summary>
    /// Adds <paramref name="columns"/> to the end of the <see cref="SqlColumnCollection"/>.
    /// </summary>
    /// <param name="columns">Columns to be added.</param>
    public void AddRange(params SqlColumn[] columns) => base.AddRange(columns);

    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    public SqlColumnCollection()
      : this([])
    {
    }
  }
}
