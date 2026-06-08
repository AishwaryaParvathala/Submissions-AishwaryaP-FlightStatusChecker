# Flight Status Checker - Specification

**Version:** 1.0  
**Date:** 2026-06-09  
**Status:** Design Phase (Pre-Implementation)

---

## Table of Contents

1. [Overview](#overview)
2. [Unified Data Model](#unified-data-model)
3. [Provider Interface & Abstraction](#provider-interface--abstraction)
4. [Normalization Rules](#normalization-rules)
5. [API Contract](#api-contract)
6. [Error Handling](#error-handling)
7. [Architecture & Dependencies](#architecture--dependencies)
8. [Assumptions & Constraints](#assumptions--constraints)

---

## Overview

The **Flight Status Checker** is a full-stack system that aggregates flight status information from two external providers (AeroTrack and QuickFlight), normalises their responses into a unified format, and presents the result to users.

### Key Features
- Query two independent flight data providers (stubs)
- Normalise provider-specific vocabularies into a single unified status enum
- Select the provider with the most recent `lastUpdatedUtc`
- Handle provider failures gracefully
- Display results via a React + TypeScript web UI
- Comprehensive unit test coverage for mapping and selection logic

### Design Principles
- **Provider Abstraction:** All providers implement `IFlightStatusProvider`; the endpoint references only the interface, not concrete types
- **Deterministic Stubs:** No real APIs; hardcoded, reproducible test data
- **Unified Normalization:** Single source of truth for status mapping and provider selection logic
- **Graceful Degradation:** If both providers fail, return `Unknown` with a descriptive reason
- **Comprehensive Logging:** All provider calls, selections, and errors are logged with correlation IDs and timestamps

---

## Unified Data Model

### Enums

#### `UnifiedFlightStatus`

```csharp
public enum UnifiedFlightStatus
{
    OnTime,      // Departure/arrival within 15 minutes of schedule
    Delayed,     // Departure/arrival > 15 minutes beyond schedule
    Cancelled,   // Flight will not operate
    Diverted,    // Flight landed at a different airport
    Unknown      // Provider returned no usable status or both failed
}
```

---

### Core Types

#### `FlightStatusResult` (API Response)

Primary response object returned by the GET endpoint. Always returned with HTTP 200.

```csharp
public class FlightStatusResult
{
    /// <summary>
    /// Flight number/code (e.g., "AA123")
    /// </summary>
    public string FlightNumber { get; set; }

    /// <summary>
    /// Date of flight in ISO format (yyyy-MM-dd), interpreted as UTC calendar date
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// Unified status (OnTime, Delayed, Cancelled, Diverted, Unknown)
    /// </summary>
    public UnifiedFlightStatus Status { get; set; }

    /// <summary>
    /// Human-friendly reason for the status (e.g., "Departure 25 minutes late (ATC congestion)")
    /// Null if no reason available or status is Unknown
    /// </summary>
    public string? StatusReason { get; set; }

    /// <summary>
    /// Scheduled departure time (ISO 8601 UTC, e.g., "2026-06-10T00:00:00Z")
    /// Null if not provided by provider
    /// </summary>
    public string? ScheduledDepartureUtc { get; set; }

    /// <summary>
    /// Actual departure time (ISO 8601 UTC)
    /// Null if flight has not departed or not provided by provider
    /// </summary>
    public string? ActualDepartureUtc { get; set; }

    /// <summary>
    /// Scheduled arrival time (ISO 8601 UTC)
    /// Null if not provided by provider
    /// </summary>
    public string? ScheduledArrivalUtc { get; set; }

    /// <summary>
    /// Actual arrival time (ISO 8601 UTC)
    /// Null if flight has not arrived or not provided by provider
    /// </summary>
    public string? ActualArrivalUtc { get; set; }

    /// <summary>
    /// Terminal (AeroTrack only)
    /// Null if not provided or provider is QuickFlight
    /// </summary>
    public string? Terminal { get; set; }

    /// <summary>
    /// Gate (AeroTrack only)
    /// Null if not provided or provider is QuickFlight
    /// </summary>
    public string? Gate { get; set; }
}
```

**JSON Example:**
```json
{
  "flightNumber": "AA123",
  "date": "2026-06-10",
  "status": "Delayed",
  "statusReason": "Departure 25 minutes late (ATC congestion)",
  "scheduledDepartureUtc": "2026-06-10T00:00:00Z",
  "actualDepartureUtc": "2026-06-10T00:25:00Z",
  "scheduledArrivalUtc": "2026-06-10T03:00:00Z",
  "actualArrivalUtc": null,
  "terminal": "2",
  "gate": "A12"
}
```

---

#### `ProviderResult` (Internal)

Wraps the response from a single provider. Used internally by the normalizer; not exposed in the API.

```csharp
public class ProviderResult
{
    /// <summary>
    /// True if provider returned data; false if error, timeout, or no usable status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Name of the provider ("AeroTrack" or "QuickFlight")
    /// </summary>
    public string ProviderName { get; set; }

    /// <summary>
    /// Last updated timestamp from provider (UTC)
    /// Null if IsSuccess == false or provider did not include timestamp
    /// Used for tie-breaking between providers
    /// </summary>
    public DateTime? LastUpdatedUtc { get; set; }

    /// <summary>
    /// Raw JSON response from provider (for logging and debugging)
    /// Null if error
    /// </summary>
    public object? RawPayload { get; set; }

    /// <summary>
    /// Error message if IsSuccess == false
    /// Null otherwise
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Parsed/normalized flight data (only when IsSuccess == true)
    /// Null if error
    /// </summary>
    public FlightStatusResult? NormalizedResult { get; set; }
}
```

---

## Provider Interface & Abstraction

### `IFlightStatusProvider` Interface

All concrete providers implement this contract. The backend endpoint depends only on the interface, not on concrete implementations.

```csharp
public interface IFlightStatusProvider
{
    /// <summary>
    /// Gets the flight status from this provider.
    /// </summary>
    /// <param name="flightNumber">Flight number/code (e.g., "AA123")</param>
    /// <param name="dateUtc">Date of flight as DateTime (UTC, 00:00:00 time component)</param>
    /// <returns>
    /// ProviderResult with IsSuccess=true and normalized data if successful.
    /// ProviderResult with IsSuccess=false and ErrorMessage if provider fails/unavailable.
    /// Never throws an exception for expected provider errors (timeouts, invalid responses, etc.).
    /// </returns>
    Task<ProviderResult> GetStatusAsync(string flightNumber, DateTime dateUtc);
}
```

### Implementation Notes

- **Error Handling:** Providers must **never throw** for expected errors (network timeouts, invalid status codes, unparseable responses). Instead, return `ProviderResult { IsSuccess = false, ErrorMessage = "..." }`.
- **Unexpected Exceptions:** If an unexpected exception occurs, catch it at the provider level and return `IsSuccess = false`.
- **Normalization:** Each provider is responsible for parsing its own response format and returning `NormalizedResult` when successful.
- **Logging:** Providers should log their call (start, success/failure, time taken, lastUpdatedUtc).

---

## Normalization Rules

### 1. Status Determination Logic

#### Rule 1a: Time-Based Determination (Primary)

If the provider supplies both **scheduled** and **actual** times:

1. Calculate delta = `ActualDeparture - ScheduledDeparture` (in seconds)
   - If departure times unavailable, use arrival times: delta = `ActualArrival - ScheduledArrival`
   - If both unavailable, fall through to Rule 1b

2. Apply threshold:
   - If delta ≤ 900 seconds (15 minutes): **OnTime**
   - If delta > 900 seconds: **Delayed**
   - Note: Negative delta (early departure/arrival) counts as OnTime

#### Rule 1b: Token-Based Determination (Fallback)

If times are unavailable or unparseable, use the provider's status token:

**AeroTrack Token Mapping:**
| Token | Maps To | Notes |
|-------|---------|-------|
| `SCHEDULED`, `ON_TIME`, `BOARDING` | OnTime | Pre-departure, on schedule |
| `DEPARTED` | OnTime | Departed on time; test Rule 1a if actual times available |
| `DELAYED` | Delayed | Explicit delay indication |
| `LANDED`, `ARRIVED` | OnTime | Arrival on time (unless Rule 1a shows >15m delay) |
| `CANCELLED` | Cancelled | Flight will not operate |
| `DIVERTED` | Diverted | Flight landed at different airport |
| Unknown token | Unknown | Log unknown token; default to Unknown |

**QuickFlight Token Mapping:**
| Token | Maps To | Notes |
|-------|---------|-------|
| `OK` | OnTime | Nominal status |
| `LATE` | Delayed | Delay indicated (no actual times provided) |
| `CXL` | Cancelled | Cancelled |
| `DIVERTED` | Diverted | Diverted |
| `UNKNOWN` | Unknown | Provider unable to determine |
| Unknown token | Unknown | Log unknown token; default to Unknown |

#### Rule 1c: Conflict Resolution

If provider returns conflicting information (e.g., status token "DELAYED" but Rule 1a shows within 15 minutes):
- **Priority:** Rule 1a (computed time delta) > Rule 1b (token mapping)
- **Rationale:** Actual times are more reliable than tokens

##### Exception - Cancellation and Diversion

While Rule 1a generally takes precedence, there is an important exception: provider tokens that indicate an operational state change such as `CANCELLED` or `DIVERTED` MUST override an OnTime determination derived from the 15-minute time delta. In practice:

- If Rule 1a computes `OnTime` (delta ≤ 15 minutes) but the provider token is `CANCELLED`, the normalized status MUST be `Cancelled`.
- If Rule 1a computes `OnTime` but the provider token is `DIVERTED`, the normalized status MUST be `Diverted`.
- For other conflicting tokens (for example token `DELAYED` while Rule 1a shows OnTime), prefer the time-based result (OnTime) unless additional provider metadata clearly indicates otherwise.

When a cancellation or diversion token overrides the computed time-based result, include any provided actual/scheduled times in the response (they may be useful for diagnostics) and set `StatusReason` to indicate the provider token (for example: "Diverted per provider token; actual arrival recorded at alternate airport"). All such conflicts should be logged (token, times, provider, lastUpdatedUtc) for auditability.

**Rationale:** Cancellation and diversion represent qualitative operational changes that are not appropriately captured by a ±15-minute threshold; treating them as higher priority avoids misleading "OnTime" results when the flight will not operate or landed elsewhere.

### 2. Provider Selection (When Both Respond)

When both `AeroTrack` and `QuickFlight` return `IsSuccess = true`:

1. Compare `lastUpdatedUtc` timestamps
2. **Select the provider with the later timestamp** (most recent data)
3. **Tie-breaker:** If timestamps are equal, prefer `AeroTrack` (richer fields)
4. Use the selected provider's `FlightStatusResult` for the API response

### 3. Provider Selection (When One Fails)

- If only one provider returns `IsSuccess = true`, use that provider
- If one provider fails and the other succeeds, log the failure but return the successful result

### 4. Both Providers Fail

- Return `FlightStatusResult` with:
  - `Status = Unknown`
  - `StatusReason = "Both providers unavailable. Please try again later."`
  - All time fields = `null`
  - All optional fields = `null`
  - `Provider = null`
  - `LastUpdatedUtc = null`
- HTTP status: **200** (not 5xx)

---

## API Contract

### Endpoint

```
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}
```

### Query Parameters

| Parameter | Type | Required | Format | Example | Notes |
|-----------|------|----------|--------|---------|-------|
| `flightNumber` | string | Yes | alphanumeric (2-6 chars) | `AA123`, `BA456` | Case-insensitive (normalize to uppercase internally) |
| `date` | string | Yes | ISO date `yyyy-MM-dd` | `2026-06-10` | Interpreted as UTC calendar date; must be valid date |

### Response (HTTP 200)

**Content-Type:** `application/json`

**Body:** `FlightStatusResult` object (see [Unified Data Model](#unified-data-model))

### Error Responses

#### HTTP 400 Bad Request

Returned when input validation fails.

```json
{
  "error": "Bad Request",
  "message": "flightNumber is required and must be 2-6 characters.",
  "timestamp": "2026-06-09T12:00:00Z"
}
```

**Scenarios:**
- `flightNumber` is missing or empty
- `flightNumber` is longer than 6 characters or contains invalid characters
- `date` is missing or invalid (not `yyyy-MM-dd` or not a valid date)

#### HTTP 500 Internal Server Error

Returned for unexpected server errors (e.g., application crash, unhandled exception in normalizer).

```json
{
  "error": "Internal Server Error",
  "message": "An unexpected error occurred. Please contact support.",
  "requestId": "abc-123-def-456"
}
```

---

## Error Handling

### Provider Call Error Handling

```
Provider Call
    ↓
[Successful Response]
    → Parse response
    → Map to ProviderResult (IsSuccess = true, NormalizedResult = ...)
    
[Provider Error: HTTP 4xx/5xx, Timeout, Invalid JSON]
    → Catch exception
    → Return ProviderResult (IsSuccess = false, ErrorMessage = "...")
    
[Unexpected Exception]
    → Log exception with stack trace
    → Return ProviderResult (IsSuccess = false, ErrorMessage = "Provider unavailable")
```

### Normalizer Error Handling

```
Normalizer.Normalize(aeroTrackResult, quickFlightResult)
    ↓
[Both IsSuccess = true]
    → Select provider by lastUpdatedUtc
    → Return FlightStatusResult from selected provider
    
[Exactly one IsSuccess = true]
    → Use successful provider
    → Return FlightStatusResult
    
[Both IsSuccess = false]
    → Return FlightStatusResult (Status = Unknown, StatusReason = "Both providers unavailable")
```

### Logging Strategy

All operations logged with:
- **RequestId:** Correlation ID for tracking end-to-end flow
- **Timestamp:** ISO 8601 UTC
- **Level:** Info, Warn, Error
- **Fields:** flightNumber, date, providerName, lastUpdatedUtc, status, errorMessage
- **No Secrets:** Never log API keys, auth tokens, or sensitive data

---

## Architecture & Dependencies

### Dependency Injection

The application uses .NET Dependency Injection. Configuration:

```csharp
// In Program.cs
builder.Services.AddScoped<IFlightStatusProvider, AeroTrackStub>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightStub>();
builder.Services.AddScoped<StatusNormalizer>();
// (Or use a factory/collection to manage multiple implementations)
```

**Note:** The endpoint receives `IEnumerable<IFlightStatusProvider>` (or individual instances) and calls both, then delegates to the normalizer.

### Key Services

| Service | Responsibility |
|---------|---|
| `AeroTrackStub` | Implements `IFlightStatusProvider`; returns hardcoded AeroTrack responses |
| `QuickFlightStub` | Implements `IFlightStatusProvider`; returns hardcoded QuickFlight responses |
| `StatusNormalizer` | Implements mapping rules, provider selection, and normalization |
| `ErrorHandlingMiddleware` | Catches unhandled exceptions; returns HTTP 500 with requestId |

### Folder Structure

```
FlightStatus.Api/
├── Program.cs                          # Startup, DI configuration, endpoint definition
├── FlightStatus.Api.csproj
├── Models/
│   ├── FlightStatusResult.cs           # Main response model
│   ├── ProviderResult.cs               # Internal provider wrapper
│   ├── UnifiedFlightStatus.cs          # Status enum
├── Providers/
│   ├── IFlightStatusProvider.cs        # Provider interface
│   ├── AeroTrackStub.cs                # AeroTrack implementation
│   ├── QuickFlightStub.cs              # QuickFlight implementation
├── Services/
│   ├── StatusNormalizer.cs             # Core normalization logic
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs      # Exception handling

FlightStatus.Tests/
├── FlightStatus.Tests.csproj
├── NormalizerTests.cs                  # Unit tests for mapping and selection
├── ProviderStubTests.cs                # Tests for provider implementations
```

---

## Assumptions & Constraints

### Assumptions

1. **Time Zone Handling:** All timestamps and input dates are in **UTC**. Input `date` parameter (yyyy-MM-dd) is interpreted as a UTC calendar date (00:00:00 UTC start of day).

2. **Date-only Flight Identification:** Flights are uniquely identified by `flightNumber + date`. No timezone-dependent offset corrections are applied.

3. **Provider Response Determinism:** Provider stubs return hardcoded, deterministic responses. No randomness or state changes.

4. **Status Token Vocabularies:** AeroTrack and QuickFlight use the specific status tokens listed in [Normalization Rules](#normalization-rules). Unknown tokens default to `Unknown`.

5. **Provider Availability:** Provider calls are fire-and-forget; no retries are implemented. Real providers (if integrated later) would require retry/backoff logic.

6. **Tie-Breaking:** When both providers have identical `lastUpdatedUtc`, **AeroTrack is preferred** because it provides richer fields (gate, terminal, delay reason).

7. **Field Nullability:** AeroTrack-specific fields (terminal, gate) are always `null` for QuickFlight responses. UI must handle null gracefully.

8. **No Real Secrets:** All API credentials and endpoints are hardcoded in stubs; no real external APIs are called.

### Constraints

1. **No External APIs:** Providers are stubs; no real AeroTrack or QuickFlight APIs are called.

2. **Single-Region Deployment:** No multi-region failover or redundancy mechanisms implemented.

3. **No Caching:** Each request queries both providers directly; no caching layer.

4. **No Retry Logic:** Provider calls do not retry; a single failure is final.

5. **Synchronous Provider Calls:** Both providers are queried sequentially (or in parallel with `Task.WhenAll`), blocking the endpoint until both complete or timeout.

6. **No Rate Limiting:** No rate limiting or throttling on the endpoint.

7. **Basic Logging:** Structured logs are written to console; no external log aggregation (e.g., Application Insights).

---

## Next Steps

1. **Implement Phase 1:** Create backend .NET project, models, and provider interface.
2. **Implement Phase 2:** Implement `AeroTrackStub` and `QuickFlightStub` with sample data.
3. **Implement Phase 3:** Implement `StatusNormalizer` with mapping rules and selection logic.
4. **Implement Phase 4:** Expose GET endpoint in `Program.cs` with validation and DI.
5. **Implement Phase 5:** Write unit tests (`NormalizerTests`, `ProviderStubTests`).
6. **Implement Phase 6:** Build React frontend with search form and result card.
7. **Implement Phase 7:** Documentation, deployment scripts, and final testing.

---

**Document Status:** ✅ Ready for implementation
