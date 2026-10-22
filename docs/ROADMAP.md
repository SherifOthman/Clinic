# Clinic — Feature Roadmap

Each feature is implemented **fully** (backend + frontend + validation + i18n en/ar + RTL) before moving to the next. Update the checkbox when a feature ships.

Reference source (cloned): `reference/care` (backend), `reference/care_fe` (frontend).

## Milestone 0 — Foundation (DONE)
- [x] Backend: Clean Architecture solution (Domain/Application/Infrastructure/Api)
- [x] Backend: base `AuditableEntity` (ExternalId, soft-delete, auditable)
- [x] Backend: EF Core DbContext (SQLite) + auto-migrate + seed admin
- [x] Backend: ASP.NET Identity + JWT auth, roles
- [x] Backend: FluentValidation pipeline, AutoMapper, MediatR wiring
- [x] Backend: exception handling, Swagger (dev), CORS, SPA hosting
- [x] Backend: builds + runs + serves `/health`
- [x] Frontend: Vite + React 19 + TS + Tailwind v4 + shadcn/ui
- [x] Frontend: react-router, TanStack Query, react-hook-form + zod, Zustand
- [x] Frontend: i18next (en + ar) + RTL + LanguageSelector
- [x] Frontend: API client (fetch + bearer + Accept-Language)
- [x] Frontend: app shell (sidebar, topbar), login page wired to backend
- [x] Frontend: build output served by Clinic.Api (single-deploy)

## Milestone 1 — Auth & Access (Feature 1)  — ref: `care/security`, `config/auth_views.py`
- [ ] Login (username/password) + JWT, logout, token refresh
- [ ] Roles: Admin, Doctor, Nurse, Staff, Pharmacist, Receptionist
- [ ] User CRUD (admin), assign role, activate/deactivate, reset password
- [ ] Frontend: login page, session/auth context, role-based nav, user management screen
- [ ] i18n + RTL for all auth screens

## Milestone 2 — Facility & Departments (Feature 2) — ref: `care/facility`, `care/emr` org
- [ ] Facility CRUD (single clinic + metadata)
- [ ] Facility organization / departments CRUD
- [ ] Users-in-facility membership + role
- [ ] Frontend: facility settings, departments, members screens

## Milestone 3 — Patient Registration (Feature 3) — ref: `care/emr/models/patient.py`
- [ ] Patient CRUD, identifiers (UHID/MRN), PatientUser access
- [ ] Patient search (by name / phone / identifier)
- [ ] Patient profile with tabs (demographics + clinical summary)
- [ ] Frontend: patient list, registration form, profile, search
- [ ] Printable patient summary

## Milestone 4 — Appointments & Queue (Feature 4) — ref: `care/emr/models/scheduling/`
- [ ] SchedulableResource (doctors), Schedule + Availability (slots)
- [ ] TokenSlot, TokenBooking (appointments)
- [ ] TokenQueue / Token / TokenCategory (walk-in queue)
- [ ] Frontend: appointments board, booking form, queue management
- [ ] Today's visit queue view

## Milestone 5 — Consultation / Encounter (Feature 5) — ref: `care/emr/models/encounter.py`
- [ ] Encounter create + status transitions, link to appointment
- [ ] Vitals (Observation), symptoms/diagnoses (Condition)
- [ ] Clinical notes, advice, follow-up
- [ ] Frontend: EncounterShow tabbed consultation workspace
- [ ] Link encounter → appointment (mark completed)

## Milestone 6 — Prescriptions (Feature 6) — ref: `care/emr/models/medication_request.py`
- [ ] MedicationRequestPrescription (group) + MedicationRequest (lines)
- [ ] Dosage instructions, requester, status
- [ ] Frontend: prescription editor in encounter, printable prescription

## Milestone 7 — Investigations / Labs (Feature 7) — ref: `service_request`, `observation`, `diagnostic_report`
- [ ] ServiceRequest (lab orders), Observation (results), DiagnosticReport
- [ ] Frontend: order entry, results entry, report view/print

## Milestone 8 — Billing (Feature 8) — ref: `care/emr/models/{account,charge_item,invoice}.py`
- [ ] Account, ChargeItem + ChargeItemDefinition, Invoice (create/lock/cancel)
- [ ] PaymentReconciliation, receipts
- [ ] Frontend: billing account, invoices, payments, printable invoice/receipt

## Milestone 9 — Dashboard & Reports (Feature 9) — ref: reports/templates
- [ ] Stats dashboard (patients today, queue, revenue, outstanding)
- [ ] Report templates from recorded clinical data
- [ ] Frontend: dashboard, report viewer

## Milestone 10 — Settings & Configuration (Feature 10)
- [ ] patient_identifier_config, charge_item_definitions, token_category
- [ ] User preferences, language, theme
- [ ] Backup/restore of the SQLite DB (admin)

## Deployment hardening (after core features)
- [ ] Self-contained single-file publish for the host PC
- [ ] run script / installer guidance for host + LAN clients
- [ ] HTTPS option with local cert for the LAN
