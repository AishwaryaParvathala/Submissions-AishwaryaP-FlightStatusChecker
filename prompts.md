# Prompts — Organized & Readable

This document captures AI prompts used across the FlightStatusChecker project and is reorganized into four sections for readability and implementation flow:

- Architecture & Analysis
- Backend Development
- Frontend Development
- Writing Unit Tests

Each entry lists Intent, Outcome, Files touched, rationale, and recommended next steps. The goal is to provide a single reference that documents the reasoning and decisions behind each change the AI suggested or applied. Sections are ordered to match a typical implementation flow: Architecture & Analysis → Backend → Frontend → Tests.

---

## 1. Architecture & Analysis

1.1 Requirements Analysis & Research Report
- Intent: Produce a clear, structured translation of the high-level requirements into actionable engineering artifacts. This includes a prioritized list of functional requirements, an explicit data model, provider contracts, test scenarios, and a list of assumptions and open questions.
- Detailed Outcome:
	- Functional requirements (high level):
		1. Query flight status for a flight number and date (UTC-localized date semantics).
		2. Aggregate results from multiple providers and return a single unified status.
		3. Apply deterministic business rules (15-minute delay threshold, Cancelled/Diverted overrides).
		4. Expose a stable, documented API contract to the frontend.
		5. Provide deterministic provider stubs for offline/local testing.
	- Data model (excerpt):
		- `FlightStatusResult`
			- `FlightNumber` (string)
			- `Date` (ISO date, yyyy-MM-dd)
			- `Status` (OnTime|Delayed|Cancelled|Diverted|Unknown)
			- `ScheduledDepartureUtc` (ISO8601 timestamp)
			- `EstimatedDepartureUtc` (ISO8601 timestamp | null)
			- `Terminal` (string | null)
			- `Gate` (string | null)
			- `StatusReason` (string | null)
			- `Provider` (internal only — omitted in public API)
	- Provider contract (`IFlightStatusProvider`) example signature:
		```csharp
		Task<ProviderResult> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken ct);
		```
	- Mapping rules summary:
		- Map provider-specific tokens to unified statuses.
		- If any provider reports `Cancelled` or `Diverted`, the final status is that value.
		- Delay rules: treat an arrival/departure as `Delayed` only when estimated - scheduled >= 15 minutes.
	- Assumptions and trade-offs:
		- All date inputs are interpreted as UTC calendar dates (user-visible date → UTC midnight). This avoids ambiguous timezone handling in the evaluator scope.
		- Frontend receives a sanitized model (no provider metadata) and displays provider-specific fields only when present.
	- Open questions (for the user):
		- Should the API accept time zone hints (future enhancement)?
		- Exact UI preferences for cancelled/diverted presentation and accessibility color contrasts.

- Files / Artifacts: `spec.md`, `IMPLEMENTATION_GUIDE.md`, `copilot-instructions.md`.
- Recommended Next Steps:
	1. Resolve the timezone UX question if users need local-time semantics.
	2. Approve sample flight numbers for provider stubs (AA123, BA456, UA789 recommended).

1.2 Prompt Capture & Automation
- Intent: Provide a reproducible format and tooling guidance so future AI interactions are recorded consistently (Intent, Outcome, Files, Rationale).
- Outcome: `copilot-instructions.md` created and `prompts.md` restructured to that schema.
- Rationale: Captured prompts serve as an audit trail linking code changes to the original instruction and decision rationale.

---

## 2. Backend Development

2.1 API Contract & Endpoint Shape
- Intent: Design a simple, deterministic, easy-to-test HTTP contract. Use path parameters for clarity and caching friendliness.
- API example (contract):

	- Request: GET /flights/status/{flightNumber}/{date}
		- `flightNumber`: alphanumeric flight code (case-insensitive)
		- `date`: `yyyy-MM-dd` (UTC calendar date)

	- Response: 200 OK, JSON body (example):
		```json
		{
			"flightNumber": "AA123",
			"date": "2026-06-09",
			"status": "Delayed",
			"scheduledDepartureUtc": "2026-06-09T10:00:00Z",
			"estimatedDepartureUtc": "2026-06-09T10:25:00Z",
			"terminal": "T1",
			"gate": "A12",
			"statusReason": "Late arrival of inbound aircraft"
		}
		```

- Files changed: `FlightStatusBackend/Program.cs`, `FlightStatusBackend/Models/FlightStatusResult.cs`.
- Rationale: Path params simplify client integration and make the endpoint cache-friendly.
- Recommended Next Steps:
	1. Add OpenAPI/Swagger description for the endpoint with example responses.
	2. Add schema validation tests that exercise missing and malformed path params.

2.2 Status Normalizer Implementation
- Intent: Centralize business rules so provider-specific tokens are mapped consistently to a small set of statuses.
- Implementation details:
	- Token maps (example):
		- AeroTrack: `{"ONTIME":"OnTime","DELAY":"Delayed","CXL":"Cancelled","DIV":"Diverted"}`
		- QuickFlight: `{"OK":"OnTime","LATE":"Delayed","CANCELLED":"Cancelled"}`
	- Algorithm:
		1. Collect provider responses.
		2. If any response token maps to `Cancelled` or `Diverted`, return that status.
		3. Otherwise, if any response meets the 15-minute delay rule → `Delayed`.
		4. Otherwise `OnTime` or `Unknown` if no valid data.

- Files: `FlightStatusBackend/Services/StatusNormalizer.cs`.
- Recommended Next Steps:
	- Add unit tests that exercise the tie-breaker logic and timestamp edge cases (exactly 15 minutes, negative offsets).

2.3 Provider Stubs and DI Registration
- Intent: Provide deterministic responses for local testing and CI without external dependencies.
- Implementation details:
	- `AeroTrackStub` returns tokenized payloads for a fixed set of flightNumbers and dates.
	- `QuickFlightStub` implements the same interface but with slightly different tokens and timestamps.
	- Register both with DI (AddSingleton<IFlightStatusProvider, AeroTrackStub>(), AddSingleton<IFlightStatusProvider, QuickFlightStub>()).

- Files: `FlightStatusBackend/Providers/AeroTrackStub.cs`, `FlightStatusBackend/Providers/QuickFlightStub.cs`, `FlightStatusBackend/Providers/IFlightStatusProvider.cs`, `FlightStatusBackend/Program.cs` (DI registration).
- Recommended Next Steps:
	- Add environment flag to enable/disable external providers vs stubs.
	- Document stub payloads in `spec.md` so tests can assert exact values.

2.4 CORS and Local Dev Proxy
- Intent: Make local development frictionless when frontend runs on Vite dev server.
- Outcome: Add minimal CORS policy to backend that allows `http://localhost:5173` (Vite default) in development only, and configure `vite.config.js` proxy to route `/api` to backend port.
- Files: `FlightStatusBackend/Program.cs`, `flight-status-ui/vite.config.js`.
- Security note: Restrict CORS to development only and do not allow wide-open policies in production.

2.5 Diagnostics, Runtime Fixes, and Build Guidance
- Intent: Provide reproducible steps for resolving CoreCLR or runtime configuration issues.
- Steps recommended:
	1. Delete `bin/` and `obj/` from projects.
	2. Run `dotnet restore` and `dotnet build`.
	3. If IIS/ANCM errors occur, enable stdout log capturing or run `dotnet run` directly.
- Rationale: Many startup errors are caused by mismatched runtimes or stale artifacts.

---

## 3. Frontend Development

3.1 Scaffold: Vite + React + TypeScript
- Intent: Provide a small, fast development environment with type-safety.
- Outcome: Vite project scaffolded with `main.tsx`, `index.html`, `package.json`, and a lightweight CSS file. Setup includes React Testing Library for unit tests.
- Files: `FlightStatusFrontend/package.json`, `FlightStatusFrontend/index.html`, `FlightStatusFrontend/src/main.tsx`, `FlightStatusFrontend/src/index.css`.

3.2 `SearchForm` Component (UX + Validation)
- Intent: Capture `flightNumber` (string) and `date` (date picker) with validation and normalization (uppercase flight number, date → yyyy-MM-dd UTC).
- Props and behavior:
	- Props: `onSearch({flightNumber, date})`
	- Validation: flight number non-empty and matches expected pattern; date not in the past beyond a configured threshold (optional).
	- Normalization: Uppercase flight number and format date as `yyyy-MM-dd`.
- Files: `FlightStatusFrontend/src/components/SearchForm.tsx`.
- Testing notes: Mock user typing, date selection, and assert `onSearch` received normalized values.

3.3 `ResultCard` Component (Display & Accessibility)
- Intent: Present the unified `FlightStatusResult` with clear status badges, accessible labels, and conditional fields for provider-specific data (`Terminal`, `Gate`, `StatusReason`).
- Rendering rules:
	- Only show `Terminal`/`Gate` when non-empty.
	- Status badge color mapping: `OnTime` → green, `Delayed` → amber, `Cancelled`/`Diverted` → red, `Unknown` → grey.
	- Provide `aria-live` region for status updates to support screen readers.
- Files: `FlightStatusFrontend/src/components/ResultCard.tsx`.

3.4 `App` Integration
- Intent: Wire `SearchForm` to call backend, normalize API results to the UI model, and render `ResultCard`.
- Behavior:
	- On search, call `GET /flights/status/{flightNumber}/{date}`.
	- Convert camelCase server response to PascalCase UI model.
	- Handle errors gracefully: show user-friendly message and allow retry.
- Files: `FlightStatusFrontend/src/App.tsx`, `FlightStatusFrontend/src/services/flightStatusApi.ts`.

3.5 Styling, MUI and Icons
- Intent: Improve UX using MUI components for inputs where helpful; keep styling simple for evaluator clarity.
- Files: `FlightStatusFrontend/src/index.css`, `FlightStatusFrontend/package.json` (MUI deps).

3.6 Test Compatibility and Tooling Notes
- Intent: Make the frontend robust under both Vite runtime and Jest test environments.
- Actions taken:
	- Avoid direct use of `import.meta.env` in code paths exercised by Jest; fallback to `process.env` or `globalThis` in tests.
	- Add `src/styles.d.ts` to declare CSS modules to TypeScript.
	- Configure `jest.config.ts` with `ts-jest` transform and proper `testEnvironment`.

---

## 4. Writing Unit Test Cases (exhaustive list)

4.1 Backend unit-test cases (recommended)
- `StatusNormalizerTests`:
	- Case: Two providers both OnTime → final OnTime.
	- Case: One reports Delayed >=15m, other OnTime → final Delayed.
	- Case: Any provider Cancelled → final Cancelled.
	- Edge cases: exactly 15 minutes difference, null estimated times.
- `FlightStatusHandlerTests`:
	- Case: Both providers throw → API returns `Unknown` with 200 OK.
	- Case: Mixed valid + provider error → normalized valid result returned.
- `ProviderStubTests`:
	- Validate deterministic stub payloads for the chosen sample flights.

4.2 Frontend unit-test cases (Jest + RTL)
- `ResultCard.test.tsx`:
	- Renders status badge color for each status.
	- Hides Terminal/Gate when absent; shows when present.
	- Exposes `aria-live` updates when status changes.
- `SearchForm.test.tsx`:
	- Validates required fields and correct normalization (uppercasing flight number, date formatting).
	- Submits correct payload to `onSearch` handler.
- `App.test.tsx`:
	- Mocks `fetch`/API and validates UI updates for success and failure responses.
	- Ensures error UI shows on network failure and retry works.

4.3 Commands to run tests (local guidance)
```
dotnet test FlightStatus.Tests
cd FlightStatusFrontend
npm install
npm test
```

---

Notes, Rationale, and Next Steps
- This expanded document captures both the high-level design decisions and the low-level, actionable implementation items. It is intentionally verbose so engineers joining the project can quickly understand the why and the how.
- If you want, I can:
	1. Expand this into a per-file changelog showing diffs for every touched file.
	2. Generate OpenAPI `openapi.yaml` (minimal) for the backend endpoint.
	3. Create sample Postman/HTTPie requests for quick manual verification.

---

Updated: expanded all sections with more detailed intents, outcomes, examples, and test guidance.


---

## 1. Architecture & Analysis

1.1 Requirements Analysis & Research Report
- Intent: Extract requirements from `FlightStatusCheckerRequirement.md` and produce a structured research report (functional requirements, data model, provider interface, stubs, mapping rules, unit-test plan, gaps).
- Outcome: Comprehensive report with unified `FlightStatusResult` model, `IFlightStatusProvider` interface, deterministic stubs (AeroTrack, QuickFlight), mapping rules (15-minute delay rule), and 7 unit-test descriptions.
- Files / Artifacts: `spec.md`, `IMPLEMENTATION_GUIDE.md`.

1.2 Prompt Capture & Automation
- Intent: Standardize AI prompt capture format and automation guidance.
- Outcome: `copilot-instructions.md` added; this `prompts.md` organizes captured prompts.

---

## 2. Backend Development (recommended order)

2.1 API Contract & Endpoint Shape
- Intent: Define API contract: `GET /flights/status/{flightNumber}/{date}` (date = `yyyy-MM-dd` UTC), return `status` as `OnTime|Delayed|Cancelled|Diverted|Unknown`.
- Outcome: Endpoint and model updated.
- Files: `FlightStatusBackend/Program.cs`, `FlightStatusBackend/Models/FlightStatusResult.cs`.

2.2 Status Normalizer
- Intent: Normalize provider tokens, apply 15-minute delay rule, and ensure Cancelled/Diverted override OnTime.
- Outcome: `StatusNormalizer` implemented with mapping rules.
- Files: `FlightStatusBackend/Services/StatusNormalizer.cs`.

2.3 Provider Stubs & DI
- Intent: Add deterministic stubs and register via DI so the handler iterates `IEnumerable<IFlightStatusProvider>`.
- Outcome: `AeroTrackStub` and `QuickFlightStub` implemented and registered.
- Files: `FlightStatusBackend/Providers/AeroTrackStub.cs`, `FlightStatusBackend/Providers/QuickFlightStub.cs`, `FlightStatusBackend/Providers/IFlightStatusProvider.cs`.

2.4 CORS / Local Dev Proxy
- Intent: Allow Vite frontend to proxy API calls during development and avoid CORS.
- Outcome: CORS policy added and Vite proxy suggested.
- Files: `FlightStatusBackend/Program.cs`, `flight-status-ui/vite.config.js`.

2.5 Diagnostics & Runtime Fixes
- Intent: Troubleshoot CoreCLR/ASP.NET runtime startup errors and provide clean-rebuild steps.
- Outcome: Recommended `dotnet clean` / remove `bin`/`obj` / `dotnet restore` / `dotnet build` and retargeting guidance to `net8.0` where necessary.

---

## 3. Frontend Development

3.1 Scaffold Vite + React + TypeScript
- Intent: Create a Vite React TypeScript app for the evaluator UI.
- Outcome: App scaffolded with `main.tsx`, `index.html`, and `package.json`.
- Files: `FlightStatusFrontend/src/main.tsx`, `FlightStatusFrontend/index.html`, `FlightStatusFrontend/package.json`.

3.2 SearchForm (validation → MUI DatePicker)
- Intent: Implement flight number + date form with validation; upgrade to MUI `DatePicker`.
- Outcome: `SearchForm.tsx` added and later updated to MUI.
- Files: `FlightStatusFrontend/src/components/SearchForm.tsx`.

3.3 ResultCard & App Integration
- Intent: Build `ResultCard` to display unified status and `App` to call backend + normalize responses.
- Outcome: Implemented with camelCase → PascalCase mapping.
- Files: `FlightStatusFrontend/src/components/ResultCard.tsx`, `FlightStatusFrontend/src/App.tsx`.

3.4 Styling & MUI
- Intent: Add enterprise styling and MUI icons (e.g., `FlightTakeoff`).
- Outcome: CSS updates and `package.json` MUI deps.

3.5 Test Compatibility Fixes
- Intent: Make frontend testable under Jest + ts-jest (test env differs from Vite).
- Outcome: Guarded `import.meta.env` usage, added `src/styles.d.ts`, updated `jest.config.ts` and `tsconfig.json` for tests.

---

## 4. Writing Unit Tests

4.1 Backend Unit Tests
- Intent: Add tests for validator, normalizer, handler, and provider stubs.
- Outcome: Tests added/updated in `FlightStatus.Tests` and included in solution.
- Files: `FlightStatus.Tests/*` (see tests folder).

4.2 Frontend Unit Tests (Jest)
- Intent: Add tests for `ResultCard`, `SearchForm`, and `App` (React Testing Library + Jest).
- Outcome: Tests added; fixes applied for test stability (`act(...)`, query selectors improvements).
- Files: `FlightStatusFrontend/src/components/ResultCard.test.tsx`, `.../SearchForm.test.tsx`, `.../App.test.tsx`, `flight-status-ui/jest.config.ts`.

4.3 Test Run & Fixes
- Intent: Run test suites and apply minimal, targeted fixes to keep runtime behavior unchanged.
- Outcome: Adjusted tests and configs to pass under CI/local environments.

---

How to use this file
- Follow the section order when implementing features: Architecture → Backend → Frontend → Tests.
- If you want a compact per-file changelog, say so and I will generate it.

Updated: reorganized prompts into four clear sections for readability and traceability.

