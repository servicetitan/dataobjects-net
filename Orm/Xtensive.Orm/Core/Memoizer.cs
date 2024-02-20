using System;
using System.Collections.Concurrent;
using BitFaster.Caching.Lru;

namespace Xtensive.Core;

internal static class Memoizer
{
    public const int DefaultMaxCacheSize = 1000;

    private static class MemoizerCache<TKey, TValue>
    {
        internal static readonly ConcurrentDictionary<(Func<TKey, TValue> Factory, int MaxSize), FastConcurrentLru<TKey, TValue>> cacheByFactoryFunction = new();
    }

    public static TValue Get<TKey, TValue>(TKey key, Func<TKey, TValue> factory, int maxSize = DefaultMaxCacheSize) =>
        MemoizerCache<TKey, TValue>.cacheByFactoryFunction.GetOrAdd((factory, maxSize), static t => new(t.MaxSize))
            .GetOrAdd(key, factory);
}