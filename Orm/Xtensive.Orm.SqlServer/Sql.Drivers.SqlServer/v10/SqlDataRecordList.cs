using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient.Server;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Sql.Drivers.SqlServer.v10
{
  public class SqlDataRecordList : List<SqlDataRecord>
  {
    public SqlDbType SqlDbType {  get; set; } = SqlDbType.BigInt;

    public override string ToString()
    {
      StringBuilder sb = new();
      _ = sb.Append('[');
      for (int i = 0; i < this.Count; i++) {
        _ = sb.Append(this[i]);
        if (i < this.Count - 1) {
          _ = sb.Append(", ");
        }

      }
      _ = sb.Append(']');
      return sb.ToString();
    }

    private SqlDbType GetSqlDbType(object v) =>
      v switch {
        byte or short or ushort or int or uint or long or decimal or Enum => SqlDbType.BigInt,
        string => SqlDbType.NVarChar,
        null => throw new NotSupportedException($"null is not supported by TVP"),
        _ => throw new NotSupportedException($"Type {v.GetType()} is not supported by TVP")
      };

    public SqlDataRecordList(List<Tuple> tuples)
    {
      SqlMetaData[] metaDatas = null;

      int maxStringLength = 20;

      foreach (var tuple in tuples) {
        for (int i = 0; i < tuple.Count; ++i) {
          if (tuple.GetValueOrDefault(i) is string s) {
            maxStringLength = Math.Max(maxStringLength, s.Length);
          }
        }
      }

      foreach (var tuple in tuples) {
        if (metaDatas == null) {
          metaDatas = new SqlMetaData[tuple.Count];
          for (int i = 0; i < tuple.Count; ++i) {
            var fieldName = "Value";
            SqlDbType = GetSqlDbType(tuple.GetValueOrDefault(i));
            metaDatas[i] = SqlDbType == SqlDbType.NVarChar
              ? new SqlMetaData(fieldName, SqlDbType, maxStringLength)
              : new SqlMetaData(fieldName, SqlDbType);
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
            Enum e => Convert.ToInt64(e),
            var o => o
          };
          record.SetValue(i, fieldValue);
        }
        Add(record);
      }
    }
  }
}
