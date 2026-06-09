using System;
using System.Threading.Tasks;
using FlightStatusBackend.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlightStatus.Tests
{
    public class ProviderStubTests
    {
        [Fact]
        public async Task AeroTrack_stub_returns_expected_sample()
        {
            var stub = new AeroTrackStub(NullLogger<AeroTrackStub>.Instance);
            var res = await stub.GetStatusAsync("AA123", new DateTime(2026,6,10));
            Assert.NotNull(res);
            Assert.Equal("AeroTrack", res.ProviderName);
        }

        [Fact]
        public async Task QuickFlight_stub_returns_expected_sample()
        {
            var stub = new QuickFlightStub(NullLogger<QuickFlightStub>.Instance);
            var res = await stub.GetStatusAsync("AA123", new DateTime(2026,6,10));
            Assert.NotNull(res);
            Assert.Equal("QuickFlight", res.ProviderName);
        }
    }
}
