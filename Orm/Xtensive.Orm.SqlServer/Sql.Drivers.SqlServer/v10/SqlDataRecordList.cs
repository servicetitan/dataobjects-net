using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient.Server;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Sql.Drivers.SqlServer.v10
{
  public class SqlDataRecordList : List<SqlDataRecord>
  {
    public override string ToString() =>
      $"[{string.Join(", ", this.Select(o => o.GetValue(0) switch {
        string s => $"\"{s}\"",
        var ns => ns.ToString()
      }))}]";

    public SqlDataRecordList(IReadOnlyList<Tuple> tuples, SqlDbType sqlDbType)
      : base(tuples.Count)
    {
      SqlMetaData[] metaDatas = null;
      foreach (var valueObj in tuples.Select(t => t.GetValueOrDefault(0)).Where(o => o != null)) {
        metaDatas ??= [
          sqlDbType == SqlDbType.BigInt
            ? new SqlMetaData("Value", sqlDbType)
            : new SqlMetaData("Value", sqlDbType, tuples.Max(t => (t.GetValueOrDefault(0) as string)?.Length ?? 20))
        ];
        var castValue = valueObj switch {
          byte n => (long) n,
          short n => (long) n,
          ushort n => (long) n,
          int n => (long) n,
          uint n => (long) n,
          decimal d => (long) d,
          Enum e => Convert.ToInt64(e),
          var o => o
        };
        SqlDataRecord record = new(metaDatas);
        record.SetValue(0, castValue);
        Add(record);
      }
    }
  }
}
