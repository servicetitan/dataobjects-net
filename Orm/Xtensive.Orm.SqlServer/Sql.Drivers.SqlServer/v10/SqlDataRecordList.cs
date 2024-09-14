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
    public SqlDbType SqlDbType {  get; set; } = SqlDbType.BigInt;

    public override string ToString() =>
      $"[{string.Join(", ", this.Select(o => o.GetValue(0) switch {
        string s => $"\"{s}\"",
        var ns => ns.ToString()
      }))}]";

    private SqlDbType GetSqlDbType(object v) =>
      v switch {
        byte or short or ushort or int or uint or long or decimal or Enum => SqlDbType.BigInt,
        string => SqlDbType.NVarChar,
        null => throw new NotSupportedException("null is not supported by TVP"),
        _ => throw new NotSupportedException($"Type {v.GetType()} is not supported by TVP")
      };

    public SqlDataRecordList(IReadOnlyList<Tuple> tuples)
      : base(tuples.Count)
    {
      SqlMetaData[] metaDatas = null;

      foreach (var tuple in tuples) {
        if (metaDatas == null) {
          SqlDbType = GetSqlDbType(tuple.GetValueOrDefault(0));
          metaDatas = [
            SqlDbType == SqlDbType.NVarChar
              ? new SqlMetaData("Value", SqlDbType, tuples.Max(t => (t.GetValueOrDefault(0) as string)?.Length ?? 20))
              : new SqlMetaData("Value", SqlDbType)
          ];
        }
        
        var valueObj = tuple.GetValueOrDefault(0);
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
        if (castValue is not null) {
          SqlDataRecord record = new(metaDatas);
          record.SetValue(0, castValue);
          Add(record);
        }
      }
    }
  }
}
