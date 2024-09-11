// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.07.02

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Sql.Drivers.SqlServer.v10
{
  internal class TypeMapper(SqlDriver driver) : v09.TypeMapper(driver)
  {
    public override void BindDateTime(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.DateTime2;
      parameter.Value = value ?? DBNull.Value;
    }

    public override DateTime ReadDateTime(DbDataReader reader, int index)
    {
      string type = reader.GetDataTypeName(index);
      if (type=="time") {
        var time = (TimeSpan) reader.GetValue(index);
        return new DateTime(time.Ticks / 100);
      }
      return base.ReadDateTime(reader, index);
    }

    private SqlDbType GetSqlDbType(object v) =>
      v switch {
        byte or short or ushort or int or uint or long or decimal => SqlDbType.BigInt,
        string => SqlDbType.NVarChar,
        null => throw new NotSupportedException($"null is not supported by TVP"),
        _ => throw new NotSupportedException($"Type {v.GetType()} is not supported by TVP")
      };

    public override void BindTable(DbParameter parameter, object value)
    {
      SqlParameter sqlParameter = (SqlParameter) parameter;
      sqlParameter.SqlDbType = SqlDbType.Structured;
      sqlParameter.TypeName = sqlParameter.ParameterName + "_tvp";

      SqlMetaData[] metaDatas = null;
      List<SqlDataRecord> records = null;
      var tuples = (List<Tuple>) value;
      int maxStringLength = 20;

      foreach (var tuple in tuples) {
        for (int i = 0; i < tuple.Count; ++i) {
          if (tuple.GetValueOrDefault(i) is string s) {
            maxStringLength = Math.Max(maxStringLength, s.Length);
          }
        }
      }

      SqlDbType sqlDbType = SqlDbType.BigInt;
      foreach (var tuple in tuples) {
        if (metaDatas == null) {
          records = new();
          metaDatas = new SqlMetaData[tuple.Count];
          for (int i = 0; i < tuple.Count; ++i) {
            var fieldName = "Value";
            sqlDbType = GetSqlDbType(tuple.GetValueOrDefault(i));
            metaDatas[i] = sqlDbType == SqlDbType.NVarChar
              ? new SqlMetaData(fieldName, sqlDbType, maxStringLength)
              : new SqlMetaData(fieldName, sqlDbType);
          }
        }

        SqlDataRecord record = new(metaDatas);
        for (int i = 0; i < tuple.Count; ++i) {
          var fieldValue = tuple.GetValueOrDefault(i) switch {
            byte n => (long) n,
            short n => (long) n,
            ushort n => (long) n,
            int n => (long) n,
            uint n => (long) n,
            decimal d => (long) d,
            var o => o
          };
          record.SetValue(i, fieldValue);
        }
        records.Add(record);
      }
      sqlParameter.Value = records;
      sqlParameter.TypeName = sqlDbType == SqlDbType.BigInt ? "_DO_LongList" : "_DO_StringList";
    }
  }
}
