# Repository Audit

**Document ID**: DOC-001  
**Version**: 1.0  
**Date**: January 2025  
**Status**: Complete

---

## Executive Summary

This document provides a complete audit of:
1. The care_clinic v0.1.15 reference implementation
2. Pinned CARE backend and frontend dependencies
3. Existing ClinicCare implementation status
4. Repository structure and current state

**Critical Finding**: care_clinic is a **desktop deployment wrapper** (Wails + Docker Compose), not the clinical application itself. The actual clinic functionality resides in CARE backend (Django) and CARE frontend (React).

---

## 1. care_clinic v0.1.15 Audit

### 1.1 Repository Information

- **Repository**: https://github.com/ohcnetwork/care_clinic
- **Release**: v0.1.15
- **Commit**: `ef9617d` (ef9617da9b57139f87e12ac851e49f37098cbdc1)
- **Location**: `d:\Clinic\reference\care_clinic\`
- **Verified**: ✅ Cloned and checked out at exact commit

### 1.2 What care_clinic Actually Is

**Architecture**:
```
care_clinic (Wails Desktop App)
├── Go Backend (app/internal/)
│   ├── Clinic orchestration
│   ├── Docker Compose management
│   ├── Backup/restore
│   ├── Certificate management
│   └── Native system integration
├── React UI (app/frontend/)
│   ├── Setup wizard
│   ├── Control panel
│   ├── Settings management
│   └── Plugin management
└── Deployment Kit (deployments/)
    ├── docker-compose.yml
    ├── Caddyfile
    ├── .env (pinned versions)
    └── Scripts (backup, etc.)
```

**Key Insight**: care_clinic is **NOT** the clinical application. It's a deployment orchestrator that:
- Installs Docker/Git/dependencies
- Builds CARE backend and frontend from pinned upstream commits
- Runs PostgreSQL, Redis, Minio, Caddy as containers
- Provides desktop control panel for server management
- Handles backup/restore and TLS certificates
- Advertises `.local` mDNS for client discovery

### 1.3 Pinned Dependencies (v0.1.15)

From `deployments/.env`:

**Third-Party Images**:
- PostgreSQL: `17.10-alpine`
- Redis: `8.8.0-alpine`
- Minio (Silo): `RELEASE.2026-09-03T13-18-01Z`
- Caddy: `2.11.4`
- Coraza WAF: `v2.6.1`

**CARE Source** (Built at Installation Time):
- **Backend Repository**: https://github.com/ohcnetwork/care.git
- **Backend Branch**: `ENG-737` ⚠️ **NOT** a release tag
- **Frontend Repository**: https://github.com/ohcnetwork/care_fe.git
- **Frontend Branch**: `bodhi/questionnaire-actions` ⚠️ **NOT** a release tag

**Critical Note**: v0.1.15 pins to development branches, not stable releases. This means:
- Features may be in flux
- Some functionality may be incomplete
- Documentation may not match implementation

### 1.4 care_clinic Components

**Desktop Application** (`app/`):
- Wails framework (Go + React)
- Native OS integration (Windows, macOS, Linux)
- System checks (Docker, Git, disk space, ports)
- Certificate generation and trust installation
- mDNS advertisement
- Backup encryption with recovery codes

**Server Operations**:
- Docker Compose orchestration
- Image builds from pinned commits
- Service health monitoring
- Start/stop/restart
- Settings management
- Plugin installation and updates

**Client Mode**:
- Connects to existing server via `.local` address
- Certificate trust-on-first-use
- No Docker/Git required on client machines
- Browser-based access after setup

**Backup System**:
- Encrypted PostgreSQL dumps
- Minio data backup
- Daily scheduled backups (cron/scheduled task)
- Recovery file export (required for decryption)
- Staged restore with safety checks

### 1.5 Documentation Quality

**Excellent Documentation** in `docs/`:
- `README.md` - Comprehensive feature overview
- `desktop-workflows.md` - Complete user workflows
- `architecture.md` - System design and boundaries
- `repository-map.md` - Code structure (29KB!)
- `wails-application.md` - API and concurrency model (64KB!)
- `clinic-lifecycle.md` - Deployment and services
- `backups-and-restore.md` - Backup procedures
- `native-integrations.md` - OS-specific behavior
- `development-and-release.md` - Build and release process

**Finding**: care_clinic documentation is **production-quality** and provides excellent reference for:
- Desktop deployment challenges
- Backup/restore strategies
- Certificate management
- Local network discovery
- Safe update procedures

---

## 2. CARE Backend Audit (Branch ENG-737)

### 2.1 Repository Information

- **Repository**: https://github.com/ohcnetwork/care
- **Location**: `d:\Clinic\reference\care\`
- **Current Branch**: `develop` (local has develop, not ENG-737)
- **Framework**: Django 5.x, Django REST Framework
- **Database**: PostgreSQL (with extensive use of JSONField and ArrayField)

### 2.2 EMR Module Structure

**Location**: `care/emr/`

**Subdirectories**:
- `models/` - 40+ entity models (FHIR-aligned)
- `api/` - REST API viewsets and endpoints
- `resources/` - Pydantic resource specs (DTOs)
- `fhir/` - FHIR resource mappings
- `registries/` - Plugin registries
- `signals/` - Django signals for model lifecycle
- `tasks/` - Celery background tasks
- `admin/` - Django admin customizations
- `tests/` - Test suite
- `utils/` - Utilities
- `locks/` - Distributed locking
- `tagging/` - Tag management system
- `extensions/` - Extension framework

### 2.3 Domain Model Summary

**Core Entities** (40+):
- `Patient` - Demographics, identifiers, organization access
- `Encounter` - Ambulatory visits, inpatient stays, status workflow
- `Observation` - Vitals, lab results, assessments
- `Condition` - Diagnoses
- `MedicationRequest` - Prescriptions
- `MedicationRequestPrescription` - Prescription groups
- `ServiceRequest` - Investigation/procedure orders
- `DiagnosticReport` - Lab results summary

**Scheduling** (7 entities):
- `SchedulableResource` - Doctors, locations, services
- `Schedule` - Doctor's schedule with charge items
- `Availability` - Recurring time slots
- `AvailabilityException` - Blocks/holidays
- `TokenSlot` - Generated booking slots
- `TokenBooking` - Appointments with status workflow
- `TokenQueue` / `Token` / `TokenCategory` / `TokenSubQueue` - Walk-in queue system

**Billing** (5+ entities):
- `Account` - Patient billing account (episode of care)
- `ChargeItemDefinition` - Service catalog with pricing
- `ChargeItem` - Actual service charges
- `Invoice` - Groups charges, can lock
- `PaymentReconciliation` - Payment records

**Organization & Access**:
- `Organization` - Hierarchical with caching
- `FacilityOrganization` - Facility-specific departments
- `OrganizationUser` / `FacilityOrganizationUser` - User-org-role assignments
- `PatientOrganization` - Patient accessibility by org
- `PatientUser` - Individual user access to patient

**Other Entities**:
- `Location` / `FacilityLocation` - Rooms, beds, areas
- `HealthcareService` - Services offered
- `AllergyIntolerance` - Allergies
- `Consent` - Consent records
- `FileUpload` - Document attachments
- `Product` / `ProductKnowledge` / `InventoryItem` - Inventory
- `Specimen`, `Device`, `Notes`, `Questionnaire`, etc.

**Key Patterns**:
- `EMRBaseModel` base class: history (JSON), meta (JSON), created_by, updated_by
- `external_id` (UUID) for public API, integer PK internally
- Soft delete with `deleted` field
- Heavy use of JSON for FHIR flexibility
- Caching patterns (organization_cache, users_cache, etc.)
- Extensions (JSON) for plugin data

### 2.4 Billing Flow (FHIR-Aligned)

```
Service Catalog (ChargeItemDefinition)
    ↓
Service Rendered → ChargeItem (billable)
    ↓
Invoice Created → Groups ChargeItems
    ↓
Invoice Locked → Immutable
    ↓
Payment Made → PaymentReconciliation
    ↓
Account Updated → Totals recalculated
```

**Key Features**:
- Account caches totals (performance optimization)
- Invoice can be locked to prevent changes
- ChargeItem snapshots pricing at time of charge
- PaymentReconciliation supports partial payments
- Refunds via `is_credit_note` flag
- Decimal(20,6) precision for all amounts

### 2.5 Scheduling & Queue Integration

**Scheduled Appointments**:
1. SchedulableResource (doctor) has Schedule
2. Schedule has Availability (recurring slots)
3. TokenSlot generated from Availability
4. Patient books TokenBooking
5. Patient arrives → Token created → enters queue
6. Token called → Encounter created
7. Encounter completed → Token fulfilled → TokenBooking fulfilled

**Walk-in Patients**:
1. Patient arrives without appointment
2. Token created directly in TokenQueue
3. Token number assigned (sequential per queue+category)
4. Token called → Encounter created
5. Encounter completed → Token fulfilled

**Key Insight**: System supports BOTH workflows with shared queue management.

---

## 3. CARE Frontend Audit (Branch bodhi/questionnaire-actions)

### 3.1 Repository Information

- **Repository**: https://github.com/ohcnetwork/care_fe
- **Location**: `d:\Clinic\reference\care_fe\`
- **Current Branch**: `develop` (local has develop, not bodhi/questionnaire-actions)
- **Framework**: React 19 + Vite
- **UI Library**: Tailwind CSS + Radix UI primitives
- **State Management**: Jotai
- **Routing**: raviger (custom router)
- **Forms**: react-hook-form + zod

### 3.2 Frontend Structure

```
src/
├── components/ - Reusable components
├── CAREUI/ - Design system components
├── Routers/ - Route definitions and navigation
├── Redux/ - Legacy Redux (being migrated to Jotai)
├── Utils/ - Utilities and API client
└── Locale/ - i18n translations
```

**Key Features**:
- Component library based on Tailwind + Radix
- Multi-language support (English + regional languages)
- Real-time updates (likely via WebSocket or polling)
- Responsive design
- Accessibility features

**Note**: Frontend investigation limited as focus was on backend domain model. Frontend patterns will be investigated further if needed.

---

## 4. Existing ClinicCare Implementation Audit

### 4.1 Repository Structure

**Location**: `d:\Clinic\`

```
Clinic/
├── backend/
│   ├── Clinic.slnx (Solution file)
│   └── src/
│       ├── Clinic.Domain/
│       ├── Clinic.Application/
│       ├── Clinic.Infrastructure/
│       └── Clinic.Api/
├── frontend/
│   ├── src/
│   ├── public/
│   ├── package.json
│   └── vite.config.ts
├── reference/ (Cloned repositories)
│   ├── care/
│   ├── care_fe/
│   └── care_clinic/
└── docs/
    ├── ARCHITECTURE.md
    ├── ROADMAP.md
    └── planning/ (This package)
```

### 4.2 Backend Implementation Status

**Framework**: ASP.NET Core (.NET 10), Clean Architecture

**Layers**:
1. **Domain** (`Clinic.Domain`)
   - Entities: `ApplicationUser`, `ApplicationRole` ONLY
   - Base: `AuditableEntity` (Id, ExternalId, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted)
   - Constants: `Roles` (Admin, Doctor, Nurse, Staff, Pharmacist, Receptionist)
   - **NO clinical domain entities**

2. **Application** (`Clinic.Application`)
   - CQRS Commands/Queries: `LoginCommand`, `GetCurrentUserQuery` ONLY
   - Pipeline Behaviors: `ValidationBehavior<TRequest, TResponse>`
   - Exceptions: `AppException`, `NotFoundException`, `ForbiddenException`, `ValidationException`
   - Interfaces: `IApplicationDbContext`, `IIdentityService`, `IJwtTokenService`, `ICurrentUserService`
   - Dependencies: MediatR, FluentValidation, AutoMapper (configured but no profiles yet)

3. **Infrastructure** (`Clinic.Infrastructure`)
   - `ApplicationDbContext`: Inherits `IdentityDbContext<ApplicationUser, ApplicationRole, int>`
   - Global query filter for soft delete
   - SaveChanges override for audit fields
   - Services: `IdentityService`, `JwtTokenService`
   - Database: SQLite with EF Core
   - Migrations: One migration (Identity tables only)
   - Seeding: Roles + admin user

4. **Api** (`Clinic.Api`)
   - Controllers: `AuthController` ONLY (`/api/auth/login`, `/api/auth/me`)
   - Middleware: `ExceptionHandlingMiddleware`
   - Configuration: JWT bearer, CORS (permissive), Swagger
   - SPA hosting: Serves built React app from wwwroot
   - Health endpoint: `/health`

**Key Findings**:
- ✅ Foundation is solid (Clean Architecture, patterns properly implemented)
- ✅ Authentication working (ASP.NET Identity + JWT)
- ✅ Audit infrastructure ready (AuditableEntity, soft delete)
- ✅ CQRS pipeline established (MediatR + FluentValidation)
- ✅ Single-deploy model working (API + SPA on one port)
- ❌ **Zero clinical domain entities**
- ❌ **Zero clinical features** (everything after Milestone 0)
- ❌ No tests whatsoever
- ❌ No repository pattern (direct DbContext access intended)

### 4.3 Frontend Implementation Status

**Framework**: React 19 + Vite + TypeScript

**Tech Stack**:
- **UI**: Tailwind CSS v4 + shadcn/ui (Radix primitives)
- **Routing**: react-router-dom
- **State**: TanStack Query (server state) + Zustand (client state)
- **Forms**: react-hook-form + zod
- **i18n**: i18next + react-i18next (en + ar with RTL)
- **Icons**: lucide-react

**Implementation Status**:
- ✅ i18n infrastructure (en.json, ar.json)
- ✅ RTL support (dir attribute, logical properties)
- ✅ Login page working
- ✅ Dashboard placeholder
- ✅ Auth context and routing
- ✅ API client with Authorization header
- ❌ **No clinical UI** (only login and empty dashboard)

**Structure**:
```
src/
├── features/
│   └── auth/ (Login feature only)
├── pages/
│   ├── Dashboard.tsx
│   └── Placeholder.tsx
├── components/
├── lib/
└── i18n/
    └── locale/
        ├── en.json
        └── ar.json
```

### 4.4 Pattern Analysis

**CQRS Usage**:
- Every operation becomes Command or Query
- MediatR acts as mediator between API and business logic
- Handlers are isolated and testable

**Evaluation**:
- CARE doesn't use CQRS, works fine for complex hospital workflows
- Question: Is this pattern adding value or just abstraction overhead?
- Recommendation: Keep for complex operations, don't force for simple CRUD

**AutoMapper**:
- Configured but no profiles yet (nothing to map)
- CARE uses Pydantic serializers (simpler)
- Question: Is mapping layer always needed?
- Recommendation: Use for complex transformations, manual mapping acceptable for simple DTOs

**FluentValidation**:
- Properly integrated in pipeline
- CARE uses built-in validation
- Recommendation: Keep - valuable for complex validation rules

**Repository Pattern**:
- Not implemented (direct DbContext access intended)
- CARE uses Django ORM directly
- Recommendation: Evaluate per feature - not every entity needs a repository

### 4.5 Technology Comparison

| Aspect | CARE | ClinicCare |
|--------|------|------------|
| **Backend** | Django + DRF | ASP.NET Core + Clean Architecture |
| **Database** | PostgreSQL | SQLite (initial), PostgreSQL (future) |
| **ORM** | Django ORM | Entity Framework Core |
| **Pattern** | MVT | Clean Architecture + CQRS |
| **Validation** | Django validators | FluentValidation |
| **Serialization** | Pydantic | AutoMapper |
| **Background Jobs** | Celery + Redis | Not yet implemented |
| **Real-time** | Likely WebSocket | Not yet implemented |
| **Frontend** | React + Vite | React + Vite ✓ |
| **UI Library** | Tailwind + Radix | Tailwind v4 + shadcn/ui ✓ |
| **State** | Jotai | TanStack Query + Zustand |
| **Router** | raviger | react-router-dom |
| **i18n** | Custom | i18next ✓ |

**Key Observations**:
- Frontend stack alignment is good
- Backend pattern difference significant (MVT vs Clean + CQRS)
- CARE is simpler, ClinicCare more abstracted
- Both approaches can work - different trade-offs

---

## 5. Git Status and Working Tree

### 5.1 Main Workspace

**Status**: Not a git repository

**Implication**: No version control currently. Recommendation: Initialize git repository before starting implementation to track changes.

### 5.2 Reference Repositories

**care_clinic**: ✅ At commit ef9617d (v0.1.15, detached HEAD)
**care**: ✅ On branch `develop`, commit 2bae933
**care_fe**: ✅ On branch `develop`, commit 872ed91

**Note**: Local care/care_fe are on `develop`, not the pinned branches (`ENG-737` and `bodhi/questionnaire-actions`). For accurate reference, should checkout pinned branches.

### 5.3 Recommended Git Setup

1. Initialize git in main workspace:
   ```powershell
   git init
   git add .
   git commit -m "Initial commit: Foundation (Milestone 0 complete)"
   ```

2. Checkout pinned branches in reference repositories:
   ```powershell
   git -C reference/care checkout ENG-737
   git -C reference/care_fe checkout bodhi/questionnaire-actions
   ```

---

## 6. Additional Open-Source Systems

### 6.1 OpenEMR

**Clone Attempt**: ⚠️ Timed out during clone (large repository)
**Status**: Partially cloned in `reference/openemr/`
**Action**: Complete clone or investigate via online repository

**Known Information**:
- PHP-based EHR system
- Comprehensive clinic and hospital features
- Strong billing module
- Active community
- HIPAA-compliant design

**Research Needed**:
- Clinic/ambulatory workflow patterns
- Billing cycle and payment collection
- Prescription templates
- Report generation

### 6.2 OpenMRS

**Status**: Not cloned yet
**Action**: Clone or investigate via documentation

**Known Information**:
- Java-based medical records system
- Modular architecture
- Billing module available as add-on
- Strong in resource-constrained settings
- Observation-based data model

**Research Needed**:
- Encounter workflow
- Billing module design
- Patient identifier systems
- Queue/appointment management

### 6.3 GNU Health

**Status**: Not cloned yet
**Action**: Investigate via official documentation

**Known Information**:
- Python + Tryton framework
- Hospital Information System
- Integrated accounting (financial management)
- Strong in developing countries
- Covers clinic, hospital, lab

**Research Needed**:
- Financial/accounting integration patterns
- Ambulatory clinic workflows
- Prescription and pharmacy workflows
- Reporting and analytics

### 6.4 TPT Doctor

**Status**: Not investigated yet
**Action**: Research via GitHub repository

**Known Information**:
- Vietnamese clinic management system
- Smaller scale than CARE/OpenEMR
- Practical clinic-focused features

**Research Needed**:
- Feature scope for small clinic
- Workflow simplifications
- Implementation approach

---

## 7. Key Findings Summary

### 7.1 Critical Insights

1. **care_clinic is a wrapper**, not the clinical app
   - Desktop deployment orchestrator
   - Actual clinic functionality in CARE backend/frontend
   - Provides valuable reference for deployment challenges

2. **CARE domain model is comprehensive**
   - 40+ FHIR-aligned entities
   - Supports complex hospital workflows
   - **Too complex for small clinic** - simplification needed

3. **Existing ClinicCare foundation is solid**
   - Clean Architecture properly implemented
   - Patterns established and working
   - **But patterns need evaluation** - may be over-engineered

4. **CQRS is not mandatory**
   - CARE doesn't use it, handles complexity fine
   - Consider selective use, not universal requirement

5. **Branch model is simpler than Organization hierarchy**
   - Don't need CARE's complex org structure
   - Simple Branch entity with shared patient records sufficient

6. **Billing can be simplified**
   - CARE's 5-entity flow is FHIR-compliant but complex
   - Smaller clinic can use 3-entity flow without losing functionality

### 7.2 Architecture Evaluation Required

**Questions to Answer**:
- Is Clean Architecture + CQRS the right fit for small clinic?
- Can we simplify without losing benefits?
- Which CARE patterns are essential vs optional?
- What's the right balance between flexibility and complexity?

**Recommendation**: Create Architecture Decisions document (ADR) to formally evaluate and decide on each pattern.

---

## 8. Next Steps

1. ✅ **Complete remaining research**
   - Finish OpenEMR clone and investigation
   - Investigate OpenMRS billing module
   - Research GNU Health financial integration
   - Review TPT Doctor if applicable

2. **Checkout pinned branches in reference repos**
   ```powershell
   git -C reference/care checkout ENG-737
   git -C reference/care_fe checkout bodhi/questionnaire-actions
   ```

3. **Initialize version control for main workspace**
   ```powershell
   cd d:\Clinic
   git init
   git add .
   git commit -m "Initial commit: Milestone 0 complete"
   ```

4. **Continue planning documents**
   - Product scope (P0/P1/P2/P3 classification)
   - Workflows (complete clinic operational workflows)
   - Domain model (ClinicCare entities adapted from CARE)
   - Architecture decisions (ADRs for all patterns)
   - Database design (schema with integrity constraints)

---

## Document Changelog

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-XX | Planning Agent | Initial repository audit based on investigation |

---

## References

- care_clinic v0.1.15: https://github.com/ohcnetwork/care_clinic/releases/tag/v0.1.15
- CARE Backend: https://github.com/ohcnetwork/care
- CARE Frontend: https://github.com/ohcnetwork/care_fe
- Existing docs: `d:\Clinic\docs\ARCHITECTURE.md`, `d:\Clinic\docs\ROADMAP.md`
