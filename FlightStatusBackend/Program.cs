using FlightStatusBackend.Middleware;
using FlightStatusBackend.Providers;
using FlightStatusBackend.Services;
using FlightStatusBackend.Models;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.AddConsole();

// DI
builder.Services.AddScoped<StatusNormalizer>();
builder.Services.AddScoped<FlightStatusValidator>();
builder.Services.AddScoped<FlightStatusHandler>();
builder.Services.AddScoped<IFlightStatusProvider, AeroTrackStub>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightStub>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
// Enable CORS early
app.UseCors("AllowFrontend");

app.MapGet("/health", () => Results.Ok("Flight Status API is running"));

app.MapGet("/flights/status/{flightNumber}/{date}", async (
    string flightNumber,
    string date,
    FlightStatusHandler handler,
    HttpContext httpContext) =>
{
    var requestId = httpContext.TraceIdentifier;
    return await handler.HandleAsync(flightNumber, date, requestId);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
