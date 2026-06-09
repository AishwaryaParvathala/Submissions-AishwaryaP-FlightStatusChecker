namespace FlightStatusBackend.Models;

public class ProviderResult
{
    public bool IsSuccess { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? LastUpdatedUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public FlightStatusResult? NormalizedResult { get; set; }
}
