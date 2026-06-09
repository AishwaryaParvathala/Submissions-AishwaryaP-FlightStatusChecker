using System.Text.Json.Serialization;

namespace FlightStatusBackend.Models;

public class FlightStatusResult
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    // Status as human-readable word, e.g. "OnTime", "Delayed", "Cancelled", "Diverted", "Unknown"
    public string Status { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
    public string? ScheduledDepartureUtc { get; set; }
    public string? ActualDepartureUtc { get; set; }
    public string? ScheduledArrivalUtc { get; set; }
    public string? ActualArrivalUtc { get; set; }
    public string? Terminal { get; set; }
    public string? Gate { get; set; }

}
