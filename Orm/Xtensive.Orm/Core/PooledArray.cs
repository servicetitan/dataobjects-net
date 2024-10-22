using System.Buffers;

namespace Xtensive.Core;

internal readonly struct PooledArray<T>(int length, bool clearArray = false) : IDisposable
{
  public T[] Array { get; } = length > 0 ? ArrayPool<T>.Shared.Rent(length) : [];

  public static implicit operator T[](in PooledArray<T> ar) => ar.Array;

  public void Dispose()
  {
    if (Array.Length > 0) {
      ArrayPool<T>.Shared.Return(Array, clearArray);
    }
  }
}
