# 19. Decision Log

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [06-architecture-decisions.md](./06-architecture-decisions.md), [16-open-questions.md](./16-open-questions.md)

---

## 1. Overview

### 1.1 Purpose
Chronological log of all significant decisions made during ClinicCare planning and implementation, capturing rationale, alternatives considered, and outcomes.

### 1.2 Decision Categories
- **Architecture:** Technical architecture, design patterns
- **Product:** Feature prioritization, scope decisions
- **Process:** Development methodology, workflows
- **Technology:** Technology stack, tool selection
- **Operational:** Deployment, backup strategies

### 1.3 Decision Template
```markdown
## DEC-XXX: Decision Title
**Date:** YYYY-MM-DD  
**Category:** Architecture | Product | Process | Technology | Operational  
**Status:** ✅ Approved | 🔵 Proposed | ❌ Rejected | 🔄 Superseded  
**Stakeholders:** List of people involved  
**Context:** Why this decision was needed  
**Decision:** What was decided  
**Alternatives Considered:**
1. Option A - Reason rejected
2. Option B - Reason rejected
**Rationale:** Why this decision was chosen  
**Consequences:** Implications (positive and negative)  
**Related ADRs:** Links to architecture decisions  
**Review Date:** When to revisit this decision  
```

---

## 2. Decision Log

### DEC-001: Adopt Hybrid Research Approach
**Date:** 2026-10-19 (early planning phase)  
**Category:** Process  
**Status:** ✅ Approved  
**Stakeholders:** User, Planning Agent  

**Context:**  
Three options for research approach:
- Option A: Audit existing ClinicCare only
- Option B: Reference care_clinic v0.1.15 as source of truth
- Option C: Hybrid approach (audit existing + care_clinic + independent research)

**Decision:**  
Adopt Option C (Hybrid Approach):
1. Audit existing ClinicCare implementation
2. Clone and analyze care_clinic v0.1.15 (commit ef9617d)
3. Conduct independent research (OpenEMR, OpenMRS, GNU Health)
4. Create comprehensive planning documents

**Alternatives Considered:**
1. **Option A (Audit only):** Too limited, misses proven patterns from care_clinic
2. **Option B (care_clinic only):** Risk of blindly copying without understanding, may inherit unnecessary complexity

**Rationale:**
- care_clinic is proven (v0.1.15 released), provides design guidance
- Independent research fills gaps, validates care_clinic decisions
- Comprehensive planning reduces implementation risk

**Consequences:**
- ✅ **Positive:** Evidence-based design, validated patterns, comprehensive planning
- ✅ **Positive:** Flexibility to simplify for small clinic context
- ⚠️ **Negative:** Longer planning phase (2-3 weeks), but saves time in implementation

**Related ADRs:** All ADRs in 06-architecture-decisions.md derived from this research  
**Review Date:** N/A (planning decision, not revisited)

---

### DEC-002: Expand Billing Model from 4 to 7 Entities
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent, informed by OpenMRS/OpenEMR research  

**Context:**  
Initial domain model (05-domain-model.md v1.0) had 4 billing entities: Invoice, InvoiceItem, Payment, PaymentMethod. Research into OpenMRS Cashier module and OpenEMR ar_session revealed missing cash drawer management patterns.

**Decision:**  
Expand to 7 entities for v1.0:
1. Invoice
2. InvoiceItem
3. Payment
4. CashDrawer
5. Expense
6. CashMovement (deferred to v1.1)
7. (PaymentMethod removed - use enum instead)

**Alternatives Considered:**
1. **Keep 4 entities:** Insufficient for cash reconciliation, no shift management
2. **Add all 7 immediately:** CashMovement complex, defer to v1.1

**Rationale:**
- Cash drawer reconciliation critical for small clinics (cash-heavy operations)
- OpenMRS Timesheet pattern (CashPoint + shift management) proven in production
- Expense tracking required for accurate end-of-day reconciliation

**Consequences:**
- ✅ **Positive:** Accurate cash management, discrepancy detection, audit trail
- ✅ **Positive:** Matches real-world clinic workflows
- ⚠️ **Negative:** Increased complexity (3 more entities), but justified by business need

**Related ADRs:** ADR-005 (Simplified Billing), ADR-007 (SQLite then PostgreSQL)  
**Related Documents:** 05-domain-model.md (updated), 08-financial-workflows.md  
**Review Date:** After v1.0 deployment (validate cash drawer workflows)

---

### DEC-003: SQLite for v1.0, PostgreSQL Migration Path v1.1+
**Date:** 2026-10-20  
**Category:** Technology  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
Database technology choice: SQLite (simple, embedded) vs PostgreSQL (robust, scalable) vs stay-on-SQLite-indefinitely.

**Decision:**  
Use SQLite for v1.0, provide PostgreSQL migration path in v1.1+:
- v1.0: SQLite (Data Source=data/clinic.db)
- v1.1: Optional migration script (SQLite → PostgreSQL)
- Trigger points: DB size >10GB, concurrency issues, need advanced features

**Alternatives Considered:**
1. **SQLite only:** May hit limits (file size, concurrency), no migration path
2. **PostgreSQL from start:** Over-engineered for small clinic, requires PostgreSQL server management

**Rationale:**
- Small clinic likely <1000 patients in year 1 (SQLite sufficient)
- care_clinic uses PostgreSQL, but deployed via Docker (not desktop-first)
- Simplicity in v1.0: no separate database server, single file backup
- EF Core abstracts database, migration relatively straightforward

**Consequences:**
- ✅ **Positive:** Simpler deployment, lower resource requirements
- ✅ **Positive:** Faster development (no Docker Postgres setup initially)
- ⚠️ **Negative:** Migration effort later if PostgreSQL needed
- ⚠️ **Negative:** SQLite concurrency limits (mitigated by WAL mode)

**Related ADRs:** ADR-007 (SQLite → PostgreSQL)  
**Related Documents:** 07-database-design.md, 13-deployment-recovery.md  
**Review Date:** v1.0 deployment + 3 months (assess SQLite performance)

---

### DEC-004: Defer Multi-Invoice Payment to v2.0
**Date:** 2026-10-20  
**Category:** Product  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
Question: Should v1.0 support single payment against multiple invoices (e.g., patient pays $500 covering 3 invoices)?

**Decision:**  
Keep 1:1 Payment → Invoice relationship in v1.0:
- One payment pays one invoice (can be partial payment)
- Multi-invoice payment deferred to v2.0
- No Account entity, no PaymentAllocation table

**Alternatives Considered:**
1. **Implement multi-invoice payment in v1.0:** Requires Account entity, PaymentAllocation table, complex UI
2. **Add Account entity but no multi-invoice:** Unnecessary complexity for single-invoice payments

**Rationale:**
- Small clinic workflow: typically bill per visit (one invoice)
- Multi-invoice payment rare (patient with outstanding balance across visits)
- Simplifies v1.0: fewer entities, simpler payment form, faster development

**Consequences:**
- ✅ **Positive:** Simpler implementation, faster v1.0 delivery
- ✅ **Positive:** Easier to understand for users
- ⚠️ **Negative:** Workaround needed if patient wants to pay multiple invoices (record separate payments)
- ⚠️ **Negative:** Refactoring required in v2.0 if feature added

**Related ADRs:** ADR-005 (Simplified Billing)  
**Related Documents:** 05-domain-model.md, 16-open-questions.md (Q6 - resolved)  
**Review Date:** v1.0 user feedback (assess need for multi-invoice payment)

---

### DEC-005: ICD-10 Codes - 500 Common Codes v1.0, Full Database v1.1
**Date:** 2026-10-20  
**Category:** Product  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
ICD-10 code database has ~20,000 codes (5MB+). Should v1.0 include all codes or subset?

**Decision:**  
Seed ~500 common codes in v1.0:
- Most frequently used in primary care (headache, fever, hypertension, diabetes, etc.)
- Allow free-text custom diagnosis if code not found
- Full ICD-10 database in v1.1 if users request

**Alternatives Considered:**
1. **Full ICD-10 database (20K codes):** Large seed data, slower search, unnecessary for small clinic
2. **No ICD-10 codes, free-text only:** Loses coding benefit (reports, billing codes)

**Rationale:**
- Primary care clinics use ~100-200 codes regularly
- 500 codes cover 95% of common diagnoses
- Free-text fallback ensures no workflow blockage
- Smaller database = faster search, less storage

**Consequences:**
- ✅ **Positive:** Faster implementation, smaller database
- ✅ **Positive:** Adequate for small clinic use case
- ⚠️ **Negative:** Users may need to enter custom diagnoses occasionally
- ⚠️ **Negative:** Expansion needed if specialty clinic (dermatology, cardiology)

**Related Documents:** 05-domain-model.md (Section 5.9), 10-frontend-specification.md (DiagnosisSearch)  
**Related ADRs:** ADR-006 (Concrete Entities + JSON for flexible data)  
**Review Date:** v1.0 user feedback (assess code coverage)

---

### DEC-006: No Multi-Tenancy in v1.0 (Single Clinic)
**Date:** 2026-10-19 (project directive)  
**Category:** Product  
**Status:** ✅ Approved  
**Stakeholders:** User (explicit directive), Planning Agent  

**Context:**  
User directive: "No multi-tenancy. One doctor, one installation. Patient records shared across branches, but appointments/queue are branch-specific."

**Decision:**  
Single-tenant architecture in v1.0:
- No TenantId in database schema
- One installation per clinic (not SaaS)
- Multi-branch support (v1.1) within same tenant

**Alternatives Considered:**
1. **Multi-tenant SaaS:** Out of scope per user directive
2. **No branch support at all:** Limits growth, user explicitly wants multi-branch

**Rationale:**
- Simplifies architecture (no tenant isolation)
- Matches deployment model (desktop app via care_clinic)
- Focus on single-doctor clinic use case

**Consequences:**
- ✅ **Positive:** Simpler database schema, faster development
- ✅ **Positive:** No tenant management overhead
- ⚠️ **Negative:** Cannot easily convert to SaaS later (major refactor needed)
- ⚠️ **Negative:** Each clinic needs separate installation (not scalable for SaaS)

**Related ADRs:** ADR-002 (Simple Branch Model)  
**Related Documents:** 03-product-scope.md (Constraints section)  
**Review Date:** N/A (fundamental architecture decision)

---

### DEC-007: Selective CQRS (Not Universal)
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
CQRS pattern choice: Universal CQRS (separate models for all reads/writes) vs Selective CQRS (only where needed) vs No CQRS.

**Decision:**  
Selective CQRS:
- Use CQRS (MediatR) for complex operations (multi-entity workflows)
- Simple CRUD: Commands/Queries can share DTOs
- Read models optimized only where performance critical (e.g., queue dashboard)

**Alternatives Considered:**
1. **Universal CQRS:** Verbose, over-engineered for small clinic
2. **No CQRS:** Loses MediatR pipeline benefits (logging, validation, authorization)

**Rationale:**
- MediatR provides clean separation, testability
- Small clinic doesn't need separate read/write databases
- Pragmatic approach balances simplicity and structure

**Consequences:**
- ✅ **Positive:** Clean architecture, testable handlers
- ✅ **Positive:** Less boilerplate than universal CQRS
- ⚠️ **Negative:** Inconsistent patterns (some shared DTOs, some separate)

**Related ADRs:** ADR-001 (Selective CQRS)  
**Related Documents:** 06-architecture-decisions.md  
**Review Date:** After v1.0 (assess if pattern works)

---

### DEC-008: No Repository Pattern (Direct DbContext Access)
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
Should we use Repository pattern (IRepository<T>) or access DbContext directly in handlers?

**Decision:**  
No Repository abstraction:
- Handlers access ApplicationDbContext directly
- EF Core DbSet<T> already acts as repository
- Less boilerplate, simpler code

**Alternatives Considered:**
1. **Generic Repository (IRepository<T>):** Adds abstraction layer, hides EF Core features
2. **Per-Entity Repositories (IPatientRepository):** Too much boilerplate for small project

**Rationale:**
- EF Core DbContext is already unit-of-work pattern
- Modern EF Core encourages direct DbContext access
- Testing: In-memory database provider for tests (no mocking needed)

**Consequences:**
- ✅ **Positive:** Less code, faster development
- ✅ **Positive:** Full access to EF Core features (Include, AsNoTracking, etc.)
- ⚠️ **Negative:** Handlers coupled to EF Core (but unlikely to switch ORM)

**Related ADRs:** ADR-008 (Direct DbContext Access)  
**Related Documents:** 06-architecture-decisions.md  
**Review Date:** N/A (standard modern practice)

---

### DEC-009: Enum-Based Entity IDs (Not Integer Auto-Increment)
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
Entity ID format: Integer auto-increment (1, 2, 3...) vs GUID vs Prefixed GUID (pat_abc123).

**Decision:**  
Use prefixed GUID format:
- Patient: `pat_<guid>`
- Encounter: `enc_<guid>`
- Invoice: `inv_<guid>`
- etc.

Stored as TEXT in database, generated on creation.

**Alternatives Considered:**
1. **Integer auto-increment:** Simple, but IDs guessable, no semantic meaning
2. **Plain GUID:** Unique, but no semantic meaning in logs/URLs

**Rationale:**
- Prefix aids debugging (know entity type at a glance)
- GUIDs prevent enumeration attacks (security)
- No collision risk across branches

**Consequences:**
- ✅ **Positive:** Better debugging, logs readable
- ✅ **Positive:** Security (non-sequential IDs)
- ⚠️ **Negative:** Slightly larger storage (TEXT vs INT), negligible impact

**Related ADRs:** ADR-003 (Enum-Based Identifiers)  
**Related Documents:** 07-database-design.md  
**Review Date:** N/A (design decision)

---

### DEC-010: Integrated Queue (No Separate Queue Service)
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ✅ Approved  
**Stakeholders:** Planning Agent  

**Context:**  
Queue management: Separate microservice (care_clinic pattern) vs integrated into main API.

**Decision:**  
Integrated queue:
- QueueToken entity in main database
- Queue endpoints in main API
- Real-time updates via polling (WebSocket deferred to v1.1)

**Alternatives Considered:**
1. **Separate Queue Service:** Over-engineered for small clinic, adds deployment complexity
2. **In-Memory Queue (Redis):** Loses data on restart, not durable

**Rationale:**
- Small clinic: <50 tokens/day, no need for separate service
- Simplifies deployment (one service, one database)
- Durable queue (database-backed)

**Consequences:**
- ✅ **Positive:** Simpler deployment, fewer moving parts
- ✅ **Positive:** Queue data persisted in database (reports possible)
- ⚠️ **Negative:** Polling-based updates (10s delay), but acceptable for small clinic

**Related ADRs:** ADR-004 (Integrated Queue)  
**Related Documents:** 04-workflows.md (Queue workflow)  
**Review Date:** v1.0 user feedback (assess if real-time needed)

---

### DEC-011: Default Mode - Autopilot (Not Supervised)
**Date:** 2026-10-21  
**Category:** Process  
**Status:** ✅ Approved  
**Stakeholders:** User, Planning Agent  

**Context:**  
User directive: "Continue automatically. You already have approval to complete the full research-and-planning package. Do not ask me again whether to proceed."

**Decision:**  
Proceed in Autopilot mode for planning phase:
- Complete all 21 planning documents without asking for confirmation
- User can interrupt at any time (system feature)
- Supervised mode for implementation (user can review hunks)

**Alternatives Considered:**
1. **Supervised mode:** Slower, unnecessary for planning documents (no code changes)

**Rationale:**
- Planning phase: low risk (documentation only)
- User explicitly granted autonomy
- Faster completion (no wait for approvals)

**Consequences:**
- ✅ **Positive:** Faster planning completion
- ✅ **Positive:** User can review all documents at end
- ⚠️ **Negative:** User may miss errors until final review (mitigated by final audit)

**Related Documents:** N/A (process decision)  
**Review Date:** N/A (applies to planning only)

---

## 3. Pending Decisions (Open Questions)

### PENDING-001: Real-Time Queue Updates - WebSocket vs Polling
**Date:** 2026-10-21  
**Category:** Technology  
**Status:** 🔵 Proposed  
**Source:** 16-open-questions.md (Q1)  

**Context:**  
Queue dashboard needs real-time updates. Options: WebSocket (SignalR), Server-Sent Events (SSE), or polling.

**Proposal:**  
Polling (10-second interval) for v1.0, upgrade to WebSocket in v1.1 if users request.

**Alternatives:**
1. **WebSocket (SignalR):** Real-time, but requires SignalR setup, testing
2. **SSE:** One-way updates, simpler than WebSocket
3. **Polling:** Simplest, but higher latency (10s)

**Pending Decision:**  
Validate with users if 10-second delay acceptable. If not, accelerate WebSocket implementation.

**Decision Date:** Sprint 2 Week 1 (after queue implementation)

---

### PENDING-002: Offline Support for Desktop App
**Date:** 2026-10-21  
**Category:** Product  
**Status:** 🔵 Proposed  
**Source:** 10-frontend-specification.md (Q2)  

**Context:**  
Should frontend work offline with local storage (sync when back online)?

**Proposal:**  
No offline support in v1.0. Desktop app likely has stable internet. Revisit if users request.

**Alternatives:**
1. **Full offline support:** Complex, requires sync logic, conflict resolution
2. **Partial offline (read-only cache):** Still complex
3. **No offline:** Simplest, acceptable for desktop with stable internet

**Pending Decision:**  
Assess user environment (internet reliability) before v1.0 release.

**Decision Date:** v1.0 beta testing (Week 8)

---

### PENDING-003: Sprint Duration - 1 Week vs 2 Weeks
**Date:** 2026-10-21  
**Category:** Process  
**Status:** 🔵 Proposed  
**Source:** 14-implementation-roadmap.md (Q1)  

**Context:**  
Sprint length choice: 1-week sprints (faster feedback) vs 2-week sprints (less overhead).

**Proposal:**  
2-week sprints for v1.0 (less disruptive), 1-week sprints for v1.1+ (faster iteration).

**Alternatives:**
1. **1-week sprints:** More frequent demos, faster feedback, but more planning overhead
2. **3-week sprints:** Too long, slower feedback

**Pending Decision:**  
Start with 2-week sprints, adjust based on team feedback after Sprint 1.

**Decision Date:** After Sprint 1 (Week 3)

---

## 4. Rejected Decisions

### REJECTED-001: Use CARE's Full 40+ Entity Domain Model
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ❌ Rejected  
**Stakeholders:** Planning Agent  

**Context:**  
CARE backend has 40+ domain entities (Patient, Encounter, Bed, FacilityUser, AssetLocation, etc.). Should we use same model?

**Decision:**  
**Rejected.** Simplify to 29 entities for ClinicCare.

**Rationale:**
- CARE is hospital system (beds, facilities, ambulances) - out of scope for small clinic
- ClinicCare focus: outpatient clinic (no beds, no ambulances)
- Simplified model = faster development, easier to understand

**Consequences:**
- Cannot easily migrate to CARE backend later (different schema)
- Acceptable trade-off for simplicity

**Related Documents:** 01-repository-audit.md, 05-domain-model.md  

---

### REJECTED-002: Universal CQRS with Separate Read/Write Databases
**Date:** 2026-10-20  
**Category:** Architecture  
**Status:** ❌ Rejected  
**Stakeholders:** Planning Agent  

**Context:**  
Should we use CQRS with separate read database (e.g., Elasticsearch for queries, PostgreSQL for writes)?

**Decision:**  
**Rejected.** Use single database with selective CQRS (separate command/query handlers only).

**Rationale:**
- Small clinic doesn't need separate databases (no scale requirements)
- Over-engineered for <10K patients
- Adds operational complexity (sync, consistency)

**Consequences:**
- Simpler architecture, faster development
- Cannot scale to millions of records (not required)

**Related ADRs:** ADR-001 (Selective CQRS)  
**Related Documents:** 06-architecture-decisions.md  

---

## 5. Superseded Decisions

### DEC-004-SUPERSEDED: 4-Entity Billing Model
**Date:** 2026-10-19  
**Category:** Architecture  
**Status:** 🔄 Superseded by DEC-002  
**Stakeholders:** Planning Agent  

**Original Decision:**  
Use 4-entity billing model: Invoice, InvoiceItem, Payment, PaymentMethod.

**Superseded By:**  
DEC-002 (7-entity model with CashDrawer, Expense)

**Reason for Change:**  
Research revealed cash drawer management critical for small clinics. 4-entity model insufficient for reconciliation.

**Date Superseded:** 2026-10-20  

---

## 6. Decision Statistics

**Total Decisions:** 11 (approved), 3 (pending), 2 (rejected), 1 (superseded)

**By Category:**
- Architecture: 5 approved, 1 pending, 2 rejected
- Product: 3 approved, 1 pending
- Process: 2 approved, 1 pending
- Technology: 1 approved

**By Status:**
- ✅ Approved: 11
- 🔵 Proposed (Pending): 3
- ❌ Rejected: 2
- 🔄 Superseded: 1

---

## 7. Related Documents

- [06-architecture-decisions.md](./06-architecture-decisions.md) - Formal ADRs (subset of decisions)
- [16-open-questions.md](./16-open-questions.md) - Questions awaiting decisions
- All planning documents - Informed by these decisions

---

## 8. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial decision log (11 approved, 3 pending, 2 rejected) |

---

**Document Confidence:** HIGH  
**Evidence Base:** Decisions documented throughout planning process  
**Assumptions:** All significant decisions captured (may be minor decisions not logged)  
**Next Review:** Ongoing (log decisions as made during implementation)
