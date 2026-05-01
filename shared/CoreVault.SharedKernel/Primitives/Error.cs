namespace CoreVault.SharedKernel.Primitives;

/// <summary>
/// Represents a structured error in the system.
/// Every failure has a code (machine-readable) 
/// and a message (human-readable).
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public string Code { get; }
    public string Message { get; }

    private Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error Create(string code, string message) => new(code, message);

    // Pre-built common banking errors
    public static class Validation
    {
        public static Error Required(string field) =>
            Create($"Validation.{field}.Required", $"{field} is required.");

        public static Error InvalidFormat(string field) =>
            Create($"Validation.{field}.InvalidFormat", $"{field} format is invalid.");

        public static Error OutOfRange(string field) =>
            Create($"Validation.{field}.OutOfRange", $"{field} is out of acceptable range.");
    }

    public static class Auth
    {
        public static readonly Error Unauthorized =
            Create("Auth.Unauthorized", "You are not authorized to perform this action.");

        public static readonly Error InvalidCredentials =
            Create("Auth.InvalidCredentials", "Email or password is incorrect.");

        public static readonly Error TokenExpired =
            Create("Auth.TokenExpired", "Your session has expired. Please log in again.");

        public static readonly Error AccountLocked =
            Create("Auth.AccountLocked", "This account has been locked. Contact support.");
    }

    public static class NotFound
    {
        public static Error Resource(string resource, object id) =>
            Create($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");
    }

    public override string ToString() => $"[{Code}] {Message}";
}