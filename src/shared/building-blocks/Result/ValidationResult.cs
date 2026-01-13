namespace Platform.BuildingBlocks.Result;

/// <summary>
/// Represents a validation result containing multiple errors.
/// Used for FluentValidation integration.
/// </summary>
public sealed class ValidationResult : Result, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

/// <summary>
/// Represents a validation result with a value of type T.
/// </summary>
public sealed class ValidationResult<T> : Result<T>, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(default, false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationResult<T> WithErrors(Error[] errors) => new(errors);
}

/// <summary>
/// Interface for validation results with multiple errors.
/// </summary>
public interface IValidationResult
{
    public static readonly Error ValidationError = Error.ValidationError(
        "Validation.Error",
        "One or more validation failures occurred");

    Error[] Errors { get; }
}
