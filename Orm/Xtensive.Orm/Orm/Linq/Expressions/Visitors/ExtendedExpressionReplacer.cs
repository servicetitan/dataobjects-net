// Copyright (C) 2009-2026 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.04.27

using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Orm.Rse.Providers;

namespace Xtensive.Orm.Linq.Expressions.Visitors
{
  internal sealed class ExtendedExpressionReplacer : PersistentExpressionVisitor
  {
    private readonly Func<Expression, Expression> replaceDelegate;
    private readonly CompilableProviderVisitor providerVisitor;

    public Expression Replace(Expression e)
    {
      return Visit(e);
    }

    public override Expression Visit(Expression e)
    {
      if (e==null)
        return null;
      var result = replaceDelegate(e);
      return result ?? base.Visit(e);
    }

    internal protected override ProjectionExpression VisitProjectionExpression(ProjectionExpression projectionExpression)
    {
      var item = Visit(projectionExpression.ItemProjector.Item);
      var provider = providerVisitor.VisitCompilable(projectionExpression.ItemProjector.DataSource);
      var providerChanged = provider != projectionExpression.ItemProjector.DataSource;
      var itemChanged = item != projectionExpression.ItemProjector.Item;
      if (providerChanged || itemChanged) {
        var itemProjector = new ItemProjectorExpression(item, provider, projectionExpression.ItemProjector.Context);
        return projectionExpression.ApplyItemProjector(itemProjector);
      }
      return projectionExpression;
    }

    internal protected override GroupingExpression VisitGroupingExpression(GroupingExpression expression)
    {
      var keyExpression = Visit(expression.KeyExpression);
      if (keyExpression!=expression.KeyExpression)
        return new GroupingExpression(
          expression.Type,
          expression.OuterParameter,
          expression.DefaultIfEmpty,
          expression.ProjectionExpression,
          expression.ApplyParameter,
          keyExpression,
          expression.SelectManyInfo);
      return expression;
    }

    internal protected override FullTextExpression VisitFullTextExpression(FullTextExpression expression)
    {
      var rankExpression = (ColumnExpression) Visit(expression.RankExpression);
      var entityExpression = (EntityExpression) Visit(expression.EntityExpression);
      if (rankExpression!=expression.RankExpression || entityExpression!=expression.EntityExpression)
        return new FullTextExpression(expression.FullTextIndex, entityExpression, rankExpression, expression.OuterParameter);
      return expression;
    }

    internal protected override SubQueryExpression VisitSubQueryExpression(SubQueryExpression expression)
    {
      return expression;
    }

    private Expression TranslateExpression(CompilableProvider provider, Expression original)
    {
      var result = Visit(original);
      return result ?? original;
    }

    internal protected override FieldExpression VisitFieldExpression(FieldExpression expression)
    {
      return expression;
    }

    internal protected override StructureFieldExpression VisitStructureFieldExpression(StructureFieldExpression expression)
    {
      return expression;
    }

    internal protected override KeyExpression VisitKeyExpression(KeyExpression expression)
    {
      return expression;
    }

    internal protected override EntityExpression VisitEntityExpression(EntityExpression expression)
    {
      return expression;
    }

    internal protected override EntityFieldExpression VisitEntityFieldExpression(EntityFieldExpression expression)
    {
      return expression;
    }

    internal protected override EntitySetExpression VisitEntitySetExpression(EntitySetExpression expression)
    {
      return expression;
    }

    internal protected override ColumnExpression VisitColumnExpression(ColumnExpression expression)
    {
      return expression;
    }

    internal protected override ConstructorExpression VisitConstructorExpression(ConstructorExpression expression)
    {
      IList<Expression> arguments = new List<Expression>();
      var bindings = new Dictionary<MemberInfo, Expression>(expression.Bindings.Count);
      var nativeBindings = new Dictionary<MemberInfo, Expression>(expression.NativeBindings.Count);
      bool recreate = false;
      var arguments = new Expression[expression.ConstructorArguments.Count];
      int i = 0;
      foreach (var argument in expression.ConstructorArguments) {
        var result = Visit(argument);
        recreate |= (result != argument);
        arguments[i++] = result;
      }
      foreach (var binding in expression.Bindings) {
        var result = Visit(binding.Value);
        recreate |= (result != binding.Value);
        bindings.Add(binding.Key, result);
      }
      foreach (var nativeBinding in expression.NativeBindings) {
        var result = Visit(nativeBinding.Value);
        recreate |= (result != nativeBinding.Value);
        nativeBindings.Add(nativeBinding.Key, result);
      }
      if (!recreate)
        return expression;
      return new ConstructorExpression(
        expression.Type,
        bindings,
        nativeBindings,
        expression.Constructor,
        arguments.Count > 0 ? arguments : Array.Empty<Expression>());
    }

    internal protected override MarkerExpression VisitMarker(MarkerExpression expression)
    {
      var target = Visit(expression.Target);
      return target == expression.Target 
        ? expression 
        : new MarkerExpression(target, expression.MarkerType);
    }

    // Constructors

    public ExtendedExpressionReplacer(Func<Expression, Expression> replaceDelegate)
    {
      this.replaceDelegate = replaceDelegate;
      providerVisitor = new CompilableProviderVisitor(TranslateExpression);
    }
  }
}
