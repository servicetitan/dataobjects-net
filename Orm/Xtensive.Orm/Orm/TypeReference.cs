// Copyright (C) 2003-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Dmitri Maximov
// Created:    2009.10.09

using Xtensive.Orm.Model;

namespace Xtensive.Orm;

/// <summary>
/// Reference to <see cref="TypeInfo"/> with the specified degree of accuracy.
/// </summary>
[Serializable]
public readonly record struct TypeReference(TypeInfo Type, TypeReferenceAccuracy Accuracy);
