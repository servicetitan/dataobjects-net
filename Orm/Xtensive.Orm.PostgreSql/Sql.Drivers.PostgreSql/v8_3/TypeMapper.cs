// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.06.23

using System;
using System.Data;
using System.Data.Common;

namespace Xtensive.Sql.Drivers.PostgreSql.v8_3
{
  internal class TypeMapper : v8_2.TypeMapper
  {
    public override void BindGuid(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Guid;
      parameter.Value = value ?? DBNull.Value;
    }

    public override Guid ReadGuid(DbDataReader reader, int index) => reader.GetGuid(index);

    public override SqlValueType MapGuid(int? length, int? precision, int? scale) => new SqlValueType(SqlType.Guid);

    // Constructors

    public TypeMapper(SqlDriver driver)
      : base(driver)
    {
    }
  }
}