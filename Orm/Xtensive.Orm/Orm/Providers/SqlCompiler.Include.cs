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
        algorithm = IncludeAlgorithm.ComplexCondition;
      }
      else if (algorithm == IncludeAlgorithm.Auto && tableValuedParametersSupported) {
        if (provider.FilteredColumnsExtractionTransform.Descriptor.Count == 1) {
          var fieldType = provider.FilteredColumnsExtractionTransform.Descriptor[0];
          if (fieldType == WellKnownTypes.Int64 || fieldType == WellKnownTypes.Int32 || fieldType == WellKnownTypes.String) {
            tableValuedParameterType = fieldType;
            algorithm = IncludeAlgorithm.ComplexCondition;
          }
        }
      }

      switch (algorithm) {
      case IncludeAlgorithm.Auto:
        var temporaryTableExpression = CreateIncludeViaTemporaryTableExpression(
          provider, sourceColumns, out tableDescriptor);
        (var complexConditionExpression, extraBinding) = CreateIncludeViaComplexConditionExpression(
          provider, BuildAutoRowFilterParameterAccessor(tableDescriptor),
          sourceColumns, tableValuedParameterType);
        resultExpression = SqlDml.Variant(extraBinding,
          complexConditionExpression, temporaryTableExpression);
        anyTemporaryTablesRequired = true;
        break;
      case IncludeAlgorithm.ComplexCondition:
        (resultExpression, extraBinding) = CreateIncludeViaComplexConditionExpression(
          provider, BuildComplexConditionRowFilterParameterAccessor(filterDataSource),
          sourceColumns, tableValuedParameterType);
        if (!anyTemporaryTablesRequired) {
          requestOptions |= QueryRequestOptions.AllowOptimization;
        }

        break;
      case IncludeAlgorithm.TemporaryTable:
        resultExpression = CreateIncludeViaTemporaryTableExpression(
          provider, sourceColumns, out tableDescriptor);
        anyTemporaryTablesRequired = true;
        break;
      default:
        throw new ArgumentOutOfRangeException("provider.Algorithm");
      }
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
      IReadOnlyList<SqlExpression> sourceColumns, Type tableValuedParameterType)
    {
      var filterTupleDescriptor = provider.FilteredColumnsExtractionTransform.Descriptor;
      var mappings = tableValuedParameterType != null
        ? [Driver.GetTypeMapping(tableValuedParameterType == WellKnownTypes.String ? typeof(List<string>) : typeof(List<long>))]
        : filterTupleDescriptor.Select(type => Driver.GetTypeMapping(type)).ToArray(filterTupleDescriptor.Count).AsSafeWrapper();
      QueryRowFilterParameterBinding binding = new(mappings, valueAccessor, tableValuedParameterType != null);
      return tableValuedParameterType != null
        ? (SqlDml.TvpDynamicFilter(binding, provider.FilteredColumns.Select(index => sourceColumns[index]).ToArray()), binding)
        : (SqlDml.DynamicFilter(binding, provider.FilteredColumns.Select(index => sourceColumns[index]).ToArray()), binding);
    }

    protected SqlExpression CreateIncludeViaTemporaryTableExpression(
      IncludeProvider provider, IReadOnlyList<SqlExpression> sourceColumns,
      out TemporaryTableDescriptor tableDescriptor)
    {
      var filterTupleDescriptor = provider.FilteredColumnsExtractionTransform.Descriptor;
      var filteredColumns = provider.FilteredColumns.Select(index => sourceColumns[index]).ToList();
      tableDescriptor = DomainHandler.TemporaryTableManager
        .BuildDescriptor(Mapping, Guid.NewGuid().ToString(), filterTupleDescriptor);
      var filterQuery = tableDescriptor.QueryStatement.ShallowClone();
      var tableRef = filterQuery.From;
      for (int i = 0; i < filterTupleDescriptor.Count; i++)
        filterQuery.Where &= filteredColumns[i]==tableRef[i];
      var resultExpression = SqlDml.Exists(filterQuery);
      return resultExpression;
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
