using System;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Orm.Linq.Expressions
{
  internal readonly struct ColumnMap
  {
    private readonly ColNum[] reverseMap;

    public ColNum IndexOf(ColNum column) =>
      (ushort)column >= reverseMap.Length
        ? (ColNum) (-1)
        : reverseMap[column];

    public ColumnMap(IReadOnlyList<ColNum> map)
    {
      if (map.Count == 0) {
        reverseMap = Array.Empty<ColNum>();
      }
      else {
        reverseMap = new ColNum[map.Max() + 1];
        Array.Fill(reverseMap, (ColNum) (-1));
        for (int i = map.Count; i-- > 0;) {
          reverseMap[map[i]] = (ColNum) i;
        }
      }
    }
  }
}
