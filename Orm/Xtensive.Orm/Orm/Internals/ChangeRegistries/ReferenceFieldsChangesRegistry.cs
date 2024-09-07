// Copyright (C) 2014-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kulakov
// Created:    2014.04.07

using System;
using System.Collections.Generic;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Internals
{
  /// <summary>
  /// Registers information about changed reference fields.
  /// </summary>
  internal struct ReferenceFieldsChangesRegistry()
  {
    private readonly HashSet<ReferenceFieldChangeInfo> changes = new();

    /// <summary>
    /// Registers information about field which value was set.
    /// </summary>
    /// <param name="fieldOwner">Key of entity which field was set.</param>
    /// <param name="fieldValue">Value of field.</param>
    /// <param name="field">Field which value was set./</param>
    public void Register(Key fieldOwner, Key fieldValue, FieldInfo field)
    {
      ArgumentNullException.ThrowIfNull(fieldOwner);
      ArgumentNullException.ThrowIfNull(fieldValue);
      ArgumentNullException.ThrowIfNull(field);
      Register(new ReferenceFieldChangeInfo(fieldOwner, fieldValue, field));
    }

    /// <summary>
    /// Registers information about field which value was set.
    /// </summary>
    /// <param name="fieldOwner">Key of entity which field was set.</param>
    /// <param name="fieldValue">Value of field.</param>
    /// <param name="auxiliaryEntity">Key of auxiliary entity which associated with <see cref="EntitySet{T}"/> field.</param>
    /// <param name="field">Field which value was set.</param>
    public void Register(Key fieldOwner, Key fieldValue, Key auxiliaryEntity, FieldInfo field)
    {
      ArgumentNullException.ThrowIfNull(fieldOwner);
      ArgumentNullException.ThrowIfNull(fieldValue);
      ArgumentNullException.ThrowIfNull(auxiliaryEntity);
      ArgumentNullException.ThrowIfNull(field);
      Register(new ReferenceFieldChangeInfo(fieldOwner, fieldValue, auxiliaryEntity, field));
    }

    /// <summary>
    /// Gets all registered items.
    /// </summary>
    /// <returns>All registered items.</returns>
    public IReadOnlySet<ReferenceFieldChangeInfo> GetItems() => changes;

    /// <summary>
    /// Removes all registered items.
    /// </summary>
    public void Clear() => changes.Clear();
    
    private void Register(ReferenceFieldChangeInfo fieldChangeInfo) => changes.Add(fieldChangeInfo);
  }
}
