namespace Platform.BuildingBlocks.Result;

/// <summary>
/// Represents an error with a code and message.
/// Supports categorization via error types for standardized API responses.
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "The specified value is null", ErrorType.Failure);
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found", ErrorType.NotFound);
    public static readonly Error Unauthorized = new("Error.Unauthorized", "Authentication is required", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("Error.Forbidden", "Access is denied", ErrorType.Forbidden);
    public static readonly Error Conflict = new("Error.Conflict", "A conflict occurred with the current state", ErrorType.Conflict);
    public static readonly Error Locked = new("Error.Locked", "The resource is locked", ErrorType.Locked);
    public static readonly Error Validation = new("Error.Validation", "One or more validation errors occurred", ErrorType.Validation);

    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error NotFoundError(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error UnauthorizedError(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error ForbiddenError(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error ConflictError(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error LockedError(string code, string message) =>
        new(code, message, ErrorType.Locked);

    public static Error ValidationError(string code, string message) =>
        new(code, message, ErrorType.Validation);
}

/// <summary>
/// Categorizes errors for appropriate HTTP status code mapping.
/// </summary>
public enum ErrorType
{
    None = 0,
    Failure = 1,        // 500 Internal Server Error
    Validation = 2,     // 400 Bad Request
    NotFound = 3,       // 404 Not Found
    Unauthorized = 4,   // 401 Unauthorized
    Forbidden = 5,      // 403 Forbidden
    Conflict = 6,       // 409 Conflict
    Locked = 7          // 423 Locked
}
