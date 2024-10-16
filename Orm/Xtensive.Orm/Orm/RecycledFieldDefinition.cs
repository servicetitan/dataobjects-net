// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Andrey Turkov
// Created:    2013.08.21

using System;
using Xtensive.Core;

namespace Xtensive.Orm
{
  /// <summary>
  /// Recycled field definition.
  /// </summary>
  public class RecycledFieldDefinition : RecycledDefinition
  {
    /// <summary>
    /// Owner type with recycled field.
    /// </summary>
    public Type OwnerType { get; private set; }

    /// <summary>
    /// Name of recycled field.
    /// </summary>
    public string FieldName { get; private set; }

    /// <summary>
    /// Type of recycled field.
    /// </summary>
    public Type FieldType { get; private set; }

    /// <summary>
    /// Original field name.
    /// </summary>
    public string OriginalFieldName { get; private set; }

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="ownerType">Owner type with recycled field.</param>
    /// <param name="fieldName">Name of recycled field.</param>
    /// <param name="fieldType">Type of recycled field.</param>
    public RecycledFieldDefinition(Type ownerType, string fieldName, Type fieldType)
    {
      Initialize(ownerType, fieldName, fieldType);
    }

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    /// <param name="ownerType">Owner type with recycled field.</param>
    /// <param name="fieldName">Name of recycled field.</param>
    /// <param name="fieldType">Type of recycled field.</param>
    /// <param name="originalFieldName">Original field name.</param>
    public RecycledFieldDefinition(Type ownerType, string fieldName, Type fieldType, string originalFieldName)
    {
      Initialize(ownerType, fieldName, fieldType);
      ArgumentException.ThrowIfNullOrEmpty(originalFieldName);

      OriginalFieldName = originalFieldName;
    }

    private void Initialize(Type ownerType, string fieldName, Type fieldType)
    {
      ArgumentNullException.ThrowIfNull(ownerType);
      ArgumentException.ThrowIfNullOrEmpty(fieldName);
      ArgumentNullException.ThrowIfNull(fieldType);

      OwnerType = ownerType;
      FieldName = fieldName;
      FieldType = fieldType;
    }

    public override string ToString()
    {
      var fieldName = !string.IsNullOrEmpty(OriginalFieldName)
        ? $"{FieldName}({OriginalFieldName})"
        : FieldName;
      return $"RecycledFieldDefinition(Owner: {OwnerType.Name}, Field: {fieldName}, Type: {FieldType.Name})";
    }
  }
}
