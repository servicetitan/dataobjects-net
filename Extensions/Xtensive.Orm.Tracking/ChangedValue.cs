// Copyright (C) 2012-2022 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using Xtensive.Orm.Model;

namespace Xtensive.Orm.Tracking;

/// <summary>
/// Represents a pair of original and changed values for a persistent field
/// </summary>
public readonly record struct ChangedValue(FieldInfo Field, object OriginalValue, object NewValue);
