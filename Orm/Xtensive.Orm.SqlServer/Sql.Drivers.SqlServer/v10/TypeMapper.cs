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
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Sql.Drivers.SqlServer.v10
{
  internal class TypeMapper(SqlDriver driver) : v09.TypeMapper(driver)
  {
    public const string
      LongListTypeName = "_DO_LongList",
      StringListTypeName = "_DO_StringList";

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

    private static void BindList(DbParameter parameter, object value, SqlDbType sqlDbType)
    {
      var sqlParameter = (SqlParameter) parameter;
      sqlParameter.SqlDbType = SqlDbType.Structured;
      sqlParameter.Value = new SqlDataRecordList((List<Tuple>) value, sqlDbType) switch { var o => o.IsEmpty ? null : o };
      sqlParameter.TypeName = sqlDbType == SqlDbType.BigInt ? LongListTypeName : StringListTypeName;
    }

    public override void BindLongList(DbParameter parameter, object value) => BindList(parameter, value, SqlDbType.BigInt);
    public override void BindStringList(DbParameter parameter, object value) => BindList(parameter, value, SqlDbType.NVarChar);
  }
}
