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

    public override void BindTable(DbParameter parameter, object value)
    {
      SqlParameter sqlParameter = (SqlParameter) parameter;
      sqlParameter.SqlDbType = SqlDbType.Structured;
      sqlParameter.TypeName = sqlParameter.ParameterName + "_tvp";

      var tuples = (List<Tuple>) value;
      SqlDataRecordList records = tuples.Count == 0 ? null : new(tuples);
      sqlParameter.Value = records;
      sqlParameter.TypeName = records?.SqlDbType == SqlDbType.BigInt ? LongListTypeName : StringListTypeName;
    }
  }
}
