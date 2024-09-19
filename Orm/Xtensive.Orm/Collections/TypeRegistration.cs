// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.21

using System.Diagnostics;
using System.Reflection;

namespace Xtensive.Collections;

/// <summary>
/// Describes a single type registration call to <see cref="TypeRegistry"/>.
/// </summary>
[Serializable]
[DebuggerDisplay("Type = {Type}, Assembly = {Assembly}, Namespace = {Namespace}")]
public readonly struct TypeRegistration : IEquatable<TypeRegistration>
{
  /// <summary>
  /// Gets the type registered by this action.
  /// </summary>
  public Type Type { get; }

  /// <summary>
  /// Gets the assembly registered by this action.
  /// </summary>
  public Assembly Assembly { get; }

  /// <summary>
  /// Gets the namespace registered by this action.
  /// </summary>
  public string Namespace { get; }

  #region Equality members

  /// <inheritdoc/>
  public bool Equals(TypeRegistration other) => Type == other.Type && Assembly == other.Assembly && Namespace == other.Namespace;

  /// <inheritdoc/>
  public override bool Equals(object obj) => obj is TypeRegistration other && Equals(other);

  /// <inheritdoc/>
  public override int GetHashCode() => HashCode.Combine(Type, Assembly, Namespace);

  /// <inheritdoc/>
  public static bool operator ==(in TypeRegistration left, in TypeRegistration right) => left.Equals(right);

  /// <inheritdoc/>
  public static bool operator !=(in TypeRegistration left, in TypeRegistration right) => !left.Equals(right);

  #endregion

  // Constructors

  /// <summary>
  /// Initializes new instance of this type.
  /// </summary>
  /// <param name="type">The type to register.</param>
  public TypeRegistration(Type type)
  {
    ArgumentNullException.ThrowIfNull(type);
    Type = type;
  }

  /// <summary>
  /// Initializes new instance of this type.
  /// </summary>
  /// <param name="assembly">The assembly to register.</param>
  public TypeRegistration(Assembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    Assembly = assembly;
  }

  /// <summary>
  /// Initializes new instance of this type.
  /// </summary>
  /// <param name="assembly">The assembly to register.</param>
  /// <param name="namespace">The namespace to register.</param>
  public TypeRegistration(Assembly assembly, string @namespace)
    : this(assembly)
  {
    ArgumentNullException.ThrowIfNull(@namespace);
    Namespace = @namespace;
  }
}
