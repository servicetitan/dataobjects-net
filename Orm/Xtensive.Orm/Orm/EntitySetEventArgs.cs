// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.10.23

using Xtensive.Orm.Model;

namespace Xtensive.Orm
{
  /// <summary>
  /// Describes <see cref="Orm.EntitySet{TItem}"/>-related events.
  /// </summary>
  public readonly struct EntitySetEventArgs
  {
    public Entity Entity { get; }
    public FieldInfo Field { get; }

    /// <summary>
    /// Gets the <see cref="EntitySetBase"/> to which this event is related.
    /// </summary>
    public EntitySetBase EntitySet { get; }


    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="entitySet">The entity set.</param>
    public EntitySetEventArgs(EntitySetBase entitySet)
    {
      Entity = entitySet.Owner;
      Field = entitySet.Field;
      EntitySet = entitySet;
    }
  }
}