// Copyright (C) 2009-2024 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Ivan Galkin
// Created:    2009.10.09

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xtensive.Core;


namespace Xtensive.Orm.Upgrade
{
  /// <summary>
  /// Remove field hint.
  /// </summary>
  [Serializable]
  public class RemoveFieldHint : UpgradeHint,
    IEquatable<RemoveFieldHint>
  {
    /// <summary>
    /// Gets the source type.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the source field.
    /// </summary>
    public string Field { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is explicit.
    /// </summary>
    public bool IsExplicit { get; set; }

    /// <summary>
    /// Gets affected column paths.
    /// </summary>
    public IReadOnlyList<string> AffectedColumns { get; internal set; }

    /// <inheritdoc/>
    public bool Equals(RemoveFieldHint other)
    {
      if (other is null)
        return false;
      if (ReferenceEquals(this, other))
        return true;
      return base.Equals(other)
        && other.Type == Type
        && other.Field == Field;
    }

    /// <inheritdoc/>
    public override bool Equals(UpgradeHint other) => Equals(other as RemoveFieldHint);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Type, Field);

    /// <inheritdoc/>
    public override string ToString() => $"Remove field: {Type}.{Field}";


    // Constructors

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="typeName">Full name of type that contains removing field.</param>
    /// <param name="fieldName">Removing field name.</param>
    public RemoveFieldHint(string typeName, string fieldName)
    {
      ArgumentException.ThrowIfNullOrEmpty(typeName);
      ArgumentException.ThrowIfNullOrEmpty(fieldName);
      Type = typeName;
      Field = fieldName;
      AffectedColumns = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="type">The type that contains removing field.</param>
    /// <param name="fieldName">Removing field name.</param>
    public RemoveFieldHint(Type type, string fieldName)
    {
      ArgumentNullException.ThrowIfNull(type);
      ArgumentException.ThrowIfNullOrEmpty(fieldName);

      Type = type.FullName;
      Field = fieldName;
      AffectedColumns = Array.Empty<string>();
    }

    /// <summary>
    /// Creates the instance of this hint.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="propertyAccessExpression">The field access expression.</param>
    /// <returns>The newly created instance of this hint.</returns>
    public static RemoveFieldHint Create<T>(Expression<Func<T, object>> propertyAccessExpression)
      where T: Entity
    {
      return new RemoveFieldHint(typeof(T), propertyAccessExpression.GetProperty().Name);
    }
  }
}
