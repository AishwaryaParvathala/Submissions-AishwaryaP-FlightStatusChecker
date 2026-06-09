using System;
using System.Threading.Tasks;
using FlightStatusBackend.Models;
using FlightStatusBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlightStatus.Tests
{
    public class NormalizerTests
    {
        private readonly StatusNormalizer _normalizer = new StatusNormalizer(NullLogger<StatusNormalizer>.Instance);

        [Fact]
        public void OnTime_when_estimated_within_15_minutes_returns_OnTime()
        {
            var scheduled = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var estimated = scheduled.AddMinutes(10);
            var provider = new ProviderResult
            {
                IsSuccess = true,
                ProviderName = "AeroTrack",
                LastUpdatedUtc = DateTime.UtcNow,
                NormalizedResult = new FlightStatusResult
                {
                    ScheduledDepartureUtc = scheduled.ToString("o"),
                    ActualDepartureUtc = estimated.ToString("o")
                }
            };

            var other = new ProviderResult { IsSuccess = false };
            var result = _normalizer.Normalize(provider, other, "AA123", scheduled);

            Assert.Equal("OnTime", result.Status);
        }

        [Fact]
        public void Delayed_when_estimated_exceeds_15_minutes_returns_Delayed()
        {
            var scheduled = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var estimated = scheduled.AddMinutes(30);
            var provider = new ProviderResult
            {
                IsSuccess = true,
                ProviderName = "QuickFlight",
                LastUpdatedUtc = DateTime.UtcNow,
                NormalizedResult = new FlightStatusResult
                {
                    ScheduledDepartureUtc = scheduled.ToString("o"),
                    ActualDepartureUtc = estimated.ToString("o")
                }
            };

            var other = new ProviderResult { IsSuccess = false };
            var result = _normalizer.Normalize(provider, other, "AA123", scheduled);

            Assert.Equal("Delayed", result.Status);
        }

        [Fact]
        public void Cancelled_token_overrides_OnTime()
        {
            var scheduled = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var estimated = scheduled.AddMinutes(5);
            var provider = new ProviderResult
            {
                IsSuccess = true,
                ProviderName = "QuickFlight",
                LastUpdatedUtc = DateTime.UtcNow,
                NormalizedResult = new FlightStatusResult
                {
                    ScheduledDepartureUtc = scheduled.ToString("o"),
                    ActualDepartureUtc = estimated.ToString("o")
                }
            };
            // provider token indicating cancellation should override timing
            provider.NormalizedResult.Status = "CNCL";

            var other = new ProviderResult { IsSuccess = false };
            var result = _normalizer.Normalize(provider, other, "AA123", scheduled);

            Assert.Equal("Cancelled", result.Status);
        }

        [Fact]
        public void Diverted_token_overrides_OnTime()
        {
            var scheduled = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var estimated = scheduled.AddMinutes(0);
            var provider = new ProviderResult
            {
                IsSuccess = true,
                ProviderName = "AeroTrack",
                LastUpdatedUtc = DateTime.UtcNow,
                NormalizedResult = new FlightStatusResult
                {
                    ScheduledDepartureUtc = scheduled.ToString("o"),
                    ActualDepartureUtc = estimated.ToString("o")
                }
            };
            // provider token indicating diversion should override timing
            provider.NormalizedResult.Status = "DIVERTED";

            var other = new ProviderResult { IsSuccess = false };
            var result = _normalizer.Normalize(provider, other, "AA123", scheduled);

            Assert.Equal("Diverted", result.Status);
        }
    }
}
