// Copyright (C) 2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xtensive.Core
{
  public readonly struct ReversedReadOnlyList<T> : IReadOnlyList<T>
  {
    private readonly IReadOnlyList<T> implementation;

    public int Count => implementation.Count;
    public T this[int index] => implementation[Count - index - 1];

    public IEnumerator<T> GetEnumerator() => implementation.Reverse().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ReversedReadOnlyList(IReadOnlyList<T> implementation) =>
      this.implementation = implementation;
  }
}
