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
      (ushort)column >= reverseMap.Length
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
      if (map.Count == 0) {
        reverseMap = Array.Empty<ColNum>();
      }
      else {
        reverseMap = ArrayPool<ColNum>.Shared.Rent(map.Max() + 1);
        Array.Fill(reverseMap, (ColNum) (-1));
        for (int i = map.Count; i-- > 0;) {
          reverseMap[map[i]] = (ColNum) i;
        }
      }
    }
  }
}
