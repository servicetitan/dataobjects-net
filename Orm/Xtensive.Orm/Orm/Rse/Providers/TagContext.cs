// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using Xtensive.Core;

namespace Xtensive.Orm.Rse.Providers
{
  public readonly struct TagContext : IDisposable
  {
    private readonly List<string> tags;

    public void Dispose()
    {
      if (tags != null) {
        tags.RemoveAt(tags.Count - 1);
      }
    }

    public TagContext(List<string> tags, string tag)
    {
      ArgumentValidator.EnsureArgumentNotNull(tag, nameof(tag));
      (this.tags = tags)?.Add(tag);
    }
  }
}
