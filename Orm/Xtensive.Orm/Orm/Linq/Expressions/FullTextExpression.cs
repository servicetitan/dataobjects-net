// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Gamzov
// Created:    2009.12.28

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Model;
using Xtensive.Orm.Linq.Expressions.Visitors;
using Xtensive.Reflection;

namespace Xtensive.Orm.Linq.Expressions
{
  [Serializable]
  internal sealed class FullTextExpression : ParameterizedExpression
  {
    public FullTextIndexInfo FullTextIndex { get; private set; }

    public ColumnExpression RankExpression { get; private set; }

    public EntityExpression EntityExpression { get; private set; }

    public override Expression BindParameter(ParameterExpression parameter, Dictionary<Expression, Expression> processedExpressions)
    {
      Expression result;
      if (processedExpressions.TryGetValue(this, out result))
        return result;

      var entityExpression = (EntityExpression) EntityExpression.BindParameter(parameter, processedExpressions);
      var rankExpression = (ColumnExpression) RankExpression.BindParameter(parameter, processedExpressions);
      return new FullTextExpression(FullTextIndex, entityExpression, rankExpression, parameter);
    }

    public override Expression RemoveOuterParameter(Dictionary<Expression, Expression> processedExpressions)
    {
      Expression result;
      if (processedExpressions.TryGetValue(this, out result))
        return result;

      var entityExpression = (EntityExpression) EntityExpression.RemoveOuterParameter(processedExpressions);
      var rankExpression = (ColumnExpression) RankExpression.RemoveOuterParameter(processedExpressions);
      return new FullTextExpression(FullTextIndex, entityExpression, rankExpression, null);
    }

    public override Expression Remap(ColNum offset, Dictionary<Expression, Expression> processedExpressions)
    {
      if (!CanRemap)
        return this;

      Expression result;
      if (processedExpressions.TryGetValue(this, out result))
        return result;

      var remappedEntityExpression = EntityExpression.Remap(offset, processedExpressions);
      var remappedRankExpression = RankExpression.Remap(offset, processedExpressions);
      return new FullTextExpression(FullTextIndex, remappedEntityExpression, remappedRankExpression, OuterParameter);
    }

    public override Expression Remap(ColumnMap map, Dictionary<Expression, Expression> processedExpressions)
    {
      if (!CanRemap)
        return this;

      Expression result;
      if (processedExpressions.TryGetValue(this, out result))
        return result;

      var remappedEntityExpression = EntityExpression.Remap(map, processedExpressions);
      var remappedRankExpression = RankExpression.Remap(map, processedExpressions);
      return new FullTextExpression(FullTextIndex, remappedEntityExpression, remappedRankExpression, OuterParameter);
    }

    internal override Expression Accept(ExtendedExpressionVisitor visitor) => visitor.VisitFullTextExpression(this);

    public FullTextExpression(FullTextIndexInfo fullTextIndex, EntityExpression entityExpression, ColumnExpression rankExpression, ParameterExpression parameter)
      : base(ExtendedExpressionType.FullText, WellKnownOrmTypes.FullTextMatchOfT.CachedMakeGenericType(fullTextIndex.PrimaryIndex.ReflectedType.UnderlyingType), parameter, false)
    {
      FullTextIndex = fullTextIndex;
      RankExpression = rankExpression;
      EntityExpression = entityExpression;
    }
  }
}