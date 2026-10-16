# Zyara - زيـارة

![Status](https://img.shields.io/badge/status-in%20development-yellow)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Architecture](https://img.shields.io/badge/architecture-layered%20monolith-blue)
![DEPI](https://img.shields.io/badge/DEPI-graduation%20project-blue)

**A web platform for booking verified doctors and nurses for home visits.**

Patients browse verified providers, pick a service and an arrival window, and book a home visit.
Providers set their own prices and manage their requests. Payment is made in cash at the visit,
and the platform charges the provider a 15% commission on completed visits.

> **Status:** In development. This is the graduation project of a six-member team in the
> **Digital Egypt Pioneers Initiative (DEPI)**.
> The foundation (solution structure, database context, first domain rule and tests) is in place.
> Features are being built slice by slice (see the [Roadmap](#roadmap)).

---

## Table of Contents

- [Planned Features](#planned-features)
- [Business Rules](#business-rules)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Testing](#testing)
- [Team Workflow](#team-workflow)
- [Roadmap](#roadmap)
- [Team](#team)
- [License](#license)

---

## Planned Features

- **Patients:** search verified providers, book a visit in a chosen arrival window, cancel, and rate a completed visit.
- **Providers:** register and get verified, set prices per service, accept or reject requests, and run the visit.
- **Admin:** verify providers, handle reports, and review commission records.
- **Booking lifecycle:** every booking moves through a defined set of statuses with a full history trail.
- **Commission ledger:** a commission record is created when a visit is completed.

## Business Rules

| Rule | Value |
|---|---|
| Price | Set by the provider |
| Payment | Cash at the visit |
| Commission | 15% of the price, charged to the provider on `Completed` |
| Arrival windows | 2-hour windows, one booking per provider per window |
| Minimum lead time | 3 hours before the window |
| Booking horizon | Up to 14 days ahead |
| Provider response time | 2 hours, then the request expires |
| Free cancellation | Until 6 hours before the window |
| No-show | Marked 30 minutes after the window starts |

**Booking statuses:** `Pending`, `Confirmed`, `InProgress`, `Completed`, `Rejected`, `Expired`, `Cancelled`, `NoShow`.

## Architecture

Zyara is a **layered monolith with an independent Domain**, inspired by Clean Architecture.

- **Monolith:** one application, one database, deployed as a single unit.
- **Layered:** the code is split into three projects with a strict dependency direction.
- **Independent Domain:** the Domain project has no references to any other project or package.

```
Zyara.Web  ──►  Zyara.Services  ──►  Zyara.Domain
 (pages)         (use cases + DB)      (entities + rules)
```

| Project | Responsibility |
|---|---|
| `Zyara.Web` | Controllers, ViewModels, Views. Handles HTTP and presentation only. |
| `Zyara.Services` | Application operations (e.g. create a booking), EF Core `ZyaraDbContext`, background jobs. |
| `Zyara.Domain` | Entities, enums, and business rules (e.g. commission calculation, booking state machine). No dependencies. |

**The rule:** dependencies point downward only. `Domain` knows nothing about the other layers,
and the compiler enforces it because each layer is a separate project.

**Design decisions the implementation will follow**

- The server never trusts client-supplied prices. The price is read from the database and snapshotted on the booking.
- Double-booking is prevented by a filtered unique index in the database. Application checks are only a fast path.
- Status changes go through a single service method so every transition is validated and recorded.
- Business rules are pure and testable without a database or a clock.

> This is not a full Clean Architecture implementation: there is no separate Application layer,
> and Services depend directly on EF Core.

## Tech Stack

- **.NET 10** (LTS), **ASP.NET Core MVC** with Areas
- **Entity Framework Core 10** with **SQL Server** (LocalDB for development)
- **xUnit** for tests, **SQLite in-memory** for service-level tests
- **Git / GitHub** with pull-request workflow

## Project Structure

```
Zyara/
├── src/
│   ├── Zyara.Domain/        Entities, Enums, Rules
│   ├── Zyara.Services/      Data, Features, Jobs, Common
│   └── Zyara.Web/           Areas/{Patient,Provider,Admin}, Views, Program.cs
├── tests/
│   └── Zyara.Tests/
├── Directory.Build.props
└── Zyara.sln
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio's *ASP.NET and web development* workload)
- Visual Studio 2026 or a compatible IDE (VS Code and Rider also work)
- [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) tool: `dotnet tool install --global dotnet-ef`

### Setup

```bash
git clone https://github.com/ali-sweilem/Zyara.git
cd Zyara
dotnet restore
dotnet build
```

The default connection string is in `src/Zyara.Web/appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=Zyara;Trusted_Connection=True;TrustServerCertificate=True
```

### Database and run

Database migrations and application features are not implemented yet.
Once migrations are added to the repository, this section will describe how to create the
database and run the application.

## Testing

```bash
dotnet test
```

Currently the test project covers the commission calculation rule (`CommissionCalculator`).
As features are added, the strategy will be:

- **Domain rule tests** run without a database or a clock.
- **Service tests** use an in-memory SQLite database (not EF's InMemory provider, which ignores unique indexes).

## Team Workflow

- One short-lived branch per task: `feature/<slice>-<short-description>`.
- Open a Pull Request into `main`; at least one teammate reviews before merging.
- Merge `main` into your branch daily to avoid large conflicts.
- Use **Squash and merge**.
- Shared files (`ZyaraDbContext`, `ServiceCollectionExtensions`, `Program.cs`, migrations) are changed with a heads-up to the team.
- Only the team lead edits `.csproj` files.

## Roadmap

The work is divided into six vertical slices. Mark each one when it is done.

- [ ] 1. Accounts and Admin
- [ ] 2. Provider registration and verification
- [ ] 3. Catalog and search
- [ ] 4. Booking core
- [ ] 5. Visit execution
- [ ] 6. Money, notifications, and quality

## Team

| Name | Role |
|---|---|
| Ali Ashour Farag | Team Leader & Full-Stack Developer |
| Abdelrhman Fathy | Full-Stack Developer |
| Adham Mohamed | Full-Stack Developer |
| Youssef Mohamed | Full-Stack Developer |
| Mostafa Hamdy | Full-Stack Developer |
| Omar Hesham | Full-Stack Developer |

**Program:** Digital Egypt Pioneers Initiative (DEPI) · **Track:** Full Stack .Net Web Developer · **Instructor:** Abdulrahman Elwakeel · **Round:** 5

## License

This project is developed for educational purposes as a DEPI graduation project.