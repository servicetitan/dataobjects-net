// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.10

using System;


namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Abstract <see cref="CompilableProvider"/> visitor class.
  /// </summary>
  [Serializable]
  public abstract class ProviderVisitor
  {
    /// <summary>
    /// Visits the specified <paramref name="cp"/>.
    /// </summary>
    /// <param name="cp">The <see cref="CompilableProvider"/> to visit.</param>
    /// <returns>Visit result.</returns>
    protected virtual Provider Visit(CompilableProvider cp) => cp?.Visit(this);

    /// <summary>
    /// Visits <see cref="PredicateJoinProvider"/>.
    /// </summary>
    /// <param name="provider">Predicate join provider.</param>
    internal protected abstract Provider VisitPredicateJoin(PredicateJoinProvider provider);

    /// <summary>
    /// Visits <see cref="ExistenceProvider"/>.
    /// </summary>
    /// <param name="provider">Existence provider.</param>
    internal protected abstract Provider VisitExistence(ExistenceProvider provider);

    /// <summary>
    /// Visits <see cref="ApplyProvider"/>.
    /// </summary>
    /// <param name="provider">Apply provider.</param>
    internal protected abstract Provider VisitApply(ApplyProvider provider);

    /// <summary>
    /// Visits <see cref="RowNumberProvider"/>.
    /// </summary>
    /// <param name="provider">Row number provider.</param>
    internal protected abstract Provider VisitRowNumber(RowNumberProvider provider);

    /// <summary>
    /// Visits <see cref="TakeProvider"/>.
    /// </summary>
    /// <param name="provider">Take provider.</param>
    internal protected abstract Provider VisitTake(TakeProvider provider);

    /// <summary>
    /// Visits <see cref="SkipProvider"/>.
    /// </summary>
    /// <param name="provider">Skip provider.</param>
    internal protected abstract Provider VisitSkip(SkipProvider provider);

    /// <summary>
    /// Visits <see cref="PagingProvider"/>.
    /// </summary>
    /// <param name="provider">Paging provider.</param>
    internal protected abstract Provider VisitPaging(PagingProvider provider);

    /// <summary>
    /// Visits <see cref="SelectProvider"/>.
    /// </summary>
    /// <param name="provider">Select provider.</param>
    internal protected abstract Provider VisitSelect(SelectProvider provider);

    /// <summary>
    /// Visits <see cref="TagProvider"/>.
    /// </summary>
    /// <param name="provider">Tag provider.</param>
    internal protected abstract Provider VisitTag(TagProvider provider);
    
    /// <summary>
    /// Visits <see cref="IndexHintProvider"/>.
    /// </summary>
    /// <param name="provider">IndexHint provider.</param>
    internal protected abstract Provider VisitIndexHint(IndexHintProvider provider);

    /// <summary>
    /// Visits <see cref="SeekProvider"/>.
    /// </summary>
    /// <param name="provider">Seek provider.</param>
    internal protected abstract Provider VisitSeek(SeekProvider provider);

    /// <summary>
    /// Visits <see cref="RawProvider"/>.
    /// </summary>
    /// <param name="provider">Raw provider.</param>
    internal protected abstract Provider VisitRaw(RawProvider provider);

    /// <summary>
    /// Visits <see cref="SortProvider"/>.
    /// </summary>
    /// <param name="provider">Sort provider.</param>
    internal protected abstract Provider VisitSort(SortProvider provider);

    /// <summary>
    /// Visits <see cref="JoinProvider"/>.
    /// </summary>
    /// <param name="provider">Join provider.</param>
    internal protected abstract Provider VisitJoin(JoinProvider provider);

    /// <summary>
    /// Visits <see cref="FilterProvider"/>.
    /// </summary>
    /// <param name="provider">Filter provider.</param>
    internal protected abstract Provider VisitFilter(FilterProvider provider);

    /// <summary>
    /// Visits <see cref="DistinctProvider"/>.
    /// </summary>
    /// <param name="provider">Distinct provider.</param>
    internal protected abstract Provider VisitDistinct(DistinctProvider provider);

    /// <summary>
    /// Visits <see cref="CalculateProvider"/>.
    /// </summary>
    /// <param name="provider">Calculate provider.</param>
    internal protected abstract Provider VisitCalculate(CalculateProvider provider);

    /// <summary>
    /// Visits <see cref="AliasProvider"/>.
    /// </summary>
    /// <param name="provider">Alias provider.</param>
    internal protected abstract Provider VisitAlias(AliasProvider provider);

    /// <summary>
    /// Visits <see cref="AggregateProvider"/>.
    /// </summary>
    /// <param name="provider">Aggregate provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitAggregate(AggregateProvider provider);

    /// <summary>
    /// Visits <see cref="StoreProvider"/>.
    /// </summary>
    /// <param name="provider">Store provider.</param>
    internal protected abstract Provider VisitStore(StoreProvider provider);

    /// <summary>
    /// Visits <see cref="IndexProvider"/>.
    /// </summary>
    /// <param name="provider">Index provider.</param>
    internal protected abstract Provider VisitIndex(IndexProvider provider);

    /// <summary>
    /// Visits the <see cref="IntersectProvider"/>.
    /// </summary>
    /// <param name="provider">Intersect provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitIntersect(IntersectProvider provider);

    /// <summary>
    /// Visits the <see cref="ExceptProvider"/>.
    /// </summary>
    /// <param name="provider">Except provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitExcept(ExceptProvider provider);

    /// <summary>
    /// Visits the <see cref="ConcatProvider"/>.
    /// </summary>
    /// <param name="provider">Concat provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitConcat(ConcatProvider provider);

    /// <summary>
    /// Visits the <see cref="UnionProvider"/>.
    /// </summary>
    /// <param name="provider">Union provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitUnion(UnionProvider provider);

    /// <summary>
    /// Visits the <see cref="LockProvider"/>.
    /// </summary>
    /// <param name="provider">Lock provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitLock(LockProvider provider);

    /// <summary>
    /// Visits the <see cref="IncludeProvider"/>.
    /// </summary>
    /// <param name="provider">Include provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitInclude(IncludeProvider provider);

    /// <summary>
    /// Visits the <see cref="FreeTextProvider"/>.
    /// </summary>
    /// <param name="provider">FreeText provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitFreeText(FreeTextProvider provider);

    /// <summary>
    /// Visits the <see cref="ContainsTableProvider"/>.
    /// </summary>
    /// <param name="provider">SearchCondition provider.</param>
    /// <returns></returns>
    internal protected abstract Provider VisitContainsTable(ContainsTableProvider provider);
  }
}