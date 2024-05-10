// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.26

using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;

namespace Xtensive.Orm.Linq.Expressions.Visitors
{
  internal sealed class OwnerRemover : PersistentExpressionVisitor
  {
    public static Expression RemoveOwner(Expression target)
    {
      var remover = new OwnerRemover();
      return remover.Visit(target);
    }

    internal override Expression VisitGroupingExpression(GroupingExpression expression)
    {
      return expression;
    }

    internal override Expression VisitSubQueryExpression(SubQueryExpression expression)
    {
      return expression;
    }

    internal override Expression VisitFieldExpression(FieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal override Expression VisitStructureFieldExpression(StructureFieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal override Expression VisitKeyExpression(KeyExpression expression)
    {
      return expression;
    }

    internal override Expression VisitConstructorExpression(ConstructorExpression expression)
    {
      var oldConstructorArguments = expression.ConstructorArguments;
      var newConstructorArguments = VisitExpressionList(oldConstructorArguments);

      var oldBindings = expression.Bindings.Values.ToArray(expression.Bindings.Count);
      var newBindings = VisitExpressionList(oldBindings);

      var oldNativeBindings = expression.NativeBindings.Select(b => b.Value).ToList().AsReadOnly();
      var newNativeBindings = VisitExpressionList(oldNativeBindings);
      
      var notChanged =
        ReferenceEquals(oldConstructorArguments, newConstructorArguments)
        && ReferenceEquals(oldBindings, newBindings)
        && ReferenceEquals(oldNativeBindings, newNativeBindings);

      if (notChanged)
        return expression;

      var bindings = expression.Bindings
        .Zip(newBindings, (first, second) => (first, second))
        .ToDictionary(item => item.first.Key, item => item.second);
      var nativeBingings = expression.NativeBindings
        .Zip(newNativeBindings, (first, second) => (first, second))
        .ToDictionary(item => item.first.Key, item => item.second);
      return new ConstructorExpression(expression.Type, bindings, nativeBingings, expression.Constructor, newConstructorArguments);
    }

    internal override Expression VisitEntityExpression(EntityExpression expression)
    {
      return expression;
    }

    internal override Expression VisitEntityFieldExpression(EntityFieldExpression expression)
    {
      return expression.RemoveOwner();
    }

    internal override Expression VisitEntitySetExpression(EntitySetExpression expression)
    {
      return expression;
    }

    internal override Expression VisitColumnExpression(ColumnExpression expression)
    {
      return expression;
    }
  }
}