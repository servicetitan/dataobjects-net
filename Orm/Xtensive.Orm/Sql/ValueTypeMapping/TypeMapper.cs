// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.06.19

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xtensive.Sql.Info;

namespace Xtensive.Sql
{
  /// <summary>
  /// Abstract base class for any value (data) type mapper.
  /// </summary>
  public abstract class TypeMapper
  {
    private const int DecimalPrecisionLimit = 60;

    private static readonly ValueRange<TimeSpan> Int64TimeSpanRange = new ValueRange<TimeSpan>(
      TimeSpan.FromTicks(TimeSpan.MinValue.Ticks / 100),
      TimeSpan.FromTicks(TimeSpan.MaxValue.Ticks / 100));

    public SqlDriver Driver { get; private set; }

    protected int? MaxDecimalPrecision { get; private set; }
    protected int? VarCharMaxLength { get; private set; }
    protected int? VarBinaryMaxLength { get; private set; }

    public virtual bool IsParameterCastRequired(Type type)
    {
      return false;
    }

    #region BindXxx methods

    public virtual void BindBoolean(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Boolean;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindChar(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.String;
      if (value == null) {
        parameter.Value = DBNull.Value;
        return;
      }
      var _char = (char) value;
      parameter.Value = _char == default(char) ? string.Empty : _char.ToString();
    }

    public virtual void BindString(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.String;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindByte(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Byte;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindSByte(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.SByte;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindShort(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int16;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindUShort(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.UInt16;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindInt(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int32;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindUInt(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.UInt32;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindLong(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int64;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindULong(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.UInt64;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindFloat(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Single;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindDouble(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Double;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindDecimal(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Decimal;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindDateTime(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.DateTime;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindDateOnly(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Date;
      parameter.Value = value != null ? ((DateOnly) value).ToDateTime(TimeOnly.MinValue) : DBNull.Value;
    }

    public virtual void BindTimeOnly(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Time;
      parameter.Value = value != null ? ((TimeOnly) value).ToTimeSpan() : DBNull.Value;
    }

    public virtual void BindDateTimeOffset(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.DateTimeOffset;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindTimeSpan(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int64;
      if (value != null) {
        var timeSpan = ValueRangeValidator.Correct((TimeSpan) value, Int64TimeSpanRange);
        parameter.Value = timeSpan.Ticks * 100;
      }
      else
        parameter.Value = DBNull.Value;
    }

    public virtual void BindGuid(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Guid;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindByteArray(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Binary;
      parameter.Value = value ?? DBNull.Value;
    }

    public virtual void BindLongList(DbParameter parameter, object value) => throw new NotSupportedException("Table-Valued Paramenters not supported");
    public virtual void BindStringList(DbParameter parameter, object value) => throw new NotSupportedException("Table-Valued Paramenters not supported");

    #endregion

    #region ReadXxx methods

    public virtual bool ReadBoolean(DbDataReader reader, int index) => reader.GetBoolean(index);
    public object ReadBoxedBoolean(DbDataReader reader, int index) => ReadBoolean(reader, index);

    public virtual char ReadChar(DbDataReader reader, int index) => reader.GetString(index).SingleOrDefault();
    public object ReadBoxedChar(DbDataReader reader, int index) => ReadChar(reader, index);

    public virtual string ReadString(DbDataReader reader, int index) => reader.GetString(index);

    public virtual byte ReadByte(DbDataReader reader, int index) => reader.GetByte(index);
    public object ReadBoxedByte(DbDataReader reader, int index) => ReadByte(reader, index);

    public virtual sbyte ReadSByte(DbDataReader reader, int index) => Convert.ToSByte(reader[index]);
    public object ReadBoxedSByte(DbDataReader reader, int index) => ReadSByte(reader, index);

    public virtual short ReadShort(DbDataReader reader, int index) => reader.GetInt16(index);
    public object ReadBoxedShort(DbDataReader reader, int index) => ReadShort(reader, index);

    public virtual ushort ReadUShort(DbDataReader reader, int index) => Convert.ToUInt16(reader[index]);
    public object ReadBoxedUShort(DbDataReader reader, int index) => ReadUShort(reader, index);

    public virtual int ReadInt(DbDataReader reader, int index) => reader.GetInt32(index);
    public object ReadBoxedInt(DbDataReader reader, int index) => ReadInt(reader, index);

    public virtual uint ReadUInt(DbDataReader reader, int index) => Convert.ToUInt32(reader[index]);
    public object ReadBoxedUInt(DbDataReader reader, int index) => ReadUInt(reader, index);

    public virtual long ReadLong(DbDataReader reader, int index) => reader.GetInt64(index);
    public object ReadBoxedLong(DbDataReader reader, int index) => ReadLong(reader, index);

    public virtual ulong ReadULong(DbDataReader reader, int index) => Convert.ToUInt64(reader[index]);
    public object ReadBoxedULong(DbDataReader reader, int index) => ReadULong(reader, index);

    public virtual float ReadFloat(DbDataReader reader, int index) => reader.GetFloat(index);
    public object ReadBoxedFloat(DbDataReader reader, int index) => ReadFloat(reader, index);

    public virtual double ReadDouble(DbDataReader reader, int index) => reader.GetDouble(index);
    public object ReadBoxedDouble(DbDataReader reader, int index) => ReadDouble(reader, index);

    public virtual decimal ReadDecimal(DbDataReader reader, int index) => reader.GetDecimal(index);
    public object ReadBoxedDecimal(DbDataReader reader, int index) => ReadDecimal(reader, index);

    public virtual DateTime ReadDateTime(DbDataReader reader, int index) => reader.GetDateTime(index);
    public object ReadBoxedDateTime(DbDataReader reader, int index) => ReadDateTime(reader, index);

    public virtual DateOnly ReadDateOnly(DbDataReader reader, int index) => DateOnly.FromDateTime(reader.GetFieldValue<DateTime>(index));
    public object ReadBoxedDateOnly(DbDataReader reader, int index) => ReadDateOnly(reader, index);

    public virtual TimeOnly ReadTimeOnly(DbDataReader reader, int index) => TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(index));
    public object ReadBoxedTimeOnly(DbDataReader reader, int index) => ReadTimeOnly(reader, index);

    public virtual DateTimeOffset ReadDateTimeOffset(DbDataReader reader, int index) => (DateTimeOffset) reader.GetValue(index);
    public object ReadBoxedDateTimeOffset(DbDataReader reader, int index) => ReadDateTimeOffset(reader, index);

    public virtual TimeSpan ReadTimeSpan(DbDataReader reader, int index)
    {
      long value;
      try {
        value = reader.GetInt64(index);
      }
      catch (InvalidCastException) {
        value = (long) reader.GetDecimal(index);
      }
      return TimeSpan.FromTicks(value / 100);
    }
    public object ReadBoxedTimeSpan(DbDataReader reader, int index) => ReadTimeSpan(reader, index);

    public virtual Guid ReadGuid(DbDataReader reader, int index) => reader.GetGuid(index);
    public object ReadBoxedGuid(DbDataReader reader, int index) => ReadGuid(reader, index);

    public virtual byte[] ReadByteArray(DbDataReader reader, int index) =>
      reader[index] switch {
        null => null,
        byte[] bytes => bytes,
        _ => throw new NotSupportedException("There is no support of SqlGeometry, SqlGeography, or other complex SQL types to the moment")
      };
      // As far as SqlGeometry and SqlGeography have no support in .Net 5
      // we don't need to provide a functionality reading those data as byte arrays

      // var formatter = new BinaryFormatter();
      // var stream = new MemoryStream();
      // formatter.Serialize(stream, value);
      // return stream.ToArray();

    #endregion

    #region MapXxx methods

    public virtual SqlValueType MapBoolean(int? length, int? precision, int? scale) => SqlValueType.Boolean;

    public virtual SqlValueType MapChar(int? length, int? precision, int? scale) => SqlValueType.Char;

    public virtual SqlValueType MapString(int? length, int? precision, int? scale) =>
      ChooseStreamType(SqlType.VarChar, SqlType.VarCharMax, length, VarCharMaxLength);

    public virtual SqlValueType MapByte(int? length, int? precision, int? scale) => SqlValueType.UInt8;
    public virtual SqlValueType MapSByte(int? length, int? precision, int? scale) => SqlValueType.Int8;
    public virtual SqlValueType MapShort(int? length, int? precision, int? scale) => SqlValueType.Int16;
    public virtual SqlValueType MapUShort(int? length, int? precision, int? scale) => SqlValueType.UInt16;
    public virtual SqlValueType MapInt(int? length, int? precision, int? scale) => SqlValueType.Int32;
    public virtual SqlValueType MapUInt(int? length, int? precision, int? scale) => SqlValueType.UInt32;
    public virtual SqlValueType MapLong(int? length, int? precision, int? scale) => SqlValueType.Int64;
    public virtual SqlValueType MapULong(int? length, int? precision, int? scale) => SqlValueType.UInt64;
    public virtual SqlValueType MapFloat(int? length, int? precision, int? scale) => SqlValueType.Float;
    public virtual SqlValueType MapDouble(int? length, int? precision, int? scale) => SqlValueType.Double;

    public virtual SqlValueType MapDecimal(int? length, int? precision, int? scale)
    {
      if (MaxDecimalPrecision == null)
        return SqlValueType.Decimal;
      if (precision == null) {
        var resultPrecision = Math.Min(DecimalPrecisionLimit, MaxDecimalPrecision.Value);
        var resultScale = resultPrecision / 2;
        return new SqlValueType(SqlType.Decimal, resultPrecision, resultScale);
      }
      if (precision.Value > MaxDecimalPrecision.Value)
        throw new InvalidOperationException(string.Format(
          Strings.ExSpecifiedPrecisionXIsGreaterThanMaximumSupportedByStorageY,
          precision.Value, MaxDecimalPrecision.Value));
      return new SqlValueType(SqlType.Decimal, null, null, precision, scale);
    }

    public virtual SqlValueType MapDateTime(int? length, int? precision, int? scale) => SqlValueType.DateTime;
    public virtual SqlValueType MapDateOnly(int? length, int? precision, int? scale) => SqlValueType.Date;
    public virtual SqlValueType MapTimeOnly(int? length, int? precision, int? scale) => SqlValueType.Time;
    public virtual SqlValueType MapDateTimeOffset(int? length, int? precision, int? scale) => SqlValueType.DateTimeOffset;
    public virtual SqlValueType MapTimeSpan(int? length, int? precision, int? scale) => SqlValueType.Int64;
    public virtual SqlValueType MapGuid(int? length, int? precision, int? scale) => SqlValueType.Guid;

    public virtual SqlValueType MapByteArray(int? length, int? precision, int? scale) =>
      ChooseStreamType(SqlType.VarBinary, SqlType.VarBinaryMax, length, VarBinaryMaxLength);

    #endregion

    protected static SqlValueType ChooseStreamType(SqlType varType, SqlType varMaxType, int? length, int? varTypeMaxLength)
    {
      if (varTypeMaxLength == null)
        return new SqlValueType(varMaxType);
      if (length == null)
        return new SqlValueType(varType, varTypeMaxLength.Value);
      if (length.Value > varTypeMaxLength.Value)
        return new SqlValueType(varMaxType);
      return new SqlValueType(varType, length.Value);
    }

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    public virtual void Initialize()
    {
      var varchar = Driver.ServerInfo.DataTypes.VarChar;
      if (varchar != null)
        VarCharMaxLength = varchar.MaxLength;
      var varbinary = Driver.ServerInfo.DataTypes.VarBinary;
      if (varbinary != null)
        VarBinaryMaxLength = varbinary.MaxLength;
      var _decimal = Driver.ServerInfo.DataTypes.Decimal;
      if (_decimal != null)
        MaxDecimalPrecision = _decimal.MaxPrecision;
    }

    // Constructors

    protected TypeMapper(SqlDriver driver)
    {
      Driver = driver;
    }
  }

  public readonly record struct MapperReader(
    TypeMapper Mapper,
    Func<DbDataReader, int, object> Reader,
    DbDataReader DbDataReader,
    int FieldIndex
  );
}
