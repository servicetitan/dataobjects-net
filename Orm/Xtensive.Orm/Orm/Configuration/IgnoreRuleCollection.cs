// Copyright (C) 2013 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2013.08.16

using System;
using Xtensive.Collections;

namespace Xtensive.Orm.Configuration
{
  /// <summary>
  /// <see cref="IgnoreRule"/> collection
  /// </summary>
  public sealed class IgnoreRuleCollection : CollectionBaseSlim<IgnoreRule>, ICloneable
  {
    /// <summary>
    /// Adds to collectionn <see cref="IgnoreRule"/> for targeted specified <paramref name="tableName"/>.
    /// </summary>
    /// <param name="tableName">Table to ignore</param>
    /// <returns><see cref="IgnoreRule"/> construction flow</returns>
    public IIgnoreRuleConstructionFlow IgnoreTable(string tableName)
    {
      var rule = new IgnoreRule {Table = tableName};
      Add(rule);
      return new IgnoreRuleConstructionFlow(rule);
    }

    /// <summary>
    /// Adds to collection <see cref="IgnoreRule"/> for targeted specified <paramref name="columnName"/>. 
    /// </summary>
    /// <param name="columnName">Column to ignore</param>
    /// <returns><see cref="IgnoreRule"/> construction flow</returns>
    public IIgnoreRuleConstructionFlow IgnoreColumn(string columnName)
    {
      var rule = new IgnoreRule {Column = columnName};
      Add(rule);
      return new IgnoreRuleConstructionFlow(rule);
    }

    /// <summary>
    /// Adds to collection <see cref="IgnoreRule"/> for targeted specified <paramref name="indexName"/>. 
    /// </summary>
    /// <param name="indexName">Index to ignore</param>
    /// <returns><see cref="IgnoreRule"/> construction flow</returns>
    public IIgnoreRuleConstructionFlow IgnoreIndex(string indexName)
    {
      var rule = new IgnoreRule { Index = indexName };
      Add(rule);
      return new IgnoreRuleConstructionFlow(rule);
    }

    /// <inheritdoc />
    public IgnoreRuleCollection Clone()
    {
      var result = new IgnoreRuleCollection();
      foreach (var rule in this)
        result.Add(rule.Clone());
      return result;
    }

    object ICloneable.Clone() => Clone();

    /// <inheritdoc />
    public override void Lock(bool recursive)
    {
      if (recursive)
        foreach (var rule in this)
          rule.Lock(true);
      base.Lock(recursive);
    }
  }
}
