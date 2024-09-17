// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.04.24

using System;
using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Collections;
using Xtensive.Orm.Rse.Providers;
using System.Linq;

namespace Xtensive.Orm.Rse.Transformation
{
  internal sealed class OrderingRewriter : CompilableProviderVisitor
  {
    private readonly Func<CompilableProvider, ProviderOrderingDescriptor> descriptorResolver;
    private DirectionCollection<ColNum> sortOrder;
    private ProviderOrderingDescriptor descriptor;
    private ProviderOrderingDescriptor consumerDescriptor;

    public static CompilableProvider Rewrite(
      CompilableProvider originProvider,
      Func<CompilableProvider, ProviderOrderingDescriptor> orderingDescriptorResolver)
    {
      ArgumentNullException.ThrowIfNull(originProvider, "originProvider");
      var rewriter = new OrderingRewriter(orderingDescriptorResolver);
      if (originProvider.Type == ProviderType.Select) {
        var selectProvider = (SelectProvider) originProvider;
        var source = rewriter.VisitCompilable(selectProvider.Source);
        return new SelectProvider(
          rewriter.InsertSortProvider(source),
          selectProvider.ColumnIndexes);
      }
      var visited = rewriter.VisitCompilable(originProvider);
      return rewriter.InsertSortProvider(visited);
    }

    protected override CompilableProvider Visit(CompilableProvider cp)
    {
      var prevConsumerDescriptor = consumerDescriptor;
      consumerDescriptor = descriptor;
      descriptor = descriptorResolver(cp);

      var prevDescriptor = descriptor;
      var visited = base.Visit(cp);
      descriptor = prevDescriptor;
      visited = CorrectOrder(visited);

      descriptor = consumerDescriptor;
      consumerDescriptor = prevConsumerDescriptor;
      return visited;
    }

    internal protected override CompilableProvider VisitSort(SortProvider provider)
    {
      var source = VisitCompilable(provider.Source);
      sortOrder = provider.Order;
      if (consumerDescriptor.IsOrderSensitive)
        return new SortProvider(source, provider.Order);
      return source;
    }

    internal protected override CompilableProvider VisitSelect(SelectProvider provider)
    {
      var result = provider;
      var source = VisitCompilable(provider.Source);
      if (source != provider.Source)
        result = new SelectProvider(source, provider.ColumnIndexes);

      if (sortOrder.Count > 0) {
        var selectOrdering = new DirectionCollection<ColNum>();
        var columnIndexes = result.ColumnIndexes;
        foreach (var pair in sortOrder) {
          var columnIndex = (ColNum)columnIndexes.IndexOf(pair.Key);
          if (columnIndex < 0) {
            if (selectOrdering.Count > 0)
              selectOrdering.Clear();
            break;
          }
          selectOrdering.Add(columnIndex, pair.Value);
        }
        sortOrder = selectOrdering;
      }

      if (sortOrder.Count > 0
        && provider.Header.Order.Count==0
        && !consumerDescriptor.BreaksOrder
        && !consumerDescriptor.PreservesOrder) {
        throw new InvalidOperationException(Strings.ExSelectProviderRemovesColumnsUsedForOrdering);
      }
      return result;
    }

    internal protected override CompilableProvider VisitAggregate(AggregateProvider provider)
    {
      var result = provider;
      var source = VisitCompilable(provider.Source);
      if (source != provider.Source) {
        var acds = provider.AggregateColumns
           .Select(ac => new AggregateColumnDescriptor(ac.Name, ac.SourceIndex, ac.AggregateType));
        result = new AggregateProvider(source, provider.GroupColumnIndexes, acds);
      }
      if (sortOrder.Count > 0) {
        var selectOrdering = new DirectionCollection<ColNum>();
        foreach (var pair in sortOrder) {
          ColNum columnIndex = (ColNum)result.GroupColumnIndexes.IndexOf(pair.Key);
          if (columnIndex < 0) {
            if (selectOrdering.Count > 0)
              selectOrdering.Clear();
            break;
          }
          selectOrdering.Add(columnIndex, pair.Value);
        }
        sortOrder = selectOrdering;
      }

      return result;
    }

    internal protected override CompilableProvider VisitIndex(IndexProvider provider)
    {
      sortOrder = new();
      return provider;
    }

    internal protected override CompilableProvider VisitFreeText(FreeTextProvider provider)
    {
      sortOrder = new();
      return provider;
    }

    internal protected override CompilableProvider VisitContainsTable(ContainsTableProvider provider)
    {
      sortOrder = new();
      return provider;
    }

    internal protected override RawProvider VisitRaw(RawProvider provider)
    {
      sortOrder = new();
      return provider;
    }

    internal protected override CompilableProvider VisitStore(StoreProvider provider)
    {
      sortOrder = new();
      return provider;
    }

    internal protected override ApplyProvider VisitApply(ApplyProvider provider)
    {
      var left = VisitCompilable(provider.Left);
      var leftOrder = sortOrder;
      var right = VisitCompilable(provider.Right);
      var rightOrder = sortOrder;
      var result = left == provider.Left && right == provider.Right
        ? provider
        : new ApplyProvider(provider.ApplyParameter, left, right, provider.IsInlined, provider.SequenceType, provider.ApplyType);
      sortOrder = ComputeBinaryOrder(provider, leftOrder, rightOrder);
      return result;
    }

    internal protected override JoinProvider VisitJoin(JoinProvider provider)
    {
      var left = VisitCompilable(provider.Left);
      var leftOrder = sortOrder;
      var right = VisitCompilable(provider.Right);
      var rightOrder = sortOrder;
      var result = left == provider.Left && right == provider.Right
        ? provider
        : new JoinProvider(left, right, provider.JoinType, provider.EqualIndexes);
      sortOrder = ComputeBinaryOrder(provider, leftOrder, rightOrder);
      return result;
    }

    internal protected override PredicateJoinProvider VisitPredicateJoin(PredicateJoinProvider provider)
    {
      var left = VisitCompilable(provider.Left);
      var leftOrder = sortOrder;
      var right = VisitCompilable(provider.Right);
      var rightOrder = sortOrder;
      var result = left == provider.Left && right == provider.Right
        ? provider
        : new PredicateJoinProvider(left, right, provider.Predicate, provider.JoinType);
      sortOrder = ComputeBinaryOrder(provider, leftOrder, rightOrder);
      return result;
    }

    #region Private \ internal methods

    private CompilableProvider InsertSortProvider(CompilableProvider visited)
    {
      return sortOrder.Count==0
        ? visited
        : new SortProvider(visited, sortOrder);
    }

    private CompilableProvider CorrectOrder(CompilableProvider visited)
    {
      var result = visited;
      if (sortOrder.Count > 0) {
        if (descriptor.IsSorter)
          return result;
        if (descriptor.BreaksOrder) {
          sortOrder = new();
          return result;
        }
        if (consumerDescriptor.IsOrderSensitive && !descriptor.IsOrderSensitive)
          result = InsertSortProvider(visited);
      }
      return result;
    }

    private static DirectionCollection<ColNum> ComputeBinaryOrder(BinaryProvider provider, DirectionCollection<ColNum> leftOrder, DirectionCollection<ColNum> rightOrder)
    {
      if (leftOrder.Count > 0)
        return new DirectionCollection<ColNum>(
          leftOrder.Concat(
            rightOrder.Select(p => new KeyValuePair<ColNum, Direction>((ColNum) (p.Key + provider.Left.Header.Length), p.Value))));
      return new();
    }

    #endregion

    // Constructors

    private OrderingRewriter(Func<CompilableProvider, ProviderOrderingDescriptor> orderingDescriptorResolver)
    {
      ArgumentNullException.ThrowIfNull(orderingDescriptorResolver, "orderingDescriptorResolver");
      descriptorResolver = orderingDescriptorResolver;
      sortOrder = new();
    }
  }
}