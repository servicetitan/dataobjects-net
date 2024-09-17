// Copyright (C) 2007-2023 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Xtensive.Collections;
using Xtensive.Core;
using Xtensive.Reflection;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;
using System.Linq;

namespace Xtensive.Sql
{
  /// <summary>
  /// A factory for SQL DML operations.
  /// </summary>
  public static class SqlDml
  {
    private static readonly Type
      SqlArrayType = typeof(SqlArray<>),
      SqlLiteralType = typeof(SqlLiteral<>);

    public static readonly SqlDefaultValue DefaultValue = new SqlDefaultValue();
    public static readonly SqlNull Null = new SqlNull();
    public static readonly SqlBreak Break = new SqlBreak();
    public static readonly SqlContinue Continue = new SqlContinue();
    public static readonly SqlNative Asterisk = Native("*");

    #region Aggregates

    public static SqlAggregate Count()
    {
      return Count(Asterisk);
    }

    public static SqlAggregate Count(SqlExpression expression)
    {
      return Count(expression, false);
    }

    public static SqlAggregate Count(SqlExpression expression, bool distinct)
    {
      return new SqlAggregate(SqlNodeType.Count, expression, distinct);
    }

    public static SqlAggregate Avg(SqlExpression expression)
    {
      return Avg(expression, false);
    }

    public static SqlAggregate Avg(SqlExpression expression, bool distinct)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlAggregate(SqlNodeType.Avg, expression, distinct);
    }

    public static SqlAggregate Sum(SqlExpression expression)
    {
      return Sum(expression, false);
    }

    public static SqlAggregate Sum(SqlExpression expression, bool distinct)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlAggregate(SqlNodeType.Sum, expression, distinct);
    }

    public static SqlAggregate Min(SqlExpression expression)
    {
      return Min(expression, false);
    }

    public static SqlAggregate Min(SqlExpression expression, bool distinct)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlAggregate(SqlNodeType.Min, expression, distinct);
    }

    public static SqlAggregate Max(SqlExpression expression)
    {
      return Max(expression, false);
    }

    public static SqlAggregate Max(SqlExpression expression, bool distinct)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlAggregate(SqlNodeType.Max, expression, distinct);
    }

    #endregion

    #region Arithmetic

    public static SqlBinary Add(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.Add, left, right);
    }

    public static SqlBinary Subtract(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.Subtract, left, right);
    }

    public static SqlBinary Multiply(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.Multiply, left, right);
    }

    public static SqlBinary Divide(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.Divide, left, right);
    }

    public static SqlBinary Modulo(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.Modulo, left, right);
    }

    #endregion

    #region Array

    public static SqlArray Array(IEnumerable<object> values)
    {
      var valueList = values.ToList();
      if (valueList.Count==0)
        return Array(System.Array.Empty<int>());
      var itemType = valueList[0].GetType();
      foreach (var t in values.Select(value => value.GetType())) {
        if (!itemType.IsAssignableFrom(t))
          throw new ArgumentException(Strings.ExTypesOfValuesAreDifferent);
      }
      var resultType = SqlArrayType.CachedMakeGenericType(itemType);
      var result = Activator.CreateInstance(
        resultType,
        BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic,
        null, new object[] {valueList}, null);
      return (SqlArray) result;
    }

    public static SqlArray<bool> Array(params bool[] value)
    {
      return new SqlArray<bool>(value);
    }

    public static SqlArray<char> Array(params char[] value)
    {
      return new SqlArray<char>(value);
    }

    public static SqlArray<sbyte> Array(params sbyte[] value)
    {
      return new SqlArray<sbyte>(value);
    }

    public static SqlArray<byte> Array(params byte[] value)
    {
      return new SqlArray<byte>(value);
    }

    public static SqlArray<short> Array(params short[] value)
    {
      return new SqlArray<short>(value);
    }

    public static SqlArray<ushort> Array(params ushort[] value)
    {
      return new SqlArray<ushort>(value);
    }

    public static SqlArray<int> Array(params int[] value)
    {
      return new SqlArray<int>(value);
    }

    public static SqlArray<uint> Array(params uint[] value)
    {
      return new SqlArray<uint>(value);
    }

    public static SqlArray<long> Array(params long[] value)
    {
      return new SqlArray<long>(value);
    }

    public static SqlArray<ulong> Array(params ulong[] value)
    {
      return new SqlArray<ulong>(value);
    }

    public static SqlArray<float> Array(params float[] value)
    {
      return new SqlArray<float>(value);
    }

    public static SqlArray<double> Array(params double[] value)
    {
      return new SqlArray<double>(value);
    }

    public static SqlArray<decimal> Array(params decimal[] value)
    {
      return new SqlArray<decimal>(value);
    }

    public static SqlArray<string> Array(params string[] value)
    {
      return new SqlArray<string>(value);
    }

    public static SqlArray<DateTime> Array(params DateTime[] value)
    {
      return new SqlArray<DateTime>(value);
    }

    public static SqlArray<TimeSpan> Array(params TimeSpan[] value)
    {
      return new SqlArray<TimeSpan>(value);
    }

    #endregion

    #region Binary

    public static SqlBinary BitAnd(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.BitAnd, left, right);
    }

    public static SqlBinary BitOr(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.BitOr, left, right);
    }

    public static SqlBinary BitXor(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      return Binary(SqlNodeType.BitXor, left, right);
    }

    public static SqlBinary And(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsBooleanExpression(left);
      SqlValidator.EnsureIsBooleanExpression(right);
      return Binary(SqlNodeType.And, left, right);
    }

    public static SqlBinary Or(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsBooleanExpression(left);
      SqlValidator.EnsureIsBooleanExpression(right);
      return Binary(SqlNodeType.Or, left, right);
    }

    public static SqlQueryExpression Except(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Except(left, right, false);
    }

    public static SqlQueryExpression ExceptAll(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Except(left, right, true);
    }

    private static SqlQueryExpression Except(ISqlQueryExpression left, ISqlQueryExpression right, bool all)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlQueryExpression(SqlNodeType.Except, left, right, all);
    }

    public static SqlQueryExpression Intersect(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Intersect(left, right, false);
    }

    public static SqlQueryExpression IntersectAll(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Intersect(left, right, true);
    }

    private static SqlQueryExpression Intersect(ISqlQueryExpression left, ISqlQueryExpression right, bool all)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlQueryExpression(SqlNodeType.Intersect, left, right, all);
    }

    public static SqlQueryExpression Union(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Union(left, right, false);
    }

    public static SqlQueryExpression UnionAll(ISqlQueryExpression left, ISqlQueryExpression right)
    {
      return Union(left, right, true);
    }

    private static SqlQueryExpression Union(ISqlQueryExpression left, ISqlQueryExpression right, bool all)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlQueryExpression(SqlNodeType.Union, left, right, all);
    }

    public static SqlBinary In(SqlExpression left, ISqlQueryExpression right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.In, left, SubQuery(right));
    }

    public static SqlBinary In(SqlExpression left, SqlRow right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.In, left, right);
    }

    public static SqlBinary In(SqlExpression left, SqlArray right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.In, left, right);
    }

    public static SqlBinary NotIn(SqlExpression left, ISqlQueryExpression right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.NotIn, left, SubQuery(right));
    }

    public static SqlBinary NotIn(SqlExpression left, SqlRow right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.NotIn, left, right);
    }

    public static SqlBinary NotIn(SqlExpression left, SqlArray right)
    {
      SqlValidator.EnsureIsRowValueConstructor(left);
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.NotIn, left, right);
    }

    public static SqlBetween Between(
      SqlExpression expression, SqlExpression leftBoundary, SqlExpression rightBoundary)
    {
      SqlValidator.EnsureIsRowValueConstructor(expression);
      SqlValidator.EnsureIsRowValueConstructor(leftBoundary);
      SqlValidator.EnsureIsRowValueConstructor(rightBoundary);
      return
        new SqlBetween(
          SqlNodeType.Between, expression, leftBoundary, rightBoundary);
    }

    public static SqlBetween NotBetween(
      SqlExpression expression, SqlExpression leftBoundary, SqlExpression rightBoundary)
    {
      SqlValidator.EnsureIsRowValueConstructor(expression);
      SqlValidator.EnsureIsRowValueConstructor(leftBoundary);
      SqlValidator.EnsureIsRowValueConstructor(rightBoundary);
      return
        new SqlBetween(
          SqlNodeType.NotBetween, expression, leftBoundary, rightBoundary);
    }

    internal static SqlBinary Binary(SqlNodeType nodeType, SqlExpression left, SqlExpression right)
    {
      return new SqlBinary(nodeType, left, right);
    }

    #endregion

    #region Cast

    public static SqlCast Cast(SqlExpression operand, SqlValueType type)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      return new SqlCast(operand, type);
    }

    public static SqlCast Cast(SqlExpression operand, SqlType type)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlCast(operand, new SqlValueType(type));
    }

    public static SqlCast Cast(SqlExpression operand, SqlType type, int size)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlCast(operand, new SqlValueType(type, size));
    }

    public static SqlCast Cast(SqlExpression operand, SqlType type, short precision, short scale)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlCast(operand, new SqlValueType(type, precision, scale));
    }

    public static SqlCast Cast(SqlExpression operand, SqlType type, short precision)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlCast(operand, new SqlValueType(type, precision, 0));
    }

    #endregion

    #region Comparison

    public static SqlBinary Equals(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.Equals, left, right);
    }

    public static SqlBinary NotEquals(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.NotEquals, left, right);
    }

    public static SqlBinary GreaterThan(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.GreaterThan, left, right);
    }

    public static SqlBinary LessThan(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.LessThan, left, right);
    }

    public static SqlBinary GreaterThanOrEquals(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.GreaterThanOrEquals, left, right);
    }

    public static SqlBinary LessThanOrEquals(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return Binary(SqlNodeType.LessThanOrEquals, left, right);
    }

    #endregion

    # region Date and/or Time functions

    public static SqlFunctionCall CurrentDate()
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentDate);
    }

    /*
    public static SqlFunctionCall CurrentTime()
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentTime);
    }

    public static SqlFunctionCall CurrentTime(uint precision)
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentTime, Literal(precision));
    }

    public static SqlFunctionCall CurrentTime(SqlExpression precision)
    {
      SqlValidator.EnsureIsArithmeticExpression(precision);
      return new SqlFunctionCall(SqlFunctionType.CurrentTime, precision);
    }
    */

    public static SqlFunctionCall CurrentTimeStamp()
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentTimeStamp);
    }

    public static SqlFunctionCall CurrentTimeStamp(uint precision)
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentTimeStamp, Literal(precision));
    }

    public static SqlFunctionCall CurrentTimeStamp(SqlExpression precision)
    {
      SqlValidator.EnsureIsArithmeticExpression(precision);
      return new SqlFunctionCall(SqlFunctionType.CurrentTimeStamp, precision);
    }

    public static SqlExtract Extract(SqlDateTimePart part, SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsArithmeticExpression(operand);
      if (part == SqlDateTimePart.Nothing) {
        throw new ArgumentException(string.Format("Unable to extract {0} part", SqlDateTimePart.Nothing.ToString()));
      }
      return new SqlExtract(part, operand);
    }

    public static SqlExtract Extract(SqlDatePart part, SqlExpression operand)
    {
      ArgumentNullException.ThrowIfNull(operand, nameof(operand));
      SqlValidator.EnsureIsArithmeticExpression(operand);
      if (part == SqlDatePart.Nothing) {
        throw new ArgumentException(string.Format("Unable to extract {0} part", SqlDatePart.Nothing.ToString()));
      }
      return new SqlExtract(part, operand);
    }

    public static SqlExtract Extract(SqlTimePart part, SqlExpression operand)
    {
      ArgumentNullException.ThrowIfNull(operand, nameof(operand));
      SqlValidator.EnsureIsArithmeticExpression(operand);
      if (part == SqlTimePart.Nothing) {
        throw new ArgumentException(string.Format("Unable to extract {0} part", SqlTimePart.Nothing.ToString()));
      }
      return new SqlExtract(part, operand);
    }

    public static SqlExtract Extract(SqlIntervalPart part, SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsArithmeticExpression(operand);
      if (part == SqlIntervalPart.Nothing) {
        throw new ArgumentException(string.Format("Unable to extract {0} part", SqlIntervalPart.Nothing.ToString()));
      }
      return new SqlExtract(part, operand);
    }

    public static SqlFunctionCall DateTimeConstruct(SqlExpression year, SqlExpression month, SqlExpression day)
    {
      ArgumentValidator.EnsureArgumentNotNull(year, "year");
      ArgumentValidator.EnsureArgumentNotNull(month, "month");
      ArgumentValidator.EnsureArgumentNotNull(day, "day");
      SqlValidator.EnsureIsArithmeticExpression(year);
      SqlValidator.EnsureIsArithmeticExpression(month);
      SqlValidator.EnsureIsArithmeticExpression(day);
      return new SqlFunctionCall(SqlFunctionType.DateTimeConstruct, year, month, day);
    }

    public static SqlFunctionCall DateConstruct(SqlExpression year, SqlExpression month, SqlExpression day)
    {
      ArgumentNullException.ThrowIfNull(year);
      ArgumentNullException.ThrowIfNull(month);
      ArgumentNullException.ThrowIfNull(day);
      SqlValidator.EnsureIsArithmeticExpression(year);
      SqlValidator.EnsureIsArithmeticExpression(month);
      SqlValidator.EnsureIsArithmeticExpression(day);
      return new SqlFunctionCall(SqlFunctionType.DateConstruct, year, month, day);
    }

    public static SqlFunctionCall TimeConstruct(SqlExpression hour,
      SqlExpression minute,
      SqlExpression second,
      SqlExpression millisecond)
    {
      ArgumentNullException.ThrowIfNull(hour);
      ArgumentNullException.ThrowIfNull(minute);
      ArgumentNullException.ThrowIfNull(second);
      ArgumentNullException.ThrowIfNull(millisecond);
      SqlValidator.EnsureIsArithmeticExpression(hour);
      SqlValidator.EnsureIsArithmeticExpression(minute);
      SqlValidator.EnsureIsArithmeticExpression(second);
      SqlValidator.EnsureIsArithmeticExpression(millisecond);

      return new SqlFunctionCall(SqlFunctionType.TimeConstruct, hour, minute, second, millisecond);
    }

    public static SqlFunctionCall TimeConstruct(SqlExpression ticks)
    {
      ArgumentNullException.ThrowIfNull(ticks);
      SqlValidator.EnsureIsArithmeticExpression(ticks);

      return new SqlFunctionCall(SqlFunctionType.TimeConstruct, ticks);
    }

    public static SqlBinary DateTimePlusInterval(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimePlusInterval, left, right);
    }

    public static SqlBinary TimePlusInterval(SqlExpression left, SqlExpression right)
    {
      ArgumentNullException.ThrowIfNull(left, nameof(left));
      ArgumentNullException.ThrowIfNull(right, nameof(right));
      return new SqlBinary(SqlNodeType.TimePlusInterval, left, right);
    }

    public static SqlBinary TimeMinusTime(SqlExpression left, SqlExpression right)
    {
      ArgumentNullException.ThrowIfNull(left, nameof(left));
      ArgumentNullException.ThrowIfNull(right, nameof(right));
      return new SqlBinary(SqlNodeType.TimeMinusTime, left, right);
    }

    public static SqlBinary DateTimeMinusInterval(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimeMinusInterval, left, right);
    }

    public static SqlBinary DateTimeMinusDateTime(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimeMinusDateTime, left, right);
    }

    public static SqlFunctionCall DateTimeAddYears(SqlExpression source, SqlExpression years)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      ArgumentValidator.EnsureArgumentNotNull(years, "years");
      return new SqlFunctionCall(SqlFunctionType.DateTimeAddYears, source, years);
    }

    public static SqlFunctionCall DateTimeAddMonths(SqlExpression source, SqlExpression months)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      ArgumentValidator.EnsureArgumentNotNull(months, "months");
      return new SqlFunctionCall(SqlFunctionType.DateTimeAddMonths, source, months);
    }

    public static SqlFunctionCall DateTimeToTime(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.DateTimeToTime, expression);
    }

    public static SqlFunctionCall DateAddYears(SqlExpression source, SqlExpression years)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      ArgumentNullException.ThrowIfNull(years, nameof(years));
      return new SqlFunctionCall(SqlFunctionType.DateAddYears, source, years);
    }

    public static SqlFunctionCall DateAddMonths(SqlExpression source, SqlExpression months)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      ArgumentNullException.ThrowIfNull(months, nameof(months));
      return new SqlFunctionCall(SqlFunctionType.DateAddMonths, source, months);
    }

    public static SqlFunctionCall DateAddDays(SqlExpression source, SqlExpression days)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      ArgumentNullException.ThrowIfNull(days, nameof(days));
      return new SqlFunctionCall(SqlFunctionType.DateAddDays, source, days);
    }

    public static SqlFunctionCall DateToString(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.DateToString, expression);
    }

    public static SqlFunctionCall DateToDateTime(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.DateToDateTime, expression);
    }

    public static SqlFunctionCall DateTimeToDate(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.DateTimeToDate, expression);
    }

    public static SqlFunctionCall DateToDateTimeOffset(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.DateToDateTimeOffset, expression);
    }

    public static SqlFunctionCall TimeAddHours(SqlExpression source, SqlExpression hours)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      ArgumentNullException.ThrowIfNull(hours, nameof(hours));
      return new SqlFunctionCall(SqlFunctionType.TimeAddHours, source, hours);
    }

    public static SqlFunctionCall TimeAddMinutes(SqlExpression source, SqlExpression minutes)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      ArgumentNullException.ThrowIfNull(minutes, nameof(minutes));
      return new SqlFunctionCall(SqlFunctionType.TimeAddMinutes, source, minutes);
    }

    public static SqlFunctionCall TimeToString(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.TimeToString, expression);
    }

    public static SqlFunctionCall TimeToDateTime(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.TimeToDateTime, expression);
    }

    public static SqlExpression TimeToNanoseconds(SqlExpression source)
    {
      ArgumentNullException.ThrowIfNull(source, nameof(source));
      return new SqlFunctionCall(SqlFunctionType.TimeToNanoseconds, source);
    }

    public static SqlFunctionCall TimeToDateTimeOffset(SqlExpression expression)
    {
      ArgumentNullException.ThrowIfNull(expression, nameof(expression));
      return new SqlFunctionCall(SqlFunctionType.TimeToDateTimeOffset, expression);
    }

    public static SqlFunctionCall DateTimeToStringIso(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlFunctionCall(SqlFunctionType.DateTimeToStringIso, expression);
    }

    public static SqlFunctionCall DateTimeTruncate(SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      return new SqlFunctionCall(SqlFunctionType.DateTimeTruncate, source);
    }

    public static SqlFunctionCall IntervalConstruct(SqlExpression nanoseconds)
    {
      ArgumentValidator.EnsureArgumentNotNull(nanoseconds, "nanoseconds");
      return new SqlFunctionCall(SqlFunctionType.IntervalConstruct, nanoseconds);
    }

    public static SqlFunctionCall IntervalToMilliseconds(SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      return new SqlFunctionCall(SqlFunctionType.IntervalToMilliseconds, source);
    }

    public static SqlFunctionCall IntervalToNanoseconds(SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      return new SqlFunctionCall(SqlFunctionType.IntervalToNanoseconds, source);
    }

    public static SqlFunctionCall IntervalAbs(SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      return new SqlFunctionCall(SqlFunctionType.IntervalAbs, source);
    }

    public static SqlFunctionCall IntervalNegate(SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      return new SqlFunctionCall(SqlFunctionType.IntervalNegate, source);
    }

    #endregion

    # region DateTimeOffset functions

    public static SqlFunctionCall CurrentDateTimeOffset()
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentDateTimeOffset);
    }

    public static SqlExtract Extract(SqlDateTimeOffsetPart part, SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsArithmeticExpression(operand);
      if (part==SqlDateTimeOffsetPart.Nothing)
        throw new ArgumentException();
      return new SqlExtract(part, operand);
    }

    public static SqlFunctionCall DateTimeOffsetConstruct(SqlExpression dateTime, SqlExpression offset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTime, "dateTime");
      ArgumentValidator.EnsureArgumentNotNull(offset, "offset");
      SqlValidator.EnsureIsArithmeticExpression(offset);
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetConstruct, dateTime, offset);
    }

    public static SqlBinary DateTimeOffsetPlusInterval(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimeOffsetPlusInterval, left, right);
    }

    public static SqlBinary DateTimeOffsetMinusInterval(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimeOffsetMinusInterval, left, right);
    }

    public static SqlBinary DateTimeOffsetMinusDateTimeOffset(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.DateTimeOffsetMinusDateTimeOffset, left, right);
    }

    public static SqlFunctionCall DateTimeOffsetAddYears(SqlExpression source, SqlExpression years)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      ArgumentValidator.EnsureArgumentNotNull(years, "years");
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetAddYears, source, years);
    }

    public static SqlFunctionCall DateTimeOffsetAddMonths(SqlExpression source, SqlExpression months)
    {
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      ArgumentValidator.EnsureArgumentNotNull(months, "months");
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetAddMonths, source, months);
    }

    public static SqlFunctionCall DateTimeOffsetTimeOfDay(SqlExpression dateTimeOffset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTimeOffset, "dateTimeOffset");
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetTimeOfDay, dateTimeOffset);
    }

    public static SqlFunctionCall DateTimeOffsetToLocalTime(SqlExpression dateTimeOffset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTimeOffset, "dateTimeOffset");
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetToLocalTime, dateTimeOffset);
    }

    public static SqlFunctionCall DateTimeOffsetToUtcTime(SqlExpression dateTimeOffset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTimeOffset, "dateTimeOffset");
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetToUtcTime, dateTimeOffset);
    }

    public static SqlFunctionCall DateTimeToDateTimeOffset(SqlExpression dateTime)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTime, "dateTime");
      return new SqlFunctionCall(SqlFunctionType.DateTimeToDateTimeOffset, dateTime);
    }

    public static SqlFunctionCall DateTimeOffsetToDateTime(SqlExpression dateTimeOffset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTimeOffset, nameof(dateTimeOffset));
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetToDateTime, dateTimeOffset);
    }

    public static SqlFunctionCall DateTimeOffsetToTime(SqlExpression dateTimeOffset)
    {
      ArgumentNullException.ThrowIfNull(dateTimeOffset, nameof(dateTimeOffset));
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetToTime, dateTimeOffset);
    }

    public static SqlFunctionCall DateTimeOffsetToDate(SqlExpression dateTimeOffset)
    {
      ArgumentValidator.EnsureArgumentNotNull(dateTimeOffset, nameof(dateTimeOffset));
      ArgumentNullException.ThrowIfNull(dateTimeOffset, nameof(dateTimeOffset));
      return new SqlFunctionCall(SqlFunctionType.DateTimeOffsetToDate, dateTimeOffset);
    }

    #endregion

    #region FunctionCall

    public static SqlUserFunctionCall FunctionCall(string name, IReadOnlyList<SqlExpression> expressions)
    {
      SqlValidator.EnsureAreSqlRowArguments(expressions);
      return new SqlUserFunctionCall(name, expressions);
    }

    public static SqlUserFunctionCall FunctionCall(string name, params SqlExpression[] expressions)
    {
      SqlValidator.EnsureAreSqlRowArguments(expressions);
      return new SqlUserFunctionCall(name, expressions);
    }

    public static SqlUserFunctionCall FunctionCall(string name) =>
      new SqlUserFunctionCall(name, System.Array.Empty<SqlExpression>());

    public static SqlFunctionCall CurrentUser()
    {
      return new SqlFunctionCall(SqlFunctionType.CurrentUser);
    }

    public static SqlFunctionCall SessionUser()
    {
      return new SqlFunctionCall(SqlFunctionType.SessionUser);
    }

    public static SqlFunctionCall SystemUser()
    {
      return new SqlFunctionCall(SqlFunctionType.SystemUser);
    }

    public static SqlFunctionCall User()
    {
      return new SqlFunctionCall(SqlFunctionType.User);
    }

    public static SqlFunctionCall NullIf(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      //SqlCase c = new SqlCase(null);
      //c[left==right] = Null;
      //c.Else = left;
      //return c;
      return new SqlFunctionCall(SqlFunctionType.NullIf, left, right);
    }

    public static SqlFunctionCall Coalesce(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      //SqlCase c = new SqlCase(null);
      //c[IsNotNull(left)] = left;
      //c.Else = right;
      //return c;
      return new SqlFunctionCall(SqlFunctionType.Coalesce, left, right);
    }

    public static SqlFunctionCall Coalesce(
      SqlExpression left, SqlExpression right, params SqlExpression[] values)
    {
      //if (values!=null && values.Length>0) {
      //  SqlExpression[] v = null;
      //  if (values.Length>1) {
      //    v = new SqlExpression[values.Length-1];
      //    for (int i = 1, l = values.Length; i<l; i++)
      //      v[i-1] = values[i];
      //  }
      //  ArgumentValidator.EnsureArgumentNotNull(left, "left");
      //  SqlValidator.VerifyArithmeticalOperatorsArgs(left);
      //  SqlCase c = new SqlCase(null);
      //  c[IsNotNull(left)] = left;
      //  c.Else = Coalesce(right, values[0], v);
      //  return c;
      //}
      //else {
      //  return Coalesce(left, right);
      //}
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(left);
      SqlValidator.EnsureIsArithmeticExpression(right);
      SqlExpression[] expressions;
      if (values!=null) {
        int l = values.Length;
        expressions = new SqlExpression[l+2];
        expressions[0] = left;
        expressions[1] = right;
        for (int i = 0; i<l; i++) {
          ArgumentValidator.EnsureArgumentNotNull(values[i], "values");
          SqlValidator.EnsureIsArithmeticExpression(values[i]);
          expressions[i+2] = values[i];
        }
      }
      else
        expressions = new SqlExpression[2] {left, right};
      return new SqlFunctionCall(SqlFunctionType.Coalesce, expressions);
    }

    #endregion

    #region Join

    public static SqlJoinedTable Join(SqlJoinType joinType, SqlTable left, SqlTable right)
    {
      return Join(joinType, left, right, null);
    }

    public static SqlJoinedTable Join(SqlJoinType joinType, SqlTable left, SqlTable right,
      IReadOnlyList<SqlColumn> leftColumns, IReadOnlyList<SqlColumn> rightColumns)
    {
      return Join(joinType, left, right, leftColumns, rightColumns, null);
    }

    public static SqlJoinedTable Join(SqlJoinType joinType, SqlTable left, SqlTable right, SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      if (expression is not null && (joinType == SqlJoinType.CrossApply || joinType == SqlJoinType.LeftOuterApply))
        throw new ArgumentException(Strings.ExJoinExpressionShouldBeNullForCrossApplyAndOuterApply, "expression");
      return new SqlJoinedTable(new SqlJoinExpression(joinType, left, right, expression));
    }

    public static SqlJoinedTable Join(SqlJoinType joinType, SqlTable left, SqlTable right,
      IReadOnlyList<SqlColumn> leftColumns, IReadOnlyList<SqlColumn> rightColumns, SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      if (expression is not null && (joinType == SqlJoinType.CrossApply || joinType == SqlJoinType.LeftOuterApply))
        throw new ArgumentException(Strings.ExJoinExpressionShouldBeNullForCrossApplyAndOuterApply, "expression");
      return new SqlJoinedTable(new SqlJoinExpression(joinType, left, right, expression), leftColumns, rightColumns);
    }

    public static SqlJoinedTable Join(SqlTable left, SqlTable right, params SqlColumn[] columns)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlJoinedTable(new SqlJoinExpression(SqlJoinType.UsingJoin, left, right, Row(columns)));
    }

    #endregion

    #region Literal

    public static SqlLiteral<bool> Literal(bool value)
    {
      return new SqlLiteral<bool>(value);
    }

    public static SqlLiteral<char> Literal(char value)
    {
      return new SqlLiteral<char>(value);
    }

    public static SqlLiteral<sbyte> Literal(sbyte value)
    {
      return new SqlLiteral<sbyte>(value);
    }

    public static SqlLiteral<byte> Literal(byte value)
    {
      return new SqlLiteral<byte>(value);
    }

    public static SqlLiteral<short> Literal(short value)
    {
      return new SqlLiteral<short>(value);
    }

    public static SqlLiteral<ushort> Literal(ushort value)
    {
      return new SqlLiteral<ushort>(value);
    }

    public static SqlLiteral<int> Literal(int value)
    {
      return new SqlLiteral<int>(value);
    }

    public static SqlLiteral<uint> Literal(uint value)
    {
      return new SqlLiteral<uint>(value);
    }

    public static SqlLiteral<long> Literal(long value)
    {
      return new SqlLiteral<long>(value);
    }

    public static SqlLiteral<ulong> Literal(ulong value)
    {
      return new SqlLiteral<ulong>(value);
    }

    public static SqlLiteral<float> Literal(float value)
    {
      return new SqlLiteral<float>(value);
    }

    public static SqlLiteral<double> Literal(double value)
    {
      return new SqlLiteral<double>(value);
    }

    public static SqlLiteral<decimal> Literal(decimal value)
    {
      return new SqlLiteral<decimal>(value);
    }

    public static SqlLiteral<string> Literal(string value)
    {
      return new SqlLiteral<string>(value);
    }

    public static SqlLiteral<DateTime> Literal(DateTime value)
    {
      return new SqlLiteral<DateTime>(value);
    }

    public static SqlLiteral<DateOnly> Literal(DateOnly value)
    {
      return new SqlLiteral<DateOnly>(value);
    }

    public static SqlLiteral<TimeOnly> Literal(TimeOnly value)
    {
      return new SqlLiteral<TimeOnly>(value);
    }

    public static SqlLiteral<TimeSpan> Literal(TimeSpan value)
    {
      return new SqlLiteral<TimeSpan>(value);
    }

    public static SqlLiteral<byte[]> Literal(byte[] value)
    {
      return new SqlLiteral<byte[]>(value);
    }

    public static SqlLiteral<Guid> Literal(Guid value)
    {
      return new SqlLiteral<Guid>(value);
    }

    public static SqlLiteral Literal(object value)
    {
      var valueType = value.GetType();
      var resultType = SqlLiteralType.CachedMakeGenericType(valueType);
      var result = Activator.CreateInstance(
        resultType,
        BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic,
        null, new[] {value}, null);
      return (SqlLiteral) result;
    }

    public static SqlExpression LiteralOrContainer(object value)
    {
      var valueType = value.GetType();
      return SqlValidator.IsLiteralTypeSupported(valueType)
        ? (SqlExpression) Literal(value)
        : Container(value);
    }

    #endregion

    # region Match

    public static SqlMatch Match(SqlRow value, ISqlQueryExpression query, bool unique, SqlMatchType matchType)
    {
      ArgumentValidator.EnsureArgumentNotNull(value, "value");
      ArgumentValidator.EnsureArgumentNotNull(query, "query");
      return new SqlMatch(value, SubQuery(query), unique, matchType);
    }

    public static SqlMatch Match(SqlRow value, ISqlQueryExpression query, bool unique)
    {
      return Match(value, query, unique, SqlMatchType.None);
    }

    public static SqlMatch Match(SqlRow value, ISqlQueryExpression query, SqlMatchType matchType)
    {
      return Match(value, query, false, matchType);
    }

    public static SqlMatch Match(SqlRow value, ISqlQueryExpression query)
    {
      return Match(value, query, false, SqlMatchType.None);
    }

    public static SqlMatch Match(ISqlQueryExpression value, ISqlQueryExpression query, bool unique, SqlMatchType matchType)
    {
      ArgumentValidator.EnsureArgumentNotNull(value, "value");
      ArgumentValidator.EnsureArgumentNotNull(query, "query");
      return new SqlMatch(SubQuery(value), SubQuery(query), unique, matchType);
    }

    public static SqlMatch Match(ISqlQueryExpression value, ISqlQueryExpression query, bool unique)
    {
      return Match(value, query, unique, SqlMatchType.None);
    }

    public static SqlMatch Match(ISqlQueryExpression value, ISqlQueryExpression query, SqlMatchType matchType)
    {
      return Match(value, query, false, matchType);
    }

    public static SqlMatch Match(ISqlQueryExpression value, ISqlQueryExpression query)
    {
      return Match(value, query, false, SqlMatchType.None);
    }

    # endregion

    # region Math functions

    public static SqlFunctionCall Abs(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Abs, argument);
    }

    public static SqlFunctionCall Acos(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Acos, argument);
    }

    public static SqlFunctionCall Asin(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Asin, argument);
    }

    public static SqlFunctionCall Atan(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Atan, argument);
    }

    public static SqlFunctionCall Atan2(SqlExpression argument1, SqlExpression argument2)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument1, "argument1");
      ArgumentValidator.EnsureArgumentNotNull(argument2, "argument2");
      SqlValidator.EnsureIsArithmeticExpression(argument1);
      SqlValidator.EnsureIsArithmeticExpression(argument2);
      return new SqlFunctionCall(SqlFunctionType.Atan2, argument1, argument2);
    }

    public static SqlFunctionCall Ceiling(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Ceiling, argument);
    }

    public static SqlFunctionCall Cos(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Cos, argument);
    }

    public static SqlFunctionCall Cot(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Cot, argument);
    }

    public static SqlFunctionCall Degrees(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Degrees, argument);
    }

    public static SqlFunctionCall Exp(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Exp, argument);
    }

    public static SqlFunctionCall Floor(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Floor, argument);
    }

    public static SqlFunctionCall Log(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Log, argument);
    }

    public static SqlFunctionCall Log10(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Log10, argument);
    }

    public static SqlFunctionCall Pi()
    {
      return new SqlFunctionCall(SqlFunctionType.Pi);
    }

    public static SqlFunctionCall Power(SqlExpression argument, SqlExpression power)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      ArgumentValidator.EnsureArgumentNotNull(power, "power");
      SqlValidator.EnsureIsArithmeticExpression(power);
      return new SqlFunctionCall(SqlFunctionType.Power, argument, power);
    }

    public static SqlFunctionCall Radians(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Radians, argument);
    }

    public static SqlFunctionCall Rand(SqlExpression seed)
    {
      if (seed is not null) {
        SqlValidator.EnsureIsArithmeticExpression(seed);
        return new SqlFunctionCall(SqlFunctionType.Rand, seed);
      }
      else
        return new SqlFunctionCall(SqlFunctionType.Rand, seed);
    }

    public static SqlFunctionCall Rand()
    {
      return Rand(null);
    }

    public static SqlFunctionCall Round(SqlExpression argument, SqlExpression length)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      ArgumentValidator.EnsureArgumentNotNull(length, "length");
      SqlValidator.EnsureIsArithmeticExpression(length);
      return new SqlFunctionCall(SqlFunctionType.Round, argument, length);
    }

    public static SqlFunctionCall Round(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Round, argument);
    }

    public static SqlRound Round(SqlExpression argument, SqlExpression length, TypeCode type, MidpointRounding mode)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      if (type!=TypeCode.Decimal && type!=TypeCode.Double)
        throw new ArgumentOutOfRangeException("type");
      return new SqlRound(argument, length, type, mode);
    }

    public static SqlFunctionCall Truncate(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Truncate, argument);
    }

    public static SqlFunctionCall Sign(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Sign, argument);
    }

    public static SqlFunctionCall Sin(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Sin, argument);
    }

    public static SqlFunctionCall Sqrt(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Sqrt, argument);
    }

    public static SqlFunctionCall Square(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Square, argument);
    }

    public static SqlFunctionCall Tan(SqlExpression argument)
    {
      ArgumentValidator.EnsureArgumentNotNull(argument, "argument");
      SqlValidator.EnsureIsArithmeticExpression(argument);
      return new SqlFunctionCall(SqlFunctionType.Tan, argument);
    }

    # endregion

    #region Misc

    public static SqlNative Native(string value)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(value, "value");
      return new SqlNative(value);
    }

    public static SqlMetadata Metadata(SqlExpression expression, object value)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, nameof(expression));
      return new SqlMetadata(expression, value);
    }

    public static SqlSubQuery SubQuery(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlSubQuery(operand);
    }

    public static SqlCursor Cursor(string name)
    {
      return Cursor(name, null);
    }

    public static SqlCursor Cursor(string name, ISqlQueryExpression query)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlCursor(name, query);
    }

    public static SqlParameterRef ParameterRef(object parameter)
    {
      ArgumentValidator.EnsureArgumentNotNull(parameter, "parameter");
      return new SqlParameterRef(parameter);
    }

    public static SqlParameterRef ParameterRef(string name)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlParameterRef(name);
    }

    public static SqlCase Case(SqlExpression value)
    {
      return new SqlCase(value);
    }

    public static SqlCase Case()
    {
      return Case(null);
    }

    public static SqlNextValue NextValue(Sequence sequence)
    {
      ArgumentValidator.EnsureArgumentNotNull(sequence, "sequence");
      return new SqlNextValue(sequence);
    }

    public static SqlNextValue NextValue(Sequence sequence, int increment)
    {
      ArgumentValidator.EnsureArgumentNotNull(sequence, "sequence");
      return new SqlNextValue(sequence, increment);
    }

    public static SqlStatementBlock StatementBlock()
    {
      return new SqlStatementBlock();
    }

    public static SqlVariant Variant(object id, SqlExpression main, SqlExpression alternative)
    {
      ArgumentValidator.EnsureArgumentNotNull(id, "id");
      ArgumentValidator.EnsureArgumentNotNull(main, "main");
      ArgumentValidator.EnsureArgumentNotNull(alternative, "alternative");
      return new SqlVariant(id, main, alternative);
    }

    public static SqlPlaceholder Placeholder(object id)
    {
      ArgumentValidator.EnsureArgumentNotNull(id, "id");
      return new SqlPlaceholder(id);
    }

    public static SqlDynamicFilter DynamicFilter(object id, IReadOnlyList<SqlExpression> expressions)
    {
      ArgumentValidator.EnsureArgumentNotNull(id, "id");
      return new SqlDynamicFilter(id, expressions);
    }

    public static SqlTvpDynamicFilter TvpDynamicFilter(object id, IReadOnlyList<SqlExpression> expressions)
    {
      ArgumentValidator.EnsureArgumentNotNull(id, "id");
      return new(id, expressions);
    }

    public static SqlContainer Container(object value)
    {
      return new SqlContainer(value);
    }

    public static SqlFunctionCall LastAutoGeneratedId()
    {
      return new SqlFunctionCall(SqlFunctionType.LastAutoGeneratedId);
    }

    public static SqlRowNumber RowNumber()
    {
      return new SqlRowNumber();
    }

    #endregion

    #region OrderBy

    public static SqlOrder Order(SqlExpression expression)
    {
      return Order(expression, true);
    }

    public static SqlOrder Order(SqlExpression expression, bool ascending)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlOrder(expression, ascending);
    }

    public static SqlOrder Order(int position)
    {
      return Order(position, true);
    }

    public static SqlOrder Order(int position, bool ascending)
    {
      if (position<=0)
        throw new ArgumentOutOfRangeException(
          "position", position, Strings.ExPositionValueShouldBeGreaterThanZero);
      return new SqlOrder(position, ascending);
    }

    #endregion

    #region Row

    public static SqlRow Row(IReadOnlyList<SqlExpression> expressions)
    {
      SqlValidator.EnsureAreSqlRowArguments(expressions);
      return new SqlRow(expressions);
    }

    public static SqlRow Row(params SqlExpression[] expressions) => Row((IReadOnlyList<SqlExpression>)expressions);

    #endregion

    #region Statement

    public static SqlFragment Fragment(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlFragment(expression);
    }

    public static SqlWhile While(SqlExpression condition)
    {
      ArgumentValidator.EnsureArgumentNotNull(condition, "condition");
      SqlValidator.EnsureIsBooleanExpression(condition);
      return new SqlWhile(condition);
    }

    public static SqlBatch Batch()
    {
      return new SqlBatch();
    }

    public static SqlAssignment Assign(SqlVariable left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(right);
      return new SqlAssignment(left, right);
    }

    public static SqlAssignment Assign(SqlParameterRef left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsArithmeticExpression(right);
      return new SqlAssignment(left, right);
    }

    public static SqlIf If(SqlExpression condition, SqlStatement ifTrue, SqlStatement ifFalse)
    {
      ArgumentValidator.EnsureArgumentNotNull(condition, "condition");
      SqlValidator.EnsureIsBooleanExpression(condition);
      ArgumentValidator.EnsureArgumentNotNull(ifTrue, "ifTrue");
      return new SqlIf(condition, ifTrue, ifFalse);
    }

    public static SqlIf If(SqlExpression condition, SqlStatement ifTrue)
    {
      return If(condition, ifTrue, null);
    }

    public static SqlDelete Delete()
    {
      return new SqlDelete();
    }

    public static SqlDelete Delete(SqlTableRef table)
    {
      ArgumentValidator.EnsureArgumentNotNull(table, "table");
      return new SqlDelete(table);
    }

    public static SqlUpdate Update()
    {
      return new SqlUpdate();
    }

    public static SqlUpdate Update(SqlTableRef tableRef)
    {
      ArgumentValidator.EnsureArgumentNotNull(tableRef, "table");
      return new SqlUpdate(tableRef);
    }

    public static SqlInsert Insert()
    {
      return new SqlInsert();
    }

    public static SqlInsert Insert(SqlTableRef tableRef)
    {
      ArgumentValidator.EnsureArgumentNotNull(tableRef, "table");
      return new SqlInsert(tableRef);
    }

    public static SqlSelect Select()
    {
      return new SqlSelect();
    }

    public static SqlSelect Select(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      var result = new SqlSelect();
      result.Columns.Add(expression);
      return result;
    }

    public static SqlSelect Select(SqlTable table)
    {
      ArgumentValidator.EnsureArgumentNotNull(table, "table");
      return new SqlSelect(table);
    }

    #endregion

    # region String functions

    public static SqlConcat Concat(SqlExpression left, SqlExpression right)
    {
      return new SqlConcat(new [] {left, right});
    }

    public static SqlConcat Concat(params SqlExpression[] items)
    {
      ArgumentValidator.EnsureArgumentNotNull(items, "items");
      foreach (var item in items)
        SqlValidator.EnsureIsCharacterExpression(item);
      return new SqlConcat(items);
    }

    /// <summary>
    /// Concates underlying expression without any sign between.
    /// </summary>
    /// <param name="left">Left expression</param>
    /// <param name="right">Right expression</param>
    /// <returns>New <see cref="SqlBinary"/> expression.</returns>
    public static SqlBinary RawConcat(SqlExpression left, SqlExpression right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      SqlValidator.EnsureIsCharacterExpression(left);
      SqlValidator.EnsureIsCharacterExpression(right);
      return new SqlBinary(SqlNodeType.RawConcat, left, right);
    }

    public static SqlFunctionCall Substring(SqlExpression operand, int start)
    {
      return Substring(operand, start, null);
    }

    public static SqlFunctionCall Substring(SqlExpression operand, int start, int? length)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      if (length<0)
        throw new ArgumentException(Strings.ExLengthShouldBeNotNegativeValue, "length");
      return new SqlFunctionCall(SqlFunctionType.Substring, length.HasValue
        ? [operand, new SqlLiteral<int>(start), new SqlLiteral<int>(length.Value)]
        : [operand, new SqlLiteral<int>(start)]);
    }

    public static SqlFunctionCall Substring(
      SqlExpression operand, SqlExpression start, SqlExpression length = null)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(start, "start");
      SqlValidator.EnsureIsCharacterExpression(operand);
      SqlValidator.EnsureIsArithmeticExpression(start);
      if (length != null) {
        SqlValidator.EnsureIsArithmeticExpression(length);
        return new SqlFunctionCall(SqlFunctionType.Substring, operand, start, length);
      }
      return new SqlFunctionCall(SqlFunctionType.Substring, operand, start);
    }

    public static SqlFunctionCall Upper(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      return new SqlFunctionCall(SqlFunctionType.Upper, operand);
    }

    public static SqlFunctionCall Lower(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      return new SqlFunctionCall(SqlFunctionType.Lower, operand);
    }

    public static SqlTrim Trim(SqlExpression operand, SqlTrimType trimType, string trimCharacters)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      ArgumentValidator.EnsureArgumentNotNull(trimCharacters, "trimCharacters");
      return new SqlTrim(operand, trimCharacters, trimType);
    }

    public static SqlTrim Trim(SqlExpression operand, SqlTrimType trimType)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      return new SqlTrim(operand, null, trimType);
    }

    public static SqlTrim Trim(SqlExpression operand)
    {
      return Trim(operand, SqlTrimType.Both);
    }

    public static SqlLike Like(
      SqlExpression expression, SqlExpression pattern, SqlExpression escape)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      SqlValidator.EnsureIsCharacterExpression(expression);
      SqlValidator.EnsureIsCharacterExpression(pattern);
      SqlValidator.EnsureIsCharacterExpression(escape);
      return new SqlLike(expression, pattern, escape, false);
    }

    public static SqlLike Like(SqlExpression expression, SqlExpression pattern)
    {
      return Like(expression, pattern, null);
    }

    public static SqlLike Like(SqlExpression expression, string pattern)
    {
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      return Like(expression, new SqlLiteral<string>(pattern), null);
    }

    public static SqlLike Like(SqlExpression expression, string pattern, char escape)
    {
      SqlValidator.EnsureIsCharacterExpression(expression);
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      return Like(expression, new SqlLiteral<string>(pattern), new SqlLiteral<char>(escape));
    }

    public static SqlLike NotLike(
      SqlExpression expression, SqlExpression pattern, SqlExpression escape)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      SqlValidator.EnsureIsCharacterExpression(expression);
      SqlValidator.EnsureIsCharacterExpression(pattern);
      SqlValidator.EnsureIsCharacterExpression(escape);
      return new SqlLike(expression, pattern, escape, true);
    }

    public static SqlLike NotLike(SqlExpression expression, SqlExpression pattern)
    {
      return NotLike(expression, pattern, null);
    }

    public static SqlLike NotLike(SqlExpression expression, string pattern)
    {
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      return NotLike(expression, new SqlLiteral<string>(pattern), null);
    }

    public static SqlLike NotLike(SqlExpression expression, string pattern, char escape)
    {
      SqlValidator.EnsureIsCharacterExpression(expression);
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      return NotLike(expression, new SqlLiteral<string>(pattern), new SqlLiteral<char>(escape));
    }

    public static SqlBinary Overlaps(DateTime from1, TimeSpan span1, DateTime from2, TimeSpan span2) =>
      new(SqlNodeType.Overlaps, Row([from1, span1]), Row([from2, span2]));

    public static SqlBinary Overlaps(DateTime from1, DateTime to1, DateTime from2, DateTime to2) =>
      new(SqlNodeType.Overlaps, Row([from1, to1]), Row([from2, to2]));

    public static SqlBinary Overlaps(SqlExpression from1, SqlExpression toOrSpan1, SqlExpression from2, SqlExpression toOrSpan2)
    {
      ArgumentValidator.EnsureArgumentNotNull(from1, "from1");
      ArgumentValidator.EnsureArgumentNotNull(toOrSpan1, "toOrSpan1");
      ArgumentValidator.EnsureArgumentNotNull(from2, "from2");
      ArgumentValidator.EnsureArgumentNotNull(toOrSpan2, "toOrSpan2");
      return new(SqlNodeType.Overlaps, Row([from1, toOrSpan1]), Row([from2, toOrSpan2]));
    }

    public static SqlBinary Overlaps(SqlRow left, SqlRow right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.Overlaps, left, right);
    }

    public static SqlBinary Overlaps(SqlSelect left, SqlRow right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.Overlaps, SubQuery(left), right);
    }

    public static SqlBinary Overlaps(SqlRow left, SqlSelect right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.Overlaps, left, SubQuery(right));
    }

    public static SqlBinary Overlaps(SqlSelect left, SqlSelect right)
    {
      ArgumentValidator.EnsureArgumentNotNull(left, "left");
      ArgumentValidator.EnsureArgumentNotNull(right, "right");
      return new SqlBinary(SqlNodeType.Overlaps, left, right);
    }

    public static SqlFunctionCall BinaryLength(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      return new SqlFunctionCall(SqlFunctionType.BinaryLength, operand);
    }

    public static SqlFunctionCall CharLength(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      return new SqlFunctionCall(SqlFunctionType.CharLength, operand);
    }

    public static SqlFunctionCall Position(SqlExpression pattern, SqlExpression source)
    {
      ArgumentValidator.EnsureArgumentNotNull(pattern, "pattern");
      ArgumentValidator.EnsureArgumentNotNull(source, "source");
      SqlValidator.EnsureIsCharacterExpression(pattern);
      SqlValidator.EnsureIsCharacterExpression(source);
      return new SqlFunctionCall(SqlFunctionType.Position, pattern, source);
    }

    public static SqlFunctionCall Replace(SqlExpression text, SqlExpression from, SqlExpression to)
    {
      ArgumentValidator.EnsureArgumentNotNull(text, "text");
      ArgumentValidator.EnsureArgumentNotNull(from, "from");
      ArgumentValidator.EnsureArgumentNotNull(to, "to");
      SqlValidator.EnsureIsCharacterExpression(text);
      SqlValidator.EnsureIsCharacterExpression(from);
      SqlValidator.EnsureIsCharacterExpression(to);
      return new SqlFunctionCall(SqlFunctionType.Replace, text, from, to);
    }

    public static SqlCollate Collate(SqlExpression operand, Collation collation)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsCharacterExpression(operand);
      ArgumentValidator.EnsureArgumentNotNull(collation, "collation");
      return new SqlCollate(operand, collation);
    }


    public static SqlFunctionCall PadLeft(SqlExpression operand, SqlExpression length)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(length, "length");
      return new SqlFunctionCall(SqlFunctionType.PadLeft, operand, length);
    }

    public static SqlFunctionCall PadLeft(SqlExpression operand, SqlExpression length, SqlExpression padChar)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(length, "length");
      ArgumentValidator.EnsureArgumentNotNull(padChar, "padChar");
      return new SqlFunctionCall(SqlFunctionType.PadLeft, operand, length, padChar);
    }

    public static SqlFunctionCall PadRight(SqlExpression operand, SqlExpression length)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(length, "length");
      return new SqlFunctionCall(SqlFunctionType.PadRight, operand, length);
    }

    public static SqlFunctionCall PadRight(SqlExpression operand, SqlExpression length, SqlExpression padChar)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      ArgumentValidator.EnsureArgumentNotNull(length, "length");
      ArgumentValidator.EnsureArgumentNotNull(padChar, "padChar");
      return new SqlFunctionCall(SqlFunctionType.PadRight, operand, length, padChar);
    }

    # endregion

    #region DataTable & Column

    public static SqlTableColumn TableColumn(SqlTable sqlTable)
    {
      ArgumentValidator.EnsureArgumentNotNull(sqlTable, "table");
      return new SqlTableColumn(sqlTable, string.Empty);
    }

    public static SqlTableColumn TableColumn(SqlTable sqlTable, string name)
    {
      ArgumentValidator.EnsureArgumentNotNull(sqlTable, "table");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlTableColumn(sqlTable, name);
    }

    public static SqlUserColumn Column(SqlExpression expression)
    {
      ArgumentValidator.EnsureArgumentNotNull(expression, "expression");
      return new SqlUserColumn(expression);
    }

    public static SqlColumnRef ColumnRef(SqlColumn column)
    {
      ArgumentValidator.EnsureArgumentNotNull(column, "column");
      return new SqlColumnRef(column);
    }

    public static SqlColumnRef ColumnRef(SqlColumn column, string alias)
    {
      ArgumentNullException.ThrowIfNull(column);
      ArgumentException.ThrowIfNullOrEmpty(alias);
      return new SqlColumnRef(column, alias);
    }

    public static SqlColumnStub ColumnStub(SqlColumn column)
    {
      ArgumentValidator.EnsureArgumentNotNull(column, "column");
      return new SqlColumnStub(column);
    }

    public static SqlFreeTextTable FreeTextTable(DataTable dataTable, SqlExpression freeText, IList<string> columnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(freeText, "freeText");
      return new SqlFreeTextTable(dataTable, freeText, columnNames);
    }

    public static SqlFreeTextTable FreeTextTable(DataTable dataTable, SqlExpression freeText, IList<string> columnNames, IList<string> targetColumnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(freeText, "freeText");
      ArgumentValidator.EnsureArgumentNotNull(columnNames, "columnNames");
      return new SqlFreeTextTable(dataTable, freeText, columnNames, targetColumnNames);
    }

    public static SqlFreeTextTable FreeTextTable(DataTable dataTable, SqlExpression freeText, IList<string> columnNames, SqlExpression topN)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(freeText, "freeText");
      ArgumentValidator.EnsureArgumentNotNull(topN, "topN");
      return new SqlFreeTextTable(dataTable, freeText, columnNames, topN);
    }

    public static SqlFreeTextTable FreeTextTable(DataTable dataTable, SqlExpression freeText, IList<string> columnNames, IList<string> targetColumNames, SqlExpression topN)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(freeText, "freeText");
      ArgumentValidator.EnsureArgumentNotNull(targetColumNames, "targetColumNames");
      ArgumentValidator.EnsureArgumentNotNull(topN, "topN");
      return new SqlFreeTextTable(dataTable, freeText, columnNames, targetColumNames, topN);
    }

    public static SqlContainsTable ContainsTable(DataTable dataTable, SqlExpression searchText, IList<string> columnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(searchText, "searchText");
      return new SqlContainsTable(dataTable, searchText, columnNames);
    }

    public static SqlContainsTable ContainsTable(DataTable dataTable, SqlExpression searchText, IList<string> columnNames, IList<string> targetColumnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(searchText, "searchText");
      ArgumentValidator.EnsureArgumentNotNull(columnNames, "columnNames");
      return new SqlContainsTable(dataTable, searchText, columnNames, targetColumnNames);
    }

    public static SqlContainsTable ContainsTable(DataTable dataTable, SqlExpression searchText, IList<string> columnNames, SqlExpression topN)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(searchText, "searchText");
      return new SqlContainsTable(dataTable, searchText, columnNames, topN);
    }

    public static SqlContainsTable ContainsTable(DataTable dataTable, SqlExpression searchText, IList<string> columnNames, IList<string> targetColumnNames, SqlExpression topN)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(searchText, "searchText");
      ArgumentValidator.EnsureArgumentNotNull(columnNames, "columnNames");
      return new SqlContainsTable(dataTable, searchText, columnNames, targetColumnNames, topN);
    }

    public static SqlTableRef TableRef(DataTable dataTable)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      return new SqlTableRef(dataTable);
    }

    public static SqlTableRef TableRef(DataTable dataTable, IEnumerable<string> columnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNull(columnNames, "columnNames");
      return new SqlTableRef(dataTable, string.Empty, columnNames.ToArray());
    }

    public static SqlTableRef TableRef(DataTable dataTable, string name)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlTableRef(dataTable, name);
    }

    public static SqlTableRef TableRef(DataTable dataTable, string name, IEnumerable<string> columnNames)
    {
      ArgumentValidator.EnsureArgumentNotNull(dataTable, "dataTable");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      ArgumentValidator.EnsureArgumentNotNull(columnNames, "columnNames");
      return new SqlTableRef(dataTable, name, columnNames.ToArray());
    }

    public static SqlQueryRef QueryRef(ISqlQueryExpression query)
    {
      ArgumentValidator.EnsureArgumentNotNull(query, "query");
      return new SqlQueryRef(query);
    }

    public static SqlQueryRef QueryRef(ISqlQueryExpression query, string alias)
    {
      ArgumentValidator.EnsureArgumentNotNull(query, "query");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(alias, "alias");
      return new SqlQueryRef(query, alias);
    }

    #endregion

    #region Unary

    public static SqlUnary BitNot(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsArithmeticExpression(operand);
      return Unary(SqlNodeType.BitNot, operand);
    }

    public static SqlUnary Negate(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsArithmeticExpression(operand);
      return Unary(SqlNodeType.Negate, operand);
    }

    public static SqlUnary Not(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      SqlValidator.EnsureIsBooleanExpression(operand);
      return Unary(SqlNodeType.Not, operand);
    }

    public static SqlUnary IsNull(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.IsNull, operand);
    }

    public static SqlUnary IsNotNull(SqlExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.IsNotNull, operand);
    }

    public static SqlUnary Unique(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.Unique, SubQuery(operand));
    }

    public static SqlUnary Exists(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return new SqlUnary(SqlNodeType.Exists, SubQuery(operand));
    }

    public static SqlUnary All(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.All, SubQuery(operand));
    }

    public static SqlUnary Any(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.Any, SubQuery(operand));
    }

    public static SqlUnary Some(ISqlQueryExpression operand)
    {
      ArgumentValidator.EnsureArgumentNotNull(operand, "operand");
      return Unary(SqlNodeType.Some, SubQuery(operand));
    }

    internal static SqlUnary Unary(SqlNodeType nodeType, SqlExpression operand)
    {
      return new SqlUnary(nodeType, operand);
    }

    #endregion

    #region Variable

    public static SqlVariable Variable(string name, SqlValueType type)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlVariable(name, type);
    }

    public static SqlVariable Variable(string name, SqlType type)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlVariable(name, new SqlValueType(type));
    }

    public static SqlVariable Variable(string name, SqlType type, int size)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlVariable(name, new SqlValueType(type, size));
    }

    public static SqlVariable Variable(string name, SqlType type, short precision, short scale)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");
      return new SqlVariable(name, new SqlValueType(type, precision, scale));
    }

    public static SqlVariable Variable(string name, SqlType type, short precision)
    {
      return Variable(name, type, precision, 0);
    }

    #endregion

    #region Hints

    public static SqlJoinHint JoinHint(SqlJoinMethod method, SqlTable table)
    {
      ArgumentValidator.EnsureArgumentNotNull(table, "table");
      return new SqlJoinHint(method, table);
    }

    public static SqlForceJoinOrderHint ForceJoinOrderHint()
    {
      return new SqlForceJoinOrderHint();
    }

    public static SqlForceJoinOrderHint ForceJoinOrderHint(params SqlTable[] sqlTables)
    {
      ArgumentValidator.EnsureArgumentNotNull(sqlTables, "sqlTables");
      return new SqlForceJoinOrderHint(sqlTables);
    }

    public static SqlFastFirstRowsHint FastFirstRowsHint(int amount)
    {
      ArgumentValidator.EnsureArgumentIsGreaterThan(amount, 0, "amount");
      return new SqlFastFirstRowsHint(amount);
    }

    public static SqlNativeHint NativeHint(string hintText)
    {
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(hintText, "hintText");
      return new SqlNativeHint(hintText);
    }

    #endregion
  }
}
