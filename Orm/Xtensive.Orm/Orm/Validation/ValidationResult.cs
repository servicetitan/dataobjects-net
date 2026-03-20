using System;
using Xtensive.Core;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Validation
{
  /// <summary>
  /// Validation result.
  /// </summary>
  public class ValidationResult
  {
    private static readonly ValidationResult SuccessInstance = new();

    /// <summary>
    /// Gets successful validation result.
    /// </summary>
    public static ValidationResult Success { get { return SuccessInstance; } }

    /// <summary>
    /// Gets validator that produced validation error.
    /// </summary>
    public IValidator Source { get; }

    /// <summary>
    /// Gets value indicating validation status.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Gets error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets field validated field.
    /// </summary>
    public FieldInfo Field { get; }

    /// <summary>
    /// Gets validated value.
    /// </summary>
    public object Value { get; }

    private ValidationResult()
    {
    }

    /// <summary>
    /// Initializes new instance of this type.
    /// </summary>
    /// <param name="source">Validator that produced this object.</param>
    /// <param name="errorMessage">Validation error message.</param>
    /// <param name="field">Validated field.</param>
    /// <param name="value">Validated value.</param>
    public ValidationResult(IValidator source, string errorMessage, FieldInfo field = null, object value = null)
    {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentException.ThrowIfNullOrEmpty(errorMessage);

      IsError = true;
      Source = source;
      ErrorMessage = errorMessage;
      Field = field;
      Value = value;
    }
  }
}
