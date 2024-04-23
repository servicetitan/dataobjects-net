// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.10.22

using System;

using Xtensive.Orm.Model;

namespace Xtensive.Orm
{
  /// <summary>
  /// Describes <see cref="Entity"/> field reading completion events.
  /// </summary>
  public readonly record struct EntityFieldValueGetCompletedEventArgs(Entity Entity, FieldInfo Field, object Value, Exception Exception);
}