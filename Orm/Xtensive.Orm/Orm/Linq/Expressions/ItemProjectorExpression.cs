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
using Xtensive.Orm.Linq.Expressions.Visitors;
using Xtensive.Orm.Linq.Materialization;
using Xtensive.Orm.Linq.Rewriters;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Linq.Expressions
{
  internal sealed class ItemProjectorExpression : ExtendedExpression
  {
    public CompilableProvider DataSource { get; set; }
    public TranslatorContext Context { get; }
    public Expression Item { get; }
    public AggregateType? AggregateType { get; }

    public bool IsPrimitive => CheckItemIsPrimitive(Item);

    private static bool CheckItemIsPrimitive(Expression item)
    {
      if (!(item.StripCasts() is ExtendedExpression extendedItem)) {
        return false;
      }

      switch (extendedItem.ExtendedType) {
        case ExtendedExpressionType.Column:
        case ExtendedExpressionType.Field:
          return true;
        case ExtendedExpressionType.Marker:
          var marker = (MarkerExpression) extendedItem;
          return CheckItemIsPrimitive(marker.Target);
        default:
          return false;
      }
    }

    public IReadOnlyList<ColNum> GetColumns(ColumnExtractionModes columnExtractionModes) =>
      ColumnGatherer.GetColumns(Item, columnExtractionModes);

    public IReadOnlyList<Pair<ColNum, Expression>> GetColumnsAndExpressions(ColumnExtractionModes columnExtractionModes) =>
      ColumnGatherer.GetColumnsAndExpressions(Item, columnExtractionModes);

    public ItemProjectorExpression Remap(CompilableProvider dataSource, ColNum offset)
    {
      if (offset == 0) {
        return new ItemProjectorExpression(Item, dataSource, Context, AggregateType);
      }

      var item = GenericExpressionVisitor<IMappedExpression>
        .Process(Item, mapped => mapped.Remap(offset, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, dataSource, Context, AggregateType);
    }

    public ItemProjectorExpression Remap(CompilableProvider dataSource, ColumnMap columnMap)
    {
      var item = GenericExpressionVisitor<IMappedExpression>
        .Process(Item, mapped => mapped.Remap(columnMap, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, dataSource, Context, AggregateType);
    }

    public LambdaExpression ToLambda(TranslatorContext context) => ExpressionMaterializer.MakeLambda(Item, context);

    public MaterializationInfo Materialize(TranslatorContext context, IReadOnlySet<Parameter<Tuple>> tupleParameters) =>
      ExpressionMaterializer.MakeMaterialization(this, context, tupleParameters);

    public ItemProjectorExpression BindOuterParameter(ParameterExpression parameter)
    {
      var item = GenericExpressionVisitor<IMappedExpression>
        .Process(Item, mapped => mapped.BindParameter(parameter, new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, DataSource, Context, AggregateType);
    }

    public ItemProjectorExpression RemoveOuterParameter()
    {
      var item = GenericExpressionVisitor<IMappedExpression>
        .Process(Item, mapped => mapped.RemoveOuterParameter(new Dictionary<Expression, Expression>()));
      return new ItemProjectorExpression(item, DataSource, Context, AggregateType);
    }

    public ItemProjectorExpression RemoveOwner()
    {
      var item = OwnerRemover.RemoveOwner(Item);
      return new ItemProjectorExpression(item, DataSource, Context, AggregateType);
    }

    public ItemProjectorExpression SetDefaultIfEmpty()
    {
      var item = GenericExpressionVisitor<ParameterizedExpression>.Process(Item, mapped => {
        mapped.DefaultIfEmpty = true;
        return mapped;
      });
      return new ItemProjectorExpression(item, DataSource, Context, AggregateType);
    }

    public ItemProjectorExpression RewriteApplyParameter(ApplyParameter oldParameter, ApplyParameter newParameter)
    {
      var newDataSource = ApplyParameterRewriter.Rewrite(DataSource, oldParameter, newParameter);
      var newItemProjectorBody = ApplyParameterRewriter.Rewrite(Item, oldParameter, newParameter);
      return new ItemProjectorExpression(newItemProjectorBody, newDataSource, Context, AggregateType);
    }

    public ItemProjectorExpression EnsureEntityIsJoined()
    {
      var dataSource = DataSource;
      var newItem = new ExtendedExpressionReplacer(e => {
        if (e is EntityExpression entityExpression) {
          var typeInfo = entityExpression.PersistentType;

          if (entityExpression.Fields.Select(o => o.Name).ToHashSet(StringComparer.Ordinal).IsSupersetOf(typeInfo.Fields.Select(o => o.Name))) {
            return entityExpression;
          }

          var joinedIndex = typeInfo.Indexes.PrimaryIndex;
          var joinedRs = joinedIndex.GetQuery().Alias(Context.GetNextAlias());
          var keySegment = entityExpression.Key.Mapping;
          var keyPairs = new (ColNum Left, ColNum Right)[keySegment.Length];
          ColNum rightIndex = 0;
          foreach (var leftIndex in keySegment.GetItems()) {
            keyPairs[rightIndex] = (leftIndex, rightIndex);
            rightIndex++;
          }
          var offset = dataSource.Header.Length;
          dataSource = entityExpression.IsNullable
            || (dataSource is JoinProvider dataSourceAsJoin && dataSourceAsJoin.JoinType == JoinType.LeftOuter)
              ? dataSource.LeftJoin(joinedRs, keyPairs)
              : dataSource.Join(joinedRs, keyPairs);
          EntityExpression.Fill(entityExpression, offset);
          return entityExpression;
        }

        if (e is EntityFieldExpression entityFieldExpression) {
          if (entityFieldExpression.Entity != null) {
            return entityFieldExpression.Entity;
          }

          var typeInfo = entityFieldExpression.PersistentType;
          var joinedIndex = typeInfo.Indexes.PrimaryIndex;
          var joinedRs = joinedIndex.GetQuery().Alias(Context.GetNextAlias());
          var keySegment = entityFieldExpression.Mapping;
          var keyPairs = new (ColNum Left, ColNum Right)[keySegment.Length];
          ColNum rightIndex = 0;
          foreach (var leftIndex in keySegment.GetItems()) {
            keyPairs[rightIndex] = (leftIndex, rightIndex);
            rightIndex++;
          }
          var offset = dataSource.Header.Length;
          dataSource = entityFieldExpression.IsNullable
            || (dataSource is JoinProvider dataSourceAsJoin && dataSourceAsJoin.JoinType == JoinType.LeftOuter)
              ? dataSource.LeftJoin(joinedRs, keyPairs)
              : dataSource.Join(joinedRs, keyPairs);
          entityFieldExpression.RegisterEntityExpression(offset);
          return entityFieldExpression.Entity;
        }

        if (e is FieldExpression fe && fe.ExtendedType == ExtendedExpressionType.Field) {
          return fe.RemoveOwner();
        }

        return null;
      })
        .Replace(Item);
      return new ItemProjectorExpression(newItem, dataSource, Context, AggregateType);
    }

    public override string ToString() =>
      $"ItemProjectorExpression: IsPrimitive = {IsPrimitive} Item = {Item}, DataSource = {DataSource}";

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitItemProjectorExpression(this);

    // Constructors

    public ItemProjectorExpression(
      Expression expression, CompilableProvider dataSource, TranslatorContext context,
      AggregateType? aggregateType = default)
      : base(ExtendedExpressionType.ItemProjector, expression.Type)
    {
      DataSource = dataSource;
      Context = context;
      var newApplyParameter = Context.GetApplyParameter(dataSource);
      var applyParameterReplacer = new ExtendedExpressionReplacer(ex =>
        ex is SubQueryExpression queryExpression
          ? queryExpression.ReplaceApplyParameter(newApplyParameter)
          : null);
      Item = applyParameterReplacer.Replace(expression);
      AggregateType = aggregateType;
    }
  }
}