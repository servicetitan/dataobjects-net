// Copyright (C) 2008-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2008.12.18

using System.Linq.Expressions;
using ExpressionVisitor = Xtensive.Linq.ExpressionVisitor;

namespace Xtensive.Orm.Linq
{
  internal sealed class ParameterExtractor(ExpressionEvaluator evaluator) : ExpressionVisitor
  {
    private bool isParameter;

    public bool IsParameter(Expression e)
    {
      if (!evaluator.CanBeEvaluated(e)) {
        return false;
      }

      isParameter = false;
      Visit(e);
      return isParameter;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
      isParameter = true;
      return base.VisitMember(m);
    }

    protected override Expression VisitUnknown(Expression e) => e;

    protected override Expression VisitConstant(ConstantExpression c)
    {
      isParameter |= c.GetMemberType() is MemberType.Entity or MemberType.Structure;
      return c;
    }
  }
}