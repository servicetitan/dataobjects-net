// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Orm.Rse.Providers
{
  public readonly struct TagScope : IDisposable
  {
    private readonly List<string> tags;
    private readonly string tag;
    private readonly int index;

    // Truncating to our own slot (rather than popping the last element) keeps double and
    // out-of-order disposal from removing a tag that belongs to an enclosing scope.
    public void Dispose()
    {
      if (tags != null && index < tags.Count && ReferenceEquals(tags[index], tag)) {
        tags.RemoveRange(index, tags.Count - index);
      }
    }

    internal TagScope(List<string> tags, string tag)
    {
      ArgumentNullException.ThrowIfNull(tags);
      ArgumentNullException.ThrowIfNull(tag);
      this.tags = tags;
      this.tag = tag;
      index = tags.Count;
      tags.Add(tag);
    }
  }
}
