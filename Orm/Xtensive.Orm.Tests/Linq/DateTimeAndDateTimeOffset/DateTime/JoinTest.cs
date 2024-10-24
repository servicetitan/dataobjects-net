// Copyright (C) 2016-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Xtensive.Orm.Tests.Linq.DateTimeAndDateTimeOffset.Model;

namespace Xtensive.Orm.Tests.Linq.DateTimeAndDateTimeOffset.DateTimes
{
  public class JoinTest : DateTimeBaseTest
  {
    [Test]
    public void DateTimeJoinTest()
    {
      ExecuteInsideSession((s) => JoinPrivate<DateTimeEntity, DateTimeEntity, JoinResult<DateTime>, DateTime, long>(s,
        left => left.DateTime,
        right => right.DateTime,
        (left, right) => new JoinResult<DateTime> { LeftId = left.Id, RightId = right.Id, LeftDateTime = left.DateTime, RightDateTime = right.DateTime },
        c => c.LeftId,
        c => c.RightId));
#if NET_6_0_OR_GREATER
      ExecuteInsideSession(() => JoinPrivate<DateTimeEntity, DateTimeEntity, JoinResult<DateOnly>, DateOnly, long>(
        left => left.DateOnly,
        right => right.DateOnly,
        (left, right) => new JoinResult<DateOnly> { LeftId = left.Id, RightId = right.Id, LeftDateTime = left.DateOnly, RightDateTime = right.DateOnly },
        c => c.LeftId,
        c => c.RightId));
#endif
    }

    [Test]
    public void MillisecondDateTimeJoinTest()
    {
      ExecuteInsideSession((s) => JoinPrivate<MillisecondDateTimeEntity, MillisecondDateTimeEntity, JoinResult<DateTime>, DateTime, long>(s,
        left => left.DateTime,
        right => right.DateTime,
        (left, right) => new JoinResult<DateTime> { LeftId = left.Id, RightId = right.Id, LeftDateTime = left.DateTime, RightDateTime = right.DateTime },
        c => c.LeftId,
        c => c.RightId));
    }

    [Test]
    public void NullableDateTimeJoinTest()
    {
      ExecuteInsideSession((s) => JoinPrivate<NullableDateTimeEntity, NullableDateTimeEntity, JoinResult<DateTime?>, DateTime?, long>(s,
        left => left.DateTime,
        right => right.DateTime,
        (left, right) => new JoinResult<DateTime?> { LeftId = left.Id, RightId = right.Id, LeftDateTime = left.DateTime, RightDateTime = right.DateTime },
        c => c.LeftId,
        c => c.RightId));
    }

    private static void JoinPrivate<T1, T2, T3, TK1, TK3>(Session session,
      Expression<Func<T1, TK1>> leftJoinExpression, Expression<Func<T2, TK1>> rightJoinExpression,
      Expression<Func<T1, T2, T3>> joinResultExpression, Expression<Func<T3, TK3>> orderByExpression, Expression<Func<T3, TK3>> thenByExpression)
      where T1 : Entity
      where T2 : Entity
    {
      var compiledLeftJoinExpression = leftJoinExpression.Compile();
      var compiledRightJoinExpression = rightJoinExpression.Compile();
      var compiledJoinResultExpression = joinResultExpression.Compile();
      var compiledOrderByExpression = orderByExpression.Compile();
      var compiledThenByExpression = thenByExpression.Compile();
      var joinLocal = session.Query.All<T1>().ToArray()
        .Join(session.Query.All<T2>().ToArray(), compiledLeftJoinExpression, compiledRightJoinExpression, compiledJoinResultExpression)
        .OrderBy(compiledOrderByExpression)
        .ThenBy(compiledThenByExpression);

      var joinServer = session.Query.All<T1>()
        .Join(session.Query.All<T2>(), leftJoinExpression, rightJoinExpression, joinResultExpression)
        .OrderBy(orderByExpression)
        .ThenBy(thenByExpression);

      Assert.IsTrue(joinLocal.SequenceEqual(joinServer));

      joinServer = session.Query.All<T1>()
        .Join(session.Query.All<T2>(), leftJoinExpression, rightJoinExpression, joinResultExpression)
        .OrderByDescending(orderByExpression)
        .ThenBy(thenByExpression);
      Assert.IsFalse(joinLocal.SequenceEqual(joinServer));
    }
  }
}