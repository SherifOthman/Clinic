# Clinic — Architecture & Tech Stack

A faithful, maintainable port of OHC **CARE** (clinic/OPD scope) to **ASP.NET Core + React**, built for offline single-clinic use on a local network.

The original CARE source is cloned under `reference/care` (Django backend) and `reference/care_fe` (React frontend) and kept as a per-feature reference.

## Goals

- Cover CARE's core OPD workflows: registration, appointments/queue, consultation (Encounter), prescriptions, investigations, billing, dashboard.
- Bilingual **Arabic + English** with full RTL.
- Runs on one host PC; other PCs on the LAN use it via browser. No internet needed after setup.
- **Reusability, scalability, maintainability** — features can be added/changed without breaking others.

## Backend — Clean Architecture (ASP.NET Core, .NET 10)

```
src/
  Clinic.Domain          — entities, enums, base types. No external deps.
  Clinic.Application     — DTOs, validators, MediatR CQRS handlers, interfaces, AutoMapper profiles.
  Clinic.Infrastructure  — EF Core DbContext, ASP.NET Identity + JWT, repositories, seeding.
  Clinic.Api             — controllers, middleware, DI wiring, SPA hosting, Swagger.
```

Dependency direction: `Api → Infrastructure → Application → Domain`. Domain depends on nothing.

### Tech
- **ASP.NET Identity** (roles) + JWT bearer for auth.
- **EF Core** with **SQLite** for local offline (provider is swappable to SQL Server/Postgres).
- **MediatR** — CQRS. Each feature is an isolated slice (Command/Query/Handler/Validator/DTO) so extending never touches unrelated code.
- **FluentValidation** — request validation, run in a MediatR pipeline behavior.
- **AutoMapper** — entity ↔ DTO mapping.
- **Serilog** — structured logging (optional, pluggable).

### Base entity pattern (mirrors CARE's `BaseModel`)
Every entity inherits `AuditableEntity`: `Id` (int), `ExternalId` (Guid, for public API lookups), `CreatedAt`/`UpdatedAt`, `CreatedBy`/`UpdatedBy`, `IsDeleted` (soft delete enforced via EF global query filter; `SaveChanges` override sets `IsDeleted` instead of hard-deleting).

### Auth & RBAC
- Staff login (username + password) → JWT. Roles: Admin, Doctor, Nurse, Staff, Pharmacist, Receptionist.
- ASP.NET Identity manages users/roles/passwords/tokens.
- Authorization via roles + a permission helper (port CARE's `getPermissions` idea) for UI gating.

## Frontend — React 19 + Vite + TypeScript

### Tech
- **Tailwind CSS v4** + **shadcn/ui** (Radix primitives) — same design-system approach as CARE; we own the components.
- **TanStack Query** — server state/data fetching.
- **react-router-dom** — routing (mainstream, better ecosystem than raviger).
- **react-hook-form + zod** — forms + validation (matches CARE).
- **Zustand** — lightweight client state (simpler than Jotai).
- **i18next + react-i18next** — `en` + `ar` with full RTL.
- **recharts** (dashboard), **date-fns**, **lucide-react** (icons), decimal.js for money.

### i18n + RTL (built from scratch — CARE has neither Arabic nor RTL)
- `public/locale/en.json`, `public/locale/ar.json` (flat key→value, mirroring CARE's structure).
- `LanguageSelector`; detected language persists in `localStorage`.
- `<html dir="rtl|ltr" lang="...">` set from language; flips live on change.
- Tailwind **logical properties** (`ps-*`, `pe-*`, `ms-*`, `me-*`, `start-*`, `end-*`) instead of physical `pl/pr/ml/mr` so layouts mirror automatically. Direction-aware icons/chevrons.

### API client
- Centralized typed fetch wrapper (port of CARE's `callApi` pattern) with `Authorization: Bearer` and `Accept-Language`. Base URL from env, defaulting to same origin in single-deploy mode.

## Deployment — desktop offline + local network

- Host PC: a self-contained ASP.NET Core app (Kestrel) bound to `0.0.0.0:5000`, serving the API **and** the built React SPA (single folder, one port).
- SQLite DB file in a local `data/` folder. Auto-migrate + seed admin on first run.
- Other PCs on the LAN browse to `http://<server-ip>:5000`. No Docker, no per-client install.
- Optional: self-contained single-file publish of the host for easy deployment.

## Pragmatic deviations from CARE (all reversible)

| Area | CARE | Ours | Why |
|------|------|------|-----|
| Domain shape | FHIR resources, opaque JSON blobs | FHIR-aligned names, concrete queryable columns | Queryable, simpler, still extensible |
| IDs | UUID `external_id` as primary | int PK + `ExternalId` Guid for lookups | Simpler FKs, still hides sequence |
| Routing | raviger | react-router-dom | Mainstream, larger ecosystem |
| Client state | Jotai | Zustand | Simpler API, fewer concepts |
| DB | Postgres + Redis + Celery | SQLite (EF Core) | Zero-config local; provider swappable |
| Serializers | Pydantic EMRResource | DTOs + AutoMapper | .NET-idiomatic |

## Folder layout

Reference (cloned CARE) and our own app live in separate top-level folders:

```
Clinic/
├── reference/          cloned CARE (read-only, gitignored — per-feature reference)
│   ├── care/           Django backend
│   └── care_fe/        React frontend
├── backend/            OUR ASP.NET Core app
│   ├── Clinic.slnx
│   └── src/
│       ├── Clinic.Domain/
│       ├── Clinic.Application/
│       ├── Clinic.Infrastructure/
│       └── Clinic.Api/   (hosts built SPA in wwwroot/)
├── frontend/           OUR React app (build output → backend/src/Clinic.Api/wwwroot)
└── docs/               ARCHITECTURE.md, ROADMAP.md
```
