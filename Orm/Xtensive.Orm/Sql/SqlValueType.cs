// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;
using Xtensive.Core;


namespace Xtensive.Sql
{
  /// <summary>
  /// Represents an SQL type with specific <see cref="Length"/>, <see cref="Scale"/> and <see cref="Precision"/>.
  /// </summary>
  [Serializable]
  public sealed class SqlValueType
    : IEquatable<SqlValueType>
  {
    public static readonly SqlValueType
      Boolean = new(SqlType.Boolean),
      Char = new(SqlType.VarChar, 1),
      Int8 = new(SqlType.Int8),
      UInt8 = new(SqlType.UInt8),
      Int16 = new(SqlType.Int16),
      UInt16 = new(SqlType.UInt16),
      Int32 = new(SqlType.Int32),
      UInt32 = new(SqlType.UInt32),
      Int64 = new(SqlType.Int64),
      UInt64 = new(SqlType.UInt64),
      Float = new(SqlType.Float),
      Double = new(SqlType.Double),
      Decimal = new(SqlType.Decimal),
      DateTime = new(SqlType.DateTime),
      Date = new(SqlType.Date),
      Time = new(SqlType.Time),
      DateTimeOffset = new(SqlType.DateTimeOffset),
      Guid = new(SqlType.Guid),
      Binary = new(SqlType.Binary);

    /// <summary>
    /// Gets the <see cref="SqlType"/>.
    /// </summary>
    public SqlType Type { get; }

    /// <summary>
    /// Gets the name of the type in case when <see cref="Type"/> has value <see cref="SqlType.Unknown"/>.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets or sets the length.
    /// </summary>
    public int? Length { get; }

    /// <summary>
    /// Gets the scale.
    /// </summary>
    public int? Scale { get; }

    /// <summary>
    /// Gets the precision.
    /// </summary>
    public int? Precision { get; }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Type, Length, Scale, Precision);

    /// <inheritdoc/>
    public bool Equals(SqlValueType other) =>
      other != null &&
      other.Type == Type &&
      other.TypeName == TypeName &&
      other.Length == Length &&
      other.Precision == Precision &&
      other.Scale == Scale;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is SqlValueType other && Equals(other);

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(SqlValueType left, SqlValueType right) =>
      ReferenceEquals(left, right) || left?.Equals(right) == true;

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(SqlValueType left, SqlValueType right) => !(left == right);

    public static bool IsNumeric(SqlValueType valueType)
    {
      var sqlType = valueType.Type;

      if (sqlType==SqlType.UInt8 ||
        sqlType==SqlType.Decimal ||
        sqlType==SqlType.Double ||
        sqlType==SqlType.Float ||
        sqlType==SqlType.Int16 ||
        sqlType==SqlType.Int32 ||
        sqlType==SqlType.Int64 ||
        sqlType==SqlType.Int8 ||
        sqlType==SqlType.UInt16 ||
        sqlType==SqlType.UInt32 ||
        sqlType==SqlType.UInt64)
        return true;
      return false;
    }

    public static bool IsExactNumeric(SqlValueType valueType)
    {
      var sqlType = valueType.Type;

      if (sqlType==SqlType.UInt8 ||
        sqlType==SqlType.Decimal ||
        sqlType==SqlType.Int16 ||
        sqlType==SqlType.Int32 ||
        sqlType==SqlType.Int64 ||
        sqlType==SqlType.Int8 ||
        sqlType==SqlType.UInt16 ||
        sqlType==SqlType.UInt32 ||
        sqlType==SqlType.UInt64)
        return true;
      return false;
    }

    public override string ToString()
    {
      if (TypeName!=null)
        return TypeName;
      if (Length!=null)
        return $"{Type}({Length.Value})";
      if (Precision!=null)
        return $"{Type}({Precision.Value},{Scale.Value})";
      return Type.ToString();
    }


    // Constructors

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="type">The type.</param>
    public SqlValueType(SqlType type)
      : this(type, null, null, null, null)
    {
    }
 
    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="length">The length.</param>
    public SqlValueType(SqlType type, int length)
      : this(type, null, length, null, null)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="precision">The precision.</param>
    /// <param name="scale">The scale.</param>
    public SqlValueType(SqlType type, int precision, int scale)
      : this(type, null, null, precision, scale)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    public SqlValueType(string typeName)
      : this(SqlType.Unknown, typeName, null, null, null)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <param name="length">The length.</param>
    public SqlValueType(string typeName, int length)
      : this(SqlType.Unknown, typeName, length, null, null)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <param name="precision">The precision.</param>
    /// <param name="scale">The scale.</param>
    public SqlValueType(string typeName, int precision, int scale)
      : this(SqlType.Unknown, typeName, null, precision, scale)
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="typeName">Name of the type.</param>
    /// <param name="length">The length.</param>
    /// <param name="precision">The precision.</param>
    /// <param name="scale">The scale.</param>
    public SqlValueType(SqlType type, string typeName, int? length, int? precision, int? scale)
    {
      if ((type==SqlType.Unknown)!=(typeName!=null))
        throw new ArgumentException(Strings.ExInvalidArgumentsNonNullTypeNameIsAllowedIfAndOnlyIfTypeEqualsSqlTypeUnknown);
      if (precision.HasValue && precision != 0 && length.HasValue && length != 0)
        throw new ArgumentException(Strings.ExInvalidArgumentsPrecisionAndLengthShouldNotBeUsedTogether);
      if (precision.HasValue!=scale.HasValue)
        throw new ArgumentException(Strings.ExInvalidArgumentsScaleAndPrecisionShouldBeUsedTogether);
      if (typeName!=null)
        ArgumentException.ThrowIfNullOrEmpty(typeName);
      if (length!=null)
        ArgumentValidator.EnsureArgumentIsGreaterThan(length.Value, 0, "length");
      if (precision!=null)
        ArgumentValidator.EnsureArgumentIsInRange(scale.Value, 0, precision.Value, "scale");
      Type = type;
      TypeName = typeName;
      Length = length;
      Precision = precision;
      Scale = scale;
    }
  }
}
