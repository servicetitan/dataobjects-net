// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.09.12

using System.Collections.Generic;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Validation
{
  internal sealed class VoidValidationContext : ValidationContext
  {
    public override void Reset()
    {
    }

    public override void Validate(ValidationReason reason)
    {
    }

    public override void Validate(Entity target)
    {
    }

    public override IReadOnlyList<EntityErrorInfo> ValidateAndGetErrors() => EmptyEntityErrorCollection;

    public override IReadOnlyList<ValidationResult> ValidateAndGetErrors(Entity target) => EmptyValidationResultCollection;

    public override IReadOnlyList<ValidationResult> ValidateOnceAndGetErrors(Entity target) => EmptyValidationResultCollection;

    public override void ValidateSetAttempt(Entity target, FieldInfo field, object value)
    {
    }

    public override void RegisterForValidation(Entity target)
    {
    }

    public override void RegisterForValidation(Entity target, FieldInfo field)
    {
    }
  }
}