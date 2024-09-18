// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.03

using System;
using System.Data.Common;

namespace Xtensive.Sql
{
  /// <summary>
  /// Value (data) type mapping.
  /// </summary>
  public sealed class TypeMapping
  {
    public Func<DbDataReader, int, object> ValueReader { get; }
    private readonly Action<DbParameter, object> valueBinder;
    private readonly Func<int?, int?, int?, SqlValueType> mapper;

    public Type Type { get; }
    public bool ParameterCastRequired { get; }

    public object ReadValue(DbDataReader reader, int index) => ValueReader(reader, index);
    public void BindValue(DbParameter parameter, object value) => valueBinder(parameter, value);
    public SqlValueType MapType() => mapper(null, null, null);
    public SqlValueType MapType(int? length, int? precision, int? scale) => mapper(length, precision, scale);

    // Constructors

    internal TypeMapping(Type type,
      Func<DbDataReader, int, object> valueReader,
      Action<DbParameter, object> valueBinder,
      Func<int?, int?, int?, SqlValueType> mapper,
      bool parameterCastRequired)
    {
      Type = type;

      ValueReader = valueReader;
      this.valueBinder = valueBinder;
      this.mapper = mapper;

      ParameterCastRequired = parameterCastRequired;
    }
  }
}
