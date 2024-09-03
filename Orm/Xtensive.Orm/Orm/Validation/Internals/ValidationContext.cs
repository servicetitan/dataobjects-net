// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.01.05

using System.Collections.Generic;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Validation
{
  internal abstract class ValidationContext
  {
    protected static IReadOnlyList<EntityErrorInfo> EmptyEntityErrorCollection = [];

    protected static IReadOnlyList<ValidationResult> EmptyValidationResultCollection = [];

    public abstract void Reset();

    public abstract void Validate(ValidationReason reason);

    public abstract void Validate(Entity target);

    public abstract IReadOnlyList<EntityErrorInfo> ValidateAndGetErrors();

    public abstract IReadOnlyList<ValidationResult> ValidateAndGetErrors(Entity target);

    public abstract IReadOnlyList<ValidationResult> ValidateOnceAndGetErrors(Entity target);

    public abstract void ValidateSetAttempt(Entity target, FieldInfo field, object value);

    public abstract void RegisterForValidation(Entity target);

    public abstract void RegisterForValidation(Entity target, FieldInfo field);
  }
}