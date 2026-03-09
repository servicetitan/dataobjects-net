// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.12.28

using Xtensive.Core;

namespace Xtensive.Orm.Model;

/// <summary>
/// Describes a field that is a part of a primary key.
/// </summary>
[Serializable]
public readonly record struct KeyField(string Name, Direction Direction = Direction.Positive);
