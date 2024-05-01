// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.05.18

using System.Collections.Generic;
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

      List<SqlTable> tables = new();
      List<SqlJoinType> joinTypes = new();
      List<SqlExpression> conditions = new();

      foreach (var item in joins) {
        var left = item.Left;
        if (!(left is SqlJoinedTable))
          tables.Add(left);
        var right = item.Right;
        if (!(right is SqlJoinedTable))
          tables.Add(right);
        joinTypes.Add(item.JoinType);
        conditions.Add(item.Expression);
      }

      var pivot = tables[0];
      tables.RemoveAt(0);
      return new(pivot, tables, joinTypes, conditions);
    }

    private static void Traverse(SqlJoinedTable root, List<SqlJoinExpression> output)
    {
      var joinExpression = root.JoinExpression;
      if (joinExpression.Left is SqlJoinedTable joinedLeft) {
        Traverse(joinedLeft, output);
      }

      output.Add(joinExpression);

      if (joinExpression.Right is SqlJoinedTable joinedRight) {
        Traverse(joinedRight, output);
      }
    }
  }
}