// Copyright (C) 2009-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexis Kochetov
// Created:    2009.10.22

using System;

namespace Xtensive.Orm
{
  /// <summary>
  /// Arguments for completing entity remove event.
  /// </summary>
  public readonly record struct EntityRemoveCompletedEventArgs(Entity Entity, Exception Exception);
}