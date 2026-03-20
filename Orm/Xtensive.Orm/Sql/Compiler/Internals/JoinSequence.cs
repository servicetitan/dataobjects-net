// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.05.18

using Xtensive.Sql.Dml;

namespace Xtensive.Sql.Compiler
{
  internal readonly record struct JoinSequence
  (
    SqlTable Pivot,
    IReadOnlyList<SqlTable> Tables,
    IReadOnlyList<SqlJoinType> JoinTypes,
    IReadOnlyList<SqlExpression> Conditions
  )
  {
    public static JoinSequence Build(SqlJoinedTable root)
    {
      var joins = new List<SqlJoinExpression>(1);
      Traverse(root, joins);

      var tables = new List<SqlTable>();
      var joinTypes = new List<SqlJoinType>(joins.Count);
      var conditions = new List<SqlExpression>(joins.Count);

      foreach (var item in joins) {
        if (item.Left is not SqlJoinedTable)
          tables.Add(item.Left);
        if (item.Right is not SqlJoinedTable)
          tables.Add(item.Right);
        joinTypes.Add(item.JoinType);
        conditions.Add(item.Expression);
      }

      var pivot = tables[0];
      tables.RemoveAt(0);
      return new JoinSequence(pivot, tables, joinTypes, conditions);
    }

    private static void Traverse(SqlJoinedTable root, List<SqlJoinExpression> output)
    {
      var joinExpression = root.JoinExpression;
      var left = joinExpression.Left;
      if (left is SqlJoinedTable joinedLeft)
        Traverse(joinedLeft, output);

      output.Add(joinExpression);

      var right = root.JoinExpression.Right;
      if (right is SqlJoinedTable joinedRight)
        Traverse(joinedRight, output);
    }
  }
}
