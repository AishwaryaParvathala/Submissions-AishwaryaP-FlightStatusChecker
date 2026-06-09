using FlightStatusBackend.Models;
using FlightStatusBackend.Providers;
using Microsoft.Extensions.Logging;

namespace FlightStatusBackend.Services;

/// <summary>
/// Handles flight status requests by coordinating provider calls and normalization.
/// </summary>
public class FlightStatusHandler
{
    private readonly IEnumerable<IFlightStatusProvider> _providers;
    private readonly StatusNormalizer _normalizer;
    private readonly FlightStatusValidator _validator;
    private readonly ILogger<FlightStatusHandler> _logger;

    public FlightStatusHandler(
        IEnumerable<IFlightStatusProvider> providers,
        StatusNormalizer normalizer,
        FlightStatusValidator validator,
        ILogger<FlightStatusHandler> logger)
    {
        _providers = providers;
        _normalizer = normalizer;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Handles a flight status request.
    /// </summary>
    /// <param name="flightNumber">The flight number</param>
    /// <param name="date">The flight date in yyyy-MM-dd format</param>
    /// <param name="requestId">Request ID for tracing</param>
    /// <returns>IResult with flight status or error response</returns>
    public async Task<IResult> HandleAsync(string flightNumber, string date, string requestId)
    {
        _logger.LogInformation("[{RequestId}] Processing flight status request for {Flight} on {Date}", 
            requestId, flightNumber, date);

        // Validate input
        var validationResult = _validator.Validate(flightNumber, date, out var dateUtc);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("[{RequestId}] Validation failed: {Error}", requestId, validationResult.ErrorMessage);
            return Results.BadRequest(new
            {
                error = "Bad Request",
                message = validationResult.ErrorMessage,
                timestamp = DateTime.UtcNow
            });
        }

        // Normalize flight number for consistent lookups
        var normalizedFlightNumber = _validator.NormalizeFlightNumber(flightNumber);

        try
        {
            // Fetch status from all providers concurrently
            var normalizedStatus = await GetNormalizedFlightStatusAsync(normalizedFlightNumber, dateUtc);

            _logger.LogInformation("[{RequestId}] Flight {Flight} -> Status={Status}",
                requestId, normalizedFlightNumber, normalizedStatus.Status);

            return Results.Ok(normalizedStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Error processing request for flight {Flight}",
                requestId, normalizedFlightNumber);

            // Return generic error to client while logging details
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets normalized flight status by querying all providers and selecting the most recent.
    /// </summary>
    private async Task<FlightStatusResult> GetNormalizedFlightStatusAsync(string flightNumber, DateTime dateUtc)
    {
        _logger.LogDebug("Fetching status from all providers for flight {Flight}", flightNumber);

        // Call all providers concurrently
        var tasks = _providers.Select(p => p.GetStatusAsync(flightNumber, dateUtc)).ToList();
        var results = await Task.WhenAll(tasks);

        // Extract results from each provider
        var aeroResult = results.FirstOrDefault(r => r.ProviderName == "AeroTrack");
        var quickResult = results.FirstOrDefault(r => r.ProviderName == "QuickFlight");

        // Ensure we always have a result object (even if failure)
        aeroResult ??= new ProviderResult
        {
            IsSuccess = false,
            ProviderName = "AeroTrack",
            ErrorMessage = "No response"
        };

        quickResult ??= new ProviderResult
        {
            IsSuccess = false,
            ProviderName = "QuickFlight",
            ErrorMessage = "No response"
        };

        _logger.LogDebug("Provider results: AeroTrack={AeroSuccess}, QuickFlight={QuickSuccess}",
            aeroResult.IsSuccess, quickResult.IsSuccess);

        // Normalize the results
        return _normalizer.Normalize(aeroResult, quickResult, flightNumber, dateUtc);
    }
}
