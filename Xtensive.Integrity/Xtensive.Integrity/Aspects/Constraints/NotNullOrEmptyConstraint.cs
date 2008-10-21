// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2008.07.25

using System;
using Xtensive.Integrity.Resources;
using Xtensive.Integrity.Validation.Interfaces;

namespace Xtensive.Integrity.Aspects.Constraints
{
  /// <summary>
  /// Ensures property value is not null or empty.
  /// </summary>
  [Serializable]
  public class NotNullOrEmptyConstraintAttribute : PropertyConstraintAspect
  {
    /// <inheritdoc/>
    public override void CheckValue(IValidationAware target, object value)
    {
      string stringValue = (string) value;
      
      if (string.IsNullOrEmpty(stringValue))
        throw new ConstraintViolationException(
          string.Format(Strings.ValueCanNotBeEmpty));
    }

    /// <inheritdoc/>
    public override bool IsSupported(Type valueType)
    {
      return valueType==typeof (string);
    }
  }
}