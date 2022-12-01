// Copyright (C) 2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Linq.Expressions;
using Xtensive.Sql;
using Xtensive.Sql.Dml;
using Operator = Xtensive.Reflection.WellKnown.Operator;

namespace Xtensive.Orm.Providers.Expressions.MemberCompilers
{
#if NET6_0_OR_GREATER

  [CompilerContainer(typeof(SqlExpression))]
  internal static class DateTimeCompilers
  {
    #region Extractors

    [Compiler(typeof(DateOnly), "Year", TargetKind.PropertyGet)]
    public static SqlExpression DateOnlyYear(SqlExpression _this) =>
      ExpressionTranslationHelpers.ToInt(SqlDml.Extract(SqlDateTimePart.Year, _this));

    [Compiler(typeof(DateOnly), "Month", TargetKind.PropertyGet)]
    public static SqlExpression DateOnlyMonth(SqlExpression _this) =>
      ExpressionTranslationHelpers.ToInt(SqlDml.Extract(SqlDateTimePart.Month, _this));

    [Compiler(typeof(DateOnly), "Day", TargetKind.PropertyGet)]
    public static SqlExpression DateOnlyDay(SqlExpression _this) =>
      ExpressionTranslationHelpers.ToInt(SqlDml.Extract(SqlDateTimePart.Day, _this));


    [Compiler(typeof(DateOnly), "DayOfYear", TargetKind.PropertyGet)]
    public static SqlExpression DateTimeDayOfYear(SqlExpression _this)
    {
      return ExpressionTranslationHelpers.ToInt(SqlDml.Extract(SqlDateTimePart.DayOfYear, _this));
    }

    [Compiler(typeof(DateOnly), "DayOfWeek", TargetKind.PropertyGet)]
    public static SqlExpression DateTimeDayOfWeek(SqlExpression _this)
    {
      throw new NotImplementedException();
      //var baseExpression = ExpressionTranslationHelpers.ToInt(SqlDml.Extract(SqlDateTimePart.DayOfWeek, _this));
      //var context = ExpressionTranslationContext.Current;
      //if (context == null) {
      //  return baseExpression;
      //}
      //if (context.ProviderInfo.ProviderName == WellKnown.Provider.MySql) {
      //  return baseExpression - 1; //Mysql starts days of week from 1 unlike in .Net.
      //}
      //return baseExpression;
    }

    #endregion

    #region Constructors

    [Compiler(typeof(DateOnly), null, TargetKind.Constructor)]
    public static SqlExpression DateOnlyCtor(
        [Type(typeof(int))] SqlExpression year,
        [Type(typeof(int))] SqlExpression month,
        [Type(typeof(int))] SqlExpression day) =>
      SqlDml.DateConstruct(year, month, day);

    #endregion

    #region Operators

    [Compiler(typeof(DateOnly), Operator.Equality, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorEquality(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 == d2;
    }

    [Compiler(typeof(DateOnly), Operator.Inequality, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorInequality(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 != d2;
    }

    [Compiler(typeof(DateOnly), Operator.GreaterThan, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorGreaterThan(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 > d2;
    }

    [Compiler(typeof(DateOnly), Operator.GreaterThanOrEqual, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorGreaterThanOrEqual(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 >= d2;
    }

    [Compiler(typeof(DateOnly), Operator.LessThan, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorLessThan(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 < d2;
    }

    [Compiler(typeof(DateOnly), Operator.LessThanOrEqual, TargetKind.Operator)]
    public static SqlExpression DateOnlyOperatorLessThanOrEqual(
      [Type(typeof(DateOnly))] SqlExpression d1,
      [Type(typeof(DateOnly))] SqlExpression d2)
    {
      return d1 <= d2;
    }

    #endregion

    [Compiler(typeof(DateOnly), "AddYears")]
    public static SqlExpression DateOnlyAddYears(SqlExpression _this, [Type(typeof(int))] SqlExpression value) =>
      SqlDml.DateTimeAddYears(_this, value);

    [Compiler(typeof(DateOnly), "AddMonths")]
    public static SqlExpression DateOnlyAddMonths(SqlExpression _this, [Type(typeof(int))] SqlExpression value) =>
      SqlDml.DateTimeAddMonths(_this, value);

    [Compiler(typeof(DateOnly), "AddDays")]
    public static SqlExpression DateOnlyAddDays(SqlExpression _this, [Type(typeof(int))] SqlExpression value) =>
      SqlDml.DateAddDays(_this, value);

    [Compiler(typeof(DateOnly), "ToString")]
    public static SqlExpression DateTimeToStringIso(SqlExpression _this)
    {
      throw new NotSupportedException(Strings.ExDateTimeToStringMethodIsNotSupported);
    }

    [Compiler(typeof(DateOnly), "ToString")]
    public static SqlExpression DateTimeToStringIso(SqlExpression _this, [Type(typeof(string))] SqlExpression value)
    {
      throw new NotImplementedException();
      //var stringValue = value as SqlLiteral<string>;

      //if (stringValue == null)
      //  throw new NotSupportedException(Strings.ExTranslationOfDateTimeToStringWithArbitraryArgumentsIsNotSupported);

      //if (!stringValue.Value.Equals("s"))
      //  throw new NotSupportedException(Strings.ExTranslationOfDateTimeToStringWithArbitraryArgumentsIsNotSupported);

      //return SqlDml.DateTimeToStringIso(_this);
    }
  }
#endif
}
