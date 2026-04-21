// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using System.Collections.Generic;

namespace Xtensive.Sql.Dml
{
  /// <summary>
  /// Represents collection of <see cref="SqlOrder"/>s.
  /// </summary>
  // Inheriting from List<T> (instead of Collection<T>) so that 'foreach (var o in collection)'
  // resolves to List<T>.GetEnumerator() and uses the struct enumerator. Collection<T> only
  // exposes IEnumerator<T> through its IList<T>/IEnumerable<T> interface implementations,
  // which forces the C# foreach pattern to allocate a boxed enumerator on every iteration —
  // and this collection is iterated on every SQL select compile.
  [Serializable]
  public class SqlOrderCollection : List<SqlOrder>
  {
    public void Add(SqlExpression expression)
    {
      Add(SqlDml.Order(expression));
    }

    public void Add(SqlExpression expression, bool ascending)
    {
      Add(SqlDml.Order(expression, ascending));
    }
    
    public void Add(int position)
    {
      Add(SqlDml.Order(position));
    }

    public void Add(int position, bool ascending)
    {
      Add(SqlDml.Order(position, ascending));
    }
  }
}
