// Copyright (C) 2010-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Linq.Expressions;
using Xtensive.Orm.Rse.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse.Transformation
{
  internal abstract class ColumnMappingInspector : CompilableProviderVisitor
  {
    protected Dictionary<Provider, List<ColNum>> mappings = new();

    private readonly TupleAccessGatherer mappingsGatherer;
    private readonly Dictionary<ApplyParameter, List<ColNum>> outerColumnUsages = new();
    private readonly CompilableProviderVisitor outerColumnUsageVisitor;
    private readonly CompilableProvider rootProvider;
    private readonly Stack<List<int>> outerColumnUsageStack;

    private bool hasGrouping;

    public virtual CompilableProvider RemoveRedundantColumns()
    {
      mappings.Add(rootProvider, CollectionUtils.RangeToList(0, rootProvider.Header.Length));
      var visitedProvider = VisitCompilable(rootProvider);
      return visitedProvider != rootProvider
        ? visitedProvider
        : rootProvider;
    }

    #region Visit methods

    internal protected override IncludeProvider VisitInclude(IncludeProvider provider)
    {
      var sourceLength = provider.Source.Header.Length;
      mappings[provider.Source] = Merge(mappings[provider].Where(i => i < sourceLength), provider.FilteredColumns);
      var source = VisitCompilable(provider.Source);
      mappings[provider] = Merge(mappings[provider], mappings[provider.Source]);
      if (source == provider.Source) {
        return provider;
      }
      var filteredColumns = provider.FilteredColumns
        .Select(el => (ColNum) mappings[provider].IndexOf(el))
        .ToArray(provider.FilteredColumns.Count);
      return new IncludeProvider(source, provider.Algorithm, provider.IsInlined,
        provider.FilterDataSource, provider.ResultColumnName, filteredColumns);
    }

    internal protected override SelectProvider VisitSelect(SelectProvider provider)
    {
      var requiredColumns = mappings[provider];
      var remappedColumns = requiredColumns
        .Select(c => provider.ColumnIndexes[c])
        .ToList(requiredColumns.Count);

      mappings[provider.Source] = remappedColumns;
      var source = VisitCompilable(provider.Source);
      var sourceMap = mappings[provider.Source];

      var indexColumns = new List<ColNum>(provider.ColumnIndexes.Count);
      var newMappings = new List<ColNum>(provider.ColumnIndexes.Count);

      ColNum currentItemIndex = 0;
      foreach(var item in provider.ColumnIndexes) {
        var indexInMap = (ColNum) sourceMap.IndexOf(item);
        if (indexInMap >= 0) {
          indexColumns.Add(indexInMap);
          newMappings.Add(currentItemIndex);
        }
        currentItemIndex++;
      }

      mappings[provider] = newMappings;
      return source == provider.Source
        ? provider
        : new SelectProvider(source, indexColumns);
    }

    /// <inheritdoc/>
    internal protected override FreeTextProvider VisitFreeText(FreeTextProvider provider)
    {
      mappings[provider] = CollectionUtils.RangeToList(0, provider.Header.Length);
      return provider;
    }

    internal protected override CompilableProvider VisitContainsTable(ContainsTableProvider provider)
    {
      mappings[provider] = CollectionUtils.RangeToList(0, provider.Header.Length);
      return provider;
    }

    internal protected override IndexProvider VisitIndex(IndexProvider provider)
    {
      mappings[provider] = CollectionUtils.RangeToList(0, provider.Header.Length);
      return provider;
    }

    internal protected override SeekProvider VisitSeek(SeekProvider provider)
    {
      mappings[provider] = CollectionUtils.RangeToList(0, provider.Header.Length);
      return provider;
    }

    internal protected override FilterProvider VisitFilter(FilterProvider provider)
    {
      mappings[provider.Source] = Merge(mappings[provider], mappingsGatherer.Gather(provider.Predicate));
      var newSourceProvider = VisitCompilable(provider.Source);
      mappings[provider] = mappings[provider.Source];

      var predicate = TranslateLambda(provider, provider.Predicate);
      return newSourceProvider == provider.Source && predicate == provider.Predicate
        ? provider
        : new FilterProvider(newSourceProvider, (Expression<Func<Tuple, bool>>) predicate);
    }

    internal protected override JoinProvider VisitJoin(JoinProvider provider)
    {
      // split

      var (leftMapping, rightMapping) = SplitMappings(provider);

      var equalIndexes = provider.EqualIndexes;
      leftMapping = Merge(leftMapping, equalIndexes.Select(p => p.Left));
      rightMapping = Merge(rightMapping, equalIndexes.Select(p => p.Right));

      var newLeftProvider = provider.Left;
      var newRightProvider = provider.Right;
      VisitJoin(ref leftMapping, ref newLeftProvider, ref rightMapping, ref newRightProvider, true);

      mappings[provider] = MergeMappings(provider.Left, leftMapping, rightMapping);

      if (newLeftProvider == provider.Left && newRightProvider == provider.Right) {
        return provider;
      }

      var newIndexes = new (ColNum Left, ColNum Right)[equalIndexes.Count];
      for (int i = equalIndexes.Count; i-- > 0;) {
        var (left, right) = equalIndexes[i];
        newIndexes[i] = ((ColNum) leftMapping.IndexOf(left), (ColNum) rightMapping.IndexOf(right));
      }
      return new JoinProvider(newLeftProvider, newRightProvider, provider.JoinType, newIndexes);
    }

    internal protected override PredicateJoinProvider VisitPredicateJoin(PredicateJoinProvider provider)
    {
      var (leftMapping, rightMapping) = SplitMappings(provider);

      leftMapping.AddRange(mappingsGatherer.Gather(provider.Predicate,
        provider.Predicate.Parameters[0]));
      rightMapping.AddRange(mappingsGatherer.Gather(provider.Predicate,
        provider.Predicate.Parameters[1]));

      var newLeftProvider = provider.Left;
      var newRightProvider = provider.Right;
      VisitJoin(ref leftMapping, ref newLeftProvider, ref rightMapping, ref newRightProvider, false);
      mappings[provider] = MergeMappings(provider.Left, leftMapping, rightMapping);
      var predicate = TranslateJoinPredicate(leftMapping, rightMapping, provider.Predicate);

      return newLeftProvider == provider.Left && newRightProvider == provider.Right
        && provider.Predicate == predicate
        ? provider
        : new PredicateJoinProvider(newLeftProvider, newRightProvider, (Expression<Func<Tuple, Tuple, bool>>) predicate, provider.JoinType);
    }

    internal protected override SortProvider VisitSort(SortProvider provider)
    {
      mappings[provider.Source] = Merge(mappings[provider], provider.Order.Keys);
      var source = VisitCompilable(provider.Source);

      var sourceMap = mappings[provider.Source];
      var order = new DirectionCollection<ColNum>();
      foreach (var pair in provider.Order) {
        var index = (ColNum)sourceMap.IndexOf(pair.Key);
        if (index < 0) {
          throw Exceptions.InternalError(Strings.ExOrderKeyNotFoundInMapping, OrmLog.Instance);
        }
        order.Add(index, pair.Value);
      }
      mappings[provider] = sourceMap;

      return source == provider.Source ? provider : new SortProvider(source, order);
    }

    internal protected override ApplyProvider VisitApply(ApplyProvider provider)
    {
      // split

      var (leftMapping, rightMapping) = SplitMappings(provider);

      var applyParameter = provider.ApplyParameter;
      var currentOuterUsages = new List<ColNum>();

      using (SetOuterColumnUsage(applyParameter, currentOuterUsages)) {
        _ = outerColumnUsageVisitor.VisitCompilable(provider.Right);
      }

      leftMapping = Merge(leftMapping, currentOuterUsages);

      if (leftMapping.Count == 0) {
        leftMapping.Add(0);
      }

      // visit

      var oldMappings = ReplaceMappings(provider.Left, leftMapping);
      var newLeftProvider = VisitCompilable(provider.Left);
      leftMapping = mappings[provider.Left];

      _ = ReplaceMappings(provider.Right, rightMapping);
      CompilableProvider newRightProvider;
      using (SetOuterColumnUsage(applyParameter, leftMapping)) {
        newRightProvider = VisitCompilable(provider.Right);
      }

      var pair = OverrideRightApplySource(provider, newRightProvider, rightMapping);
      IReadOnlyList<ColNum> readOnlyRightMapping;
      if (pair.compilableProvider == null) {
        readOnlyRightMapping = mappings[provider.Right];
      }
      else {
        newRightProvider = pair.compilableProvider;
        readOnlyRightMapping = pair.mapping;
      }
      RestoreMappings(oldMappings);

      mappings[provider] = Merge(leftMapping, readOnlyRightMapping.Select(map => (ColNum)(map + provider.Left.Header.Length)));

      return newLeftProvider == provider.Left && newRightProvider == provider.Right
        ? provider
        : new ApplyProvider(applyParameter, newLeftProvider, newRightProvider, provider.IsInlined, provider.SequenceType, provider.ApplyType);
    }

    internal protected override AggregateProvider VisitAggregate(AggregateProvider provider)
    {
      var map = provider.AggregateColumns
        .Select(c => c.SourceIndex)
        .Union(provider.GroupColumnIndexes);
      mappings[provider.Source] = Merge(mappings[provider], map);

      if (provider.GroupColumnIndexes.Count > 0) {
        hasGrouping = true;
      }

      var source = VisitCompilable(provider.Source);
      hasGrouping = false;

      using ColumnMap sourceMap = new(mappings[provider.Source]);
      var currentMap = mappings[provider];

      mappings[provider] = provider.Header.Columns.Select(c => c.Index).ToList(provider.Header.Columns.Count);

      if (source == provider.Source) {
        return provider;
      }

      var columns = new List<AggregateColumnDescriptor>(provider.AggregateColumns.Length);
      for (ColNum i = 0; i < provider.AggregateColumns.Length; i++) {
        ColNum columnIndex = (ColNum)(i + provider.GroupColumnIndexes.Count);
        if (currentMap.BinarySearch(columnIndex) >= 0) {
          var column = provider.AggregateColumns[i];
          columns.Add(new AggregateColumnDescriptor(column.Name, (ColNum) sourceMap.IndexOf(column.SourceIndex), column.AggregateType));
        }
      }

      var groupColumnIndexes = provider.GroupColumnIndexes
        .Select(index => (ColNum)sourceMap.IndexOf(index))
        .ToArray(provider.GroupColumnIndexes.Count);

      return new AggregateProvider(source, groupColumnIndexes, columns);
    }

    internal protected override CompilableProvider VisitCalculate(CalculateProvider provider)
    {
      var sourceLength = provider.Source.Header.Length;
      var usedColumns = mappings[provider];
      var sourceMapping = Merge(
        mappings[provider].Where(i => i < sourceLength),
        provider.CalculatedColumns.SelectMany(c => mappingsGatherer.Gather(c.Expression)));

      mappings[provider.Source] = sourceMapping;
      var newSourceProvider = VisitCompilable(provider.Source);
      mappings[provider] = mappings[provider.Source];

      var translated = false;
      var descriptors = new List<CalculatedColumnDescriptor>(usedColumns.Count);
      var currentMapping = mappings[provider];
      for (ColNum calculatedColumnIndex = 0; calculatedColumnIndex < provider.CalculatedColumns.Length; calculatedColumnIndex++) {
        if (usedColumns.Contains(provider.CalculatedColumns[calculatedColumnIndex].Index)) {
          currentMapping.Add((ColNum)(provider.Source.Header.Length + calculatedColumnIndex));
          var column = provider.CalculatedColumns[calculatedColumnIndex];
          var expression = TranslateLambda(provider, column.Expression);
          if (expression != column.Expression) {
            translated = true;
          }
          var ccd = new CalculatedColumnDescriptor(column.Name, column.Type, (Expression<Func<Tuple, object>>) expression);
          descriptors.Add(ccd);
        }
      }
      mappings[provider] = currentMapping;
      if (descriptors.Count == 0) {
        return newSourceProvider;
      }

      return !translated && newSourceProvider == provider.Source && descriptors.Count == provider.CalculatedColumns.Length
        ? provider
        : new CalculateProvider(newSourceProvider, descriptors);
    }

    internal protected override RowNumberProvider VisitRowNumber(RowNumberProvider provider)
    {
      var sourceLength = provider.Source.Header.Length;
      mappings[provider.Source] = mappings[provider].Where(i => i < sourceLength).ToList();
      var newSource = VisitCompilable(provider.Source);
      var currentMapping = mappings[provider.Source];
      var rowNumberColumn = provider.Header.Columns.Last();
      mappings[provider] = Merge(currentMapping, [rowNumberColumn.Index]);
      return newSource == provider.Source
        ? provider
        : new RowNumberProvider(newSource, rowNumberColumn.Name);
    }

    internal protected override CompilableProvider VisitStore(StoreProvider provider)
    {
      if (!(provider.Source is CompilableProvider compilableSource)) {
        return provider;
      }

      if (hasGrouping) {
        mappings.Add(provider.Sources[0],
          Merge(mappings[provider], provider.Header.Columns.Select((c, i) => (ColNum)i)));
      }
      else {
        OnRecursionEntrance(provider);
      }

      var source = VisitCompilable(compilableSource);

      _ = OnRecursionExit(provider);
      return source == compilableSource
        ? provider
        : new StoreProvider(source, provider.Name);
    }

    internal protected override CompilableProvider VisitConcat(ConcatProvider provider) => VisitSetOperationProvider(provider);

    internal protected override CompilableProvider VisitExcept(ExceptProvider provider) => VisitSetOperationProvider(provider);

    internal protected override CompilableProvider VisitIntersect(IntersectProvider provider) => VisitSetOperationProvider(provider);

    internal protected override CompilableProvider VisitUnion(UnionProvider provider) => VisitSetOperationProvider(provider);

    private CompilableProvider VisitSetOperationProvider(BinaryProvider provider)
    {
      var leftMapping = mappings[provider];
      var rightMapping = mappings[provider];

      var oldMappings = ReplaceMappings(provider.Left, leftMapping);
      var newLeftProvider = VisitCompilable(provider.Left);
      leftMapping = mappings[provider.Left];

      _ = ReplaceMappings(provider.Right, rightMapping);
      var newRightProvider = VisitCompilable(provider.Right);
      rightMapping = mappings[provider.Right];
      RestoreMappings(oldMappings);

      var expectedColumns = mappings[provider];
      mappings[provider] = Merge(leftMapping, rightMapping);
      if (newLeftProvider == provider.Left && newRightProvider == provider.Right) {
        return provider;
      }

      newLeftProvider = BuildSetOperationSource(newLeftProvider, expectedColumns, leftMapping);
      newRightProvider = BuildSetOperationSource(newRightProvider, expectedColumns, rightMapping);
      switch (provider.Type) {
        case ProviderType.Concat:
          return new ConcatProvider(newLeftProvider, newRightProvider);
        case ProviderType.Intersect:
          return new IntersectProvider(newLeftProvider, newRightProvider);
        case ProviderType.Except:
          return new ExceptProvider(newLeftProvider, newRightProvider);
        case ProviderType.Union:
          return new UnionProvider(newLeftProvider, newRightProvider);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private static CompilableProvider BuildSetOperationSource(CompilableProvider provider, IReadOnlyList<ColNum> expectedColumns, IReadOnlyList<ColNum> returningColumns)
    {
      if (provider.Type == ProviderType.Select) {
        return provider;
      }

      var columns = expectedColumns
        .Select(originalIndex => (OriginalIndex: originalIndex, NewIndex: (ColNum) returningColumns.IndexOf(originalIndex)))
        .Select(x => x.NewIndex < 0 ? x.OriginalIndex : x.NewIndex).ToArray(expectedColumns.Count);
      return new SelectProvider(provider, columns);
    }

    protected virtual (CompilableProvider compilableProvider, IReadOnlyList<ColNum> mapping) OverrideRightApplySource(ApplyProvider applyProvider, CompilableProvider provider, IReadOnlyList<ColNum> requestedMapping) =>
      (provider, requestedMapping);

    #endregion

    #region OnRecursionExit, OnRecursionEntrance methods

    protected override object OnRecursionExit(Provider provider)
    {
      mappings[provider] = mappings[provider.Sources[0]];
      return null;
    }

    protected override void OnRecursionEntrance(Provider provider)
    {
      mappings.Add(provider.Sources[0], mappings[provider]);
    }

    #endregion

    #region Private methods

    private static List<ColNum> Merge(IEnumerable<ColNum> left, IEnumerable<ColNum> right) =>
      left.Union(right).OrderBy(i => i).ToList();

    private static List<int> Merge(IEnumerable<int> left, IEnumerable<int> right)
    {
      var hs = new HashSet<int>(left);
      foreach (var r in right) {
        _ = hs.Add(r);
      }
      var resultList = hs.ToList(hs.Count);
      resultList.Sort();
      return resultList;
    }

    private static List<int> Merge(List<int> leftMap, IEnumerable<int> rightMap)
    {
      var preReturn = leftMap.Union(rightMap).ToList(leftMap.Count * 2);
      preReturn.Sort();
      return preReturn;
    }

    private static List<int> Merge(List<int> leftMap, IList<int> rightMap)
    {
      var preReturn = leftMap.Union(rightMap).ToList(leftMap.Count + rightMap.Count);
      preReturn.Sort();
      return preReturn;
    }

    private static List<ColNum> MergeMappings(Provider originalLeft, IReadOnlyList<ColNum> leftMap, IReadOnlyList<ColNum> rightMap)
    {
      var leftCount = originalLeft.Header.Length;
      var result = leftMap
        .Concat(rightMap.Select(i => (ColNum) (i + leftCount)))
        .ToList(leftMap.Count + rightMap.Count);
      return result;
    }

    private (List<ColNum> leftMapping, List<ColNum> rightMapping) SplitMappings(BinaryProvider provider)
    {
      var binaryMapping = mappings[provider];
      var leftMapping = new List<ColNum>(binaryMapping.Count);
      var leftCount = provider.Left.Header.Length;
      var index = 0;
      while (index < binaryMapping.Count && binaryMapping[index] < leftCount) {
        leftMapping.Add(binaryMapping[index]);
        index++;
      }
      var rightMapping = new List<ColNum>(binaryMapping.Count - index);
      for (var i = index; i < binaryMapping.Count; i++) {
        rightMapping.Add((ColNum) (binaryMapping[i] - leftCount));
      }
      return (leftMapping, rightMapping);
    }

    private void RegisterOuterMapping(ApplyParameter parameter, ColNum value)
    {
      if (outerColumnUsages.TryGetValue(parameter, out var map) && !map.Contains(value)) {
        map.Add(value);
      }
    }

    private ColNum ResolveOuterMapping(ApplyParameter parameter, ColNum value)
    {
      var result = (ColNum)outerColumnUsages[parameter].IndexOf(value);
      return result < 0 ? value : result;
    }

    private Expression TranslateLambda(Provider originalProvider, LambdaExpression expression)
    {
      var replacer = new TupleAccessRewriter(mappings[originalProvider], ResolveOuterMapping, true);
      return replacer.Rewrite(expression, expression.Parameters[0]);
    }

    private Expression TranslateJoinPredicate(IReadOnlyList<ColNum> leftMapping,
      IReadOnlyList<ColNum> rightMapping, Expression<Func<Tuple, Tuple, bool>> expression)
    {
      var result = new TupleAccessRewriter(leftMapping, ResolveOuterMapping, true).Rewrite(expression,
        expression.Parameters[0]);
      return new TupleAccessRewriter(rightMapping, ResolveOuterMapping, true).Rewrite(result,
        expression.Parameters[1]);
    }

    private void VisitJoin(
      ref List<int> leftMapping, ref CompilableProvider left,
      ref List<int> rightMapping, ref CompilableProvider right, bool skipSort)
    {
      if (!skipSort) {
        leftMapping = leftMapping.Distinct().ToList(leftMapping.Count);
        leftMapping.Sort();
        rightMapping = rightMapping.Distinct().ToList(rightMapping.Count);
        rightMapping.Sort();
      }

      // visit

      var oldMapping = ReplaceMappings(left, leftMapping);
      var newLeftProvider = VisitCompilable(left);
      leftMapping = mappings[left];

      _ = ReplaceMappings(right, rightMapping);
      var newRightProvider = VisitCompilable(right);
      rightMapping = mappings[right];
      RestoreMappings(oldMapping);
      left = newLeftProvider;
      right = newRightProvider;
    }

    private Dictionary<Provider, List<ColNum>> ReplaceMappings(Provider firstNewKey, List<ColNum> firstNewValue)
    {
      var oldMappings = mappings;
      mappings = new() { { firstNewKey, firstNewValue } };
      return oldMappings;
    }

    private void RestoreMappings(Dictionary<Provider, List<ColNum>> savedMappings) => mappings = savedMappings;

    private IDisposable SetOuterColumnUsage(ApplyParameter parameter, List<int> usages)
    {
      outerColumnUsages.Add(parameter, usages);
      outerColumnUsageStack.Push(usages);
      return new Disposable(
        x => { 
          _ = outerColumnUsages.Remove(parameter);
          _ = outerColumnUsageStack.Pop();
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<int> GetOuterColumnUsage(ApplyParameter parameter) =>
      outerColumnUsages.TryGetValue(parameter, out var result) ? result : outerColumnUsageStack.Peek();

    #endregion

    // Constructors

    protected ColumnMappingInspector(CompilableProvider originalProvider)
    {
      rootProvider = originalProvider;

      mappingsGatherer = new TupleAccessGatherer((a, b) => { });

      var outerMappingsGatherer = new TupleAccessGatherer(RegisterOuterMapping);
      outerColumnUsageVisitor = new CompilableProviderVisitor((p, e) => {
        _ = outerMappingsGatherer.Gather(e);
        return e;
      });
    }
  }
}
