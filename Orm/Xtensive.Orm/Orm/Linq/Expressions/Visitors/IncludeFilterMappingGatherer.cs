// Copyright (C) 2009-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.11.16

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Linq;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Rse;

namespace Xtensive.Orm.Linq.Expressions.Visitors
{
  internal sealed class IncludeFilterMappingGatherer(Expression filterDataTuple, ApplyParameter filteredTuple, ArraySegment<IncludeFilterMappingGatherer.MappingEntry> resultMapping)
    : ExtendedExpressionVisitor
  {
    public readonly struct MappingEntry
    {
      public ColNum ColumnIndex { get; }

      public LambdaExpression CalculatedColumn { get; }

      public MappingEntry(ColNum columnIndex)
      {
        ColumnIndex = columnIndex;
      }

      public MappingEntry(LambdaExpression calculatedColumn)
      {
        ColumnIndex = -1;
        CalculatedColumn = calculatedColumn;
      }
    }

    private static readonly ParameterExpression CalculatedColumnParameter = Expression.Parameter(WellKnownOrmTypes.Tuple, "filteredRow");
    private static readonly IReadOnlyList<ParameterExpression> CalculatedColumnParameters = [CalculatedColumnParameter];

    public static void Gather(Expression filterExpression, Expression filterDataTuple, ApplyParameter filteredTuple, ArraySegment<MappingEntry> mapping)
    {
      var visitor = new IncludeFilterMappingGatherer(filterDataTuple, filteredTuple, mapping);
      _ = visitor.Visit(filterExpression);
      if (mapping.Contains(default))
        throw Exceptions.InternalError("Failed to gather mappings for IncludeProvider", OrmLog.Instance);
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
      var result = (BinaryExpression) base.VisitBinary(b);
      var expressions = new[] {result.Left, result.Right};

      var filterDataAccessor = expressions.FirstOrDefault(e => e.StripCasts().AsTupleAccess() is { } tupleAccess && tupleAccess.Object == filterDataTuple);
      if (filterDataAccessor == null) {
        return result;
      }

      var filteredExpression = expressions.FirstOrDefault(e => e!=filterDataAccessor);
      if (filteredExpression == null) {
        return result;
      }

      var filterDataIndex = filterDataAccessor.StripCasts().GetTupleAccessArgument();
      if (resultMapping.Count <= filterDataIndex) {
        return result;
      }
      resultMapping[filterDataIndex] = CreateMappingEntry(filteredExpression);
      return result;
    }

    protected override Expression VisitMember(MemberExpression m) =>
      m.Expression is { } target
          && target.NodeType == ExpressionType.Constant
          && ((ConstantExpression) target).Value == filteredTuple
      ? CalculatedColumnParameter
      : base.VisitMember(m);

    private MappingEntry CreateMappingEntry(Expression expression) =>
      expression.StripCasts().AsTupleAccess() is { } tupleAccess
        ? new(tupleAccess.GetTupleAccessArgument())
        : new(FastExpression.Lambda(ExpressionReplacer.Replace(expression, filterDataTuple, CalculatedColumnParameter), CalculatedColumnParameters));
  }
}
