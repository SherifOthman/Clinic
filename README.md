# Clinic

A bilingual (Arabic + English, RTL) clinic management web app built on **ASP.NET Core (Clean Architecture)** + **React (Vite, Tailwind v4, shadcn/ui)**, designed to run **offline on one host PC**, with other PCs on the clinic network using it via browser.

This is a faithful, maintainable re-implementation of the [OHC CARE](https://github.com/ohcnetwork/care) clinic/OPD workflows. The original CARE source is cloned under `reference/` and kept as a per-feature reference.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and [`docs/ROADMAP.md`](docs/ROADMAP.md) for the full design and the feature-by-feature plan.

## Layout

```
Clinic/
├── reference/      cloned CARE (read-only, gitignored) — per-feature reference
├── backend/        ASP.NET Core app (Clean Architecture, .NET 10)
│   ├── Clinic.slnx
│   └── src/{Clinic.Domain,Clinic.Application,Clinic.Infrastructure,Clinic.Api}
├── frontend/       React app (Vite + Tailwind v4 + shadcn/ui)
└── docs/           ARCHITECTURE.md, ROADMAP.md
```

## Prerequisites

- .NET SDK 10 (`dotnet --version`)
- Node.js 20+ and npm
- Git (only used to clone `reference/`, optional)

## Run for development (two processes)

1. **Backend** — from `backend/`:
   ```
   dotnet run --project src/Clinic.Api
   ```
   Serves the API on `http://localhost:5000`, Swagger at `/swagger` (when `ASPNETCORE_ENVIRONMENT=Development`).
2. **Frontend** — from `frontend/`:
   ```
   npm install
   npm run dev
   ```
   Vite dev server on `http://localhost:5173`, proxies `/api` and `/health` to the backend.

## Run as a LAN server (single-deploy — what CARE Clinic does)

One host PC runs everything; other PCs on the clinic network just open a browser.

1. Build the frontend into the backend's `wwwroot`:
   ```
   cd frontend
   npm install
   npm run build
   ```
   (The Vite config writes output to `../backend/src/Clinic.Api/wwwroot`.)
2. Run the backend on the host PC:
   ```
   cd backend
   dotnet run --project src/Clinic.Api
   ```
   It listens on `http://0.0.0.0:5000` (all interfaces), auto-creates/migrates the SQLite DB under `backend/src/Clinic.Api/data/clinic.db`, and seeds an admin.
3. Find the host PC's LAN IP (`ipconfig`), e.g. `192.168.1.50`.
4. From any other PC on the same network, open `http://192.168.1.50:5000`.

> **Firewall:** allow inbound TCP 5000 on the host. In an admin PowerShell:
> `New-NetFirewallRule -DisplayName "Clinic 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow`

## Default sign-in

- Username: `admin`
- Password: `admin`

Change it after first login (password reset arrives in Milestone 1 — User management).

## Languages

The app ships with **English** and **Arabic**. Toggle via the language switcher (top bar / login page). The choice persists and the layout flips to RTL automatically.

## What works now (Milestone 0 — Foundation)

- Clean Architecture backend, ASP.NET Identity + JWT, roles, SQLite, auto-migrate + seed
- Login (`/api/auth/login`), current user (`/api/auth/me`), exception handling
- React app shell, login page, dashboard placeholder, role-based nav
- i18n (en/ar) with RTL, shadcn/ui components, TanStack Query, react-hook-form + zod

Next features land one at a time — see [`docs/ROADMAP.md`](docs/ROADMAP.md).
