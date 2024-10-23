// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Vakhtina Elena
// Created:    2009.02.13

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Model;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Compilation;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Reflection;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Providers
{
  public partial class SqlCompiler : Compiler<SqlProvider>
  {
    protected readonly Stack<Pair<SqlProvider, bool>> outerReferenceStack = new Stack<Pair<SqlProvider, bool>>();

    private readonly BooleanExpressionConverter booleanExpressionConverter;
    private readonly Dictionary<SqlColumnStub, SqlExpression> stubColumnMap;
    private readonly ProviderInfo providerInfo;
    private readonly HashSet<Column> rootColumns;
    private readonly bool temporaryTablesSupported;
    private readonly bool tableValuedParametersSupported;
    private readonly bool forceApplyViaReference;
    private readonly bool useParameterForTypeId;

    private bool anyTemporaryTablesRequired;

    /// <summary>
    /// Gets model mapping.
    /// </summary>
    protected ModelMapping Mapping { get; private set; }

    /// <summary>
    /// Gets type identifier registry.
    /// </summary>
    protected TypeIdRegistry TypeIdRegistry { get; private set; }

    /// <summary>
    /// Gets the SQL domain handler.
    /// </summary>
    protected DomainHandler DomainHandler => Handlers.DomainHandler;

    /// <summary>
    /// Gets the SQL driver.
    /// </summary>
    protected StorageDriver Driver => Handlers.StorageDriver;

    /// <summary>
    /// Gets the <see cref="HandlerAccessor"/> object providing access to available storage handlers.
    /// </summary>
    protected HandlerAccessor Handlers { get; private set; }

    /// <summary>
    /// Gets collection of outer references.
    /// </summary>
    protected BindingCollection<ApplyParameter, Pair<SqlProvider, bool>> OuterReferences { get; private set; }

    /// <summary>
    /// Gets node configuration on which query is compilling.
    /// </summary>
    protected NodeConfiguration NodeConfiguration { get; private set; }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitAlias(AliasProvider provider)
    {
      var source = Compile(provider.Source);

      SqlSelect sourceSelect = source.Request.Statement;
      var sqlSelect = sourceSelect.ShallowClone();
      var sqlSelectColumns = sqlSelect.Columns;
      var tempColumns = new SqlColumn[sqlSelectColumns.Count];
      sqlSelectColumns.CopyTo(tempColumns, 0);

      sqlSelectColumns.Clear();
      for (int i = 0; i < tempColumns.Length; i++) {
        var columnName = provider.Header.Columns[i].Name;
        columnName = ProcessAliasedName(columnName);
        switch (tempColumns[i]) {
          case SqlColumnRef columnRef:
            sqlSelectColumns.Add(SqlDml.ColumnRef(columnRef.SqlColumn, columnName));
            break;
          case SqlColumnStub columnStub:
            sqlSelectColumns.Add(columnStub);
            break;
          case var column:
            sqlSelectColumns.Add(column, columnName);
            break;
        }
      }
      return CreateProvider(sqlSelect, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitCalculate(CalculateProvider provider)
    {
      var source = Compile(provider.Source);

      SqlSelect sqlSelect;
      if (provider.Source.Header.Length==0) {
        SqlSelect sourceSelect = source.Request.Statement;
        sqlSelect = sourceSelect.ShallowClone();
        sqlSelect.Columns.Clear();
      }
      else
        sqlSelect = ExtractSqlSelect(provider, source);

      var sourceColumns = ExtractColumnExpressions(sqlSelect);
      var allBindings = Enumerable.Empty<QueryParameterBinding>();
      foreach (var column in provider.CalculatedColumns) {
        var result = ProcessExpression(column.Expression, true, sourceColumns);
        var predicate = result.First;
        var bindings = result.Second;
        if (column.Type.StripNullable()==WellKnownTypes.Bool)
          predicate = GetBooleanColumnExpression(predicate);
        AddInlinableColumn(provider, column, sqlSelect, predicate);
        allBindings = allBindings.Concat(bindings);
      }
      return CreateProvider(sqlSelect, allBindings, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitDistinct(DistinctProvider provider)
    {
      var source = Compile(provider.Source);

      var sourceSelect = source.Request.Statement;
      SqlSelect query;
      if (sourceSelect.Limit is not null || sourceSelect.Offset is not null) {
        var queryRef = SqlDml.QueryRef(sourceSelect);
        query = SqlDml.Select(queryRef);
        query.Columns.AddRange(queryRef.Columns);
      }
      else {
        query = sourceSelect.ShallowClone();
      }

      query.Distinct = true;
      return CreateProvider(query, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitFilter(FilterProvider provider)
    {
      var source = Compile(provider.Source);

      var query = ExtractSqlSelect(provider, source);

      var sourceColumns = ExtractColumnExpressions(query);
      var result = ProcessExpression(provider.Predicate, true, sourceColumns);
      var predicate = result.First;
      var bindings = result.Second;

      query.Where &= predicate;

      return CreateProvider(query, bindings, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitJoin(JoinProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      // SQLite does not allow certain join combinations
      // Any right part of join expression should not be join itself
      // See IssueA363_WrongInnerJoin for example of such query

      var strictJoinWorkAround =
        providerInfo.Supports(ProviderFeatures.StrictJoinSyntax)
        && right.Request.Statement.From is SqlJoinedTable;

      var leftShouldUseReference = ShouldUseQueryReference(provider, left);
      var leftTable = leftShouldUseReference
        ? left.PermanentReference
        : left.Request.Statement.From;
      IReadOnlyList<SqlColumn> leftColumns = leftShouldUseReference
        ? leftTable.Columns
        : left.Request.Statement.Columns;
      IReadOnlyList<SqlExpression> leftExpressions = leftShouldUseReference
        ? leftTable.Columns.Cast<SqlExpression>().ToArray()
        : ExtractColumnExpressions(left.Request.Statement);

      var rightShouldUseReference = strictJoinWorkAround || ShouldUseQueryReference(provider, right);
      var rightTable = rightShouldUseReference
        ? right.PermanentReference
        : right.Request.Statement.From;
      IReadOnlyList<SqlColumn> rightColumns = rightShouldUseReference
        ? rightTable.Columns
        : right.Request.Statement.Columns;
      var rightExpressions = rightShouldUseReference
        ? rightTable.Columns.Cast<SqlExpression>().ToArray()
        : ExtractColumnExpressions(right.Request.Statement);

      var joinType = provider.JoinType==JoinType.LeftOuter
        ? SqlJoinType.LeftOuterJoin
        : SqlJoinType.InnerJoin;

      SqlExpression joinExpression = null;
      for (int i = 0, n = provider.EqualIndexes.Count(); i < n; ++i) {
        var (leftInder, rightIndex) = provider.EqualIndexes[i];
        var leftExpression = leftExpressions[leftInder];
        var rightExpression = rightExpressions[rightIndex];
        joinExpression &= GetJoinExpression(leftExpression, rightExpression, provider, i);
      }

      var joinedTable = SqlDml.Join(
        joinType,
        leftTable,
        rightTable,
        leftColumns,
        rightColumns,
        joinExpression);

      var query = SqlDml.Select(joinedTable);
      if (!leftShouldUseReference)
        query.Where &= left.Request.Statement.Where;
      if (!rightShouldUseReference)
        query.Where &= right.Request.Statement.Where;
      query.Columns.AddRange(joinedTable.AliasedColumns);
      query.Comment = SqlComment.Join(left.Request.Statement.Comment, right.Request.Statement.Comment);

      foreach (var sqlHint in left.Request.Statement.Hints.Concat(right.Request.Statement.Hints))
      {
        query.Hints.Add(sqlHint);
      }
      return CreateProvider(query, provider, left, right);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitPredicateJoin(PredicateJoinProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      var leftShouldUseReference = ShouldUseQueryReference(provider, left);
      var leftTable = leftShouldUseReference
        ? left.PermanentReference
        : left.Request.Statement.From;
      var leftColumns = leftShouldUseReference
        ? (IReadOnlyList<SqlColumn>) leftTable.Columns
        : left.Request.Statement.Columns;
      var leftExpressions = leftShouldUseReference
        ? (IReadOnlyList<SqlExpression>) leftTable.Columns
        : ExtractColumnExpressions(left.Request.Statement);

      var rightShouldUseReference = ShouldUseQueryReference(provider, right);
      var rightTable = rightShouldUseReference
        ? right.PermanentReference
        : right.Request.Statement.From;
      var rightColumns = rightShouldUseReference
        ? (IReadOnlyList<SqlColumn>) rightTable.Columns
        : right.Request.Statement.Columns;
      var rightExpressions = rightShouldUseReference
        ? (IReadOnlyList<SqlExpression>) rightTable.Columns
        : ExtractColumnExpressions(right.Request.Statement);


      var joinType = provider.JoinType==JoinType.LeftOuter ? SqlJoinType.LeftOuterJoin : SqlJoinType.InnerJoin;

      var result = ProcessExpression(provider.Predicate, false, leftExpressions, rightExpressions);
      var joinExpression = result.First;
      var bindings = result.Second;

      var joinedTable = SqlDml.Join(
        joinType,
        leftTable,
        rightTable,
        leftColumns,
        rightColumns,
        joinExpression);

      var query = SqlDml.Select(joinedTable);
      if (!leftShouldUseReference)
        query.Where &= left.Request.Statement.Where;
      if (!rightShouldUseReference)
        query.Where &= right.Request.Statement.Where;
      query.Columns.AddRange(joinedTable.AliasedColumns);
      query.Comment = SqlComment.Join(left.Request.Statement.Comment, right.Request.Statement.Comment);
      
      foreach (var sqlHint in left.Request.Statement.Hints.Concat(right.Request.Statement.Hints))
      {
        query.Hints.Add(sqlHint);
      }
      return CreateProvider(query, bindings, provider, left, right);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitSeek(SeekProvider provider)
    {
      var compiledSource = Compile(provider.Source);

      SqlSelect source = compiledSource.Request.Statement;
      var query = source.ShallowClone();
      var parameterBindings = new List<QueryParameterBinding>();
      var typeIdColumnName = Handlers.NameBuilder.TypeIdColumnName;
      Func<KeyValuePair<ColNum, Direction>, bool> filterNonTypeId =
        pair => ((MappedColumn) provider.Header.Columns[pair.Key]).ColumnInfoRef.ColumnName!=typeIdColumnName;
      var keyColumns = provider.Header.Order
        .Where(filterNonTypeId)
        .ToList(provider.Header.Order.Count);

      parameterBindings.Capacity = keyColumns.Count;
      for (int i = 0, count = keyColumns.Count; i < count; i++) {
        int columnIndex = keyColumns[i].Key;
        var sqlColumn = query.Columns[columnIndex];
        var column = headerColumns[columnIndex];
        TypeMapping typeMapping = Driver.GetTypeMapping(column.Type);
        var binding = new QueryParameterBinding(typeMapping, GetSeekKeyElementAccessor(provider.Key, i));
        parameterBindings.Add(binding);
        query.Where &= sqlColumn==binding.ParameterReference;
      }

      return CreateProvider(query, parameterBindings, provider, compiledSource);
    }

    private static Func<ParameterContext, object> GetSeekKeyElementAccessor(Func<ParameterContext, Tuple> seekKeyAccessor, int index)
    {
      return (context) => seekKeyAccessor.Invoke(context).GetValue(index);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitSelect(SelectProvider provider)
    {
      var compiledSource = Compile(provider.Source);

      var query = ExtractSqlSelect(provider, compiledSource);
      var queryColumns = query.Columns;
      var columnIndexes = provider.ColumnIndexes;

      var newIndex = 0;
      var newColumns = new SqlColumn[columnIndexes.Count];
      foreach (var index in columnIndexes) {
        newColumns[newIndex++] = queryColumns[index];
      }

      queryColumns.Clear();
      queryColumns.AddRange(newColumns);

      return CreateProvider(query, provider, compiledSource);
    }

    internal protected override SqlProvider VisitTag(TagProvider provider)
    {
      var compiledSource = Compile(provider.Source);

      var query = compiledSource.Request.Statement;
      query.Comment = SqlComment.Join(query.Comment, new SqlComment(provider.Tag));
      
      return CreateProvider(query, provider, compiledSource);
    }

    internal protected override SqlProvider VisitIndexHint(IndexHintProvider provider)
    {
      var compiledSource = Compile(provider.Source);
      
      var index = provider.Index.Resolve(Handlers.Domain.Model);
      var table = Mapping[index.ReflectedType];
      var tableRef = SqlDml.TableRef(table);
      
      var query = compiledSource.Request.Statement;
      var indexName = index.MappingName;
      query.Hints.Add(new SqlIndexHint(indexName, tableRef));

      return CreateProvider(query, provider, compiledSource);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitSort(SortProvider provider)
    {
      var compiledSource = Compile(provider.Source);

      var query = ExtractSqlSelect(provider, compiledSource);
      var rootSelectProvider = RootProvider as SelectProvider;
      var currentIsRoot = RootProvider==provider;
      var currentIsOwnedRootSelect = (rootSelectProvider!=null && rootSelectProvider.Source==provider);
      var currentIsOwnedByPaging = !currentIsRoot && Owner.Type is ProviderType.Take or ProviderType.Skip or ProviderType.Paging;

      if (currentIsRoot || currentIsOwnedRootSelect || currentIsOwnedByPaging) {
        query.OrderBy.Clear();
        if (currentIsRoot) {
          foreach (var pair in provider.Header.Order)
            query.OrderBy.Add(GetOrderByExpression(query.Columns[pair.Key], provider, pair.Key), pair.Value==Direction.Positive);
        }
        else {
          var columnExpressions = ExtractColumnExpressions(query);
          var shouldUseColumnPosition = provider.Header.Order.Any(o => o.Key >= columnExpressions.Count);
          if (shouldUseColumnPosition) {
            foreach (var pair in provider.Header.Order) {
              if (pair.Key >= columnExpressions.Count)
                query.OrderBy.Add(pair.Key + 1, pair.Value==Direction.Positive);
              else
                query.OrderBy.Add(GetOrderByExpression(columnExpressions[pair.Key], provider, pair.Key), pair.Value==Direction.Positive);
            }
          }
          else {
            foreach (var pair in provider.Header.Order)
              query.OrderBy.Add(GetOrderByExpression(columnExpressions[pair.Key], provider, pair.Key), pair.Value==Direction.Positive);
          }
        }
      }
      return CreateProvider(query, provider, compiledSource);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitStore(StoreProvider provider)
    {
      var source = provider.Source is RawProvider rawProvider
            ? (ExecutableProvider) (new Rse.Providers.ExecutableRawProvider(rawProvider))
            : Compile(provider.Source);
      var columnNames = provider.Header.Columns.Select(column => column.Name).ToArray();
      var descriptor = DomainHandler.TemporaryTableManager
        .BuildDescriptor(Mapping, provider.Name, provider.Header.TupleDescriptor, columnNames);
      var request = CreateQueryRequest(Driver, descriptor.QueryStatement, null, descriptor.TupleDescriptor, QueryRequestOptions.Empty);
      anyTemporaryTablesRequired = true;
      return new SqlStoreProvider(Handlers, request, descriptor, provider, source);
    }
    
    /// <inheritdoc/>
    internal protected override SqlProvider VisitExistence(ExistenceProvider provider)
    {
      var source = Compile(provider.Source);

      var query = source.Request.Statement.ShallowClone();
      query.Columns.Clear();
      query.Columns.Add(query.Asterisk);
      query.OrderBy.Clear();
      query.GroupBy.Clear();
      SqlExpression existsExpression = SqlDml.Exists(query);
      existsExpression = GetBooleanColumnExpression(existsExpression);
      var select = SqlDml.Select();
      select.Columns.Add(existsExpression, provider.ExistenceColumnName);

      return CreateProvider(select, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitIntersect(IntersectProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      var leftSelect = left.Request.Statement;
      var keepOrderForLeft = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if(!keepOrderForLeft)
        leftSelect.OrderBy.Clear();

      var rightSelect = right.Request.Statement;
      var keepOrderForRight = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if(!keepOrderForRight)
        rightSelect.OrderBy.Clear();

      var result = SqlDml.Intersect(leftSelect, rightSelect);
      var queryRef = SqlDml.QueryRef(result);

      var query = SqlDml.Select(queryRef);
      query.Columns.AddRange(queryRef.Columns);

      return CreateProvider(query, provider, left, right);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitExcept(ExceptProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      var leftSelect = left.Request.Statement;
      var keepOrderForLeft = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForLeft)
        leftSelect.OrderBy.Clear();

      var rightSelect = right.Request.Statement;
      var keepOrderForRight = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForRight)
        rightSelect.OrderBy.Clear();

      var result = SqlDml.Except(leftSelect, rightSelect);
      var queryRef = SqlDml.QueryRef(result);
      var query = SqlDml.Select(queryRef);
      query.Columns.AddRange(queryRef.Columns);

      return CreateProvider(query, provider, left, right);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitConcat(ConcatProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      var leftSelect = left.Request.Statement;
      var keepOrderForLeft = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForLeft)
        leftSelect.OrderBy.Clear();

      var rightSelect = right.Request.Statement;
      var keepOrderForRight = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForRight)
        rightSelect.OrderBy.Clear();

      var result = SqlDml.UnionAll(leftSelect, rightSelect);
      var queryRef = SqlDml.QueryRef(result);
      var query = SqlDml.Select(queryRef);
      query.Columns.AddRange(queryRef.Columns);

      return CreateProvider(query, provider, left, right);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitUnion(UnionProvider provider)
    {
      var left = Compile(provider.Left);
      var right = Compile(provider.Right);

      var leftSelect = left.Request.Statement;
      var keepOrderForLeft = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForLeft)
        leftSelect.OrderBy.Clear();

      var rightSelect = right.Request.Statement;
      var keepOrderForRight = (leftSelect.HasLimit || leftSelect.HasOffset) && providerInfo.Supports(ProviderFeatures.PagingRequiresOrderBy);
      if (!keepOrderForRight)
        rightSelect.OrderBy.Clear();

      var result = SqlDml.Union(leftSelect, rightSelect);
      var queryRef = SqlDml.QueryRef(result);
      var query = SqlDml.Select(queryRef);
      query.Columns.AddRange(queryRef.Columns);

      return CreateProvider(query, provider, left, right);
    }

    internal protected override SqlProvider VisitRowNumber(RowNumberProvider provider)
    {
      var header = provider.Header;
      var directionCollection = header.Order;
      if (directionCollection.Count == 0)
        directionCollection = new(1);
      var source = Compile(provider.Source);

      var query = ExtractSqlSelect(provider, source);
      var rowNumber = SqlDml.RowNumber();
      query.Columns.Add(rowNumber, header.Columns.Last().Name);
      var columns = ExtractColumnExpressions(query);
      foreach (var order in directionCollection)
        rowNumber.OrderBy.Add(columns[order.Key], order.Value==Direction.Positive);
      return CreateProvider(query, provider, source);
    }

    /// <inheritdoc/>
    internal protected override SqlProvider VisitLock(LockProvider provider)
    {
      var source = Compile(provider.Source);

      var query = source.Request.Statement.ShallowClone();
      switch (provider.LockMode.Invoke()) {
      case LockMode.Shared:
        query.Lock = SqlLockType.Shared;
        break;
      case LockMode.Exclusive:
        query.Lock = SqlLockType.Exclusive;
        break;
      case LockMode.Update:
        query.Lock = SqlLockType.Update;
        break;
      }
      switch (provider.LockBehavior.Invoke()) {
      case LockBehavior.Wait:
        break;
      case LockBehavior.ThrowIfLocked:
        query.Lock |= SqlLockType.ThrowIfLocked;
        break;
      case LockBehavior.Skip:
        query.Lock |= SqlLockType.SkipLocked;
        break;
      }
      return CreateProvider(query, provider, source);
    }

    protected override void Initialize()
    {
      foreach (var column in RootProvider.Header.Columns)
        rootColumns.Add(column.Origin);
    }


    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    public SqlCompiler(HandlerAccessor handlers, in CompilerConfiguration configuration)
    {
      Handlers = handlers;
      OuterReferences = new BindingCollection<ApplyParameter, Pair<SqlProvider, bool>>();
      var storageNode = configuration.StorageNode;
      Mapping = storageNode.Mapping;
      TypeIdRegistry = storageNode.TypeIdRegistry;
      NodeConfiguration = storageNode.Configuration;

      providerInfo = Handlers.ProviderInfo;
      temporaryTablesSupported = DomainHandler.TemporaryTableManager.Supported;
      tableValuedParametersSupported = providerInfo.Supports(ProviderFeatures.TableValuedParameters);
      forceApplyViaReference = providerInfo.ProviderName.Equals(WellKnown.Provider.PostgreSql);
      useParameterForTypeId = configuration.PreferTypeIdAsParameter && Driver.ServerInfo.Query.Features.HasFlag(Sql.Info.QueryFeatures.ParameterAsColumn);

      if (!providerInfo.Supports(ProviderFeatures.FullFeaturedBooleanExpressions)) {
        booleanExpressionConverter = new BooleanExpressionConverter(Driver);
      }

      stubColumnMap = new Dictionary<SqlColumnStub, SqlExpression>();
      rootColumns = new HashSet<Column>();
    }
  }
}
