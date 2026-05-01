namespace CoreVault.SharedKernel.Guards;

/// <summary>
/// Guard clauses validate inputs at the entry point of domain logic.
/// Fail fast — catch bad data before it corrupts domain state.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, string paramName)
    {
        if (value is null)
            throw new ArgumentNullException(paramName,
                $"{paramName} cannot be null.");
        return value;
    }

    public static string AgainstNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                $"{paramName} cannot be null or empty.", paramName);
        return value;
    }

    public static decimal AgainstNegativeOrZero(decimal value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException(
                $"{paramName} must be greater than zero. Got: {value}", paramName);
        return value;
    }

    public static decimal AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException(
                $"{paramName} cannot be negative. Got: {value}", paramName);
        return value;
    }

    public static Guid AgainstEmptyGuid(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                $"{paramName} cannot be an empty Guid.", paramName);
        return value;
    }

    public static int AgainstOutOfRange(int value, string paramName,
        int min, int max)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName,
                $"{paramName} must be between {min} and {max}. Got: {value}");
        return value;
    }
}