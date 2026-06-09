# Flight Status Checker - Implementation Guide

**Phases:** 7 sequential phases with clear deliverables, testing checkpoints, and Copilot integration points.

---

## Phase Overview

| Phase | Title | Duration | Focus | Deliverables |
|-------|-------|----------|-------|---|
| 1 | Backend Setup & Foundations | 1-2 hrs | .NET project structure, models, DI | `.csproj`, models, DI configuration |
| 2 | Provider Stubs | 1-2 hrs | Implement `IFlightStatusProvider` & stubs | Interface, `AeroTrackStub.cs`, `QuickFlightStub.cs` |
| 3 | Normalizer Service | 1-2 hrs | Mapping rules, provider selection logic | `StatusNormalizer.cs` |
| 4 | API Endpoint & Validation | 1 hr | GET `/flights/status`, input validation, error handling | `Program.cs`, endpoint wiring |
| 5 | Unit Tests | 1-2 hrs | Test mapping, selection, edge cases | `NormalizerTests.cs`, `ProviderStubTests.cs` |
| 6 | React Frontend | 2-3 hrs | Search form, result card, API client | React components, TypeScript types |
| 7 | Docs & Deployment | 1-2 hrs | README, deployment, final integration | Docs, Dockerfile, deployment guide |

**Total Duration:** ~10-15 hours (solo development with Copilot assistance)

---

## Phase 1: Backend Setup & Foundations

**Objective:** Create a .NET Minimal API project with data models and DI configuration.

**Duration:** 1-2 hours

### Steps

#### 1.1 Create .NET Project
```bash
cd FlightStatus.Api
dotnet new web -n FlightStatus.Api
# Creates Program.cs, project structure
```

#### 1.2 Define Models
Create the following files in `FlightStatus.Api/Models/`:
- `UnifiedFlightStatus.cs` – Enum with 5 values (OnTime, Delayed, Cancelled, Diverted, Unknown)
- `FlightStatusResult.cs` – Main response model (17 properties as per spec.md)
- `ProviderResult.cs` – Internal provider wrapper (5 properties)

**Copilot Prompts:**
- "Generate the `UnifiedFlightStatus` enum with XML documentation per spec.md"
- "Generate `FlightStatusResult.cs` with all 17 properties and XML documentation"
- "Generate `ProviderResult.cs` as an internal model with the 5 properties and XML documentation"

**Testing:**
- [ ] Models compile without errors
- [ ] All properties are nullable/optional as specified
- [ ] XML documentation appears in IntelliSense

#### 1.3 Configure Program.cs (Skeleton)
```csharp
// Program.cs - minimal wiring for Phase 1
var builder = WebApplicationBuilder.CreateBuilder(args);

// DI setup (placeholder for providers and normalizer - filled in later phases)
builder.Services.AddScoped<StatusNormalizer>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok("Flight Status API is running"))
    .WithName("Health Check");

app.Run();
```

**Copilot Prompts:**
- "Generate a minimal Program.cs for a .NET 6+ Minimal API with health check endpoint and DI configuration comments"

**Testing:**
- [ ] `dotnet run` starts without errors
- [ ] `GET /health` returns 200 with "Flight Status API is running"

#### 1.4 Add Project References (if needed)
- xUnit (or NUnit) for testing
- Swashbuckle/Swagger for documentation (optional but recommended)

**Copilot Prompts:**
- "Add xUnit and Moq NuGet packages to a .NET Minimal API project via dotnet CLI"

**Testing:**
- [ ] `dotnet add package xunit`
- [ ] `dotnet add package xunit.runner.visualstudio`
- [ ] `dotnet add package Moq` (optional, for mocking in tests)

---

### Checkpoint: Phase 1 Complete

**Acceptance Criteria:**
- ✅ All 3 model files created and compile
- ✅ `Program.cs` runs and serves `/health`
- ✅ Project structure matches `FlightStatus.Api/` layout in spec.md
- ✅ All models have XML documentation

**Commit Message:**
```
feat: phase1 - backend setup with models and DI skeleton
- Create UnifiedFlightStatus enum with 5 status values
- Create FlightStatusResult and ProviderResult models
- Configure Program.cs with health check endpoint and DI
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt 3: Generate models and Program.cs skeleton
- Accepted: All model signatures and DI setup
- Rejected: None
- Rationale: Standard C# patterns; Copilot output aligns with spec.md requirements

---

## Phase 2: Provider Stubs

**Objective:** Implement `IFlightStatusProvider` interface and two stub providers (AeroTrack, QuickFlight).

**Duration:** 1-2 hours

### Steps

#### 2.1 Define IFlightStatusProvider Interface
Create `FlightStatus.Api/Providers/IFlightStatusProvider.cs`:
- Single async method: `Task<ProviderResult> GetStatusAsync(string flightNumber, DateTime dateUtc)`
- XML documentation explaining return contract (success/failure semantics)

**Copilot Prompts:**
- "Generate the `IFlightStatusProvider` interface with a single async method `GetStatusAsync` per the spec.md contract. Include XML documentation and error handling guidance in comments."

**Testing:**
- [ ] Interface compiles
- [ ] Method signature matches spec.md

#### 2.2 Implement AeroTrackStub
Create `FlightStatus.Api/Providers/AeroTrackStub.cs`:
- Implements `IFlightStatusProvider`
- Hardcodes deterministic responses for 2-3 sample flights (AA123, BA456, and optionally UA789)
- Each response includes all AeroTrack fields: terminal, gate, delay reason, actual times
- Responses include realistic `lastUpdatedUtc` values
- Unknown flights return `ProviderResult { IsSuccess = false, ErrorMessage = "Flight not found" }`

**Sample Stub Data (Start with these 2; expand during Phase 5 based on test needs):**

Flight AA123 (2026-06-10): Delayed
```csharp
{
  flightId: "AA123",
  status: "DEPARTED",
  schedule: { departureUtc: "2026-06-10T00:00:00Z", arrivalUtc: "2026-06-10T03:00:00Z" },
  actual: { departureUtc: "2026-06-10T00:25:00Z", arrivalUtc: null },
  terminal: "2",
  gate: "A12",
  delayReason: "ATC congestion",
  lastUpdatedUtc: "2026-06-09T20:31:00Z"
}
```

Flight BA456 (2026-06-11): On-time
```csharp
{
  flightId: "BA456",
  status: "LANDED",
  schedule: { departureUtc: "2026-06-11T10:00:00Z", arrivalUtc: "2026-06-11T13:30:00Z" },
  actual: { departureUtc: "2026-06-11T09:58:00Z", arrivalUtc: "2026-06-11T13:27:00Z" },
  terminal: "1",
  gate: "B5",
  delayReason: null,
  lastUpdatedUtc: "2026-06-11T13:40:00Z"
}
```

**Copilot Prompts:**
- "Generate the `AeroTrackStub.cs` class implementing `IFlightStatusProvider`. Include hardcoded responses for flights AA123 (Delayed) and BA456 (On-time) per the spec. For unknown flights, return IsSuccess=false. Parse the AeroTrack response format and normalize it to `ProviderResult` with `NormalizedResult` containing the unified `FlightStatusResult`."

**Testing:**
- [ ] `GetStatusAsync("AA123", 2026-06-10)` returns success with status "Delayed"
- [ ] `GetStatusAsync("BA456", 2026-06-11)` returns success with status "On-time"
- [ ] `GetStatusAsync("ZZ999", any-date)` returns failure with `IsSuccess = false`
- [ ] Actual times and terminal/gate are populated for AeroTrack
- [ ] `lastUpdatedUtc` is parsed correctly as DateTime

#### 2.3 Implement QuickFlightStub
Create `FlightStatus.Api/Providers/QuickFlightStub.cs`:
- Implements `IFlightStatusProvider`
- Hardcodes deterministic responses for the same sample flights (AA123, BA456)
- Each response includes only: flight, state, scheduled times, lastUpdatedUtc
- **No** gate, terminal, or actual times
- `lastUpdatedUtc` slightly earlier than AeroTrack for AA123 (to test provider selection)
- Unknown flights return `IsSuccess = false`

**Sample Stub Data:**

Flight AA123 (2026-06-10): Delayed
```csharp
{
  flight: "AA123",
  state: "LATE",
  scheduledDepUtc: "2026-06-10T00:00:00Z",
  scheduledArrUtc: "2026-06-10T03:00:00Z",
  lastUpdatedUtc: "2026-06-09T20:29:00Z"  // 2 minutes earlier than AeroTrack
}
```

Flight BA456 (2026-06-11): On-time
```csharp
{
  flight: "BA456",
  state: "OK",
  scheduledDepUtc: "2026-06-11T10:00:00Z",
  scheduledArrUtc: "2026-06-11T13:30:00Z",
  lastUpdatedUtc: "2026-06-11T13:35:00Z"  // 5 minutes earlier than AeroTrack
}
```

**Copilot Prompts:**
- "Generate the `QuickFlightStub.cs` class implementing `IFlightStatusProvider`. Include hardcoded responses for flights AA123 (LATE) and BA456 (OK) with slightly earlier `lastUpdatedUtc` than AeroTrack per the spec. For unknown flights, return IsSuccess=false. Parse the QuickFlight response format and normalize it to `ProviderResult`."

**Testing:**
- [ ] `GetStatusAsync("AA123", 2026-06-10)` returns success with status "Delayed"
- [ ] `GetStatusAsync("BA456", 2026-06-11)` returns success with status "On-time"
- [ ] `GetStatusAsync("ZZ999", any-date)` returns failure
- [ ] Terminal and gate are null for QuickFlight responses
- [ ] `lastUpdatedUtc` is 2-5 minutes earlier than AeroTrack for same flights

#### 2.4 Register Providers in Program.cs
```csharp
builder.Services.AddScoped<IFlightStatusProvider, AeroTrackStub>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightStub>();
```

**Testing:**
- [ ] `dotnet build` succeeds
- [ ] No DI resolution errors

---

### Checkpoint: Phase 2 Complete

**Acceptance Criteria:**
- ✅ `IFlightStatusProvider` interface defined
- ✅ `AeroTrackStub` and `QuickFlightStub` both compile
- ✅ Both providers return expected stubs for AA123 and BA456
- ✅ Unknown flights return `IsSuccess = false`
- ✅ DI registration in `Program.cs`

**Commit Message:**
```
feat: phase2 - provider stubs (AeroTrack, QuickFlight)
- Create IFlightStatusProvider interface
- Implement AeroTrackStub with AA123 (delayed) and BA456 (on-time)
- Implement QuickFlightStub with earlier lastUpdatedUtc for testing provider selection
- Register providers in DI
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt: "Generate AeroTrackStub and QuickFlightStub with sample data"
- Accepted: Provider structure, stub data, normalization to ProviderResult
- Rejected/Modified: Ensured lastUpdatedUtc values differ to test provider selection
- Rationale: Need deterministic, testable stubs with realistic timing differences

---

## Phase 3: Normalizer Service

**Objective:** Implement the core `StatusNormalizer` service with all mapping and selection rules.

**Duration:** 1-2 hours

### Steps

#### 3.1 Create StatusNormalizer Class
Create `FlightStatus.Api/Services/StatusNormalizer.cs`:

**Methods:**
```csharp
public class StatusNormalizer
{
    /// <summary>
    /// Normalizes responses from two providers into a single FlightStatusResult.
    /// Applies mapping rules and selects the provider with the latest lastUpdatedUtc.
    /// </summary>
    public FlightStatusResult Normalize(
        ProviderResult aeroTrackResult,
        ProviderResult quickFlightResult,
        string flightNumber,
        DateTime dateUtc);

    /// <summary>
    /// Maps a time-based delta and token to the unified status.
    /// Implements the 15-minute rule and token-based fallback.
    /// </summary>
    private UnifiedFlightStatus DetermineStatus(
        TimeSpan? timeDelta,
        string? providerToken,
        string providerName);

    /// <summary>
    /// Generates a human-friendly status reason.
    /// </summary>
    private string GenerateStatusReason(
        UnifiedFlightStatus status,
        TimeSpan? timeDelta,
        string? delayReason,
        string? providerToken);
}
```

#### 3.2 Implement Mapping Rules
Implement the following logic in `DetermineStatus()`:

**Rule 1a (Time-Based - Primary):**
- If provider has both scheduled and actual times:
  - Calculate `delta = actual - scheduled`
  - If delta ≤ 900 seconds (15 min): return `OnTime`
  - If delta > 900 seconds: return `Delayed`
- Fallback to arrival times if departure missing
- If both missing, fall through to Rule 1b

**Rule 1b (Token-Based - Fallback):**
- AeroTrack tokens: SCHEDULED, ON_TIME, BOARDING → OnTime; DEPARTED → OnTime (if times unavailable); DELAYED → Delayed; LANDED, ARRIVED → OnTime; CANCELLED → Cancelled; DIVERTED → Diverted
- QuickFlight tokens: OK → OnTime; LATE → Delayed; CXL → Cancelled; DIVERTED → Diverted; UNKNOWN → Unknown
- Unknown tokens: log warning, return Unknown

**Rule 1c (Conflict):**
- If both times and token available and disagree, prefer computed delta

**Copilot Prompts:**
- "Generate the `StatusNormalizer` class with a `Normalize()` method and `DetermineStatus()` helper. Implement the mapping rules as documented in spec.md: 15-minute threshold, token vocabularies for AeroTrack and QuickFlight, conflict resolution (prefer computed delta). Include inline comments for each rule. Use switch expressions for token mapping."

#### 3.3 Implement Provider Selection
Implement logic in `Normalize()`:

1. If both `aeroTrackResult.IsSuccess` and `quickFlightResult.IsSuccess`:
   - Compare `lastUpdatedUtc` values
   - Select the one with the later timestamp
   - If equal, prefer AeroTrack
2. If one succeeds, use that one
3. If both fail, return `FlightStatusResult { Status = Unknown, StatusReason = "Both providers unavailable" }`

**Copilot Prompts:**
- "Add provider selection logic to `Normalize()`: compare `lastUpdatedUtc`, select the latest, tie-breaker AeroTrack, handle failure cases."

#### 3.4 Implement Status Reason Generation
Add logic to generate human-friendly messages:
- "Departure 25 minutes late (ATC congestion)" – for Delayed with delay reason
- "Arrival within 3 minutes" – for OnTime with computed delta
- "Flight cancelled by airline" – for Cancelled
- "Both providers unavailable. Please try again later." – for Unknown

**Copilot Prompts:**
- "Generate a `GenerateStatusReason()` method that creates human-friendly messages for each status and scenario (on-time, delayed with reason, cancelled, diverted, unknown)."

#### 3.5 Testing Strategy
Unit test planning (not implementation yet; done in Phase 5):
- [ ] Normalize: both succeed → select latest
- [ ] Normalize: both succeed, equal timestamp → select AeroTrack
- [ ] Normalize: one succeeds → use successful one
- [ ] Normalize: both fail → return Unknown
- [ ] DetermineStatus: actual 10m late → OnTime
- [ ] DetermineStatus: actual 20m late → Delayed
- [ ] DetermineStatus: actual 15m late exactly → OnTime
- [ ] DetermineStatus: no times, token DELAYED → Delayed
- [ ] DetermineStatus: conflicting time/token → prefer time

---

### Checkpoint: Phase 3 Complete

**Acceptance Criteria:**
- ✅ `StatusNormalizer.cs` compiles
- ✅ All mapping rules implemented with comments
- ✅ Provider selection logic correctly compares `lastUpdatedUtc`
- ✅ Tie-breaker prefers AeroTrack
- ✅ Status reason generation produces human-friendly messages

**Commit Message:**
```
feat: phase3 - status normalizer with mapping and selection logic
- Implement DetermineStatus() with 15-minute rule and token mapping
- Implement provider selection by latest lastUpdatedUtc (tie-breaker: AeroTrack)
- Add GenerateStatusReason() for human-friendly messages
- Full implementation of spec.md normalization rules
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt: "Implement StatusNormalizer with mapping rules, provider selection, status reason generation"
- Accepted: All rule implementations, switch expressions for tokens, provider selection logic
- Rejected/Modified: Modified error case handling to ensure no exceptions are thrown
- Rationale: Core business logic; comprehensive testing in Phase 5

---

## Phase 4: API Endpoint & Validation

**Objective:** Expose the GET `/flights/status` endpoint with input validation and error handling.

**Duration:** 1 hour

### Steps

#### 4.1 Add Input Validation Helper
Create `FlightStatus.Api/Middleware/InputValidator.cs`:
```csharp
public static class InputValidator
{
    public static (bool IsValid, string? ErrorMessage) ValidateFlightNumber(string? flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
            return (false, "flightNumber is required.");
        if (flightNumber.Length < 2 || flightNumber.Length > 6)
            return (false, "flightNumber must be 2-6 characters.");
        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage, DateTime? Date) ValidateDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return (false, "date is required in yyyy-MM-dd format.", null);
        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return (true, null, date);
        return (false, "date must be in yyyy-MM-dd format.", null);
    }
}
```

**Copilot Prompts:**
- "Generate an input validation helper class with static methods for validating flightNumber (2-6 chars) and date (yyyy-MM-dd format). Return validation tuples with error messages."

**Testing:**
- [ ] ValidateFlightNumber("AA123") returns valid
- [ ] ValidateFlightNumber("A") returns invalid
- [ ] ValidateFlightNumber("AA123456") returns invalid
- [ ] ValidateDate("2026-06-10") returns valid
- [ ] ValidateDate("06-10-2026") returns invalid

#### 4.2 Add Endpoint Handler
Create `FlightStatus.Api/Handlers/FlightStatusHandler.cs` (or inline in Program.cs):

```csharp
app.MapGet("/flights/status", async (
    string? flightNumber,
    string? date,
    IEnumerable<IFlightStatusProvider> providers,
    StatusNormalizer normalizer,
    ILogger<Program> logger,
    HttpContext httpContext) =>
{
    var requestId = httpContext.TraceIdentifier;
    logger.LogInformation("[{RequestId}] GET /flights/status: flightNumber={FlightNumber}, date={Date}", requestId, flightNumber, date);

    // Validate inputs
    var (isFlightValid, flightError) = InputValidator.ValidateFlightNumber(flightNumber);
    if (!isFlightValid)
        return Results.BadRequest(new { error = "Bad Request", message = flightError, timestamp = DateTime.UtcNow });

    var (isDateValid, dateError, parsedDate) = InputValidator.ValidateDate(date);
    if (!isDateValid)
        return Results.BadRequest(new { error = "Bad Request", message = dateError, timestamp = DateTime.UtcNow });

    // Query both providers
    var tasks = providers.Select(p => p.GetStatusAsync(flightNumber.ToUpper(), parsedDate.Value)).ToList();
    var results = await Task.WhenAll(tasks);

    // Normalize and select
    var aeroTrackResult = results[0];
    var quickFlightResult = results[1];
    var flightStatus = normalizer.Normalize(aeroTrackResult, quickFlightResult, flightNumber, parsedDate.Value);

    logger.LogInformation("[{RequestId}] Selected provider: {Provider}, status: {Status}", requestId, flightStatus.Provider, flightStatus.Status);
    return Results.Ok(flightStatus);
})
.WithName("GetFlightStatus")
.Produces<FlightStatusResult>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);
```

**Copilot Prompts:**
- "Generate the GET /flights/status endpoint in Program.cs that: validates flightNumber and date, queries both injected providers in parallel, passes results to normalizer, returns FlightStatusResult. Include input validation with 400 responses, logging with requestId."

#### 4.3 Add Error Handling Middleware
Create `FlightStatus.Api/Middleware/ErrorHandlingMiddleware.cs`:

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Unhandled exception", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = "An unexpected error occurred. Please contact support.",
                requestId = context.TraceIdentifier
            });
        }
    }
}
```

Register in `Program.cs`:
```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
```

**Copilot Prompts:**
- "Generate an ErrorHandlingMiddleware that catches unhandled exceptions, logs them with requestId, and returns HTTP 500 with a JSON error response."

#### 4.4 Configure Logging
Add structured logging to `Program.cs`:

```csharp
builder.Logging.AddConsole();
builder.Logging.AddDebug();
```

**Testing:**
- [ ] `dotnet run` starts without errors
- [ ] `GET /flights/status?flightNumber=AA123&date=2026-06-10` returns 200 with FlightStatusResult
- [ ] `GET /flights/status?flightNumber=AA123` (missing date) returns 400 with error message
- [ ] `GET /flights/status?date=2026-06-10` (missing flightNumber) returns 400 with error message
- [ ] `GET /flights/status?flightNumber=AA123&date=invalid` returns 400
- [ ] Response includes provider, lastUpdatedUtc, status, statusReason
- [ ] Logs include requestId and provider selection details

---

### Checkpoint: Phase 4 Complete

**Acceptance Criteria:**
- ✅ Endpoint compiles and runs
- ✅ Input validation returns 400 for missing/invalid inputs
- ✅ Endpoint queries both providers in parallel
- ✅ Normalizer is called with both results
- ✅ FlightStatusResult is returned with HTTP 200
- ✅ Logging includes requestId and provider selection decision
- ✅ Unhandled exceptions are caught and return HTTP 500

**Commit Message:**
```
feat: phase4 - API endpoint with validation and error handling
- Create GET /flights/status endpoint with input validation
- Query both providers in parallel
- Pass results to normalizer
- Add ErrorHandlingMiddleware for exception capture
- Add structured logging with requestId
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt: "Generate GET /flights/status endpoint with validation, parallel provider calls, normalization"
- Accepted: Endpoint structure, validation logic, error handling middleware, logging
- Rejected: None
- Rationale: Standard .NET Minimal API patterns; aligns with spec.md requirements

---

## Phase 5: Unit Tests

**Objective:** Achieve comprehensive test coverage for mapping rules, provider selection, and error cases.

**Duration:** 1-2 hours

### Steps

#### 5.1 Create NormalizerTests.cs
Create `FlightStatus.Tests/NormalizerTests.cs` with xUnit tests:

**Test Cases (Minimum; add more as needed):**

1. **Normalize_BothSucceed_SelectsLatestProvider**
   - Arrange: Both providers return success; AeroTrack has later lastUpdatedUtc
   - Act: Call Normalize()
   - Assert: Result uses AeroTrack data

2. **Normalize_BothSucceed_TieBreaker_SelectsAeroTrack**
   - Arrange: Both providers have identical lastUpdatedUtc
   - Act: Call Normalize()
   - Assert: Result uses AeroTrack data

3. **Normalize_OneSucceeds_UsesSuccessful**
   - Arrange: AeroTrack succeeds, QuickFlight fails
   - Act: Call Normalize()
   - Assert: Result uses AeroTrack data

4. **Normalize_BothFail_ReturnsUnknown**
   - Arrange: Both providers return IsSuccess=false
   - Act: Call Normalize()
   - Assert: Status=Unknown, StatusReason contains "unavailable"

5. **DetermineStatus_ActualWithin15Min_ReturnsOnTime**
   - Arrange: Actual = Scheduled + 10 minutes
   - Act: Call DetermineStatus()
   - Assert: Returns OnTime

6. **DetermineStatus_ActualOver15Min_ReturnsDelayed**
   - Arrange: Actual = Scheduled + 25 minutes
   - Act: Call DetermineStatus()
   - Assert: Returns Delayed

7. **DetermineStatus_ActualExactly15Min_ReturnsOnTime**
   - Arrange: Actual = Scheduled + 15 minutes
   - Act: Call DetermineStatus()
   - Assert: Returns OnTime (boundary test)

8. **DetermineStatus_NoActualTimes_TokenDELAYED_ReturnsDelayed**
   - Arrange: No actual times; token="DELAYED"
   - Act: Call DetermineStatus()
   - Assert: Returns Delayed

9. **DetermineStatus_NoActualTimes_TokenCANCELLED_ReturnsCancelled**
   - Arrange: No actual times; token="CANCELLED"
   - Act: Call DetermineStatus()
   - Assert: Returns Cancelled

10. **GenerateStatusReason_Delayed25MinWithReason_IncludesReason**
    - Arrange: Status=Delayed, timeDelta=1500s, delayReason="ATC congestion"
    - Act: Call GenerateStatusReason()
    - Assert: Result contains "25" and "ATC congestion"

**Copilot Prompts:**
- "Generate xUnit tests for StatusNormalizer covering: provider selection (latest timestamp, tie-breaker), one/both provider failures, 15-minute rule boundary, token-based status determination, status reason generation. Use Arrange-Act-Assert pattern. Create at least 10 test cases."

#### 5.2 Create ProviderStubTests.cs
Create `FlightStatus.Tests/ProviderStubTests.cs`:

**Test Cases:**

1. **AeroTrackStub_AA123_ReturnsDelayed**
   - Assert: Status=Delayed, terminal="2", gate="A12"

2. **AeroTrackStub_BA456_ReturnsOnTime**
   - Assert: Status=OnTime, actual times within 15 minutes

3. **AeroTrackStub_UnknownFlight_ReturnsFailure**
   - Assert: IsSuccess=false, ErrorMessage populated

4. **QuickFlightStub_AA123_ReturnsDelayed**
   - Assert: Status=Delayed, terminal=null, gate=null

5. **QuickFlightStub_BA456_ReturnsOnTime**
   - Assert: Status=OnTime

6. **QuickFlightStub_UnknownFlight_ReturnsFailure**
   - Assert: IsSuccess=false

7. **ProvidersConsistency_AA123_BothReturnDelayed**
   - Assert: Both return Delayed status (though via different mechanisms)

8. **ProvidersTimingDiff_AA123_AeroTrackNewer**
   - Assert: AeroTrack.LastUpdatedUtc > QuickFlight.LastUpdatedUtc (tests provider selection)

**Copilot Prompts:**
- "Generate xUnit tests for AeroTrackStub and QuickFlightStub. Test: AA123 returns delayed, BA456 returns on-time, unknown flights fail. Assert on specific fields (terminal, gate, provider names, lastUpdatedUtc). Verify provider timing differences for selection logic testing."

#### 5.3 Run Tests
```bash
cd FlightStatus.Tests
dotnet test
```

**Expected Output:**
- ✅ All 18+ tests pass
- ✅ Code coverage >80% for StatusNormalizer
- ✅ No warnings or errors

#### 5.4 Expand Stubs (Based on Test Gaps)
During testing, identify additional edge cases:
- Add cancelled flight stub (e.g., UA789)
- Add diverted flight stub
- Add flight with missing actual times
- Add flight with only arrival times

**Copilot Prompts:**
- "Add a cancelled flight (UA789) and a diverted flight to both AeroTrackStub and QuickFlightStub. Create deterministic responses for testing these statuses."

---

### Checkpoint: Phase 5 Complete

**Acceptance Criteria:**
- ✅ All unit tests pass
- ✅ Test coverage >80% for StatusNormalizer
- ✅ Test coverage >70% for Providers
- ✅ At least 18 test cases implemented
- ✅ Edge cases documented (boundary values, error cases)

**Commit Message:**
```
feat: phase5 - comprehensive unit tests
- Create NormalizerTests with 10+ test cases covering mapping rules and provider selection
- Create ProviderStubTests with 8+ test cases verifying stub data and consistency
- Verify 15-minute boundary, token mapping, provider selection, error handling
- Achieve >80% code coverage on core logic
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt: "Generate comprehensive unit tests for StatusNormalizer and provider stubs"
- Accepted: All test cases, Arrange-Act-Assert pattern, assertions
- Rejected/Modified: Enhanced edge case coverage (15-minute boundary, token mapping)
- Rationale: Thorough test suite ensures correctness of mapping and selection logic per spec

---

## Phase 6: React Frontend

**Objective:** Build a React + TypeScript UI with search form and result card.

**Duration:** 2-3 hours

### Steps

#### 6.1 Setup React Project
```bash
cd flight-status-ui
npx create-react-app . --template typescript
# or
npm init vite@latest . -- --template react-ts
```

Install dependencies:
```bash
npm install axios
npm install --save-dev typescript
```

#### 6.2 Define TypeScript Types
Create `flight-status-ui/src/types/FlightStatus.ts`:

```typescript
export enum UnifiedFlightStatus {
  OnTime = "OnTime",
  Delayed = "Delayed",
  Cancelled = "Cancelled",
  Diverted = "Diverted",
  Unknown = "Unknown"
}

export interface FlightStatusResult {
  flightNumber: string;
  date: string;
  status: UnifiedFlightStatus;
  statusReason: string | null;
  scheduledDepartureUtc: string | null;
  actualDepartureUtc: string | null;
  scheduledArrivalUtc: string | null;
  actualArrivalUtc: string | null;
  terminal: string | null;
  gate: string | null;
}

export interface ApiError {
  error: string;
  message: string;
  timestamp: string;
}
```

**Copilot Prompts:**
- "Generate TypeScript types for FlightStatusResult (with UnifiedFlightStatus enum) and ApiError. Use the spec.md model as reference."

#### 6.3 Create API Client Service
Create `flight-status-ui/src/services/flightStatusApi.ts`:

```typescript
import axios from "axios";
import { FlightStatusResult, ApiError } from "../types/FlightStatus";

const API_BASE_URL = process.env.REACT_APP_API_URL || "http://localhost:5000";

const apiClient = axios.create({
  baseURL: API_BASE_URL
});

export const getFlightStatus = async (
  flightNumber: string,
  date: string
): Promise<FlightStatusResult> => {
  const response = await apiClient.get<FlightStatusResult>("/flights/status", {
    params: { flightNumber, date }
  });
  return response.data;
};
```

**Copilot Prompts:**
- "Generate an API client service using axios for querying GET /flights/status. Include error handling and environment variable for API base URL."

#### 6.4 Create SearchForm Component
Create `flight-status-ui/src/components/SearchForm.tsx`:

```typescript
import React, { useState } from "react";

interface SearchFormProps {
  onSearch: (flightNumber: string, date: string) => void;
  isLoading: boolean;
}

export const SearchForm: React.FC<SearchFormProps> = ({ onSearch, isLoading }) => {
  const [flightNumber, setFlightNumber] = useState("");
  const [date, setDate] = useState("");
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  const validateForm = () => {
    const newErrors: { [key: string]: string } = {};
    if (!flightNumber.trim()) newErrors.flightNumber = "Flight number is required";
    if (!date) newErrors.date = "Date is required";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSearch(flightNumber, date);
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ marginBottom: "20px" }}>
      <div style={{ marginBottom: "10px" }}>
        <label htmlFor="flightNumber">Flight Number:</label>
        <input
          id="flightNumber"
          type="text"
          value={flightNumber}
          onChange={(e) => setFlightNumber(e.target.value)}
          placeholder="e.g., AA123"
          disabled={isLoading}
        />
        {errors.flightNumber && <div style={{ color: "red" }}>{errors.flightNumber}</div>}
      </div>

      <div style={{ marginBottom: "10px" }}>
        <label htmlFor="date">Date:</label>
        <input
          id="date"
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          disabled={isLoading}
        />
        {errors.date && <div style={{ color: "red" }}>{errors.date}</div>}
      </div>

      <button type="submit" disabled={isLoading}>
        {isLoading ? "Loading..." : "Search"}
      </button>
    </form>
  );
};
```

**Copilot Prompts:**
- "Generate a SearchForm React component with flight number input and date picker. Include form validation, error messages, and disabled state during loading."

#### 6.5 Create ResultCard Component
Create `flight-status-ui/src/components/ResultCard.tsx`:

```typescript
import React from "react";
import { FlightStatusResult, UnifiedFlightStatus } from "../types/FlightStatus";

interface ResultCardProps {
  result: FlightStatusResult;
}

const getStatusColor = (status: UnifiedFlightStatus): string => {
  switch (status) {
    case UnifiedFlightStatus.OnTime:
      return "green";
    case UnifiedFlightStatus.Delayed:
      return "orange";
    case UnifiedFlightStatus.Cancelled:
    case UnifiedFlightStatus.Diverted:
      return "red";
    case UnifiedFlightStatus.Unknown:
    default:
      return "grey";
  }
};

export const ResultCard: React.FC<ResultCardProps> = ({ result }) => {
  return (
    <div style={{ border: "1px solid #ccc", padding: "20px", borderRadius: "8px", marginTop: "20px" }}>
      <div style={{ marginBottom: "10px" }}>
        <strong>Flight:</strong> {result.flightNumber} ({result.date})
      </div>

      <div
        style={{
          marginBottom: "10px",
          padding: "10px",
          backgroundColor: getStatusColor(result.status),
          color: "white",
          borderRadius: "4px"
        }}
      >
        <strong>Status:</strong> {result.status}
      </div>

      {result.statusReason && (
        <div style={{ marginBottom: "10px" }}>
          <strong>Details:</strong> {result.statusReason}
        </div>
      )}

      {result.scheduledDepartureUtc && (
        <div style={{ marginBottom: "10px" }}>
          <strong>Scheduled Departure:</strong> {new Date(result.scheduledDepartureUtc).toLocaleString()}
        </div>
      )}

      {result.actualDepartureUtc && (
        <div style={{ marginBottom: "10px" }}>
          <strong>Actual Departure:</strong> {new Date(result.actualDepartureUtc).toLocaleString()}
        </div>
      )}

      {result.terminal && (
        <div style={{ marginBottom: "10px" }}>
          <strong>Terminal:</strong> {result.terminal}
        </div>
      )}

      {result.gate && (
        <div style={{ marginBottom: "10px" }}>
          <strong>Gate:</strong> {result.gate}
        </div>
      )}

      <div style={{ marginBottom: "10px", fontSize: "12px", color: "grey" }}>
        <strong>Provider:</strong> {result.provider} (updated {new Date(result.lastUpdatedUtc).toLocaleString()})
      </div>
    </div>
  );
};
```

**Copilot Prompts:**
- "Generate a ResultCard React component that displays FlightStatusResult with: status badge (green=OnTime, orange=Delayed, red=Cancelled/Diverted, grey=Unknown), times, terminal, gate (optional), provider, lastUpdatedUtc. Show AeroTrack fields only when present."

#### 6.6 Create App.tsx
Create/update `flight-status-ui/src/App.tsx`:

```typescript
import React, { useState } from "react";
import { SearchForm } from "./components/SearchForm";
import { ResultCard } from "./components/ResultCard";
import { getFlightStatus } from "./services/flightStatusApi";
import { FlightStatusResult, ApiError } from "./types/FlightStatus";

const App: React.FC = () => {
  const [result, setResult] = useState<FlightStatusResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSearch = async (flightNumber: string, date: string) => {
    setIsLoading(true);
    setError(null);
    setResult(null);

    try {
      const data = await getFlightStatus(flightNumber, date);
      setResult(data);
    } catch (err: any) {
      setError(err.response?.data?.message || "Failed to fetch flight status. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: "600px", margin: "0 auto", padding: "20px" }}>
      <h1>Flight Status Checker</h1>
      <SearchForm onSearch={handleSearch} isLoading={isLoading} />

      {error && (
        <div style={{ color: "red", padding: "10px", backgroundColor: "#ffebee", borderRadius: "4px" }}>
          {error}
        </div>
      )}

      {result && <ResultCard result={result} />}
    </div>
  );
};

export default App;
```

**Copilot Prompts:**
- "Generate App.tsx that: imports SearchForm and ResultCard, manages state for result/error/loading, handles search submission, calls getFlightStatus API, displays error or result."

#### 6.7 Configure Environment Variables
Create `flight-status-ui/.env`:
```
REACT_APP_API_URL=http://localhost:5000
```

#### 6.8 Setup CORS (Backend)
Update `Program.cs` to enable CORS:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ... later in app config
app.UseCors("AllowFrontend");
```

**Testing:**
- [ ] `npm start` in `flight-status-ui/` starts React dev server on localhost:3000
- [ ] Search form displays; can enter flight number and date
- [ ] Submit query sends request to backend
- [ ] Result card displays with status color and fields
- [ ] AeroTrack fields (terminal, gate) visible only when provided
- [ ] Error message displays if backend returns error

---

### Checkpoint: Phase 6 Complete

**Acceptance Criteria:**
- ✅ React app starts and runs on localhost:3000
- ✅ SearchForm component validates inputs and submits
- ✅ API client calls backend /flights/status endpoint
- ✅ ResultCard displays flight status with color coding
- ✅ AeroTrack-only fields hidden when not provided
- ✅ Error states handled gracefully
- ✅ CORS enabled on backend

**Commit Message:**
```
feat: phase6 - React frontend with search and result display
- Create TypeScript types for FlightStatusResult and ApiError
- Generate API client service with axios
- Create SearchForm component with validation
- Create ResultCard component with color coding and conditional fields
- Setup App.tsx to orchestrate search flow
- Enable CORS on backend for frontend communication
```

**Copilot Usage Note:**
Document in `prompts.md`:
- Prompt: "Generate React TypeScript components (SearchForm, ResultCard) and API client"
- Accepted: Component structure, API client, form validation, conditional rendering
- Rejected/Modified: Enhanced color mapping and accessibility
- Rationale: Standard React patterns; aligns with UI requirements in spec.md

---

## Phase 7: Documentation & Deployment

**Objective:** Create comprehensive documentation and prepare for deployment.

**Duration:** 1-2 hours

### Steps

#### 7.1 Create README.md
Create/update `README.md` at the root:

**Sections to include:**
- **Overview:** What is the Flight Status Checker?
- **Architecture:** Monorepo structure, backend (Minimal API), frontend (React + TypeScript)
- **Assumptions:** UTC times, sample flights, no external APIs
- **Quick Start:** How to run both backend and frontend
- **API Documentation:** Endpoint reference, sample requests/responses
- **Testing:** How to run unit tests
- **Deployment:** Instructions for containerization, hosting
- **Copilot Usage:** Summary of AI assistance usage

**Copilot Prompts:**
- "Generate a comprehensive README.md for a full-stack Flight Status Checker app. Include: project overview, architecture, quick start, API reference, testing, deployment, and a section on Copilot usage."

#### 7.2 Create Deployment Guide
Create `DEPLOYMENT.md`:

**Sections:**
- **Prerequisites:** .NET 6+, Node.js, Docker
- **Local Setup:**
  ```bash
  # Backend
  cd FlightStatus.Api
  dotnet run

  # Frontend (new terminal)
  cd flight-status-ui
  npm install
  npm start
  ```
- **Docker Deployment:**
  ```dockerfile
  # FlightStatus.Api/Dockerfile
  FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
  WORKDIR /app
  COPY . .
  RUN dotnet publish -c Release -o out

  FROM mcr.microsoft.com/dotnet/aspnet:6.0
  WORKDIR /app
  COPY --from=build /app/out .
  EXPOSE 5000
  ENTRYPOINT ["dotnet", "FlightStatus.Api.dll"]
  ```
- **Environment Variables:** API URL for frontend
- **Healthcheck:** /health endpoint

**Copilot Prompts:**
- "Generate a DEPLOYMENT.md file with: local setup instructions, Docker containerization for backend and frontend, environment variables, healthcheck, and deployment notes."

#### 7.3 Update prompts.md
Add entries for all Copilot prompts from Phases 1-7:

**Structure (from copilot-instructions.md):**
- Prompt N: [Title]
- Intent: [One sentence]
- Prompt Details: [What was asked]
- Outcome: [What was generated]
- Accepted: [Specific parts accepted]
- Rejected/Modified: [Changes made]
- Files Modified: [List]
- Rationale: [Why accepted/rejected]

**Copilot Prompts:**
- "Review all Copilot assistance during development and add entries to prompts.md for each phase."

#### 7.4 Create reflection.md
Create `reflection.md` (for post-implementation):

**Sections:**
- **What went well:** Aspects of design/implementation that worked smoothly
- **Challenges:** Issues encountered and how resolved
- **Copilot effectiveness:** How Copilot helped/hindered each phase
- **Trade-offs:** Design decisions and alternatives considered
- **Future improvements:** Enhancements for production (caching, retries, real APIs, etc.)
- **Evaluator feedback:** To be completed after receiving feedback

#### 7.5 Commit Final Code
```bash
git add .
git commit -m "feat: phase7 - documentation and deployment

- Create comprehensive README with architecture and quickstart
- Add DEPLOYMENT.md with local/Docker setup instructions
- Update prompts.md with all Copilot interactions
- Create reflection.md for post-implementation review
- Ready for submission and evaluation
"
```

#### 7.6 Final Integration Test
**End-to-end test checklist:**
- [ ] Backend starts on localhost:5000
- [ ] GET /health returns 200
- [ ] Frontend starts on localhost:3000
- [ ] Search for AA123 (2026-06-10) returns Delayed status
- [ ] Search for BA456 (2026-06-11) returns On-time status
- [ ] AeroTrack fields (terminal, gate) visible for AA123
- [ ] QuickFlight query shows no terminal/gate
- [ ] Provider selection works (latest lastUpdatedUtc selected)
- [ ] Error handling: invalid date returns 400
- [ ] Logs include requestId and provider details
- [ ] All unit tests pass

---

### Checkpoint: Phase 7 Complete

**Acceptance Criteria:**
- ✅ README.md completed with all sections
- ✅ DEPLOYMENT.md with local and Docker setup
- ✅ prompts.md updated with all Copilot prompts (Phases 1-7)
- ✅ reflection.md created
- ✅ All code committed to git
- ✅ End-to-end integration test passed
- ✅ Ready for submission and evaluator review

**Commit Message:**
```
feat: phase7 - documentation and deployment

- Create comprehensive README with architecture and quickstart
- Add DEPLOYMENT.md with local/Docker setup instructions
- Update prompts.md with all Copilot interactions
- Create reflection.md for post-implementation review
- Final integration test passed
- Ready for submission to evaluator
```

---

## Summary: 7-Phase Implementation Path

| Phase | Duration | Key Deliverable | Status |
|-------|----------|---|---|
| 1 | 1-2h | Models, DI, Program.cs skeleton | ✅ Pending |
| 2 | 1-2h | IFlightStatusProvider, AeroTrackStub, QuickFlightStub | ✅ Pending |
| 3 | 1-2h | StatusNormalizer (mapping, selection) | ✅ Pending |
| 4 | 1h | GET /flights/status endpoint, validation, error handling | ✅ Pending |
| 5 | 1-2h | Unit tests (18+ cases, >80% coverage) | ✅ Pending |
| 6 | 2-3h | React UI (SearchForm, ResultCard, API client) | ✅ Pending |
| 7 | 1-2h | README, DEPLOYMENT, prompts, reflection | ✅ Pending |
| **Total** | **~10-15 hours** | **Production-ready full-stack app** | ✅ Pending |

---

**Next:** Begin Phase 1 implementation. Follow commit messages and testing checkpoints closely.

**Questions?** Ask before proceeding with implementation.
