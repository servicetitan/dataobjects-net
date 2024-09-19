using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient.Server;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Sql.Drivers.SqlServer.v10;

public class SqlDataRecordList(IReadOnlyList<Tuple> tuples, SqlDbType sqlDbType) : IEnumerable<SqlDataRecord>
{
  public bool IsEmpty { get; } = !tuples.Any(t => t.GetValueOrDefault(0) != null);

  public override string ToString() =>
    $"[{string.Join(", ", this.Select(o => o.GetValue(0) switch {
      string s => $"\"{s}\"",
      var ns => ns.ToString()
    }))}]";

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public IEnumerator<SqlDataRecord> GetEnumerator()
  {
    if (IsEmpty) {
      yield break;
    }
    switch (sqlDbType) {
      case SqlDbType.BigInt: {
        SqlMetaData[] metaData = [new("Value", sqlDbType)];
        SqlDataRecord record = new(metaData);
        HashSet<long> added = new();
        foreach (var valueObj in tuples.Select(t => t.GetValueOrDefault(0)).Where(o => o != null)) {
          long castValue = valueObj switch {
            byte n => n,
            short n => n,
            ushort n => n,
            int n => n,
            uint n => n,
            long n => n,
            decimal d => (long) d,
            Enum e => Convert.ToInt64(e),
            _ => throw new NotSupportedException($"type {valueObj.GetType()} is not supported")
          };
          if (added.Add(castValue)) {
            record.SetSqlInt64(0, castValue);
            yield return record;
          }
        }
      } break;
      case SqlDbType.NVarChar: {
        SqlMetaData[] metaData = [new("Value", sqlDbType, tuples.Max(t => (t.GetValueOrDefault(0) as string)?.Length ?? 20))];
        SqlDataRecord record = new(metaData);
        HashSet<string> added = new();
        foreach (var valueObj in tuples.Select(t => t.GetValueOrDefault(0)).Where(o => o != null)) {
          string castValue = (string) valueObj;
          if (added.Add(castValue)) {
            record.SetSqlString(0, castValue);
            yield return record;
          }
        }
      } break;
    }
  }
}
