// Copyright (C) 2009-2010 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Kofman
// Created:    2009.10.08


using Xtensive.Orm.Model;

namespace Xtensive.Orm
{
  /// <summary>
  /// Describes <see cref="Entity"/> field related events containing field value.
  /// </summary>
  public readonly record struct EntityFieldValueEventArgs(Entity Entity, FieldInfo Field, object Value);
}