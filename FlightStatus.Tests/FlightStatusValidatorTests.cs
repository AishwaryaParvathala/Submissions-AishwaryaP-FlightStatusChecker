using System;
using FlightStatusBackend.Services;
using Xunit;

namespace FlightStatus.Tests
{
    public class FlightStatusValidatorTests
    {
        private readonly FlightStatusValidator _validator = new FlightStatusValidator();

        [Theory]
        [InlineData("AA123", "2026-06-10")]
        [InlineData(" aa 123 ", "2026-06-10")]
        public void Validate_returns_success_for_valid_input(string flight, string date)
        {
            var res = _validator.Validate(flight, date, out var dt);
            Assert.True(res.IsValid);
            Assert.Equal(new DateTime(2026,6,10), dt.Date);
        }

        [Theory]
        [InlineData(null, "2026-06-10")]
        [InlineData("   ", "2026-06-10")]
        public void Validate_fails_for_missing_flight(string flight, string date)
        {
            var res = _validator.Validate(flight, date, out var dt);
            Assert.False(res.IsValid);
            Assert.Contains("flightNumber", res.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_fails_for_bad_date_format()
        {
            var res = _validator.Validate("AA123", "10-06-2026", out var dt);
            Assert.False(res.IsValid);
            Assert.Contains("yyyy-MM-dd", res.ErrorMessage);
        }
    }
}
