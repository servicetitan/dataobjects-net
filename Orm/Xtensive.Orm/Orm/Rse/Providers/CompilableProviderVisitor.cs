// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.10

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Collections;
using Xtensive.Core;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// <see cref="CompilableProvider"/> visitor class. Result is <see cref="CompilableProvider"/>.
  /// </summary>
  [Serializable]
  public class CompilableProviderVisitor
  {
    protected Func<CompilableProvider, Expression, Expression> translate;

    /// <summary>
    /// Visits the compilable provider.
    /// </summary>
    /// <param name="cp">The compilable provider.</param>
    public CompilableProvider VisitCompilable(CompilableProvider cp) => Visit(cp);

    /// <summary>
    /// Visits the specified <paramref name="cp"/>.
    /// </summary>
    /// <param name="cp">The <see cref="CompilableProvider"/> to visit.</param>
    /// <returns>Visit result.</returns>
    protected virtual CompilableProvider Visit(CompilableProvider cp) =>
      cp == null
        ? null
        : cp.Type switch {
          ProviderType.Index => VisitIndex((IndexProvider) cp),
          ProviderType.Store => VisitStore((StoreProvider) cp),
          ProviderType.Aggregate => VisitAggregate((AggregateProvider) cp),
          ProviderType.Alias => VisitAlias((AliasProvider) cp),
          ProviderType.Calculate => VisitCalculate((CalculateProvider) cp),
          ProviderType.Distinct => VisitDistinct((DistinctProvider) cp),
          ProviderType.Filter => VisitFilter((FilterProvider) cp),
          ProviderType.Join => VisitJoin((JoinProvider) cp),
          ProviderType.Sort => VisitSort((SortProvider) cp),
          ProviderType.Raw => VisitRaw((RawProvider) cp),
          ProviderType.Seek => VisitSeek((SeekProvider) cp),
          ProviderType.Select => VisitSelect((SelectProvider) cp),
          ProviderType.Tag => VisitTag((TagProvider) cp),
          ProviderType.Skip => VisitSkip((SkipProvider) cp),
          ProviderType.Take => VisitTake((TakeProvider) cp),
          ProviderType.Paging => VisitPaging((PagingProvider) cp),
          ProviderType.RowNumber => VisitRowNumber((RowNumberProvider) cp),
          ProviderType.Apply => VisitApply((ApplyProvider) cp),
          ProviderType.Existence => VisitExistence((ExistenceProvider) cp),
          ProviderType.PredicateJoin => VisitPredicateJoin((PredicateJoinProvider) cp),
          ProviderType.Intersect => VisitIntersect((IntersectProvider) cp),
          ProviderType.Except => VisitExcept((ExceptProvider) cp),
          ProviderType.Concat => VisitConcat((ConcatProvider) cp),
          ProviderType.Union => VisitUnion((UnionProvider) cp),
          ProviderType.Lock => VisitLock((LockProvider) cp),
          ProviderType.Include => VisitInclude((IncludeProvider) cp),
          ProviderType.FreeText => VisitFreeText((FreeTextProvider) cp),
          ProviderType.ContainsTable => VisitContainsTable((ContainsTableProvider) cp),
          ProviderType.Void => throw new NotSupportedException(Strings.ExProcessingOfVoidProviderIsNotSupported),
          _ => throw new ArgumentOutOfRangeException()
        };

    /// <summary>
    /// Visits <see cref="TakeProvider"/>.
    /// </summary>
    /// <param name="provider">Take provider.</param>
    protected virtual CompilableProvider VisitTake(TakeProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new TakeProvider(source, provider.Count);
    }

    /// <summary>
    /// Visits <see cref="SkipProvider"/>.
    /// </summary>
    /// <param name="provider">Skip provider.</param>
    protected virtual CompilableProvider VisitSkip(SkipProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new SkipProvider(source, provider.Count);
    }

    /// <summary>
    /// Visits <see cref="PagingProvider"/>.
    /// </summary>
    /// <param name="provider">Paging provider.</param>
    protected virtual CompilableProvider VisitPaging(PagingProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new PagingProvider(source, provider);
    }

    /// <summary>
    /// Visits <see cref="SelectProvider"/>.
    /// </summary>
    /// <param name="provider">Select provider.</param>
    protected virtual CompilableProvider VisitSelect(SelectProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      var columnIndexes = (int[])OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new SelectProvider(source, columnIndexes ?? provider.ColumnIndexes);
    }

    /// <summary>
    /// Visits <see cref="TagProvider"/>.
    /// </summary>
    /// <param name="provider">Tag provider.</param>
    protected virtual CompilableProvider VisitTag(TagProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new TagProvider(source, provider.Tag);
    }

    /// <summary>
    /// Visits <see cref="SeekProvider"/>.
    /// </summary>
    /// <param name="provider">Seek provider.</param>
    protected virtual CompilableProvider VisitSeek(SeekProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source==provider.Source)
        return provider;
      return new SeekProvider(source, provider.Key);
    }

    /// <summary>
    /// Visits <see cref="RawProvider"/>.
    /// </summary>
    /// <param name="provider">Raw provider.</param>
    protected virtual CompilableProvider VisitRaw(RawProvider provider)
    {
      return provider;
    }

    /// <summary>
    /// Visits <see cref="SortProvider"/>.
    /// </summary>
    /// <param name="provider">Sort provider.</param>
    protected virtual CompilableProvider VisitSort(SortProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      var order = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new SortProvider(source, (order == null) ? provider.Order : (DirectionCollection<int>)order);
    }

    /// <summary>
    /// Visits <see cref="JoinProvider"/>.
    /// </summary>
    /// <param name="provider">Join provider.</param>
    protected virtual CompilableProvider VisitJoin(JoinProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      var equalIndexes = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new JoinProvider(left, right, provider.JoinType,
        equalIndexes != null ? (Pair<int>[])equalIndexes : provider.EqualIndexes);
    }

    /// <summary>
    /// Visits <see cref="FilterProvider"/>.
    /// </summary>
    /// <param name="provider">Filter provider.</param>
    protected virtual CompilableProvider VisitFilter(FilterProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      var predicate = translate(provider, provider.Predicate);
      if (source == provider.Source && predicate == provider.Predicate)
        return provider;
      return new FilterProvider(source, (Expression<Func<Tuple, bool>>) predicate);
    }

    /// <summary>
    /// Visits <see cref="DistinctProvider"/>.
    /// </summary>
    /// <param name="provider">Distinct provider.</param>
    protected virtual CompilableProvider VisitDistinct(DistinctProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new DistinctProvider(source);
    }

    /// <summary>
    /// Visits <see cref="CalculateProvider"/>.
    /// </summary>
    /// <param name="provider">Calculate provider.</param>
    protected virtual CompilableProvider VisitCalculate(CalculateProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      var translated = false;
      var descriptors = new List<CalculatedColumnDescriptor>(provider.CalculatedColumns.Length);
      foreach (var column in provider.CalculatedColumns) {
        var expression = translate(provider, column.Expression);
        if (expression != column.Expression)
          translated = true;
        var ccd = new CalculatedColumnDescriptor(column.Name, column.Type, (Expression<Func<Tuple, object>>) expression);
        descriptors.Add(ccd);
      }
      if (!translated && source == provider.Source)
        return provider;
      return new CalculateProvider(source, descriptors.ToArray());
    }

    /// <summary>
    /// Visits <see cref="RowNumberProvider"/>.
    /// </summary>
    /// <param name="provider">Row number provider.</param>
    protected virtual CompilableProvider VisitRowNumber(RowNumberProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new RowNumberProvider(source, provider.SystemColumn.Name);
    }


    /// <summary>
    /// Visits <see cref="AliasProvider"/>.
    /// </summary>
    /// <param name="provider">Alias provider.</param>
    protected virtual CompilableProvider VisitAlias(AliasProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new AliasProvider(source, provider.Alias);
    }

    /// <summary>
    /// Visits <see cref="AggregateProvider"/>.
    /// </summary>
    /// <param name="provider">Aggregate provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitAggregate(AggregateProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      var resultParameters = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      if (resultParameters == null) {
        var acd = new List<AggregateColumnDescriptor>(provider.AggregateColumns.Length);
        acd.AddRange(provider.AggregateColumns.Select(ac => new AggregateColumnDescriptor(ac.Name, ac.SourceIndex, ac.AggregateType)));
        return new AggregateProvider(source, provider.GroupColumnIndexes, acd.ToArray());
      }
      var result = (Pair<int[], AggregateColumnDescriptor[]>) resultParameters;
      return new AggregateProvider(source, result.First, result.Second);
    }

    /// <summary>
    /// Visits <see cref="StoreProvider"/>.
    /// </summary>
    /// <param name="provider">Store provider.</param>
    protected virtual CompilableProvider VisitStore(StoreProvider provider)
    {
      var compilableSource = provider.Source;
      OnRecursionEntrance(provider);
      var source = VisitCompilable(compilableSource);
      _ = OnRecursionExit(provider);
      if (source == compilableSource)
        return provider;
      return new StoreProvider(source, provider.Name);
    }

    /// <summary>
    /// Visits <see cref="IndexProvider"/>.
    /// </summary>
    /// <param name="provider">Index provider.</param>
    protected virtual CompilableProvider VisitIndex(IndexProvider provider)
    {
      OnRecursionEntrance(provider);
      _ = OnRecursionExit(provider);
      return provider;
    }

    /// <summary>
    /// Visits the <see cref="FreeTextProvider"/>.
    /// </summary>
    /// <param name="provider">FreeText provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitFreeText(FreeTextProvider provider)
    {
      OnRecursionEntrance(provider);
      _ = OnRecursionExit(provider);
      return provider;
    }

    /// <summary>
    /// Visits the <see cref="ContainsTableProvider"/>.
    /// </summary>
    /// <param name="provider">SearchCondition provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitContainsTable(ContainsTableProvider provider)
    {
      OnRecursionEntrance(provider);
      _ = OnRecursionExit(provider);
      return provider;
    }

    /// <summary>
    /// Visits <see cref="PredicateJoinProvider"/>.
    /// </summary>
    /// <param name="provider">Predicate join provider.</param>
    protected virtual CompilableProvider VisitPredicateJoin(PredicateJoinProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      var predicate = (Expression<Func<Tuple, Tuple, bool>>)OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new PredicateJoinProvider(left, right, predicate ?? provider.Predicate, provider.JoinType);
    }

    /// <summary>
    /// Visits <see cref="ExistenceProvider"/>.
    /// </summary>
    /// <param name="provider">Existence provider.</param>
    protected virtual CompilableProvider VisitExistence(ExistenceProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new ExistenceProvider(source, provider.ExistenceColumnName);
    }

    /// <summary>
    /// Visits <see cref="ApplyProvider"/>.
    /// </summary>
    /// <param name="provider">Apply provider.</param>
    protected virtual CompilableProvider VisitApply(ApplyProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      _ = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new ApplyProvider(provider.ApplyParameter, left, right, provider.IsInlined, provider.SequenceType, provider.ApplyType);
    }

    /// <summary>
    /// Visits the <see cref="IntersectProvider"/>.
    /// </summary>
    /// <param name="provider">Intersect provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitIntersect(IntersectProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      _ = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new IntersectProvider(left, right);
    }

    /// <summary>
    /// Visits the <see cref="ExceptProvider"/>.
    /// </summary>
    /// <param name="provider">Except provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitExcept(ExceptProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      _ = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new ExceptProvider(left, right);
    }

    /// <summary>
    /// Visits the <see cref="ConcatProvider"/>.
    /// </summary>
    /// <param name="provider">Concat provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitConcat(ConcatProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      _ = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new ConcatProvider(left, right);
    }

    /// <summary>
    /// Visits the <see cref="UnionProvider"/>.
    /// </summary>
    /// <param name="provider">Union provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitUnion(UnionProvider provider)
    {
      OnRecursionEntrance(provider);
      var left = VisitCompilable(provider.Left);
      var right = VisitCompilable(provider.Right);
      _ = OnRecursionExit(provider);
      if (left == provider.Left && right == provider.Right)
        return provider;
      return new UnionProvider(left, right);
    }

    /// <summary>
    /// Visits the <see cref="LockProvider"/>.
    /// </summary>
    /// <param name="provider">Lock provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitLock(LockProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new LockProvider(source, provider.LockMode, provider.LockBehavior);
    }

    /// <summary>
    /// Visits the <see cref="IncludeProvider"/>.
    /// </summary>
    /// <param name="provider">Include provider.</param>
    /// <returns></returns>
    protected virtual CompilableProvider VisitInclude(IncludeProvider provider)
    {
      OnRecursionEntrance(provider);
      var source = VisitCompilable(provider.Source);
      _ = OnRecursionExit(provider);
      if (source == provider.Source)
        return provider;
      return new IncludeProvider(source, provider.Algorithm, provider.IsInlined,
        provider.FilterDataSource, provider.ResultColumnName, provider.FilteredColumns);
    }

    /// <summary>
    /// Called after recursion exit.
    /// </summary>
    protected virtual object OnRecursionExit(Provider provider)
    {
      return null;
    }

    /// <summary>
    /// Called before recursion entrance.
    /// </summary>
    protected virtual void OnRecursionEntrance(Provider provider)
    {
    }

    private static Expression DefaultExpressionTranslator(CompilableProvider p, Expression e) => e;

    // Constructors

    /// <inheritdoc/>
    public CompilableProviderVisitor()
      : this(DefaultExpressionTranslator)
    {
    }

    /// <inheritdoc/>
    /// <param name="expressionTranslator">Expression translator.</param>
    public CompilableProviderVisitor(Func<CompilableProvider, Expression, Expression> expressionTranslator)
    {
      translate = expressionTranslator;
    }
  }
}