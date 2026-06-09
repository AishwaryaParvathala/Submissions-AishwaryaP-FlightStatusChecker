using System;
using System.Linq;
using FlightStatusBackend.Models;
using Microsoft.Extensions.Logging;

namespace FlightStatusBackend.Services;

public class StatusNormalizer
{
    private const int ThresholdSeconds = 15 * 60;
    private readonly ILogger<StatusNormalizer> _logger;

    public StatusNormalizer(ILogger<StatusNormalizer> logger)
    {
        _logger = logger;
    }

    public FlightStatusResult Normalize(ProviderResult aero, ProviderResult quick, string flightNumber, DateTime dateUtc)
    {
        _logger.LogInformation("Starting normalization for flight {Flight} on {Date}", flightNumber, dateUtc.ToString("yyyy-MM-dd"));
        _logger.LogDebug("AeroTrack result: IsSuccess={AeroSuccess}, LastUpdated={AeroUpdated}", aero.IsSuccess, aero.LastUpdatedUtc);
        _logger.LogDebug("QuickFlight result: IsSuccess={QuickSuccess}, LastUpdated={QuickUpdated}", quick.IsSuccess, quick.LastUpdatedUtc);

        ProviderResult? chosen = null;
        if (aero.IsSuccess && quick.IsSuccess)
        {
            if (aero.LastUpdatedUtc.HasValue && quick.LastUpdatedUtc.HasValue)
            {
                if (aero.LastUpdatedUtc.Value > quick.LastUpdatedUtc.Value)
                {
                    chosen = aero;
                    _logger.LogDebug("Selected AeroTrack (most recent: {AeroUpdated} > {QuickUpdated})", aero.LastUpdatedUtc.Value, quick.LastUpdatedUtc.Value);
                }
                else if (quick.LastUpdatedUtc.Value > aero.LastUpdatedUtc.Value)
                {
                    chosen = quick;
                    _logger.LogDebug("Selected QuickFlight (most recent: {QuickUpdated} > {AeroUpdated})", quick.LastUpdatedUtc.Value, aero.LastUpdatedUtc.Value);
                }
                else
                {
                    chosen = aero;
                    _logger.LogDebug("Selected AeroTrack (timestamps equal)");
                }
            }
            else if (aero.LastUpdatedUtc.HasValue)
            {
                chosen = aero;
                _logger.LogDebug("Selected AeroTrack (QuickFlight has no timestamp)");
            }
            else if (quick.LastUpdatedUtc.HasValue)
            {
                chosen = quick;
                _logger.LogDebug("Selected QuickFlight (AeroTrack has no timestamp)");
            }
            else
            {
                chosen = aero;
                _logger.LogDebug("Selected AeroTrack (neither has timestamp)");
            }
        }
        else if (aero.IsSuccess)
        {
            chosen = aero;
            _logger.LogDebug("Selected AeroTrack (QuickFlight failed)");
        }
        else if (quick.IsSuccess)
        {
            chosen = quick;
            _logger.LogDebug("Selected QuickFlight (AeroTrack failed)");
        }

        if (chosen == null)
        {
            _logger.LogWarning("No provider data available for flight {Flight} on {Date}. Returning Unknown status", flightNumber, dateUtc.ToString("yyyy-MM-dd"));
            return new FlightStatusResult
            {
                FlightNumber = flightNumber,
                Date = dateUtc.ToString("yyyy-MM-dd"),
                Status = "Unknown",
                StatusReason = "Flight information unavailable for the provided details. Please try again later."
            };
        }

        var baseResult = chosen.NormalizedResult ?? new FlightStatusResult
        {
            FlightNumber = flightNumber,
            Date = dateUtc.ToString("yyyy-MM-dd")
        };

        UnifiedFlightStatus status = DetermineStatusFromProvider(chosen);
        _logger.LogInformation("Determined unified status for {Flight}: {UnifiedStatus} from provider token: {ProviderToken}", flightNumber, status, baseResult.Status);

        // write back human readable status
        baseResult.Status = status.ToString();

        if (string.IsNullOrEmpty(baseResult.StatusReason))
        {
            baseResult.StatusReason = GenerateStatusReason(chosen, status);
        }

        _logger.LogInformation("Normalization complete for flight {Flight}. Final status: {Status}, Reason: {Reason}", flightNumber, baseResult.Status, baseResult.StatusReason);

        return baseResult;
    }

    private UnifiedFlightStatus DetermineStatusFromProvider(ProviderResult result)
    {
        var r = result.NormalizedResult;
        string? token = null;
        // try to extract token from normalized result first
        if (r != null && !string.IsNullOrEmpty(r.Status))
        {
            token = r.Status;
        }

        _logger.LogDebug("Determining status from provider {ProviderName} with token: {Token}", result.ProviderName, token);

        // Recognize AeroTrack provider tokens
        if (!string.IsNullOrEmpty(token))
        {
            var tUpper = token!.ToUpperInvariant();
            if (tUpper == "CANCELLED")
            {
                _logger.LogDebug("Mapped AeroTrack token '{Token}' to Cancelled", token);
                return UnifiedFlightStatus.Cancelled;
            }
            if (tUpper == "DIVERTED")
            {
                _logger.LogDebug("Mapped AeroTrack token '{Token}' to Diverted", token);
                return UnifiedFlightStatus.Diverted;
            }
            if (tUpper == "ON_TIME")
            {
                _logger.LogDebug("Mapped AeroTrack token '{Token}' to OnTime", token);
                return UnifiedFlightStatus.OnTime;
            }
            if (tUpper == "DELAYED")
            {
                _logger.LogDebug("Mapped AeroTrack token '{Token}' to Delayed", token);
                return UnifiedFlightStatus.Delayed;
            }
            if (tUpper == "UNKNOWN")
            {
                _logger.LogDebug("Mapped AeroTrack token '{Token}' to Unknown", token);
                return UnifiedFlightStatus.Unknown;
            }
        }

        // Recognize QuickFlight provider tokens
        if (!string.IsNullOrEmpty(token))
        {
            var tUpper = token!.ToUpperInvariant();
            if (tUpper == "OK")
            {
                _logger.LogDebug("Mapped QuickFlight token '{Token}' to OnTime", token);
                return UnifiedFlightStatus.OnTime;
            }
            if (tUpper == "DELAY")
            {
                _logger.LogDebug("Mapped QuickFlight token '{Token}' to Delayed", token);
                return UnifiedFlightStatus.Delayed;
            }
            if (tUpper == "CNCL")
            {
                _logger.LogDebug("Mapped QuickFlight token '{Token}' to Cancelled", token);
                return UnifiedFlightStatus.Cancelled;
            }
            if (tUpper == "DVTD")
            {
                _logger.LogDebug("Mapped QuickFlight token '{Token}' to Diverted", token);
                return UnifiedFlightStatus.Diverted;
            }
            if (tUpper == "UNKN")
            {
                _logger.LogDebug("Mapped QuickFlight token '{Token}' to Unknown", token);
                return UnifiedFlightStatus.Unknown;
            }
        }

        if (r != null && r.ScheduledDepartureUtc != null && r.ActualDepartureUtc != null)
        {
            if (DateTime.TryParse(r.ScheduledDepartureUtc, out var sched) && DateTime.TryParse(r.ActualDepartureUtc, out var actual))
            {
                var delta = (actual - sched).TotalSeconds;
                _logger.LogDebug("Comparing scheduled departure {Scheduled} with actual {Actual}. Delta: {DeltaSeconds} seconds", sched, actual, delta);
                if (delta <= ThresholdSeconds)
                {
                    _logger.LogDebug("Delta within threshold ({Threshold}s), returning OnTime", ThresholdSeconds);
                    return UnifiedFlightStatus.OnTime;
                }
                _logger.LogDebug("Delta exceeds threshold ({Threshold}s), returning Delayed", ThresholdSeconds);
                return UnifiedFlightStatus.Delayed;
            }
        }

        if (r != null && r.ScheduledArrivalUtc != null && r.ActualArrivalUtc != null)
        {
            if (DateTime.TryParse(r.ScheduledArrivalUtc, out var schedA) && DateTime.TryParse(r.ActualArrivalUtc, out var actualA))
            {
                var delta = (actualA - schedA).TotalSeconds;
                _logger.LogDebug("Comparing scheduled arrival {Scheduled} with actual {Actual}. Delta: {DeltaSeconds} seconds", schedA, actualA, delta);
                if (delta <= ThresholdSeconds)
                {
                    _logger.LogDebug("Delta within threshold ({Threshold}s), returning OnTime", ThresholdSeconds);
                    return UnifiedFlightStatus.OnTime;
                }
                _logger.LogDebug("Delta exceeds threshold ({Threshold}s), returning Delayed", ThresholdSeconds);
                return UnifiedFlightStatus.Delayed;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            var tok = token!.ToUpperInvariant();
            var result_status = tok switch
            {
                "OK" => UnifiedFlightStatus.OnTime,
                "LATE" => UnifiedFlightStatus.Delayed,
                "DELAYED" => UnifiedFlightStatus.Delayed,
                "DEPARTED" => UnifiedFlightStatus.OnTime,
                "LANDED" or "ARRIVED" => UnifiedFlightStatus.OnTime,
                "CXL" => UnifiedFlightStatus.Cancelled,
                _ when tok.Contains("CANC") => UnifiedFlightStatus.Cancelled,
                _ when tok.Contains("DIVERT") => UnifiedFlightStatus.Diverted,
                _ => UnifiedFlightStatus.Unknown,
            };
            _logger.LogDebug("Fallback: Mapped token '{Token}' to {Status} via pattern matching", token, result_status);
            return result_status;
        }

        _logger.LogDebug("No token or timestamp data available. Defaulting to Unknown");
        return UnifiedFlightStatus.Unknown;
    }

    private string GenerateStatusReason(ProviderResult chosen, UnifiedFlightStatus status)
    {
        var r = chosen.NormalizedResult;
        if (status == UnifiedFlightStatus.Cancelled)
        {
            _logger.LogDebug("Generating reason for Cancelled status");
            return "Cancelled per provider token";
        }
        if (status == UnifiedFlightStatus.Diverted)
        {
            _logger.LogDebug("Generating reason for Diverted status");
            return "Diverted per provider token";
        }
        if (r != null && r.ScheduledDepartureUtc != null && r.ActualDepartureUtc != null)
        {
            if (DateTime.TryParse(r.ScheduledDepartureUtc, out var sched) && DateTime.TryParse(r.ActualDepartureUtc, out var actual))
            {
                var mins = (int)Math.Round((actual - sched).TotalMinutes);
                _logger.LogDebug("Departure delta: {Minutes} minutes", mins);
                if (status == UnifiedFlightStatus.OnTime)
                {
                    var reason = $"Departure within {Math.Abs(mins)} minutes";
                    _logger.LogDebug("Returning reason: {Reason}", reason);
                    return reason;
                }
                if (status == UnifiedFlightStatus.Delayed)
                {
                    var reason = $"Departure {Math.Abs(mins)} minutes late";
                    _logger.LogDebug("Returning reason: {Reason}", reason);
                    return reason;
                }
            }
        }

        // if provider normalized result contains a status token use it
        if (chosen.NormalizedResult != null && !string.IsNullOrEmpty(chosen.NormalizedResult.Status))
        {
            var reason = $"Provider state {chosen.NormalizedResult.Status}";
            _logger.LogDebug("Returning reason from provider state: {Reason}", reason);
            return reason;
        }

        _logger.LogDebug("No reason details available, returning empty string");
        return string.Empty;
    }
}
