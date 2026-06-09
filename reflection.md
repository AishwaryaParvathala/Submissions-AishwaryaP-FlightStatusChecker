# Reflection

This file is provided for the examiner to record feedback, observations, and suggested fixes. It also documents the author's assumptions, known limitations, and how AI assistance (Copilot) was used.

## Reference
- Challenge brief: [FlightStatusCheckerRequirement.md](FlightStatusCheckerRequirement.md)

## High-level summary
- A .NET 8 minimal API backend (`FlightStatusBackend`) and a Vite + React frontend (`FlightStatusFrontend`) were implemented.
- Two stub providers (`AeroTrackStub`, `QuickFlightStub`) return deterministic test responses. A normalizer and business-rule layer map provider vocabularies to a unified status enum.

## Assumptions
- Date input is provided in `yyyy-MM-dd` (UTC calendar date) as required by the spec.
- Stubs are deterministic and contain a small set of test flight numbers (see `README.md` "Stub flight numbers").
- Frontend uses Vite; environment values may be provided via `import.meta.env` or `globalThis` for development/testing.

## Implementation
- API endpoint: `GET /flights/status/{flightNumber}/{date}` implemented in `FlightStatusBackend/Program.cs`.
- Provider abstraction: `IFlightStatusProvider` with two stubs in `FlightStatusBackend/Providers`.
- Orchestration: `FlightStatusHandler` selects provider result based on `LastUpdatedUtc` and applies normalization via `StatusNormalizer`.
- Input validation: `FlightStatusValidator` returns `400` for missing/invalid inputs.
- Frontend: `SearchForm` and `ResultCard` components with basic error handling and environment fallbacks.
- README includes run instructions, stub flight numbers, and an end-to-end flowchart.

## Tests
- Backend unit tests are present in `FlightStatus.Tests` covering normalisation and handler selection logic.
- Frontend contains component tests under `FlightStatusFrontend/src/components`.

## Copilot / AI usage
-- Copilot was used to accelerate routine code scaffolding, generate unit-test templates, and help format documentation. Significant prompts and iterations are recorded in `prompts.md`.
-- The author reviewed, adapted, and verified all Copilot suggestions; Copilot-assisted changes are noted in commit history where appropriate.

## Known limitations & future work
- Currently stubs are in-process and registered by DI; to simulate provider timeouts or partial failures more realistically, add configurable delays and error modes.
- Improve environment handling in frontend by standardizing on `import.meta.env` with a small test shim for Jest.
- Add an OpenAPI/Swagger contract and a small Postman collection (can be added on request).

## How to reproduce (quick)
1. Start backend:

```powershell
cd FlightStatusBackend
dotnet run --launch-profile https
```

2. Query a stub flight (example):

```bash
curl -k "https://localhost:5001/flights/status/OT100/2026-06-09"
```

3. Run backend tests:

```powershell
dotnet test FlightStatus.Tests
```

4. Run frontend (optional):

```powershell
cd FlightStatusFrontend
npm install
npm run dev
```

## Examiner notes (use this section to record findings)
- Feedback:

- Suggested fixes / follow-ups:

- Verified by (name / timestamp):

---

An optional `reflection.md` entry that pre-populates the "Feedback" section with common evaluation observations can be generated on request.
