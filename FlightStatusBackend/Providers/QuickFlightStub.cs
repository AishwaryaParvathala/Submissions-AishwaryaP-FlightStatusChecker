using FlightStatusBackend.Models;
using Microsoft.Extensions.Logging;

namespace FlightStatusBackend.Providers;

public enum QuickFlightStatus
{
    OK,
    DELAY,
    CNCL,
    DVTD,
    UNKN
}

public class QuickFlightStub : IFlightStatusProvider
{
    private readonly ILogger<QuickFlightStub> _logger;

    public QuickFlightStub(ILogger<QuickFlightStub> logger)
    {
        _logger = logger;
    }

    public Task<ProviderResult> GetStatusAsync(string flightNumber, DateTime dateUtc)
    {
        var fn = (flightNumber ?? string.Empty).Trim().ToUpperInvariant();
        _logger.LogInformation("QuickFlight: Fetching status for flight {Flight} on {Date}", fn, dateUtc.ToString("yyyy-MM-dd"));

        // On-time scenario
        if (fn == "OT100")
        {
            _logger.LogDebug("QuickFlight: Matched OT100 - On-time scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(8), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(11), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = schedDep;
            var actualArr = schedArr;
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(7).AddMinutes(55), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "OT100",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = actualArr,
                Status = QuickFlightStatus.OK.ToString(),
            };

            _logger.LogInformation("QuickFlight: Returning OK status for OT100. Scheduled: {Sched}, Actual: {Actual}", schedDep, actualDep);
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "QuickFlight", LastUpdatedUtc = DateTime.SpecifyKind(lastUpdated, DateTimeKind.Utc), NormalizedResult = normalized });
        }

        // Delayed scenario
        if (fn == "DL200")
        {
            _logger.LogDebug("QuickFlight: Matched DL200 - Delayed scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(14), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(17).AddMinutes(30), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(15).AddMinutes(30), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(15).AddMinutes(5), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "DL200",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = null,
                Status = QuickFlightStatus.DELAY.ToString(),
                StatusReason = "Technical issue",
            };

            _logger.LogInformation("QuickFlight: Returning DELAY status for DL200. Scheduled: {Sched}, Actual: {Actual}, Reason: {Reason}", schedDep, actualDep, "Technical issue");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "QuickFlight", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Cancelled scenario
        if (fn == "UA789")
        {
            _logger.LogDebug("QuickFlight: Matched UA789 - Cancelled scenario");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(8), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "UA789",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                Status = QuickFlightStatus.CNCL.ToString(),
                StatusReason = "Cancelled per provider token",
            };

            _logger.LogInformation("QuickFlight: Returning CNCL status for UA789");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "QuickFlight", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Diverted scenario
        if (fn == "DV300")
        {
            _logger.LogDebug("QuickFlight: Matched DV300 - Diverted scenario");
            var schedDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(6), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var schedArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(9), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualDep = DateTime.SpecifyKind(dateUtc.Date.AddHours(6).AddMinutes(10), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var actualArr = DateTime.SpecifyKind(dateUtc.Date.AddHours(9).AddMinutes(50), DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(9).AddMinutes(40), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "DV300",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                ScheduledDepartureUtc = schedDep,
                ActualDepartureUtc = actualDep,
                ScheduledArrivalUtc = schedArr,
                ActualArrivalUtc = actualArr,
                Status = QuickFlightStatus.DVTD.ToString(),
                StatusReason = "Weather diversion",
            };

            _logger.LogInformation("QuickFlight: Returning DVTD status for DV300. Scheduled: {Sched}, Actual: {Actual}, Reason: {Reason}", schedDep, actualDep, "Weather diversion");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "QuickFlight", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        // Unknown scenario
        if (fn == "UK999")
        {
            _logger.LogDebug("QuickFlight: Matched UK999 - Unknown status scenario");
            var lastUpdated = DateTime.SpecifyKind(dateUtc.Date.AddHours(12).AddMinutes(5), DateTimeKind.Utc);

            var normalized = new FlightStatusResult
            {
                FlightNumber = "UK999",
                Date = dateUtc.ToString("yyyy-MM-dd"),
                Status = QuickFlightStatus.UNKN.ToString(),
                StatusReason = "No reliable data",
            };

            _logger.LogInformation("QuickFlight: Returning UNKN status for UK999");
            return Task.FromResult(new ProviderResult { IsSuccess = true, ProviderName = "QuickFlight", LastUpdatedUtc = lastUpdated, NormalizedResult = normalized });
        }

        _logger.LogWarning("QuickFlight: Flight {Flight} not found in stub data", fn);
        return Task.FromResult(new ProviderResult { IsSuccess = false, ProviderName = "QuickFlight", ErrorMessage = "Flight not found" });
    }
}
