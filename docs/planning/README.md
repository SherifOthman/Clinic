# ClinicCare Planning Package

**Version:** 1.0  
**Date:** January 2025  
**Status:** Research & Planning Phase  
**Target Product:** ClinicCare - Local Clinic Management System

---

## Purpose

This planning package contains comprehensive research, analysis, and specifications for developing ClinicCare, a clinic management application for a single doctor with one or more branches. The package is based on:

1. **Deep reverse engineering** of CARE v0.1.15 and its underlying EMR platform
2. **Comparative analysis** of open-source healthcare systems
3. **Audit of existing ClinicCare foundation** (Clean Architecture + ASP.NET Core)
4. **Evidence-based architectural decisions** with documented rationale
5. **Complete domain model, workflows, and implementation roadmap**

---

## Document Index

### Phase 1: Research & Analysis

| Document | Purpose | Status |
|----------|---------|--------|
| [01-repository-audit.md](01-repository-audit.md) | Complete audit of reference repositories and existing ClinicCare implementation | ✅ Complete |
| [02-reference-research.md](02-reference-research.md) | Findings from CARE, OpenEMR, OpenMRS, GNU Health, and other systems | ✅ CARE Complete, Others In Progress |
| [18-existing-implementation-audit.md](18-existing-implementation-audit.md) | Detailed analysis of current ClinicCare codebase and patterns | ✅ Complete |

### Phase 2: Requirements & Scope

| Document | Purpose | Status |
|----------|---------|--------|
| [03-product-scope.md](03-product-scope.md) | Feature classification (P0/P1/P2/P3), explicit inclusions and exclusions | 🔄 In Progress |
| [04-workflows.md](04-workflows.md) | Complete clinic workflows from patient registration to billing | 🔄 In Progress |
| [16-open-questions.md](16-open-questions.md) | Unresolved decisions requiring clarification or research | 📝 Tracking |
| [17-risk-register.md](17-risk-register.md) | Technical, operational, and business risks with mitigations | 📝 Tracking |

### Phase 3: Architecture & Design

| Document | Purpose | Status |
|----------|---------|--------|
| [06-architecture-decisions.md](06-architecture-decisions.md) | Architectural Decision Records (ADRs) with context and rationale | 🔄 In Progress |
| [05-domain-model.md](05-domain-model.md) | Complete entity model, relationships, validation rules | 🔄 In Progress |
| [07-database-design.md](07-database-design.md) | Schema design, constraints, indexes, migrations | ⏳ Pending |
| [09-security-authorization.md](09-security-authorization.md) | Authentication, authorization, roles, branch access | ⏳ Pending |

### Phase 4: Financial & Clinical Specifications

| Document | Purpose | Status |
|----------|---------|--------|
| [08-financial-workflows.md](08-financial-workflows.md) | Complete billing cycle, cash management, reconciliation, reporting | 🔄 In Progress |
| [10-frontend-specification.md](10-frontend-specification.md) | Page inventory, components, workflows, i18n/RTL | ⏳ Pending |
| [11-backend-api-specification.md](11-backend-api-specification.md) | API endpoints, contracts, validation, authorization | ⏳ Pending |

### Phase 5: Implementation Planning

| Document | Purpose | Status |
|----------|---------|--------|
| [14-implementation-roadmap.md](14-implementation-roadmap.md) | Dependency-aware task breakdown with acceptance criteria | ⏳ Pending |
| [15-traceability-matrix.md](15-traceability-matrix.md) | Requirements → Features → Tasks → Tests mapping | ⏳ Pending |
| [12-testing-strategy.md](12-testing-strategy.md) | Test coverage, critical scenarios, acceptance testing | ⏳ Pending |
| [13-deployment-recovery.md](13-deployment-recovery.md) | Local installation, backup/restore, disaster recovery | ⏳ Pending |

### Phase 6: Governance & Tracking

| Document | Purpose | Status |
|----------|---------|--------|
| [19-decision-log.md](19-decision-log.md) | Chronological record of all decisions with change history | 📝 Tracking |
| [20-kaizen-log.md](20-kaizen-log.md) | Continuous improvement observations and approved actions | 📝 Tracking |
| [21-current-state.md](21-current-state.md) | Live progress tracking, current milestone, next recommended task | 📝 Tracking |

---

## Status Legend

- ✅ **Complete**: Document finished and reviewed
- 🔄 **In Progress**: Actively being written
- ⏳ **Pending**: Waiting on dependencies or research
- 📝 **Tracking**: Living document updated continuously

---

## How to Use This Package

### For Product Owners
1. Start with [03-product-scope.md](03-product-scope.md) to understand what will and won't be built
2. Review [04-workflows.md](04-workflows.md) to validate clinic operations
3. Check [16-open-questions.md](16-open-questions.md) for decisions needed

### For Architects
1. Read [06-architecture-decisions.md](06-architecture-decisions.md) for all ADRs
2. Review [05-domain-model.md](05-domain-model.md) and [07-database-design.md](07-database-design.md)
3. Check [17-risk-register.md](17-risk-register.md) for technical risks

### For Developers
1. Start with [21-current-state.md](21-current-state.md) to see current progress
2. Review [14-implementation-roadmap.md](14-implementation-roadmap.md) for task details
3. Check [15-traceability-matrix.md](15-traceability-matrix.md) to understand requirements lineage
4. Reference [11-backend-api-specification.md](11-backend-api-specification.md) and [10-frontend-specification.md](10-frontend-specification.md)

### For QA Engineers
1. Review [12-testing-strategy.md](12-testing-strategy.md)
2. Check [15-traceability-matrix.md](15-traceability-matrix.md) for test coverage
3. Reference [04-workflows.md](04-workflows.md) for end-to-end scenarios

---

## Key Principles

### 1. Evidence-Based Decision Making
Every architectural decision is supported by:
- Source code analysis from reference implementations
- Documented trade-offs and alternatives considered
- Clear rationale tied to ClinicCare's specific requirements

### 2. Branch Model (NOT Multi-Tenancy)
- **One doctor, one installation**
- Patient records shared across the doctor's branches
- Appointments, queues, encounters are branch-specific
- No Organization hierarchy or tenant isolation

### 3. Pragmatic Architecture
- Clean Architecture preserved (layer separation)
- CQRS used selectively where it adds value
- AutoMapper for complex mappings only
- Repository pattern evaluated per feature
- **Patterns serve the product, not vice versa**

### 4. Simplified FHIR-Inspired Model
- CARE's FHIR-aligned entities provide valuable reference
- ClinicCare simplifies for small-clinic context
- Concrete columns for queryable data
- JSON fields for true extensions only

### 5. Financial Integrity First
- Billing workflow must be transactionally consistent
- Historical records immutable (corrections, not destructive edits)
- Decimal precision appropriate for currency
- Cash reconciliation built-in

### 6. Kaizen Continuous Improvement
- Plan is living document, not write-once artifact
- Changes require documented rationale
- Discoveries during implementation feed back to planning
- Progress tracked in [21-current-state.md](21-current-state.md)

---

## Non-Negotiable Constraints

### ✅ Must Have
- Local-first, offline-capable deployment
- Single SQLite database (with PostgreSQL migration path)
- Arabic + English with full RTL support
- Patient identity shared across branches
- Branch-scoped appointments, queues, billing
- Financial data integrity and audit trail

### ❌ Must NOT Have
- Multi-tenancy or Organization hierarchy
- SaaS subscription billing
- Tenant provisioning or enterprise facility networks
- Hospital bed management, ICU, inpatient workflows
- Assumptions about internet connectivity for core operations

---

## Research Sources

### Primary Reference (Deep Investigation)
- **CARE Clinic v0.1.15** (commit ef9617d)
  - Repository: `reference/care_clinic/`
  - Pinned CARE Backend: Branch `ENG-737`
  - Pinned CARE Frontend: Branch `bodhi/questionnaire-actions`
  - Deployment: Wails (Go + React) orchestrating Docker Compose
  - Stack: PostgreSQL, Redis, Minio, Caddy WAF, CARE backend/frontend

- **CARE Backend EMR Module**
  - Repository: `reference/care/`
  - 40+ FHIR-aligned entities
  - Complete scheduling, queue, billing, clinical workflows
  - Complex organization and access control model

### Additional References (Comparative Analysis)
- **OpenEMR**: `reference/openemr/` (if cloned successfully)
- **OpenMRS**: Targeted investigation via documentation
- **GNU Health**: Targeted investigation via documentation
- **TPT Doctor**: Vietnam clinic system analysis

### Existing Implementation
- **ClinicCare Foundation**: `d:\Clinic\backend\` and `d:\Clinic\frontend\`
  - Clean Architecture (Domain/Application/Infrastructure/Api)
  - ASP.NET Core + React
  - Milestone 0 complete, zero clinical features

---

## Change Control

### Minor Updates (No Approval Required)
- Typo corrections
- Clarifications that don't change meaning
- Additional examples or diagrams
- Status updates in tracking documents

### Major Changes (Require Documentation)
- Scope additions or removals
- Architectural pattern changes
- Database model changes
- Workflow modifications
- Technology stack changes

**Process**: Document in [19-decision-log.md](19-decision-log.md) with:
- Change description
- Rationale
- Impact analysis
- Affected documents
- Date and author

---

## Completion Criteria

This planning phase is complete when:

1. ✅ Reference repositories cloned and verified at correct versions
2. ✅ CARE domain model fully documented with evidence
3. 🔄 Comparative research completed (OpenEMR, OpenMRS, GNU Health)
4. ⏳ Product scope finalized (P0/P1/P2/P3 with justification)
5. ⏳ All workflows documented with state transitions
6. ⏳ Domain model designed and cross-checked
7. ⏳ ADRs documented for all major architecture decisions
8. ⏳ Database schema designed with integrity constraints
9. ⏳ Financial workflows fully specified with reconciliation
10. ⏳ Security and authorization model complete
11. ⏳ Frontend pages inventoried with acceptance criteria
12. ⏳ Backend API specified with validation rules
13. ⏳ Testing strategy defined with critical scenarios
14. ⏳ Deployment and recovery procedures documented
15. ⏳ Implementation roadmap with dependency-aware tasks
16. ⏳ Traceability matrix linking requirements to tests
17. ⏳ All documents cross-checked for consistency
18. ⏳ Open questions resolved or explicitly deferred
19. ⏳ Risks identified with mitigation strategies

---

## Next Actions

See [21-current-state.md](21-current-state.md) for current progress and next recommended tasks.

---

**Contact**: This planning package is maintained as part of the ClinicCare project.  
**Last Updated**: January 2025
