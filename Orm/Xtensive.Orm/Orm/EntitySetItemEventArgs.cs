// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.10.23

using Xtensive.Orm.Model;

namespace Xtensive.Orm
{
  /// <summary>
  /// Describes an event related to <see cref="EntitySet{TItem}"/> item.
  /// </summary>
  public class EntitySetItemEventArgs
  {
    public Entity Entity { get; }
    public FieldInfo Field { get; }

    /// <summary>
    /// Gets the <see cref="EntitySetBase"/> to which this event is related.
    /// </summary>
    public EntitySetBase EntitySet { get; }

    /// <summary>
    /// Gets the item to which this event is related.
    /// </summary>
    public Entity Item { get; }


    // Cosntructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="entitySet">The entity set.</param>
    /// <param name="item">The item.</param>
    public EntitySetItemEventArgs(EntitySetBase entitySet, Entity item)
    {
      Entity = entitySet.Owner;
      Field = entitySet.Field;
      EntitySet = entitySet;
      Item = item;
    }
  }
}