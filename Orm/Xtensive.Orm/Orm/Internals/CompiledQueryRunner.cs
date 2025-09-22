// Copyright (C) 2012-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2012.01.27

using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Linq.Expressions.Visitors;
using Xtensive.Reflection;

namespace Xtensive.Orm.Internals
{
  internal record struct QueryKey(object Key, int MetadataToken, ModuleHandle ModuleHandle, string StorageNodeId);

  internal class CompiledQueryRunner
  {
    private record struct ClosureTypeInfo(Type ParameterType, PropertyInfo ValueMemberInfo, FieldInfo[] Fields);

    private static readonly ExtendedExpressionReplacer NoopReplacer = new(e => e);

    private readonly Domain domain;
    private readonly Session session;
    private readonly QueryEndpoint endpoint;
    private readonly QueryKey queryKey;
    private readonly object queryTarget;
    private readonly ParameterContext outerContext;

    private Parameter queryParameter;
    private ExtendedExpressionReplacer queryParameterReplacer;

    public QueryResult<TElement> ExecuteCompiled<TElement>(Func<QueryEndpoint, IQueryable<TElement>> query)
    {
      var parameterizedQuery = GetSequenceQuery(query);
      return parameterizedQuery.ExecuteSequence<TElement>(session, CreateParameterContext(parameterizedQuery));
    }

    public QueryResult<TElement> ExecuteCompiled<TElement>(Func<QueryEndpoint, IOrderedQueryable<TElement>> query)
    {
      var parameterizedQuery = GetSequenceQuery(query);
      return parameterizedQuery.ExecuteSequence<TElement>(session, CreateParameterContext(parameterizedQuery));
    }

    public TResult ExecuteCompiled<TResult>(Func<QueryEndpoint, TResult> query)
    {
      var parameterizedQuery = GetCachedQuery();
      if (parameterizedQuery!=null) {
        return parameterizedQuery.ExecuteScalar<TResult>(session, CreateParameterContext(parameterizedQuery));
      }

      GetScalarQuery(query, true, out var result);
      return result;
    }

    public Task<QueryResult<TElement>> ExecuteCompiledAsync<TElement>(
      Func<QueryEndpoint, IQueryable<TElement>> query, CancellationToken token)
    {
      var parameterizedQuery = GetSequenceQuery(query);
      token.ThrowIfCancellationRequested();
      var parameterContext = CreateParameterContext(parameterizedQuery);
      token.ThrowIfCancellationRequested();

      return parameterizedQuery.ExecuteSequenceAsync<TElement>(session, parameterContext, token);
    }

    public Task<QueryResult<TElement>> ExecuteCompiledAsync<TElement>(
      Func<QueryEndpoint, IOrderedQueryable<TElement>> query, CancellationToken token) =>
      ExecuteCompiledAsync((Func<QueryEndpoint, IQueryable<TElement>>)query, token);

    public Task<TResult> ExecuteCompiledAsync<TResult>(Func<QueryEndpoint, TResult> query, CancellationToken token)
    {
      var parameterizedQuery = GetCachedQuery();
      if (parameterizedQuery!=null) {
        token.ThrowIfCancellationRequested();
        return parameterizedQuery.ExecuteScalarAsync<TResult>(session, CreateParameterContext(parameterizedQuery), token);
      }

      parameterizedQuery = GetScalarQuery(query, false, out _);
      token.ThrowIfCancellationRequested();
      return parameterizedQuery.ExecuteScalarAsync<TResult>(session, CreateParameterContext(parameterizedQuery), token);
    }

    public DelayedScalarQuery<TResult> CreateDelayedQuery<TResult>(Func<QueryEndpoint, TResult> query)
    {
      var parameterizedQuery = GetCachedQuery() ?? GetScalarQuery(query, false, out _);
      var parameterContext = CreateParameterContext(parameterizedQuery);
      var result = new DelayedScalarQuery<TResult>(session, parameterizedQuery, parameterContext);
      session.RegisterUserDefinedDelayedQuery(result.Task);
      return result;
    }

    public DelayedQuery<TElement> CreateDelayedQuery<TElement>(Func<QueryEndpoint, IOrderedQueryable<TElement>> query) =>
      CreateDelayedSequenceQuery(query);

    public DelayedQuery<TElement> CreateDelayedQuery<TElement>(Func<QueryEndpoint, IQueryable<TElement>> query) =>
      CreateDelayedSequenceQuery(query);

    private DelayedQuery<TElement> CreateDelayedSequenceQuery<TElement>(
      Func<QueryEndpoint, IQueryable<TElement>> query)
    {
      var parameterizedQuery = GetSequenceQuery(query);
      var parameterContext = CreateParameterContext(parameterizedQuery);
      var result = new DelayedQuery<TElement>(session, parameterizedQuery, parameterContext);
      session.RegisterUserDefinedDelayedQuery(result.Task);
      return result;
    }

    private ParameterizedQuery GetScalarQuery<TResult>(
      Func<QueryEndpoint, TResult> query, bool executeAsSideEffect, out TResult result)
    {
      AllocateParameterAndReplacer();

      var parameterContext = new ParameterContext(outerContext);
      parameterContext.SetValue(queryParameter, queryTarget);
      var scope = new CompiledQueryProcessingScope(
        queryParameter, queryParameterReplacer, parameterContext, executeAsSideEffect);

      using (scope.Enter()) {
        result = query.Invoke(endpoint);
      }

      var parameterizedQuery = (ParameterizedQuery) scope.ParameterizedQuery;
      if (parameterizedQuery == null && queryTarget != null) {
        throw new NotSupportedException(Strings.ExNonLinqCallsAreNotSupportedWithinQueryExecuteDelayed);
      }

      PutQueryToCache(parameterizedQuery);

      return parameterizedQuery;
    }

    private ParameterizedQuery GetSequenceQuery<TElement>(
      Func<QueryEndpoint, IQueryable<TElement>> query)
    {
      var parameterizedQuery = GetCachedQuery();
      if (parameterizedQuery!=null) {
        return parameterizedQuery;
      }

      AllocateParameterAndReplacer();
      var scope = new CompiledQueryProcessingScope(queryParameter, queryParameterReplacer);
      using (scope.Enter()) {
        var result = query.Invoke(endpoint);
        var translatedQuery = endpoint.Provider.Translate(result.Expression);
        parameterizedQuery = (ParameterizedQuery) translatedQuery;
      }

      PutQueryToCache(parameterizedQuery);

      return parameterizedQuery;
    }

    private void AllocateParameterAndReplacer()
    {
      if (queryTarget == null) {
        queryParameter = null;
        queryParameterReplacer = NoopReplacer;
        return;
      }

      var closureType = queryTarget.GetType();
      var info = Memoizer.Get(closureType, static ct => {
        var parameterType = WellKnownOrmTypes.ParameterOfT.CachedMakeGenericType(ct);
        return new ClosureTypeInfo(
          parameterType,
          parameterType.GetProperty(nameof(Parameter<object>.Value), ct),
          ct.IsClosure() ? ct.GetFields() : null
        );
      }, 10_000);
      MemberExpression closureAccessor = null;
      queryParameter = (Parameter) System.Activator.CreateInstance(info.ParameterType, "pClosure");
      queryParameterReplacer = new ExtendedExpressionReplacer(expression => {
        if (expression.NodeType == ExpressionType.Constant) {
          if (((ConstantExpression)expression).Value is null) {
            return null;
          }
          var expressionType = expression.Type;
          if (expressionType.IsClosure()) {
            if (expressionType == closureType) {
              return GetClosureAccessor();
            }
            else {
              throw new NotSupportedException(string.Format(
                Strings.ExExpressionDefinedOutsideOfCachingQueryClosure, expression));
            }
          }

          if (closureType.DeclaringType == null) {
            if (expressionType.IsAssignableFrom(closureType))
              return GetClosureAccessor();
          }
          else {
            if (expressionType.IsAssignableFrom(closureType))
              return GetClosureAccessor();
            if (expressionType.IsAssignableFrom(closureType.DeclaringType)) {
              var members = closureType.TryGetFieldInfoFromClosure(expressionType);
              if (members != null) {
                var newExpression = members.Aggregate(
                  GetClosureAccessor(),
                  (left, right) => Expression.MakeMemberAccess(left, right));
                return newExpression;
              }
            }
          }
        }

        return null;
      });

      MemberExpression GetClosureAccessor() =>
        closureAccessor ??= Expression.MakeMemberAccess(Expression.Constant(queryParameter, info.ParameterType), info.ValueMemberInfo);
    }

    private ParameterizedQuery GetCachedQuery() =>
      domain.QueryCache.TryGetItem(queryKey, true, out var item) ? item.Item2 : null;

    private void PutQueryToCache(ParameterizedQuery parameterizedQuery) =>
      domain.QueryCache.Add((queryKey, parameterizedQuery));

    private ParameterContext CreateParameterContext(ParameterizedQuery query)
    {
      var parameterContext = new ParameterContext(outerContext);
      if (query.QueryParameter!=null) {
        parameterContext.SetValue(query.QueryParameter, queryTarget);
      }

      return parameterContext;
    }

    private CompiledQueryRunner(QueryEndpoint endpoint, (object Key, int MetadataToken, ModuleHandle ModuleHandle) keyParts, object queryTarget, ParameterContext outerContext)
    {
      session = endpoint.Provider.Session;
      domain = session.Domain;

      this.endpoint = endpoint;
      this.queryTarget = queryTarget;
      this.outerContext = outerContext;

      var domainConfig = domain.Configuration;
      queryKey = new(keyParts.Key, keyParts.MetadataToken, keyParts.ModuleHandle,
        domainConfig.ShareStorageSchemaOverNodes && domainConfig.PreferTypeIdsAsQueryParameters ? null : session.StorageNodeId
      );
    }

    public CompiledQueryRunner(QueryEndpoint endpoint, MethodInfo methodInfo, object queryTarget, ParameterContext outerContext = null)
      : this(endpoint, (methodInfo, methodInfo.MetadataToken, methodInfo.Module.ModuleHandle), queryTarget, outerContext)
    {
    }

    public CompiledQueryRunner(QueryEndpoint endpoint, object key, object queryTarget, ParameterContext outerContext = null)
      : this(endpoint,
        key is MethodInfo methodInfo ? (methodInfo, methodInfo.MetadataToken, methodInfo.Module.ModuleHandle) : (key, 0, default),
        queryTarget, outerContext)
    {
    }
  }
}
