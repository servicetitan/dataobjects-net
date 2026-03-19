// Copyright (C) 2010-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2010.01.27

using Xtensive.Orm.Internals.Prefetch;
using Xtensive.Orm.Model;
using Xtensive.Orm.Providers;

namespace Xtensive.Orm.Building;

[Serializable]
internal static class PrefetchActionContainer
{
  // Returns null if associations is empty
  public static Action<SessionHandler, IEnumerable<Key>> BuildPrefetchAction(TypeInfo type, IEnumerable<AssociationInfo> associations)
  {
    var fields = associations.Select(static association => new PrefetchFieldDescriptor(association.OwnerField, true, false)).ToArray();
    return fields.Length > 0
      ? Prefetch
      : null;

    // Returns null if associations is empty
    public Action<SessionHandler, IEnumerable<Key>> BuildPrefetchAction(IEnumerable<AssociationInfo> associations)
    {
      fields = associations.Select(static association => new PrefetchFieldDescriptor(association.OwnerField, true, false))
        .ToArray();
      return fields.Count > 0 ? Prefetch : null;
    }

    private void Prefetch(SessionHandler sh, IEnumerable<Key> keys)
    {
      foreach (var key in keys)
        sh.Prefetch(key, type, fields);
    }
  }
}
