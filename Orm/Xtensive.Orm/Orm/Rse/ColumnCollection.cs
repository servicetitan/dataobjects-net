// Copyright (C) 2007-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2007.09.24

using Xtensive.Core;

namespace Xtensive.Orm.Rse;

/// <summary>
/// Collection of <see cref="Column"/> items.
/// </summary>
[Serializable]
public readonly struct ColumnCollection
{
  private readonly Dictionary<string, int> nameIndex;

  public IReadOnlyList<Column> Columns { get; }

  /// <summary>
  /// Gets the number of <see href="Column"/>s in the collection.
  /// </summary>
  public ColNum Count => (ColNum)Columns.Count;


  /// <summary>
  /// Gets a <see href="Column"/> instance by its index.
  /// </summary>
  public Column this[int index] => Columns[index];

  /// <summary>
  /// Gets <see cref="Column"/> by provided <paramref name="fullName"/>.
  /// </summary>
  /// <remarks>
  /// Returns <see cref="Column"/> if it was found; otherwise <see langword="null"/>.
  /// </remarks>
  /// <param name="fullName">Full name of the <see cref="Column"/> to find.</param>
  public Column this[string fullName] =>
    nameIndex.TryGetValue(fullName, out var index) ? Columns[index] : null;

  /// <summary>
  /// Determines whether the collecton contains specified column
  /// </summary>
  /// <param name="column"></param>
  /// <returns></returns>
  public bool Contains(Column column) =>
    Columns is ICollection<Column> colColumns ? colColumns.Contains(column) : Columns.Contains(column);

  /// <summary>
  /// Joins this collection with specified the column collection.
  /// </summary>
  /// <param name="joined">The joined.</param>
  /// <returns>The joined collection.</returns>
  public ColumnCollection Join(IEnumerable<Column> joined)
  {
    return new ColumnCollection(Columns.Concat(joined).ToList());
  }

  /// <summary>
  /// Aliases the specified <see cref="Column"/> collection.
  /// </summary>
  /// <param name="alias">The alias to add.</param>
  /// <returns>Aliased collection of columns.</returns>
  public ColumnCollection Alias(string alias)
  {
    ArgumentException.ThrowIfNullOrEmpty(alias);
    return new ColumnCollection(Columns.Select(column => column.Clone(alias + "." + column.Name)).ToArray());
  }

  // Constructors

  /// <summary>
  /// Initializes a new instance of this class.
  /// </summary>
  /// <param name="columns">Collection of items to add.</param>
  /// <remarks>
  /// <paramref name="columns"/> is used to initialize inner field directly
  /// to save time on avoiding collection copy. If you pass an <see cref="IReadOnlyList{Column}"/>
  /// implementor that, in fact, can be changed, make sure the passed collection doesn't change afterwards.
  /// Ideally, use arrays instead of <see cref="List{T}"/> or similar collections.
  /// Changing the passed collection afterwards will lead to unpredictable results.
  /// </remarks>
  public ColumnCollection(IReadOnlyList<Column> columns)
  {
    //!!! Direct initialization by parameter is unsafe performance optimization: See remarks in ctor summary!
    Columns = columns;
    var count = Columns.Count;
    nameIndex = new Dictionary<string, int>(count);
    for (var index = count; index-- > 0;) {
      nameIndex.Add(Columns[index].Name, index);
    }
  }
}
