# Development notes

For people who want to read or change the code. To run the system, see the
[README](../README.md).

## Projects

Dependencies point inward only:

```
OxBox.Web ─► OxBox.Infrastructure ─► OxBox.Application ─► OxBox.Domain ─► OxBox.OxPointProtocol
```

| Project | Responsibility |
|---|---|
| `src/OxBox.OxPointProtocol` | Messages and commands exchanged with the box's control unit (the BU), in both directions. Pure, no I/O. |
| `src/OxBox.Domain` | Entities and their rules: cylinders, boxes, positions, cost centres, alerts. |
| `src/OxBox.Application` | Use cases (exchange, service visit, stock alerts), the per-box `BoxAgent`, and the interfaces they need. |
| `src/OxBox.Infrastructure` | EF Core + PostgreSQL, the WebSocket connection to the BU, alert outbox and e-mail, report queries and CSV exports. |
| `src/OxBox.Web` | ASP.NET Core host: the box's status display (kiosk), the admin web app with roles, and the composition root. |
| `tools/OxBox.BuSimulator` | A simulated BU with a control page, for demos and manual testing. Not deployed. |

Each box has one `BoxAgent`. Every event from the box (a scan, a connection change) goes through
one queue and is handled one at a time, so two scans can never race each other.

## Prerequisites

- .NET SDK 9.0.300 or newer (`global.json` rolls forward to newer SDKs).
- Docker Desktop, for the demo database and the database integration tests.

## Tests

```powershell
dotnet test                                    # everything; needs Docker running
dotnet test --filter "Category!=Integration"   # no Docker needed
```

The integration tests (`Category=Integration`) start their own PostgreSQL container through
Testcontainers, so they do not touch the demo database.

## Coding standards

These are enforced at build time. Warnings are errors.

- **Build settings:** `Directory.Build.props` holds nullable, analyzers (`latest-recommended`) and
  code-style enforcement.
- **Package versions:** pinned centrally in `Directory.Packages.props`. Project files never carry
  versions.
- **Formatting:** `.editorconfig` sets formatting and naming. Check with
  `dotnet format --verify-no-changes`. `.gitattributes` keeps line endings identical on every OS.
- **Layering:** Domain and Application never reference infrastructure packages. Hardware, database
  and clock access go through interfaces. Use `TimeProvider` rather than `DateTime.Now`.
- **One type per file.** The exception is a small closed hierarchy that is read as one unit.
- **One place per rule:** protocol strings live in `WireNames`; timeouts come from validated
  options classes, each holding its own defaults, and `appsettings*.json` holds only values that
  differ.
- **Errors:** expected business outcomes are return values (`ExchangeDecision`, `OpenOutcome`).
  Exceptions are for broken invariants and real failures.
- **Logging:** use `[LoggerMessage]` source-generated methods, with structured properties such as
  BoxId and position.
- **Tests:** name tests `Method_Scenario_ExpectedResult` and arrange them as arrange / act / assert.
- **Comments:** explain *why* (protocol quirks, hardware limits), not *what*.
