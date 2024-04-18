using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Orm.Linq.Expressions
{
  internal readonly struct ColumnMap : IDisposable
  {
    private readonly ColNum[] reverseMap;

    public int IndexOf(ColNum column) =>
      (ushort) column >= reverseMap.Length
        ? -1
        : reverseMap[column];

    public void Dispose()
    {
      if (reverseMap.Length > 0) {
        ArrayPool<ColNum>.Shared.Return(reverseMap);
      }
    }

    public ColumnMap(IReadOnlyList<ColNum> map)
    {
      var n = map.Count == 0 ? 0 : map.Max() + 1;
      if (n == 0) {
        reverseMap = Array.Empty<ColNum>();
      }
      else {
        reverseMap = ArrayPool<ColNum>.Shared.Rent(n);
        Array.Fill(reverseMap, (ColNum) (-1));
        for (int i = map.Count; i-- > 0;) {
          var colNum = map[i];
          if (colNum >= 0) {
            reverseMap[colNum] = (ColNum) i;
          }
        }
      }
    }
  }
}
