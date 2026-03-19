// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;

namespace Xtensive.Orm.Linq.Expressions.Visitors
{
  internal sealed class OwnerRemover : PersistentExpressionVisitor
  {
    private static readonly OwnerRemover Instance = new();

    public static Expression RemoveOwner(Expression target) =>
      Instance.Visit(target);

    internal protected override GroupingExpression VisitGroupingExpression(GroupingExpression expression)
    {
      return expression;
    }

    internal protected override SubQueryExpression VisitSubQueryExpression(SubQueryExpression expression)
    {
      return expression;
    }

    internal protected override FieldExpression VisitFieldExpression(FieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal protected override FieldExpression VisitStructureFieldExpression(StructureFieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal protected override KeyExpression VisitKeyExpression(KeyExpression expression)
    {
      return expression;
    }

    internal protected override ConstructorExpression VisitConstructorExpression(ConstructorExpression expression)
    {
      IReadOnlyList<Expression> oldConstructorArguments;
      IReadOnlyList<Expression> newConstructorArguments;

      if (ReferenceEquals(expression.ConstructorArguments, Array.Empty<Expression>())) {
        oldConstructorArguments = newConstructorArguments = Array.Empty<Expression>();
      }
      else if (expression.ConstructorArguments is IReadOnlyList<Expression> argsAsList) {
        oldConstructorArguments = argsAsList;
        newConstructorArguments = VisitExpressionList(argsAsList); // creates a copy internally
      }
      else {
        oldConstructorArguments = expression.ConstructorArguments.ToList();
        if (oldConstructorArguments.Count == 0)
          oldConstructorArguments = newConstructorArguments = Array.Empty<Expression>();
        else
          newConstructorArguments = VisitExpressionList(oldConstructorArguments);
      }

      var oldBindings = expression.Bindings.Values.ToArray();
      var newBindings = VisitExpressionList(oldBindings);

      var oldNativeBindings = expression.NativeBindings.Select(b => b.Value).ToArray().AsSafeWrapper();
      var newNativeBindings = VisitExpressionList(oldNativeBindings);
      
      var notChanged =
        ReferenceEquals(oldConstructorArguments, newConstructorArguments)
        && ReferenceEquals(oldBindings, newBindings)
        && ReferenceEquals(oldNativeBindings, newNativeBindings);

      if (notChanged)
        return expression;

      var bindings = expression.Bindings
        .Zip(newBindings)
        .ToDictionary(item => item.First.Key, item => item.Second);
      var nativeBingings = expression.NativeBindings
        .Zip(newNativeBindings)
        .ToDictionary(item => item.First.Key, item => item.Second);
      return new ConstructorExpression(expression.Type, bindings, nativeBingings, expression.Constructor, newConstructorArguments.ToReadOnlyList());
    }

    internal protected override EntityExpression VisitEntityExpression(EntityExpression expression)
    {
      return expression;
    }

    internal protected override FieldExpression VisitEntityFieldExpression(EntityFieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal protected override EntitySetExpression VisitEntitySetExpression(EntitySetExpression expression)
    {
      return expression;
    }

    internal protected override ColumnExpression VisitColumnExpression(ColumnExpression expression)
    {
      return expression;
    }

    private OwnerRemover() { }
  }
}
