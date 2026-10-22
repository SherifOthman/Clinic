# Architecture Decision Records (ADRs)

**Document ID**: DOC-006  
**Version**: 1.0  
**Date**: January 2025  
**Status**: Draft - Requires Approval

---

## Purpose

This document contains all Architectural Decision Records (ADRs) for ClinicCare. Each ADR documents a significant architectural decision, the context that led to it, alternatives considered, and the rationale for the chosen approach.

**Format**: Each ADR follows a consistent structure:
- **Status**: Proposed / Accepted / Superseded / Deprecated
- **Context**: Why this decision is needed
- **Decision**: What we decided
- **Alternatives Considered**: Other options evaluated
- **Rationale**: Why this decision was made
- **Consequences**: Trade-offs and implications
- **Evidence**: Research findings supporting the decision
- **Review Conditions**: What would make us reconsider

---

## ADR Index

| ADR | Title | Status | Priority |
|-----|-------|--------|----------|
| [ADR-001](#adr-001-clean-architecture-with-selective-cqrs) | Clean Architecture with Selective CQRS | 📝 Proposed | 🔴 Critical |
| [ADR-002](#adr-002-branch-model-not-organization-hierarchy) | Branch Model (Not Organization Hierarchy) | 📝 Proposed | 🔴 Critical |
| [ADR-003](#adr-003-simplified-patient-identifier-system) | Simplified Patient Identifier System | 📝 Proposed | 🟡 High |
| [ADR-004](#adr-004-integrated-queue-appointment-workflow) | Integrated Queue-Appointment Workflow | 📝 Proposed | 🟡 High |
| [ADR-005](#adr-005-simplified-billing-flow) | Simplified Billing Flow | 📝 Proposed | 🔴 Critical |
| [ADR-006](#adr-006-concrete-columns-vs-json-fields) | Concrete Columns vs JSON Fields | 📝 Proposed | 🟡 High |
| [ADR-007](#adr-007-sqlite-with-postgresql-migration-path) | SQLite with PostgreSQL Migration Path | 📝 Proposed | 🟡 High |
| [ADR-008](#adr-008-direct-dbcontext-access-pattern) | Direct DbContext Access Pattern | 📝 Proposed | 🟢 Medium |

---

## ADR-001: Clean Architecture with Selective CQRS

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🔴 Critical

### Context

The existing ClinicCare implementation uses:
- **Clean Architecture**: 4-layer separation (Domain/Application/Infrastructure/Api)
- **CQRS**: Every operation is a Command or Query via MediatR
- **MediatR Pipeline**: All requests go through validation and other behaviors
- **Handler Pattern**: Each operation has a dedicated handler class

CARE (the reference implementation) uses:
- **Django MVT**: Model-View-Template pattern
- **Direct ORM Access**: Views query Django ORM directly
- **No CQRS**: Simple request → view → model → response
- **DRF ViewSets**: REST framework handles CRUD operations

The question: **Should ClinicCare use CQRS universally or selectively?**

### Decision

**Use Clean Architecture with Selective CQRS:**

1. **Preserve Clean Architecture** - Layer separation has value:
   - Domain remains dependency-free
   - Business logic isolated from infrastructure
   - Clear boundaries between concerns

2. **Apply CQRS Selectively** based on operation complexity:
   - **Complex Operations** → CQRS + MediatR:
     - Multi-step workflows (e.g., complete encounter with billing)
     - Operations with significant business rules
     - Operations requiring transaction coordination
     - Operations benefiting from pipeline behaviors (validation, authorization, audit)
   
   - **Simple Operations** → Direct Service/Repository:
     - Basic CRUD (e.g., update patient phone number)
     - Simple queries (e.g., get patient by ID)
     - List operations with basic filtering
     - Operations with minimal business logic

3. **Keep MediatR Pipeline Behaviors** for operations that use it:
   - Validation behavior (FluentValidation)
   - Authorization behavior (future)
   - Audit logging behavior (future)
   - Transaction behavior (future)

### Alternatives Considered

**Option A: Universal CQRS (Current Implementation)**
- ✅ Consistent pattern across all operations
- ✅ All operations benefit from pipeline
- ✅ Easy to add cross-cutting concerns
- ❌ Verbose for simple operations (5+ files for basic CRUD)
- ❌ Increased cognitive load
- ❌ More abstractions than necessary
- ❌ CARE proves this isn't required for complexity management

**Option B: No CQRS (Pure Clean Architecture)**
- ✅ Simpler, less abstraction
- ✅ Fewer files and classes
- ✅ Direct service layer pattern
- ❌ Loses MediatR pipeline benefits
- ❌ Cross-cutting concerns harder to apply uniformly
- ❌ Complex operations still need structure

**Option C: Selective CQRS (RECOMMENDED)**
- ✅ Best of both worlds
- ✅ Complexity matched to operation complexity
- ✅ Pipeline benefits where they add value
- ✅ Simple operations remain simple
- ⚠️ Requires judgment on which pattern to use
- ⚠️ Mixed patterns in codebase (mitigated by clear guidelines)

### Rationale

**Evidence from CARE**:
- Manages complex hospital workflows WITHOUT CQRS
- 40+ entities, complex scheduling/billing/clinical workflows
- Direct Django ORM access is sufficient
- Proves CQRS is not mandatory for healthcare applications

**Existing Implementation**:
- LoginCommand: 3 classes (Command, Validator, Handler) for simple authentication
- GetCurrentUserQuery: 2 classes (Query, Handler) for database lookup
- Overhead is 50-200% for these simple operations

**Selective CQRS Guideline**:

Use CQRS when operation has **3 or more** of:
- [ ] Multiple database entities affected
- [ ] Complex validation rules (beyond simple field validation)
- [ ] Business rule enforcement
- [ ] Transaction coordination across entities
- [ ] Side effects (notifications, audit, events)
- [ ] Authorization checks beyond simple role checks
- [ ] Operation would benefit from pipeline behaviors

Otherwise, use direct service/repository pattern.

**Examples**:

| Operation | Pattern | Justification |
|-----------|---------|---------------|
| Create Encounter with Billing | CQRS | Multi-entity, business rules, transaction, billing side effects |
| Complete Appointment → Queue | CQRS | Status transitions, multiple entities, queue integration |
| Update Patient Phone | Service | Single field update, simple validation |
| Get Patient List | Service/Repository | Simple query with filters |
| Generate Invoice | CQRS | Multi-entity, calculations, locking rules, payment tracking |
| Record Payment | CQRS | Financial transaction, account updates, audit trail |

### Consequences

**Positive**:
- Reduced boilerplate for 60-70% of operations (estimated)
- Clearer code for simple operations
- MediatR benefits retained where valuable
- Flexibility to choose appropriate pattern

**Negative**:
- Developers must understand when to use each pattern
- Mixed patterns in codebase (requires guidelines)
- Potential inconsistency if guidelines not followed

**Mitigation**:
- Document clear decision criteria (3+ complexity factors)
- Provide examples in implementation guide
- Code review checks for pattern appropriateness
- Refactoring allowed when complexity increases

### Evidence

**Source**: 01-repository-audit.md Section 4.4

CARE Backend:
- No CQRS, no MediatR equivalent
- Direct ORM access in views
- Handles complex workflows successfully

LoginCommand Analysis:
```
Files Required:
1. LoginCommand.cs (Command + DTOs)
2. LoginCommandValidator.cs (Validation)
3. LoginCommandHandler.cs (Handler)

vs Direct Service:
1. AuthService.cs (Method)

Result: 3x file overhead for simple operation
```

**Conclusion**: CQRS overhead justified only when operation complexity warrants it.

### Review Conditions

Reconsider if:
- Simple operations consistently need complex behaviors
- Pattern confusion causes significant bugs
- Team finds selective approach too difficult
- Performance issues arise from pattern choice

---

## ADR-002: Branch Model (Not Organization Hierarchy)

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🔴 Critical

### Context

**Requirement** (from directive):
- One doctor, one application installation
- Doctor may operate one or more branches
- All branches belong to same doctor
- Patient records shared across branches
- Appointments, queues, encounters are branch-specific
- **NO multi-tenancy, NO organization hierarchy, NO tenant isolation**

**CARE's Approach**:
- Complex `Organization` hierarchy (parent-child relationships, caching)
- `FacilityOrganization` for departments within facilities
- `PatientOrganization` M2M for patient access by organizations
- `OrganizationUser` for user access with roles per organization
- Caching layers: `parent_cache`, `organization_cache`, `cached_parent_json`
- Designed for large hospitals with departmental structures

**The Question**: Should ClinicCare implement CARE's Organization model?

### Decision

**Implement Simple Branch Model:**

```csharp
public class Branch : AuditableEntity
{
    public string Name { get; set; }           // "Main Clinic", "Downtown Branch"
    public string Code { get; set; }           // Short code: "MAIN", "DT"
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }        // Default branch for new data
    public BranchSettings Settings { get; set; } // JSON: working hours, etc.
}

// User-Branch access (which branches can user work at?)
public class UserBranch
{
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public bool IsActive { get; set; }
}
```

**Data Ownership Rules**:

| Entity | Ownership | Rationale |
|--------|-----------|-----------|
| Patient | Shared (no BranchId) | Patient identity is global to doctor |
| PatientIdentifier | Branch-scoped OR global | MRN can be per-branch or system-wide |
| Appointment | Branch-specific | Appointments belong to one branch |
| Queue | Branch-specific | Each branch has its own queue |
| Encounter | Branch-specific | Visit happens at one branch |
| Prescription | Branch-specific (via Encounter) | Issued at one branch |
| Invoice | Branch-specific | Billing happens at branch |
| Payment | Branch-specific | Money collected at branch |
| User | Shared | Staff work across branches |
| UserBranch | M2M | Controls which branches user can access |

**NO Organization Hierarchy**:
- No parent-child relationships
- No organization caching
- No complex organization-based access control
- No departmental structure

### Alternatives Considered

**Option A: Full CARE Organization Model**
- ✅ Proven pattern for large facilities
- ✅ Supports complex departmental structures
- ✅ Flexible for future expansion
- ❌ Massive overkill for single-doctor clinic
- ❌ Violates explicit directive requirement
- ❌ Introduces multi-tenancy concepts not needed
- ❌ Complex caching and maintenance overhead

**Option B: No Branch Concept (Single Location)**
- ✅ Simplest possible
- ✅ No branch selection overhead
- ❌ Doesn't support future multi-branch need
- ❌ Requires major refactoring when second branch added
- ❌ Requirement explicitly mentions "one or more branches"

**Option C: Simple Branch Model (RECOMMENDED)**
- ✅ Meets requirement exactly
- ✅ Patient records naturally shared
- ✅ Branch-specific operations isolated
- ✅ Simple to understand and implement
- ✅ Expandable without major refactoring
- ✅ No unnecessary abstractions

### Rationale

**Directive Requirement**:
> "Do not introduce Multi-tenancy or an Organization entity. The doctor is the owner of the installed application. The doctor may initially operate one branch and may add additional branches later."

This is crystal clear: **Simple branches, not organizational hierarchy.**

**Use Case Analysis**:

**Single Branch** (Initial):
- One Branch record with IsDefault=true
- No branch selection needed in UI
- BranchId populated automatically

**Multiple Branches** (Later):
- Additional Branch records added
- User selects branch on login or per-session
- Reports can filter by branch or aggregate
- Patient searchable across all branches
- Appointments/queue per selected branch

**Why Not Organization**:
- Organization implies departments, teams, hierarchies
- Multi-tenancy implies tenant isolation, separate data
- This is ONE doctor's system, not a SaaS platform
- Branches are locations, not separate businesses

### Consequences

**Positive**:
- Simple data model
- Clear ownership rules
- Patient sharing automatic (no complex access control)
- Branch access control straightforward (UserBranch M2M)
- Reports easily aggregatable or filterable by branch
- No performance overhead from caching hierarchies

**Negative**:
- If clinic later becomes multi-doctor practice, may need revision
- Cannot model complex departmental structures (not a requirement)
- Less flexible than CARE's Organization model (not needed)

**Single-Branch UX**:
- Auto-select default branch
- Hide branch selection when only one exists
- No unnecessary "which branch?" prompts
- Simple and clean for initial users

**Multi-Branch UX**:
- Branch selector appears when multiple exist
- Selected branch persists in session
- Clear branch context in UI (header/footer indicator)
- Easy branch switching without logout

### Evidence

**From Directive**:
```
"There is one doctor and one application installation. There is no Organization or 
tenant hierarchy. Use a simple Branch concept where appropriate."

"Patient identity and shared patient records are available across the doctor's 
branches according to authorization rules."

"Appointments, queues, working hours, and other branch-specific operational data 
belong to the relevant branch."
```

**From CARE Analysis** (01-repository-audit.md):
- Organization model has parent_cache (array), level_cache (int), cached_parent_json (JSON)
- FacilityOrganization separate from Organization
- Complex cache rebuilding on save
- Designed for hospital departments and multi-facility networks
- **Overkill for single-doctor clinic**

### Review Conditions

Reconsider if:
- Clinic becomes multi-doctor practice requiring separate doctor records
- Need arises for departmental structure within branches
- Access control requires more granularity than branch-level
- Requirement changes to support organizational hierarchy

---

## ADR-003: Simplified Patient Identifier System

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🟡 High

### Context

**CARE's Approach**:
- `PatientIdentifierConfig`: Defines identifier types with JSON configuration
- `PatientIdentifier`: Actual identifier values per config
- `instance_identifiers` (JSON): System-wide IDs
- `facility_identifiers` (JSON): Per-facility IDs
- Complex configuration-driven system
- Caching via `PatientIdentifierConfigCache`

**Typical Clinic Needs**:
- **MRN** (Medical Record Number): Per-branch or system-wide
- **UHID** (Unique Health ID): Government/standard ID (if applicable)
- **External IDs**: Integration with external systems (optional)

**The Question**: Do we need CARE's config-driven identifier system?

### Decision

**Implement Simplified Identifier Model:**

```csharp
public enum PatientIdentifierType
{
    MRN,        // Medical Record Number (most common)
    UHID,       // Unique Health ID (national/standard)
    PassportNo, // Passport number
    NationalID, // National ID card
    ExternalID  // External system identifier
}

public class PatientIdentifier : AuditableEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; }
    
    public int? BranchId { get; set; }  // null = system-wide
    public Branch Branch { get; set; }
    
    public PatientIdentifierType Type { get; set; }
    public string Value { get; set; }   // Actual identifier value
    
    public bool IsActive { get; set; }
    
    // Unique constraint: (Type, Value, BranchId)
}
```

**Rules**:
- Each patient can have multiple identifiers
- MRN typically branch-specific (different MRN per branch)
- UHID typically system-wide (same across branches)
- Unique constraint prevents duplicates within scope
- No complex configuration entities needed

### Alternatives Considered

**Option A: CARE's Config-Driven System**
- ✅ Highly flexible (can define any identifier type)
- ✅ JSON configuration for display rules
- ❌ Complex (3 entities: Config, Identifier, Cache)
- ❌ Overkill for small clinic
- ❌ Configuration burden for simple use case

**Option B: Fixed Fields on Patient**
- ✅ Simplest possible
- ❌ Inflexible (can't add new identifier types)
- ❌ Can't support multiple identifiers of same type
- ❌ No branch-specific vs system-wide distinction

**Option C: Simplified Enum-Based (RECOMMENDED)**
- ✅ Simple and clear
- ✅ No configuration overhead
- ✅ Extensible (add enum values as needed)
- ✅ Supports branch-specific and system-wide
- ✅ Adequate for clinic needs

### Rationale

**Typical Clinic Usage**:
1. Patient registers → Gets MRN for that branch
2. Patient visits another branch → Gets new MRN or uses existing UHID
3. Patient has national ID → Recorded once, used everywhere

**Simple is Better**:
- No configuration UI needed
- No caching complexity
- Enum is easily localizable
- Database constraints handle uniqueness

**Extensibility**:
- Add new enum values as needed
- Can later introduce configuration if truly required
- Migration path exists if complexity needed

### Consequences

**Positive**:
- Simple implementation (1 entity vs 3)
- Clear uniqueness rules
- No caching overhead
- Easy to query and display

**Negative**:
- Adding new identifier type requires code change (enum update)
- Cannot customize identifier validation per type (can add if needed)
- Less flexible than config-driven (not needed now)

**Migration Path**:
If configuration becomes necessary:
1. Add optional `PatientIdentifierConfig` entity
2. Add `ConfigId` FK to `PatientIdentifier` (nullable)
3. Keep enum-based as default, config as optional

### Evidence

**Common Clinic Practice**:
- MRN most common, rarely more than 2-3 identifier types
- Complex configuration usually not needed
- Simplicity valued over flexibility

**OpenEMR** (to be verified in 02-reference-research.md):
- Uses simple identifier fields
- No complex configuration system observed

### Review Conditions

Reconsider if:
- Clinic needs 5+ different identifier types
- Identifier validation rules become complex and varied
- Integration requirements demand flexible configuration
- Government regulations require specific identifier formats

---

## ADR-004: Integrated Queue-Appointment Workflow

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🟡 High

### Context

**CARE's Approach**:
- Separate but linked systems:
  - **TokenBooking** for scheduled appointments
  - **Token** for walk-in queue
  - Can link: TokenBooking ← → Token
  - Complex: 7 entities (SchedulableResource, Schedule, Availability, AvailabilityException, TokenSlot, TokenBooking, TokenQueue + Token + TokenCategory + TokenSubQueue)

**Clinic Reality**:
- Patients book appointments OR walk in
- All patients (scheduled + walk-in) enter queue when arrived
- Doctor sees patients in queue order (with some priority adjustments)
- Encounter links to appointment if patient had one

**The Question**: Should ClinicCare replicate CARE's complex scheduling system?

### Decision

**Implement Integrated Single-Queue Model:**

```csharp
// Scheduled Appointments
public class Appointment : AuditableEntity
{
    public int BranchId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    
    public DateTime AppointmentDateTime { get; set; }
    public int DurationMinutes { get; set; }
    
    public AppointmentType Type { get; set; }  // NewVisit, FollowUp
    public AppointmentStatus Status { get; set; }  // Scheduled, Arrived, InProgress, Completed, Cancelled, NoShow
    
    public string Reason { get; set; }
    public string Notes { get; set; }
}

// Active Queue (Today's waiting patients)
public class QueueEntry : AuditableEntity
{
    public int BranchId { get; set; }
    public int PatientId { get; set; }
    public int? DoctorId { get; set; }
    
    public int? AppointmentId { get; set; }  // nullable: walk-in has no appointment
    public Appointment Appointment { get; set; }
    
    public int TokenNumber { get; set; }  // Sequential per branch per day
    public QueueStatus Status { get; set; }  // Waiting, Called, InConsultation, Completed
    public QueuePriority Priority { get; set; }  // Normal, Urgent
    
    public DateTime CheckInTime { get; set; }
    public DateTime? CalledTime { get; set; }
    public DateTime? CompletionTime { get; set; }
}

// Encounter links to both
public class Encounter : AuditableEntity
{
    public int BranchId { get; set; }
    public int PatientId { get; set; }
    
    public int? AppointmentId { get; set; }
    public int? QueueEntryId { get; set; }
    
    // ... clinical fields
}
```

**Workflow**:
1. **Scheduled Patient**: Appointment → Patient Arrives → QueueEntry created (with AppointmentId) → Called → Encounter created
2. **Walk-in Patient**: Patient Arrives → QueueEntry created (no AppointmentId) → Called → Encounter created

**Token Numbering**:
- Sequential per branch per day
- Resets daily
- Format: Branch prefix + number (e.g., "MAIN-001", "DT-015")

### Alternatives Considered

**Option A: Replicate CARE's Full System**
- ✅ Proven for complex scheduling
- ✅ Handles advanced scenarios (sub-queues, slot allocation, etc.)
- ❌ 7+ entities for scheduling alone
- ❌ Complex slot generation logic
- ❌ Overkill for single-doctor clinic
- ❌ Implementation and maintenance overhead

**Option B: Appointments Only (No Queue)**
- ✅ Simplest
- ❌ Doesn't support walk-ins
- ❌ No real-time queue management
- ❌ Poor fit for clinic workflow

**Option C: Integrated Single Queue (RECOMMENDED)**
- ✅ Supports both scheduled and walk-in
- ✅ Single queue simplifies workflow
- ✅ Clear patient journey
- ✅ 2 main entities vs 7+
- ✅ Adequate for clinic needs

### Rationale

**Real Clinic Workflow**:
- Receptionist manages today's queue (screen)
- Doctor sees queue screen, calls next patient
- Whether patient had appointment or not is secondary
- Queue is the active waiting list

**Simplification Benefits**:
- No slot generation complexity
- No availability rules engine
- Schedule is simple: doctor works certain hours
- Walk-ins fill gaps or queue up

**CARE's Complexity** needed for:
- Multi-doctor scheduling with shared slots
- Complex slot allocation rules
- Sub-queue assignments (multiple rooms)
- Hospital-scale scheduling
- **Not needed for single-doctor clinic**

### Consequences

**Positive**:
- Simple implementation (2 entities vs 7+)
- Clear patient flow
- Easy to understand and use
- Sufficient for clinic operations

**Negative**:
- Cannot do complex slot allocation
- No built-in appointment conflict prevention (can add validation)
- No recurring appointment slots (can add if needed)

**Migration Path** if complexity needed:
- Can add Schedule/Slot entities later
- Appointment entity remains, just gets more constraints
- QueueEntry unchanged

### Evidence

**From 01-repository-audit.md**:
- CARE's 7-entity scheduling system designed for hospitals
- TokenSlot pre-generation from Availability rules
- Sub-queues for multiple consultation rooms
- **Clinic typically has one doctor, one queue**

### Review Conditions

Reconsider if:
- Multi-doctor scheduling becomes requirement
- Need complex slot allocation rules
- Sub-queue functionality required (multiple rooms)
- Appointment conflict prevention becomes critical issue

---

## ADR-005: Simplified Billing Flow

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🔴 Critical

### Context

**CARE's FHIR-Compliant Flow** (5 entities):
1. `ChargeItemDefinition` - Service catalog with pricing rules
2. `ChargeItem` - Service rendered, status workflow (planned → billable → billed)
3. `Account` - Patient billing account, caches totals
4. `Invoice` - Groups charges, can lock
5. `PaymentReconciliation` - Actual payment record

**Small Clinic Reality**:
- Service catalog: List of consultation types, procedures
- During visit: Record services performed
- End of visit: Generate invoice
- Payment: Record payment (cash/card)
- Report: Daily/monthly revenue

**The Question**: Is 5-entity FHIR model necessary for small clinic?

### Decision

**Implement 4-Entity Simplified Model:**

```csharp
// 1. Service Catalog
public class Service : AuditableEntity
{
    public int BranchId { get; set; }  // Services can be branch-specific
    
    public string Name { get; set; }      // "Consultation", "Follow-up", "ECG"
    public string Code { get; set; }      // Short code
    public string Description { get; set; }
    public ServiceCategory Category { get; set; }
    
    public decimal Price { get; set; }    // Base price
    public bool IsActive { get; set; }
    public bool IsTaxable { get; set; }
}

// 2. Invoice (groups services + payment tracking)
public class Invoice : AuditableEntity
{
    public string InvoiceNumber { get; set; }  // Auto-generated: MAIN-2025-001
    
    public int BranchId { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    
    public DateTime InvoiceDate { get; set; }
    public InvoiceStatus Status { get; set; }  // Draft, Finalized, Cancelled
    
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    public decimal PaidAmount { get; set; }      // Sum of payments
    public decimal BalanceAmount { get; set; }   // Total - Paid
    
    public List<InvoiceLineItem> LineItems { get; set; }
    public List<Payment> Payments { get; set; }
    
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedBy { get; set; }
    
    public string Notes { get; set; }
    public string CancellationReason { get; set; }
}

// 3. Invoice Line Items
public class InvoiceLineItem : AuditableEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; }
    
    public int ServiceId { get; set; }
    public Service Service { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // Captured at invoice time
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    
    public string Notes { get; set; }
}

// 4. Payments
public class Payment : AuditableEntity
{
    public string ReceiptNumber { get; set; }  // Auto-generated
    
    public int BranchId { get; set; }
    public int PatientId { get; set; }
    public int? InvoiceId { get; set; }  // nullable: advance/on-account payments
    
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }  // Cash, Card, BankTransfer
    
    public string ReferenceNumber { get; set; }  // Card transaction ref, etc.
    public string Notes { get; set; }
}
```

**Business Rules**:
- Invoice can be Draft (editable) or Finalized (immutable)
- Once Finalized, no changes to line items or amounts
- Payments can be partial (multiple payments per invoice)
- PaidAmount and BalanceAmount updated on payment
- Historical pricing preserved (UnitPrice captured at invoice time)

### Alternatives Considered

**Option A: Full CARE 5-Entity Model**
- ✅ FHIR-compliant
- ✅ Complex status workflows supported
- ✅ Account entity tracks patient balances
- ❌ Complex: 5 entities, status workflows, caching
- ❌ ChargeItem status workflow overkill for clinic
- ❌ Implementation and maintenance overhead

**Option B: Minimal 2-Entity (Invoice + Payment)**
- ✅ Simplest possible
- ❌ No service catalog
- ❌ No line item detail
- ❌ Hard to generate reports by service
- ❌ Too simple

**Option C: 4-Entity Simplified (RECOMMENDED)**
- ✅ Service catalog with pricing
- ✅ Detailed line items for reports
- ✅ Payment tracking with methods
- ✅ Simpler than CARE (4 vs 5 entities, no status workflows)
- ✅ Draft/Finalized is sufficient
- ✅ No separate Account entity (patient balance computed from invoices/payments)

### Rationale

**What's Removed from CARE**:
- `ChargeItem` status workflow (planned → billable → billed) - Too complex
- Separate `Account` entity - Balances can be computed from Invoices/Payments
- ChargeItem as separate entity - Line items sufficient

**What's Simplified**:
- Invoice status: Draft | Finalized | Cancelled (vs complex workflow)
- No locked/unlocked (Finalized = immutable)
- No charge item definition vs charge item split
- Direct Service → InvoiceLineItem

**What's Preserved**:
- Service catalog with pricing
- Historical price preservation (UnitPrice on line item)
- Payment tracking with methods
- Patient balance tracking (computed)
- Audit trails (via AuditableEntity)

**Financial Integrity**:
- Decimal(18,2) for all amounts
- Finalized invoices immutable
- Payments link to invoices
- Balance computed and cached on invoice
- Corrections via Cancellation + New Invoice (audit trail preserved)

### Consequences

**Positive**:
- Simpler implementation (4 entities vs 5)
- Clear workflow: Draft → Finalize → Pay
- No complex status management
- Adequate for clinic billing needs
- Good reporting foundation

**Negative**:
- Cannot do complex charge item workflows
- No separate Account entity (less caching)
- Patient balance requires aggregation (mitigated by database query)

**Reporting Queries**:
```sql
-- Daily revenue by service
SELECT s.Name, SUM(li.LineTotal)
FROM InvoiceLineItems li
JOIN Services s ON li.ServiceId = s.Id
JOIN Invoices i ON li.InvoiceId = i.Id
WHERE i.Status = 'Finalized' AND i.InvoiceDate = @date
GROUP BY s.Name

-- Outstanding balances
SELECT p.Name, SUM(i.BalanceAmount)
FROM Invoices i
JOIN Patients p ON i.PatientId = p.Id
WHERE i.Status = 'Finalized' AND i.BalanceAmount > 0
GROUP BY p.Name
```

### Evidence

**From 01-repository-audit.md Section 2.4**:
- CARE's 5-entity model designed for hospital complexity
- ChargeItem has complex status workflow
- Account caches totals (performance optimization)
- Invoice can be locked
- PaymentReconciliation supports partial payments

**Small Clinic Needs**:
- Service catalog: 10-50 services typically
- Invoice: Group services, calculate total
- Payment: Record payment, track method
- Balance: Total - Paid (simple math)

### Review Conditions

Reconsider if:
- Charge item status workflow becomes necessary
- Complex price components required (multiple types of discounts, taxes)
- Need separate Account entity for performance
- Hospital-level billing complexity required
- Insurance claim integration needed

---

## ADR-006: Concrete Columns vs JSON Fields

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🟡 High

### Context

**CARE's Approach**:
- Heavy JSON usage:
  - `history` (JSON) on every entity
  - `meta` (JSON) on every entity
  - `extensions` (JSON) for plugin data
  - `period`, `hospitalization`, `care_team` (JSON)
  - `dosage_instruction`, `availability` (JSON)
- Enables FHIR flexibility
- Supports plugin architecture
- Hard to query, index, validate

**ClinicCare Considerations**:
- Small clinic doesn't need extreme FHIR flexibility
- Queryable data should be easy to query
- Reports need to filter/aggregate on concrete fields
- JSON appropriate for truly unstructured data

**The Question**: When to use JSON vs concrete columns?

### Decision

**Use Concrete Columns for Core Data, JSON for Extensions:**

**Concrete Columns** (✅ Preferred):
- Frequently queried fields
- Fields used in WHERE clauses, JOINs, ORDER BY
- Fields with database constraints
- Fields with specific types (dates, numbers, booleans)
- Core business data

**JSON Fields** (Use When Appropriate):
- Truly unstructured data
- Plugin/extension data
- Configuration settings
- Optional metadata
- Historical snapshots

**Examples**:

| Data | Storage | Rationale |
|------|---------|-----------|
| Patient.Name | ✅ Concrete (string) | Searchable, indexable |
| Patient.DateOfBirth | ✅ Concrete (date) | Age calculations, constraints |
| Encounter.Status | ✅ Concrete (enum) | Queries, filtering |
| Encounter.Period | ❌ JSON | CARE uses JSON, but Start/End dates better as concrete |
| Prescription.Medications | ⚠️ Consider JSON array if complex structure, OR separate table | Depends on query needs |
| Invoice.LineItems | ✅ Separate table | Need to aggregate, report by service |
| Branch.Settings | ✅ JSON | Configuration, rarely queried |
| Entity.Extensions | ✅ JSON | Plugin data, future flexibility |

### Alternatives Considered

**Option A: JSON-Heavy (Like CARE)**
- ✅ Maximum flexibility
- ✅ Easy to add new fields
- ❌ Hard to query
- ❌ No database constraints
- ❌ No type safety
- ❌ Indexing challenges

**Option B: Concrete-Only (No JSON)**
- ✅ Fully queryable
- ✅ Type-safe
- ✅ Database constraints
- ❌ Inflexible
- ❌ Schema changes for new fields
- ❌ Cannot support extensions

**Option C: Balanced Approach (RECOMMENDED)**
- ✅ Core data queryable
- ✅ Extensions flexible
- ✅ Best of both worlds
- ⚠️ Requires judgment on which to use

### Rationale

**Query Performance**:
```sql
-- Concrete columns: Easy, indexed, fast
SELECT * FROM Encounters WHERE Status = 'InProgress';

-- JSON field: Harder, slower, no index
SELECT * FROM Encounters WHERE JSON_EXTRACT(period, '$.start') > '2025-01-01';
```

**Database Constraints**:
```sql
-- Concrete: Database enforces
Status VARCHAR(50) NOT NULL CHECK (Status IN ('Planned', 'InProgress', 'Completed'))

-- JSON: Application must enforce (can be bypassed)
Period JSON
```

**Reporting Clarity**:
- Concrete columns: Obvious what data exists
- JSON: Must parse to discover structure
- BI tools prefer concrete columns

**CARE's Use Case**:
- FHIR compliance requires JSON flexibility
- Plugin architecture needs extensions
- Hospital-scale needs extensibility
- **Small clinic has different priorities**

### Consequences

**Positive**:
- Core data easily queryable
- Database constraints enforce integrity
- Indexes improve performance
- Reports straightforward

**Negative**:
- Schema changes require migrations
- Less flexible than pure JSON
- Must decide which approach per field

**Guidelines**:

Ask these questions:
1. Will this field be queried frequently? → Concrete
2. Does this field have a specific type? → Concrete
3. Should database enforce constraints? → Concrete
4. Is structure variable or unknown? → JSON
5. Is this extension/plugin data? → JSON
6. Is this optional metadata? → JSON

### Evidence

**From 01-repository-audit.md**:
- CARE uses JSON extensively for FHIR flexibility
- Small clinic doesn't need that level of flexibility
- Queryability matters for reports

### Review Conditions

Reconsider if:
- Plugin architecture requires extensive JSON
- FHIR compliance becomes requirement
- Schema migration overhead becomes prohibitive
- Query performance issues arise from concrete columns

---

## ADR-007: SQLite with PostgreSQL Migration Path

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🟡 High

### Context

**Current Implementation**: SQLite
**care_clinic Uses**: PostgreSQL

**SQLite Advantages**:
- Zero configuration
- Single file database
- Simple backup (copy file)
- Perfect for local deployment
- No server process

**SQLite Concerns**:
- Write locking (one writer at time)
- Concurrent queue updates may contend
- Scaling limitations
- Less robust than PostgreSQL

**The Question**: Is SQLite sufficient for small clinic?

### Decision

**Keep SQLite for Initial Release with PostgreSQL Migration Path:**

**Phase 1: SQLite (v1.0)**
- Use SQLite for initial release
- Test concurrent operations thoroughly
- Document concurrent access patterns
- Use appropriate transaction isolation
- Design schema to be PostgreSQL-compatible

**Phase 2: PostgreSQL Option (v1.1+)**
- Provide PostgreSQL migration script
- Keep code database-agnostic (EF Core abstractions)
- Test on both databases
- Allow user choice at installation

**When to Migrate**:
- Queue update contention becomes issue
- Clinic grows beyond 5 concurrent users
- Backup/restore requirements exceed SQLite capabilities
- Performance issues arise

### Alternatives Considered

**Option A: SQLite Only**
- ✅ Simplest
- ✅ Zero config
- ❌ May not scale
- ❌ Concurrency concerns
- ❌ No migration path

**Option B: PostgreSQL Only**
- ✅ More robust
- ✅ Better concurrency
- ❌ Complex installation
- ❌ Requires server process
- ❌ Overkill for small clinic
- ❌ Violates "simple local installation" goal

**Option C: SQLite with PostgreSQL Migration (RECOMMENDED)**
- ✅ Simple initial deployment
- ✅ Migration path exists
- ✅ Can validate SQLite sufficiency
- ✅ Users can upgrade if needed
- ⚠️ Must maintain compatibility

### Rationale

**Concurrent Use Estimate**:
- Reception: 1 user
- Doctor: 1 user
- Nurse/Assistant: 0-1 users
- Total: 2-3 concurrent

**Write Operations**:
- Patient updates: Low frequency
- Queue updates: Medium frequency (check-in, call, complete)
- Encounter creation: Low-medium frequency
- Billing: Low-medium frequency

**SQLite Should Be Adequate** IF:
- Use appropriate transactions
- Keep transactions short
- Test concurrent queue operations
- Monitor for SQLITE_BUSY errors

**PostgreSQL Migration Path**:
```csharp
// In Infrastructure DependencyInjection
services.AddDbContext<ApplicationDbContext>(options =>
{
    var provider = config["Database:Provider"]; // "SQLite" or "PostgreSQL"
    var connectionString = config.GetConnectionString(provider);
    
    if (provider == "PostgreSQL")
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});
```

### Consequences

**Positive**:
- Simple initial deployment
- Can prove SQLite sufficiency
- Migration available if needed
- EF Core makes switching easier

**Negative**:
- Must test on both databases eventually
- Cannot use database-specific features
- Concurrent performance unknown until tested

**Database Compatibility Rules**:
- Use EF Core abstractions (avoid raw SQL)
- Test migrations on both databases
- Avoid SQLite-specific syntax
- Avoid PostgreSQL-specific syntax
- Use standard SQL types

### Evidence

**From 01-repository-audit.md**:
- care_clinic uses PostgreSQL (hospital scale)
- Small clinic has fewer concurrent users
- SQLite adequate for similar applications

**EF Core Support**:
- Supports both SQLite and PostgreSQL
- Migrations portable
- Switching requires configuration only

### Review Conditions

Reconsider if:
- Concurrent access testing reveals issues
- SQLITE_BUSY errors occur frequently
- Clinic grows beyond 5 concurrent users
- Backup requirements exceed SQLite capabilities

---

## ADR-008: Direct DbContext Access Pattern

**Status**: 📝 Proposed  
**Date**: 2025-01  
**Priority**: 🟢 Medium

### Context

**Repository Pattern**:
- Abstraction over data access
- Common in Clean Architecture
- Provides unit of work pattern
- Can swap data sources

**Current Implementation**:
- No repository pattern
- Direct `IApplicationDbContext` access
- EF Core DbSets accessed directly

**CARE's Approach**:
- Direct Django ORM access
- No repository abstraction
- Querysets used directly in views

**The Question**: Should ClinicCare implement Repository pattern?

### Decision

**Use Direct DbContext Access (No Repository Pattern):**

Allow handlers/services to access `IApplicationDbContext` directly:

```csharp
public class GetPatientQueryHandler : IRequestHandler<GetPatientQuery, PatientDto>
{
    private readonly IApplicationDbContext _context;
    
    public async Task<PatientDto> Handle(GetPatientQuery request, CancellationToken ct)
    {
        var patient = await _context.Patients
            .Include(p => p.Identifiers)
            .FirstOrDefaultAsync(p => p.ExternalId == request.Id, ct);
            
        // Map and return
    }
}
```

**When Repository Might Be Useful**:
- Complex queries used in multiple places (extract to repository method)
- Entity with complex invariants (encapsulate in repository)
- Caching requirements (repository can cache)

**If Repository Added**:
- Add for specific entities, not universally
- Keep interface minimal (avoid generic repository antipattern)
- Specific methods for specific queries

### Alternatives Considered

**Option A: Generic Repository Pattern**
```csharp
IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    // etc.
}
```
- ❌ Leaky abstraction
- ❌ Hides EF Core capabilities
- ❌ "Generic Repository over EF Core is antipattern"
- ❌ Adds layer without benefit

**Option B: Entity-Specific Repositories**
```csharp
IPatientRepository
{
    Task<Patient> GetWithIdentifiersAsync(Guid externalId);
    Task<List<Patient>> SearchByNameAsync(string name);
    // Specific methods only
}
```
- ✅ Encapsulates complex queries
- ✅ Testable
- ⚠️ Only if complexity warrants it

**Option C: Direct DbContext (RECOMMENDED)**
- ✅ Simple
- ✅ Leverages EF Core features
- ✅ No unnecessary abstraction
- ✅ Testable via IApplicationDbContext
- ✅ CARE proves this works

### Rationale

**EF Core IS a Repository**:
- DbContext = Unit of Work
- DbSet<T> = Repository
- Already abstracted from database
- Additional layer rarely adds value

**When Repository Adds Value**:
- Complex query used in 3+ places → Extract to method
- Entity with complex invariants → Encapsulate
- Caching needed → Repository can cache
- **Otherwise, YAGNI**

**Testing**:
- IApplicationDbContext allows mocking
- In-memory database for integration tests
- Repository not required for testability

### Consequences

**Positive**:
- Less code
- Direct EF Core feature access
- No artificial constraints
- Simple and clear

**Negative**:
- No abstraction over data source (not needed)
- Complex queries may duplicate (can extract when happens)

**If Repository Needed Later**:
- Add for specific entities
- Refactor existing code to use it
- No architectural change required

### Evidence

**From 01-repository-audit.md**:
- CARE uses direct ORM access
- No repository pattern
- Works fine for complex hospital system

**EF Core Documentation**:
- Recommends against generic repository
- DbContext already provides abstraction
- Additional repository rarely needed

### Review Conditions

Reconsider if:
- Same complex query duplicated 3+ times
- Entity invariants require encapsulation
- Data source switching becomes requirement
- Caching layer needed

---

## Summary of Proposed Decisions

| ADR | Decision | Impact | Priority |
|-----|----------|--------|----------|
| 001 | Selective CQRS | Reduces boilerplate, maintains benefits | 🔴 Critical |
| 002 | Simple Branch Model | Meets requirements exactly | 🔴 Critical |
| 003 | Enum-Based Identifiers | Simplified vs CARE's config system | 🟡 High |
| 004 | Integrated Queue | 2 entities vs CARE's 7+ | 🟡 High |
| 005 | 4-Entity Billing | Simplified vs CARE's 5-entity FHIR | 🔴 Critical |
| 006 | Balanced Concrete/JSON | Core data queryable, extensions flexible | 🟡 High |
| 007 | SQLite + PostgreSQL Path | Simple deployment with migration option | 🟡 High |
| 008 | Direct DbContext | No repository pattern | 🟢 Medium |

**Overall Theme**: Pragmatic simplification while preserving essential capabilities.

---

## Next Steps

1. **Review and Approve** these ADRs
2. Update **Status** from Proposed → Accepted (or Rejected with rationale)
3. Reference ADRs in implementation tasks
4. Update ADRs when decisions change (with rationale)

---

## Document Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01 | Initial ADRs based on CARE analysis and existing implementation audit |

---

**End of Architecture Decision Records**
