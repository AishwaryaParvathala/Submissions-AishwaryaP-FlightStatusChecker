using FlightStatusBackend.Models;

namespace FlightStatusBackend.Providers;

public interface IFlightStatusProvider
{
    Task<ProviderResult> GetStatusAsync(string flightNumber, DateTime dateUtc);
}
