// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.10.22

using Xtensive.Orm.Model;

namespace Xtensive.Orm
{
  /// <summary>
  /// Describes <see cref="Entity"/>.<see cref="Entity.VersionInfo"/> change-related events.
  /// </summary>
  public readonly record struct EntityVersionInfoChangedEventArgs(Entity Entity, FieldInfo Field, bool Changed);
}