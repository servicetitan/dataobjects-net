// Copyright (C) 2012-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2012.05.16

namespace Xtensive.Orm.Tracking
{
  internal readonly struct TrackingStackFrame()
  {
    private readonly Dictionary<Key, TrackingItem> items = new();
    public IReadOnlyCollection<TrackingItem> Items => items.Values.ToArray();

    public int Count => items.Count;

    public void Register(TrackingItem item)
    {
      ArgumentNullException.ThrowIfNull(item);

      var key = item.Key;
      if (!items.TryGetValue(key, out var existing)) {
        items.Add(key, item);
      }
      else if (item != existing) {
        existing.MergeWith(item);
      }
    }

    public void Clear() => items.Clear();

    public void MergeWith(TrackingStackFrame source)
    {
      foreach (var sourceItem in source.Items) {
        Register(sourceItem);
        var key = sourceItem.Key;
        if (items.TryGetValue(key, out var existing)) {
          existing.MergeWith(sourceItem);
        }
        else {
          items.Add(key, sourceItem);
        }
      }
    }
  }
}
