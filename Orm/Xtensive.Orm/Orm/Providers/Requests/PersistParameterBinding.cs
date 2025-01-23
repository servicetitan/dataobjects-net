// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.25

using Xtensive.Sql;

namespace Xtensive.Orm.Providers;

/// <summary>
/// A binding of a parameter for <see cref="PersistRequest"/>.
/// </summary>
public sealed class PersistParameterBinding(
  TypeMapping typeMapping,
  ushort rowIndex,
  ColNum fieldIndex,
  ParameterTransmissionType transmissionType = ParameterTransmissionType.Regular,
  PersistParameterBindingType bindingType = PersistParameterBindingType.Regular
) : ParameterBinding(typeMapping, transmissionType)
{
  public ushort RowIndex { get; } = rowIndex;
  public ColNum FieldIndex { get; } = fieldIndex;
  public PersistParameterBindingType BindingType { get; } = bindingType;

  // Constructors

  public PersistParameterBinding(TypeMapping typeMapping, ColNum fieldIndex, ParameterTransmissionType transmissionType, PersistParameterBindingType bindingType)
    : this(typeMapping, 0, fieldIndex, transmissionType, bindingType)
  {
  }

  public PersistParameterBinding(TypeMapping typeMapping, ColNum fieldIndex, ParameterTransmissionType transmissionType)
    : this(typeMapping, fieldIndex, transmissionType, PersistParameterBindingType.Regular)
  {
  }

  public PersistParameterBinding(TypeMapping typeMapping, ColNum fieldIndex)
    : this(typeMapping, fieldIndex, ParameterTransmissionType.Regular, PersistParameterBindingType.Regular)
  {
  }
}
