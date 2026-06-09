using FlightStatusBackend.Models;
using Microsoft.Extensions.Logging;

namespace FlightStatusBackend.Providers;

public enum AeroTrackStatus
{
    ON_TIME,
    DELAYED,
    CANCELLED,
    DIVERTED,
    UNKNOWN
}

public class AeroTrackStub : IFlightStatusProvider
{
    private readonly ILogger<AeroTrackStub> _logger;

    public AeroTrackStub(ILogger<AeroTrackStub> logger)
    {
        _logger = logger;
    }

    public Task<ProviderResult> GetStatusAsync(string flightNumber, DateTime dateUtc)
    {
        var fn = (flightNumber ?? string.Empty).Trim().ToUpperInvariant();
        _logger.LogInformation("AeroTrack: Fetching status for flight {Flight} on {Date}", fn, dateUtc.ToString("yyyy-MM-dd"));

        // On-time scenario
        if (fn == "OT100")
        {
            _logger.LogDebug("AeroTrack: Matched OT100 - On-time scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(8), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(11), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = schedDep;
            var actualArr = schedArr;
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(7).AddMinutes(50), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "OT100",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = actualArr,
                Terminal = "3",
                Gate = "C1",
                Status = AeroTrackStatus.ON_TIME.ToString(),
            };

            _logger.LogInformation("AeroTrack: Returning ON_TIME status for OT100. Scheduled: {Sched}, Actual: {Actual}", schedDep, actualDep);
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "AeroTrack", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Delayed scenario
        if (fn == "DL200")
        {
            _logger.LogDebug("AeroTrack: Matched DL200 - Delayed scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(14), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(17).AddMinutes(30), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(15).AddMinutes(15), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(14).AddMinutes(50), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "DL200",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = null,
                Terminal = "1",
                Gate = "A7",
                Status = AeroTrackStatus.DELAYED.ToString(),
                StatusReason = "ATC congestion",
            };

            _logger.LogInformation("AeroTrack: Returning DELAYED status for DL200. Scheduled: {Sched}, Actual: {Actual}, Reason: {Reason}", schedDep, actualDep, "ATC congestion");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "AeroTrack", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Cancelled scenario
        if (fn == "UA789")
        {
            _logger.LogDebug("AeroTrack: Matched UA789 - Cancelled scenario");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(8), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "UA789",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                Status = AeroTrackStatus.CANCELLED.ToString(),
                StatusReason = "Cancelled per provider token",
            };

            _logger.LogInformation("AeroTrack: Returning CANCELLED status for UA789");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "AeroTrack", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Diverted scenario
        if (fn == "DV300")
        {
            _logger.LogDebug("AeroTrack: Matched DV300 - Diverted scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(6), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(9), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(6).AddMinutes(10), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(9).AddMinutes(45), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(9).AddMinutes(50), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "DV300",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = actualArr,
                Terminal = "4",
                Gate = "D2",
                Status = AeroTrackStatus.DIVERTED.ToString(),
                StatusReason = "Weather diversion",
            };

            _logger.LogInformation("AeroTrack: Returning DIVERTED status for DV300. Scheduled: {Sched}, Actual: {Actual}, Reason: {Reason}", schedDep, actualDep, "Weather diversion");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "AeroTrack", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Unknown scenario
        if (fn == "UK999")
        {
            _logger.LogDebug("AeroTrack: Matched UK999 - Unknown status scenario");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(12), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "UK999",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                Status = AeroTrackStatus.UNKNOWN.ToString(),
                StatusReason = "No reliable data",
            };

            _logger.LogInformation("AeroTrack: Returning UNKNOWN status for UK999");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "AeroTrack", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        _logger.LogWarning("AeroTrack: Flight {Flight} not found in stub data", fn);
        return Task.FromResult(new ProviderResult { IsSuccess = false, ProviderName = "AeroTrack", ErrorMessage = "Flight not found" });
    }
}