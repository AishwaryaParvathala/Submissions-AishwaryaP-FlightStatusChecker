using System.Globalization;

namespace FlightStatusBackend.Services;

/// <summary>
/// Validates flight status request input parameters.
/// </summary>
public class FlightStatusValidator
{
    /// <summary>
    /// Validates flight number and date parameters.
    /// </summary>
    /// <param name="flightNumber">The flight number to validate</param>
    /// <param name="date">The date string to validate</param>
    /// <param name="parsedDate">Output: The parsed DateTime if validation succeeds</param>
    /// <returns>ValidationResult with success status and error message if applicable</returns>
    public ValidationResult Validate(string? flightNumber, string? date, out DateTime parsedDate)
    {
        parsedDate = DateTime.MinValue;

        // Validate and normalize flight number
        if (string.IsNullOrWhiteSpace(flightNumber))
            return new ValidationResult { IsValid = false, ErrorMessage = "flightNumber is required." };

        var normalizedFlightNumber = NormalizeFlightNumber(flightNumber);
        if (string.IsNullOrWhiteSpace(normalizedFlightNumber))
            return new ValidationResult { IsValid = false, ErrorMessage = "flightNumber cannot be empty or contain only whitespace." };

        // Validate date format
        if (string.IsNullOrWhiteSpace(date))
            return new ValidationResult { IsValid = false, ErrorMessage = "date is required in yyyy-MM-dd format." };

        var trimmedDate = date.Trim();
        if (!DateTime.TryParseExact(
            trimmedDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var dateUtc))
            return new ValidationResult { IsValid = false, ErrorMessage = "date must be in yyyy-MM-dd format." };

        parsedDate = dateUtc;
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Normalizes flight number by trimming and removing internal whitespace.
    /// </summary>
    public string NormalizeFlightNumber(string? flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
            return string.Empty;

        var trimmed = flightNumber.Trim();
        var withoutWhitespace = new string(trimmed.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return withoutWhitespace.ToUpperInvariant();
    }
}

/// <summary>
/// Result of input validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the input is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}
