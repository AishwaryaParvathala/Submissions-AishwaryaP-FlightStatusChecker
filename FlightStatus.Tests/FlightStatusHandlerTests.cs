using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FlightStatusBackend.Models;
using FlightStatusBackend.Providers;
using FlightStatusBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlightStatus.Tests
{
    public class FlightStatusHandlerTests
    {
        private FlightStatusHandler CreateHandler()
        {
            var aero = new AeroTrackStub(NullLogger<AeroTrackStub>.Instance);
            var quick = new QuickFlightStub(NullLogger<QuickFlightStub>.Instance);
            var normalizer = new StatusNormalizer(NullLogger<StatusNormalizer>.Instance);
            var validator = new FlightStatusValidator();
            var providers = new IFlightStatusProvider[] { aero, quick };
            return new FlightStatusHandler(providers, normalizer, validator, NullLogger<FlightStatusHandler>.Instance);
        }

        [Fact]
        public async Task HandleAsync_returns_BadRequest_for_invalid_input()
        {
            var handler = CreateHandler();
            var result = await handler.HandleAsync("", "bad-date", "req-1");

            var ctx = new DefaultHttpContext();
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging();
            ctx.RequestServices = services.BuildServiceProvider();
            ctx.Response.Body = new MemoryStream();
            await result.ExecuteAsync(ctx);
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(ctx.Response.Body);
            var body = await sr.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
            Assert.Contains("Bad Request", body);
        }

        [Fact]
        public async Task HandleAsync_returns_OnTime_for_OT100()
        {
            var handler = CreateHandler();
            var result = await handler.HandleAsync("OT100", "2026-06-10", "req-2");

            var ctx = new DefaultHttpContext();
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging();
            ctx.RequestServices = services.BuildServiceProvider();
            ctx.Response.Body = new MemoryStream();
            await result.ExecuteAsync(ctx);
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(ctx.Response.Body);
            var body = await sr.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);

            var doc = JsonSerializer.Deserialize<FlightStatusResult>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(doc);
            Assert.Equal("OnTime", doc.Status);
        }
    }
}
