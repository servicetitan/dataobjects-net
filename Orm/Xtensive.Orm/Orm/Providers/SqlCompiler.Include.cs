// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.11.13

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using Xtensive.Orm.Rse.Providers;
using Xtensive.Reflection;

namespace Xtensive.Orm.Providers
{
  partial class SqlCompiler 
  {
    /// <inheritdoc/>
    internal protected override SqlProvider VisitInclude(IncludeProvider provider)
    {
      var source = Compile(provider.Source);
      var resultQuery = ExtractSqlSelect(provider, source);
      var sourceColumns = ExtractColumnExpressions(resultQuery);
      var bindings = source.Request.ParameterBindings;
      var filterDataSource = provider.FilterDataSource.CachingCompile();
      var requestOptions = QueryRequestOptions.Empty;
      SqlExpression resultExpression;
      TemporaryTableDescriptor tableDescriptor = null;
      QueryParameterBinding extraBinding = null;
      Type tableValuedParameterType = null;
      var algorithm = provider.Algorithm;
      if (!temporaryTablesSupported) {
        switch (algorithm) {
          case IncludeAlgorithm.Auto:
          case IncludeAlgorithm.TemporaryTable:
            algorithm = tableValuedParametersSupported ? IncludeAlgorithm.Auto : IncludeAlgorithm.ComplexCondition;
            break;
        }
      }
      if (algorithm is IncludeAlgorithm.Auto or IncludeAlgorithm.TableValuedParameter
          && tableValuedParametersSupported) {
        if (provider.FilteredColumnsExtractionTransform.Descriptor.Count == 1) {
          var fieldType = provider.FilteredColumnsExtractionTransform.Descriptor[0];
          if (fieldType == WellKnownTypes.Int64 || fieldType == WellKnownTypes.Int32 || fieldType == WellKnownTypes.String) {
            tableValuedParameterType = fieldType;
          }
        }
      }

      Func<ParameterContext, object> parameterAccessor;
      switch (algorithm) {
        case IncludeAlgorithm.Auto:
          SqlExpression alternative;          
          if (tableValuedParameterType != null) {
            (alternative, extraBinding) = CreateIncludeViaTableValuedParameter(provider, BuildComplexConditionRowFilterParameterAccessor(filterDataSource), sourceColumns, tableValuedParameterType);
            parameterAccessor = BuildComplexConditionRowFilterParameterAccessor(filterDataSource);
            (var complexConditionExpression, extraBinding) = CreateIncludeViaComplexConditionExpression(provider, parameterAccessor, sourceColumns, extraBinding);
            resultExpression = SqlDml.Variant(extraBinding, complexConditionExpression, alternative);
          }
          else {
            (alternative, tableDescriptor) = CreateIncludeViaTemporaryTableExpression(provider, sourceColumns);
            parameterAccessor = BuildAutoRowFilterParameterAccessor(tableDescriptor);
            anyTemporaryTablesRequired = true;
            (var complexConditionExpression, extraBinding) = CreateIncludeViaComplexConditionExpression(provider, parameterAccessor, sourceColumns);
            resultExpression = SqlDml.Variant(extraBinding, complexConditionExpression, alternative);
          }
          break;
        case IncludeAlgorithm.ComplexCondition:
          parameterAccessor = BuildComplexConditionRowFilterParameterAccessor(filterDataSource);
          (resultExpression, extraBinding) = CreateIncludeViaComplexConditionExpression(provider, parameterAccessor, sourceColumns);
          break;
        case IncludeAlgorithm.TemporaryTable:
          (resultExpression, tableDescriptor) = CreateIncludeViaTemporaryTableExpression(provider, sourceColumns);
          anyTemporaryTablesRequired = true;
          break;
        case IncludeAlgorithm.TableValuedParameter:
          parameterAccessor = BuildComplexConditionRowFilterParameterAccessor(filterDataSource);
          (resultExpression, extraBinding) = CreateIncludeViaTableValuedParameter(provider, parameterAccessor, sourceColumns, tableValuedParameterType);
          break;
        default:
          throw new ArgumentOutOfRangeException("provider.Algorithm");
      }
      requestOptions |= anyTemporaryTablesRequired ? QueryRequestOptions.Empty : QueryRequestOptions.AllowOptimization;
      resultExpression = GetBooleanColumnExpression(resultExpression);
      var calculatedColumn = provider.Header.Columns[provider.Header.Length - 1];
      AddInlinableColumn(provider, calculatedColumn, resultQuery, resultExpression);
      if (extraBinding!=null) {
        bindings = bindings.Append(extraBinding);
      }

      var request = CreateQueryRequest(Driver, resultQuery, bindings, provider.Header.TupleDescriptor, requestOptions);
      return new SqlIncludeProvider(Handlers, request, tableDescriptor, filterDataSource, provider, source);
    }

    protected (SqlExpression, QueryParameterBinding) CreateIncludeViaComplexConditionExpression(
      IncludeProvider provider, Func<ParameterContext, object> valueAccessor,
      IReadOnlyList<SqlExpression> sourceColumns,
      QueryParameterBinding binding = null)
    {
      var filterTupleDescriptor = provider.FilteredColumnsExtractionTransform.Descriptor;
      var mappings = filterTupleDescriptor.Select(type => Driver.GetTypeMapping(type)).ToArray(filterTupleDescriptor.Count).AsSafeWrapper();
      binding ??= new QueryRowFilterParameterBinding(mappings, valueAccessor, false);
      return (SqlDml.DynamicFilter(binding, provider.FilteredColumns.Select(index => sourceColumns[index]).ToArray()), binding);
    }

    protected (SqlExpression, QueryParameterBinding) CreateIncludeViaTableValuedParameter(
      IncludeProvider provider, Func<ParameterContext, object> valueAccessor,
      IReadOnlyList<SqlExpression> sourceColumns, Type tableValuedParameterType)
    {
      var filterTupleDescriptor = provider.FilteredColumnsExtractionTransform.Descriptor;
      TypeMapping[] mappings = [Driver.GetTypeMapping(tableValuedParameterType == WellKnownTypes.String
        ? typeof(List<string>)
        : typeof(List<long>))];
      QueryRowFilterParameterBinding binding = new(mappings, valueAccessor, true);
      return (SqlDml.TvpDynamicFilter(binding, provider.FilteredColumns.Select(index => sourceColumns[index]).ToArray()), binding);
    }

    protected (SqlExpression, TemporaryTableDescriptor) CreateIncludeViaTemporaryTableExpression(
      IncludeProvider provider, IReadOnlyList<SqlExpression> sourceColumns)
    {
      var filterTupleDescriptor = provider.FilteredColumnsExtractionTransform.Descriptor;
      var filteredColumns = provider.FilteredColumns.Select(index => sourceColumns[index]).ToList();
      var tableDescriptor = DomainHandler.TemporaryTableManager.BuildDescriptor(Mapping, Guid.NewGuid().ToString(), filterTupleDescriptor);
      var filterQuery = tableDescriptor.QueryStatement.ShallowClone();
      var tableRef = filterQuery.From;
      for (int i = 0; i < filterTupleDescriptor.Count; i++)
        filterQuery.Where &= filteredColumns[i]==tableRef[i];
      return (SqlDml.Exists(filterQuery), tableDescriptor);
    }

    private static Func<ParameterContext, object> BuildComplexConditionRowFilterParameterAccessor(
      Func<ParameterContext, IEnumerable<Tuple>> filterDataSource) =>
      context => filterDataSource.Invoke(context).ToList();

    private static Func<ParameterContext, object> BuildAutoRowFilterParameterAccessor(
      TemporaryTableDescriptor tableDescriptor) =>
      context =>
        context.TryGetValue(SqlIncludeProvider.CreateFilterParameter(tableDescriptor), out var filterData)
          ? filterData
          : null;
  }
}
