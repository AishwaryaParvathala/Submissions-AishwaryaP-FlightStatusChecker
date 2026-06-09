A low-stakes, full-stack trial run. You have one day to take a simple scenario from brief to running application — analysis, architecture,
design, code, tests, deployment steps, and documentation.
There is no right framework or folder layout. No starter code. Your choices are the submission.
The agentic evaluator will score your work across eight SDLC dimensions and return specific feedback. Use that feedback to fix gaps before
the timed challenge
Scenario
The SkyRoute platform needs a Flight Status lookup feature.
A support agent enters a flight number and a date. The system queries two flight data providers, normalises their responses into a single
status model, and displays the result.
Providers
AeroTrack
Returns full status detail: flight status, scheduled and actual departure/arrival times, terminal, gate, delay reason (when delayed).
Response is verbose and uses its own field naming.
QuickFlight
Returns minimal status: flight status and scheduled times only. No gate or terminal. No delay reason.
Faster but less detailed.
Both providers use different status vocabularies. Your normalisation layer must map both to a single unified status enum:
Unified status
OnTime
Delayed
Cancelled
Diverted
Unknown
Meaning
Departing/arrived within 15 minutes of schedule
Departure or arrival pushed beyond 15 minutes
Flight will not operate
Flight landed at a different airport
Provider returned no usable status
When both providers return a result, prefer the one with the later lastUpdatedUtc timestamp. When only one provider responds, use that
result. When neither responds, return Unknown with an appropriate message.
Copilot Usage Guidelines
You are expected to leverage GitHub Copilot for code generation, refactoring, and documentation throughout this challenge. Please annotate
your code or provide a summary indicating where Copilot was used and how it influenced your solution.

Submission Instructions
Submit your solution as a GitHub repository or a zip file containing all source code, documentation, and any supporting files.
Include a README file summarizing your approach, architectural decisions, and Copilot usage.
Follow any provided naming conventions and ensure your code is well-documented and professional.
Evaluation Criteria
Your submission will be evaluated on the following dimensions:
Code quality and organization
Correctness and completeness of the solution
Effective and ethical use of Copilot
Documentation and clarity of architectural decisions
Professionalism and clarity of submission
Each dimension will be scored as pass/partial/fail with written feedback.
Functional scope
Backend (.NET Minimal API)
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}
Calls both providers (use stubs — no real APIs)
Normalises responses
Returns unified FlightStatusResult
Provider abstraction: IFlightStatusProvider with two concrete stub implementations
Providers are injected via DI — the endpoint does not reference concrete types
Basic input validation: flight number and date are required; return 400 if missing
Frontend (Choose your own)
Search form: flight number input + date picker
Result card: unified status with colour coding (green = OnTime, amber = Delayed, red = Cancelled/Diverted, grey = Unknown)
AeroTrack-only fields (gate, terminal, delay reason) shown when present, hidden when absent
Basic error state when the API returns an error
What the evaluator looks for
The evaluator scores eight dimensions independently. Each dimension is pass/partial/fail with a written remark.
Dimension
Analyze
Architect
Design
Develop
Test
What it checks
Did you identify and state your assumptions before writing code?
Is the provider abstraction clean and dependency-injected?
Did you define your data model before implementing it?
Does the code build, run, and implement the spec correctly?
Are the normalisation rules and provider selection logic covered by unit tests?
Dimension
What it checks
Deploy
Operate
Can the application be started from a clean clone using your instructions?
Does the system handle provider failures gracefully and log meaningfully?
Document Are prompts captured, README present, and assumptions noted?
Submission structure
submissions/<your-name>/UseCase/ 
├── README.md                  # setup steps, how to run, assumptions 
├── spec.md                    # your data model and interface definitions (written first) 
├── FlightStatus.Api/          # .NET Minimal API project 
│   ├── ... 
├── FlightStatus.Tests/        # xUnit or NUnit test project 
│   ├── ... 
├── flight-status-ui/          # React app 
│   ├── ... 
├── prompts.md                 # all AI prompts used, with notes on what you accepted or rejected 
└── reflection.md              # what the evaluator flagged, what you fixed, what you'd do differently 
spec.md must be committed before any implementation files. The commit timestamp is checked by the evaluator.
Ground rules
Use AI assistance freely — this is expected and encouraged.
Capture every significant prompt in prompts.md.
Do not use real flight data APIs or live credentials.
Stub providers must be deterministic — hardcode a small set of test responses.
No real secrets in committed files