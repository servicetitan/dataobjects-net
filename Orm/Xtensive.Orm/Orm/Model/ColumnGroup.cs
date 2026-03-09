// Copyright (C) 2008-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexey Kochetov
// Created:    2008.08.01

using System.Diagnostics;
using Xtensive.Collections;

namespace Xtensive.Orm.Model;

/// <summary>
/// Describes a group of columns that belongs to the specified <see cref="TypeInfoRef"/>.
/// </summary>
[Serializable]
[DebuggerDisplay("Type = {TypeInfoRef}, Keys = {Keys}, Columns = {Columns}")]
public readonly record struct ColumnGroup
(
  TypeInfoRef TypeInfoRef,
  IReadOnlyList<ColNum> Keys,      // indexes of key columns.
  IReadOnlyList<ColNum> Columns   // indexes of all columns.
);
