// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.03.18

namespace Xtensive.Storage.Serialization
{
  /// <summary>
  /// Serialization kind (serialization by reference or by value).
  /// </summary>
  public enum SerializationKind
  {
    /// <summary>
    /// Serialization by reference.
    /// </summary>
    ByReference,

    /// <summary>
    /// Serialyzation by value.
    /// </summary>
    ByValue
  }
}