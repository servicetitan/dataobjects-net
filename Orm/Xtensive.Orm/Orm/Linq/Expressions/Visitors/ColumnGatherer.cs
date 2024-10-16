// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.05.06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Orm.Rse.Providers;

namespace Xtensive.Orm.Linq.Expressions.Visitors
{
  internal sealed class ColumnGatherer : PersistentExpressionVisitor
  {
    private readonly ColumnExtractionModes columnExtractionModes;
    private readonly List<Pair<ColNum, Expression>> columns = new();
    private SubQueryExpression topSubquery;

    private bool TreatEntityAsKey
    {
      get { return (columnExtractionModes & ColumnExtractionModes.TreatEntityAsKey)!=ColumnExtractionModes.Default; }
    }

    private bool KeepTypeId
    {
      get { return (columnExtractionModes & ColumnExtractionModes.KeepTypeId)!=ColumnExtractionModes.Default; }
    }

    private bool DistinctValues
    {
      get { return (columnExtractionModes & ColumnExtractionModes.Distinct)!=ColumnExtractionModes.Default; }
    }

    private bool OrderedValues
    {
      get { return (columnExtractionModes & ColumnExtractionModes.Ordered)!=ColumnExtractionModes.Default; }
    }

    private bool OmitLazyLoad
    {
      get { return (columnExtractionModes & ColumnExtractionModes.OmitLazyLoad)!=ColumnExtractionModes.Default; }
    }

    public static IReadOnlyList<Pair<ColNum, Expression>> GetColumnsAndExpressions(Expression expression, ColumnExtractionModes columnExtractionModes)
    {
      var gatherer = new ColumnGatherer(columnExtractionModes);
      gatherer.Visit(expression);
      var distinct = gatherer.DistinctValues
        ? gatherer.columns.Distinct()
        : gatherer.columns;
      var ordered = gatherer.OrderedValues
        ? distinct.OrderBy(i => i)
        : distinct;
      return ordered.ToArray();
    }
    
    public static IReadOnlyList<ColNum> GetColumns(Expression expression, ColumnExtractionModes columnExtractionModes)
    {
      var gatherer = new ColumnGatherer(columnExtractionModes);
      gatherer.Visit(expression);
      var distinct = gatherer.DistinctValues
        ? gatherer.columns.Select(p=>p.First).Distinct()
        : gatherer.columns.Select(p=>p.First);
      var ordered = gatherer.OrderedValues
        ? distinct.OrderBy(i => i)
        : distinct;
      return ordered.ToArray();
    }

    internal override Expression VisitMarker(MarkerExpression expression)
    {
      Visit(expression.Target);
      return expression;
    }

    internal override Expression VisitFieldExpression(FieldExpression f)
    {
      ProcessFieldOwner(f);
      AddColumns(f, f.Mapping.GetItems());
      return f;
    }

    internal override Expression VisitStructureFieldExpression(StructureFieldExpression s)
    {
      ProcessFieldOwner(s);
      AddColumns(s,
        s.Fields
          .Where(f => f.ExtendedType==ExtendedExpressionType.Field)
          .Select(f => f.Mapping.Offset));
      return s;
    }

    internal override Expression VisitKeyExpression(KeyExpression k)
    {
      AddColumns(k, k.Mapping.GetItems());
      return k;
    }

    internal override Expression VisitEntityExpression(EntityExpression e)
    {
      if (TreatEntityAsKey) {
        var keyExpression = (KeyExpression) e.Fields.First(f => f.ExtendedType==ExtendedExpressionType.Key);
        AddColumns(e, keyExpression.Mapping.GetItems());
        if (KeepTypeId)
          AddColumns(e, e.Fields.First(f => f.Name==WellKnown.TypeIdFieldName).Mapping.GetItems());
      }
      else {
        AddColumns(e,
          e.Fields
            .OfType<FieldExpression>()
            .Where(f => f.ExtendedType==ExtendedExpressionType.Field)
            .Where(f => !(OmitLazyLoad && f.Field.IsLazyLoad))
            .Select(f => f.Mapping.Offset));
      }
      return e;
    }

    internal override Expression VisitEntityFieldExpression(EntityFieldExpression ef)
    {
      var keyExpression = (KeyExpression) ef.Fields.First(f => f.ExtendedType==ExtendedExpressionType.Key);
      AddColumns(ef, keyExpression.Mapping.GetItems());
      if (!TreatEntityAsKey)
        Visit(ef.Entity);
      return ef;
    }

    internal override Expression VisitEntitySetExpression(EntitySetExpression es)
    {
      VisitEntityExpression((EntityExpression) es.Owner);
      return es;
    }

    internal override Expression VisitColumnExpression(ColumnExpression c)
    {
      AddColumns(c, c.Mapping.GetItems());
      return c;
    }

    internal override Expression VisitStructureExpression(StructureExpression expression)
    {
      AddColumns(expression,
        expression.Fields
          .Where(fieldExpression => fieldExpression.ExtendedType==ExtendedExpressionType.Field)
          .Select(fieldExpression => fieldExpression.Mapping.Offset));
      return expression;
    }

    internal override Expression VisitGroupingExpression(GroupingExpression expression)
    {
      Visit(expression.KeyExpression);
      VisitSubQueryExpression(expression);
      return expression;
    }

    internal override Expression VisitSubQueryExpression(SubQueryExpression subQueryExpression)
    {
      bool isTopSubquery = false;

      if (topSubquery==null) {
        isTopSubquery = true;
        topSubquery = subQueryExpression;
      }

      Visit(subQueryExpression.ProjectionExpression.ItemProjector.Item);
      var visitor = new ApplyParameterAccessVisitor(topSubquery.ApplyParameter, (mc, index) => {
        columns.Add(new Pair<ColNum, Expression>(index, mc));
        return mc;
      });
      var providerVisitor = new CompilableProviderVisitor((provider, expression) => visitor.Visit(expression));
      providerVisitor.VisitCompilable(subQueryExpression.ProjectionExpression.ItemProjector.DataSource);

      if (isTopSubquery)
        topSubquery = null;

      return subQueryExpression;
    }

    internal override Expression VisitLocalCollectionExpression(LocalCollectionExpression expression)
    {
      foreach (var field in expression.Fields)
        Visit((Expression) field.Value);
      return expression;
    }

    internal override Expression VisitConstructorExpression(ConstructorExpression expression)
    {
      foreach (var binding in expression.Bindings)
        Visit(binding.Value);
      foreach (var argument in expression.ConstructorArguments)
        Visit(argument);
      return base.VisitConstructorExpression(expression);
    }

    private void ProcessFieldOwner(FieldExpression fieldExpression)
    {
      if (TreatEntityAsKey || fieldExpression.Owner==null)
        return;
      var entity = fieldExpression.Owner as EntityExpression;
      var structure = fieldExpression.Owner as StructureFieldExpression;
      while (entity==null && structure!=null) {
        entity = structure.Owner as EntityExpression;
        structure = structure.Owner as StructureFieldExpression;
      }
      if (entity==null)
        throw new InvalidOperationException(String.Format(Strings.ExUnableToResolveOwnerOfFieldExpressionX, fieldExpression));

      AddColumns(fieldExpression,
        entity
          .Key
          .Mapping
          .GetItems()
          .Append(entity
            .Fields
            .Single(field => field.Name==WellKnown.TypeIdFieldName)
            .Mapping
            .Offset));
    }

    private void AddColumns(ParameterizedExpression parameterizedExpression, IEnumerable<ColNum> expressionColumns)
    {
      var isSubqueryParameter = topSubquery!=null && parameterizedExpression.OuterParameter==topSubquery.OuterParameter;
      var isNotParametrized = topSubquery==null && parameterizedExpression.OuterParameter==null;

      if (isSubqueryParameter || isNotParametrized)
        columns.AddRange(expressionColumns.Select(i=>new Pair<ColNum, Expression>(i, parameterizedExpression)));
    }

    internal override Expression VisitFullTextExpression(FullTextExpression expression)
    {
      VisitEntityExpression(expression.EntityExpression);
      VisitColumnExpression(expression.RankExpression);
      return expression;
    }

    // Constructors

    private ColumnGatherer(ColumnExtractionModes columnExtractionModes)
    {
      this.columnExtractionModes = columnExtractionModes;
    }
  }
}