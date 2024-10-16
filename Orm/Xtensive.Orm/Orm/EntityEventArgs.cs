// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2009.06.04

using System;


namespace Xtensive.Orm
{
  /// <summary>
  /// Describes <see cref="Entity"/>-related events.
  /// </summary>
  public readonly record struct EntityEventArgs(Entity Entity);
}