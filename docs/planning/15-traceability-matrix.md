# 15. Traceability Matrix

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [03-product-scope.md](./03-product-scope.md), [04-workflows.md](./04-workflows.md), [14-implementation-roadmap.md](./14-implementation-roadmap.md)

---

## 1. Overview

### 1.1 Purpose
Establish bidirectional traceability between requirements, design decisions, implementation tasks, and test cases to ensure complete coverage and facilitate impact analysis for changes.

### 1.2 Trace Levels
- **Requirements** → User Stories (US-XXX)
- **User Stories** → Design Entities (from 05-domain-model.md)
- **User Stories** → Implementation Tasks (TASK-XXX)
- **Implementation Tasks** → Test Cases (TEST-XXX)
- **Requirements** → Acceptance Criteria → Test Scenarios

---

## 2. Requirements to User Stories

| Requirement ID | Requirement Name | User Stories | Priority | Status |
|----------------|------------------|--------------|----------|--------|
| **REQ-001** | Patient Registration | US-001, US-002 | P0 | 🔵 Planned |
| **REQ-002** | Patient Search | US-002 | P0 | 🔵 Planned |
| **REQ-003** | Patient History | US-003 | P1 | 🔵 Planned |
| **REQ-004** | Appointment Scheduling | US-004 | P0 | 🔵 Planned |
| **REQ-005** | Queue Management | US-005, US-006, US-007 | P0 | 🔵 Planned |
| **REQ-006** | Consultation Workflow | US-008, US-009, US-010, US-011, US-012, US-013 | P0 | 🔵 Planned |
| **REQ-007** | Billing & Invoicing | US-014, US-015 | P0 | 🔵 Planned |
| **REQ-008** | Cash Drawer Management | US-016, US-017, US-018, US-019 | P0 | 🔵 Planned |
| **REQ-009** | User Management | (Existing in M0) | P0 | ✅ Done |
| **REQ-010** | Authentication | (Existing in M0) | P0 | ✅ Done |
| **REQ-011** | Branch Management | US-026, US-027, US-028 | P1 | 🔵 Planned v1.1 |
| **REQ-012** | Lab Orders | US-020, US-021 | P2 | 🔵 Planned v1.1 |
| **REQ-013** | Imaging Orders | US-022 | P2 | 🔵 Planned v1.1 |
| **REQ-014** | Prescription Printing | US-023 | P2 | 🔵 Planned v1.1 |
| **REQ-015** | Invoice Printing | US-024 | P2 | 🔵 Planned v1.1 |
| **REQ-016** | Financial Reports | US-029, US-030, US-031 | P2 | 🔵 Planned v1.2 |
| **REQ-017** | Operational Reports | US-032, US-033 | P2 | 🔵 Planned v1.2 |
| **REQ-018** | Document Upload | US-034 | P2 | 🔵 Planned v1.2 |
| **REQ-019** | Calendar View | US-035 | P2 | 🔵 Planned v1.2 |

---

## 3. User Stories to Domain Entities

| User Story | Entities Involved | Relationships |
|------------|-------------------|---------------|
| **US-001:** Register Patient | Patient | Creates Patient |
| **US-002:** Search Patient | Patient | Queries Patient |
| **US-003:** Patient History | Patient, Encounter, Invoice | Patient → Encounters (1:N), Encounters → Invoice (1:1) |
| **US-004:** Schedule Appointment | Appointment, Patient, Branch, QueueToken | Creates Appointment + QueueToken, links Patient & Branch |
| **US-005:** View Queue | QueueToken, Patient, Appointment, Branch | Queries QueueTokens by Branch, includes Patient & Appointment |
| **US-006:** Call Next Patient | QueueToken | Updates QueueToken.Status (waiting → called → in-progress) |
| **US-007:** Mark No-Show | QueueToken | Updates QueueToken.Status = no-show |
| **US-008:** Start Consultation | Encounter, Patient, Appointment, QueueToken | Creates Encounter, updates QueueToken.Status = in-progress |
| **US-009:** Record Vitals | Observation, Encounter | Creates Observation linked to Encounter |
| **US-010:** Write Clinical Notes | Encounter | Updates Encounter.Notes (SOAP fields) |
| **US-011:** Add Diagnosis | Condition, Encounter, Icd10Code | Creates Condition linked to Encounter & Icd10Code |
| **US-012:** Prescribe Medication | MedicationRequest, Encounter | Creates MedicationRequest linked to Encounter |
| **US-013:** Complete Consultation | Encounter, QueueToken, Invoice | Updates Encounter.Status = completed, creates Invoice |
| **US-014:** Create Invoice | Invoice, InvoiceItem, Patient, Encounter | Creates Invoice with InvoiceItems |
| **US-015:** Record Payment | Payment, Invoice, CashDrawer | Creates Payment, updates Invoice & CashDrawer |
| **US-016:** Open Cash Drawer | CashDrawer, Branch, ApplicationUser | Creates CashDrawer, links Branch & User |
| **US-017:** Record Expense | Expense, CashDrawer | Creates Expense, updates CashDrawer balance |
| **US-018:** Close Cash Drawer | CashDrawer | Updates CashDrawer.Status = closed, calculates discrepancy |
| **US-019:** Cash Drawer History | CashDrawer, Payment, Expense | Queries CashDrawer with related Payments & Expenses |

---

## 4. User Stories to Implementation Tasks

| User Story | Backend Tasks | Frontend Tasks | Test Tasks |
|------------|---------------|----------------|------------|
| **US-001** | TASK-001-BE: CreatePatientCommand | TASK-001-FE: PatientForm | TASK-001-TEST: Validation tests, TASK-001-INT: API integration test |
| **US-002** | TASK-002-BE: SearchPatientsQuery | TASK-002-FE: PatientSearch autocomplete | TASK-002-TEST: Search accuracy, performance |
| **US-003** | TASK-003-BE: GetPatientHistoryQuery | TASK-003-FE: PatientHistory component | TASK-003-TEST: Pagination, sorting |
| **US-004** | TASK-004-BE: CreateAppointmentCommand | TASK-004-FE: AppointmentForm | TASK-004-TEST: Conflict detection |
| **US-005** | TASK-005-BE: GetQueueStatusQuery | TASK-005-FE: QueueDashboard with polling | TASK-005-TEST: Queue ordering |
| **US-006** | TASK-006-BE: CallNextTokenCommand | TASK-006-FE: QueueControls | TASK-006-TEST: State transitions |
| **US-007** | TASK-007-BE: MarkTokenNoShowCommand | TASK-007-FE: No-Show action | TASK-007-TEST: No-show workflow |
| **US-008** | TASK-008-BE: StartConsultationCommand | TASK-008-FE: Consultation entry | TASK-008-TEST: Single active encounter |
| **US-009** | TASK-009-BE: RecordVitalsCommand | TASK-009-FE: VitalsForm | TASK-009-TEST: Validation rules |
| **US-010** | TASK-010-BE: UpdateEncounterNotesCommand | TASK-010-FE: ClinicalNotesEditor | TASK-010-TEST: Autosave, recovery |
| **US-011** | TASK-011-BE: AddDiagnosisCommand, seed ICD-10 | TASK-011-FE: DiagnosisSearch | TASK-011-TEST: ICD-10 search |
| **US-012** | TASK-012-BE: CreateMedicationOrderCommand | TASK-012-FE: MedicationOrderForm | TASK-012-TEST: Order validation |
| **US-013** | TASK-013-BE: CompleteConsultationCommand | TASK-013-FE: Complete button | TASK-013-TEST: Immutability |
| **US-014** | TASK-014-BE: CreateInvoiceCommand | TASK-014-FE: InvoiceForm | TASK-014-TEST: Calculation accuracy |
| **US-015** | TASK-015-BE: RecordPaymentCommand | TASK-015-FE: PaymentForm | TASK-015-TEST: Balance updates |
| **US-016** | TASK-016-BE: OpenCashDrawerCommand | TASK-016-FE: CashDrawerControl | TASK-016-TEST: Single-drawer constraint |
| **US-017** | TASK-017-BE: RecordExpenseCommand | TASK-017-FE: ExpenseForm | TASK-017-TEST: Expense recording |
| **US-018** | TASK-018-BE: CloseCashDrawerCommand | TASK-018-FE: Reconciliation UI | TASK-018-TEST: Reconciliation calc |
| **US-019** | TASK-019-BE: GetCashDrawerHistoryQuery | TASK-019-FE: CashDrawerHistory page | TASK-019-TEST: Filtering, pagination |

---

## 5. Implementation Tasks to Test Cases

### 5.1 Sprint 1: Patient Management

#### TASK-001-BE: CreatePatientCommand
**Test Cases:**
- TEST-001-UNIT-001: Valid patient data creates patient successfully
- TEST-001-UNIT-002: Missing required fields returns validation error
- TEST-001-UNIT-003: Invalid phone format returns validation error
- TEST-001-UNIT-004: Future date of birth returns validation error
- TEST-001-UNIT-005: Duplicate phone number returns conflict error (optional warning)
- TEST-001-INT-001: POST /api/v1/patients returns 201 Created with patient object
- TEST-001-INT-002: POST /api/v1/patients with invalid data returns 400 Bad Request
- TEST-001-E2E-001: User can create patient via UI and see in patient list

#### TASK-002-BE: SearchPatientsQuery
**Test Cases:**
- TEST-002-UNIT-001: Search by first name returns matching patients
- TEST-002-UNIT-002: Search by phone returns exact match
- TEST-002-UNIT-003: Search by national ID returns exact match
- TEST-002-UNIT-004: Search returns max 10 results
- TEST-002-UNIT-005: Empty query returns empty list
- TEST-002-PERF-001: Search completes within 300ms for 1000 patients
- TEST-002-INT-001: GET /api/v1/patients/search?q=ahmed returns matching patients

#### TASK-003-BE: GetPatientHistoryQuery
**Test Cases:**
- TEST-003-UNIT-001: Patient with no encounters returns empty list
- TEST-003-UNIT-002: Encounters sorted by date descending (newest first)
- TEST-003-UNIT-003: Pagination returns correct page (20 per page)
- TEST-003-UNIT-004: History includes encounter, invoice, diagnosis
- TEST-003-INT-001: GET /api/v1/patients/{id}/visits returns paginated history

### 5.2 Sprint 2: Appointment & Queue

#### TASK-004-BE: CreateAppointmentCommand
**Test Cases:**
- TEST-004-UNIT-001: Valid appointment creates appointment + queue token
- TEST-004-UNIT-002: Appointment in past returns validation error
- TEST-004-UNIT-003: Patient with existing appointment same day returns conflict warning
- TEST-004-UNIT-004: Queue token number auto-increments per branch per day
- TEST-004-INT-001: POST /api/v1/appointments returns 201 Created with queue token
- TEST-004-E2E-001: User schedules appointment, patient appears in queue

#### TASK-005-BE: GetQueueStatusQuery
**Test Cases:**
- TEST-005-UNIT-001: Queue filtered by branch
- TEST-005-UNIT-002: Queue ordered by token number ascending
- TEST-005-UNIT-003: Queue excludes completed and no-show tokens
- TEST-005-UNIT-004: Queue includes patient info (name, phone)
- TEST-005-UNIT-005: Wait duration calculated correctly
- TEST-005-INT-001: GET /api/v1/queue/{branchId} returns queue with statistics

#### TASK-006-BE: CallNextTokenCommand
**Test Cases:**
- TEST-006-UNIT-001: Call next transitions waiting → called
- TEST-006-UNIT-002: Previous called token transitions to in-progress
- TEST-006-UNIT-003: No waiting tokens returns 404 Not Found
- TEST-006-UNIT-004: CalledAt timestamp set correctly
- TEST-006-CONCURRENCY-001: Two simultaneous call-next requests handled correctly (no duplicate calls)
- TEST-006-INT-001: POST /api/v1/queue/{branchId}/call-next returns called token

### 5.3 Sprint 3: Consultation

#### TASK-008-BE: StartConsultationCommand
**Test Cases:**
- TEST-008-UNIT-001: Valid request creates encounter
- TEST-008-UNIT-002: Patient with active encounter returns error (single encounter constraint)
- TEST-008-UNIT-003: Encounter status set to in-progress
- TEST-008-UNIT-004: Queue token status updated to in-progress
- TEST-008-UNIT-005: Encounter linked to patient, appointment, branch
- TEST-008-INT-001: POST /api/v1/encounters returns 201 Created

#### TASK-010-BE: UpdateEncounterNotesCommand
**Test Cases:**
- TEST-010-UNIT-001: Notes updated successfully
- TEST-010-UNIT-002: Autosave updates draft every 5 seconds
- TEST-010-UNIT-003: Draft restored after page reload
- TEST-010-UNIT-004: Completed encounter cannot be edited (immutable)
- TEST-010-INT-001: PATCH /api/v1/encounters/{id}/notes updates notes

#### TASK-013-BE: CompleteConsultationCommand
**Test Cases:**
- TEST-013-UNIT-001: Encounter status updated to completed
- TEST-013-UNIT-002: Queue token status updated to completed
- TEST-013-UNIT-003: Invoice created with consultation fee
- TEST-013-UNIT-004: EndTime set to current timestamp
- TEST-013-UNIT-005: Completed encounter is immutable (cannot edit)
- TEST-013-INT-001: POST /api/v1/encounters/{id}/complete returns completed encounter with invoice

### 5.4 Sprint 4: Billing

#### TASK-014-BE: CreateInvoiceCommand
**Test Cases:**
- TEST-014-UNIT-001: Invoice with line items calculates subtotal correctly
- TEST-014-UNIT-002: Discount (percentage) applied correctly
- TEST-014-UNIT-003: Discount (fixed) applied correctly
- TEST-014-UNIT-004: Tax calculated per item
- TEST-014-UNIT-005: Total = subtotal - discount + tax
- TEST-014-UNIT-006: Invoice number auto-generated (INV-2026-000001)
- TEST-014-MONEY-001: No floating-point errors (test with $0.01, $999,999.99)
- TEST-014-INT-001: POST /api/v1/invoices returns 201 Created

#### TASK-015-BE: RecordPaymentCommand
**Test Cases:**
- TEST-015-UNIT-001: Cash payment updates invoice and cash drawer
- TEST-015-UNIT-002: Card payment updates invoice only (no drawer)
- TEST-015-UNIT-003: Payment amount cannot exceed balance due
- TEST-015-UNIT-004: Partial payment updates status to partial
- TEST-015-UNIT-005: Full payment updates status to paid
- TEST-015-UNIT-006: Overpayment returns validation error
- TEST-015-MONEY-001: Balance due calculated correctly after multiple payments
- TEST-015-INT-001: POST /api/v1/invoices/{id}/payments returns payment object

#### TASK-018-BE: CloseCashDrawerCommand
**Test Cases:**
- TEST-018-UNIT-001: Expected balance = opening + payments - expenses
- TEST-018-UNIT-002: Discrepancy = closing - expected
- TEST-018-UNIT-003: Discrepancy >$5 flagged in summary
- TEST-018-UNIT-004: Drawer status updated to closed
- TEST-018-UNIT-005: Closed drawer cannot be reopened
- TEST-018-MONEY-001: Test with large numbers ($10,000+) for rounding errors
- TEST-018-AUTH-001: Receptionist can only close own drawer (authorization)
- TEST-018-AUTH-002: Admin can close any drawer (authorization)
- TEST-018-AUTH-003: Doctor cannot close cash drawer (authorization, expect 403 Forbidden)
- TEST-018-INT-001: POST /api/v1/cash-drawers/{id}/close returns reconciliation summary

---

### 5.5 Financial Authorization Tests

#### Branch-Level Authorization for Financial Operations
**Test Cases:**
- TEST-019-AUTH-001: Receptionist can only view invoices for assigned branches
  - Given Receptionist A assigned to Branch 1
  - When Receptionist A queries invoices
  - Then only Branch 1 invoices returned
  - And Branch 2 invoices excluded

- TEST-019-AUTH-002: Receptionist cannot create invoice for non-assigned branch
  - Given Receptionist A assigned to Branch 1
  - When Receptionist A attempts to create invoice for Branch 2
  - Then request fails with 403 Forbidden
  - And error message: "You do not have access to create invoices for this branch."

- TEST-019-AUTH-003: Doctor can view invoices for all branches
  - Given Doctor role
  - When Doctor queries invoices
  - Then invoices from all branches returned

- TEST-019-AUTH-004: Admin can create invoices for any branch
  - Given Admin role
  - When Admin creates invoice for any branch
  - Then invoice created successfully

#### Cash Drawer Authorization
**Test Cases:**
- TEST-020-AUTH-001: Receptionist can only open drawer for assigned branches
  - Given Receptionist A assigned to Branch 1
  - When Receptionist A attempts to open drawer for Branch 2
  - Then request fails with 403 Forbidden

- TEST-020-AUTH-002: Receptionist cannot have multiple open drawers
  - Given Receptionist A has open drawer in Branch 1
  - When Receptionist A attempts to open drawer in Branch 1 again
  - Then request fails with 400 Bad Request
  - And error message: "You already have an open cash drawer."

- TEST-020-AUTH-003: Multiple receptionists can have open drawers in same branch
  - Given Receptionist A has open drawer in Branch 1
  - When Receptionist B opens drawer in Branch 1
  - Then both drawers are open
  - And each receptionist sees only their own drawer

- TEST-020-AUTH-004: Receptionist can only close own drawer
  - Given Receptionist A has open drawer with ID drawer-1
  - And Receptionist B has open drawer with ID drawer-2
  - When Receptionist A attempts to close drawer-2
  - Then request fails with 403 Forbidden
  - And error message: "You can only close your own cash drawer."

- TEST-020-AUTH-005: Admin can close any drawer
  - Given Receptionist A has open drawer with ID drawer-1
  - When Admin attempts to close drawer-1
  - Then drawer closes successfully

#### Payment Authorization
**Test Cases:**
- TEST-021-AUTH-001: Receptionist cannot record cash payment without open drawer
  - Given Receptionist A has no open drawer
  - When Receptionist A attempts to record cash payment
  - Then request fails with 400 Bad Request
  - And error message: "You must have an open cash drawer to record cash payments."

- TEST-021-AUTH-002: Receptionist can record card payment without open drawer
  - Given Receptionist A has no open drawer
  - When Receptionist A attempts to record card payment
  - Then payment recorded successfully
  - And no cash drawer affected

- TEST-021-AUTH-003: Cash payment updates correct drawer
  - Given Receptionist A has open drawer drawer-1
  - And Receptionist B has open drawer drawer-2
  - When Receptionist A records $100 cash payment
  - Then drawer-1 balance increases by $100
  - And drawer-2 balance unchanged

- TEST-021-AUTH-004: Receptionist cannot record payment for non-assigned branch invoice
  - Given Receptionist A assigned to Branch 1
  - And Invoice INV-001 belongs to Branch 2
  - When Receptionist A attempts to record payment for INV-001
  - Then request fails with 403 Forbidden

#### Expense Authorization
**Test Cases:**
- TEST-022-AUTH-001: Receptionist can only record expense from own open drawer
  - Given Receptionist A has open drawer drawer-1
  - When Receptionist A records $50 expense
  - Then expense recorded successfully
  - And drawer-1 balance decreases by $50

- TEST-022-AUTH-002: Receptionist cannot record expense from another user's drawer
  - Given Receptionist A has open drawer drawer-1
  - And Receptionist B has open drawer drawer-2
  - When Receptionist A attempts to record expense from drawer-2
  - Then request fails with 403 Forbidden

- TEST-022-AUTH-003: Doctor expense requires approval
  - Given Doctor role
  - When Doctor records $200 expense
  - Then expense created with Status = Pending
  - And notification sent to Admin for approval

- TEST-022-AUTH-004: Admin expense auto-approved
  - Given Admin role
  - When Admin records $200 expense
  - Then expense created with Status = Approved
  - And no approval workflow triggered

#### Refund Authorization
**Test Cases:**
- TEST-023-AUTH-001: Receptionist can auto-approve small refunds (≤$50)
  - Given Receptionist role
  - And Payment of $40 exists
  - When Receptionist processes full refund of $40
  - Then refund auto-approved
  - And refund Status = Approved
  - And invoice paid amount reduced by $40

- TEST-023-AUTH-002: Large refund requires Doctor/Admin approval
  - Given Receptionist role
  - And Payment of $200 exists
  - When Receptionist processes full refund of $200
  - Then refund created with Status = Pending
  - And notification sent to Doctor/Admin
  - And invoice paid amount NOT reduced yet

- TEST-023-AUTH-003: Doctor can approve large refund
  - Given Refund REF-001 with Status = Pending, Amount = $200
  - When Doctor approves REF-001
  - Then refund Status = Approved
  - And invoice paid amount reduced by $200
  - And cash drawer balance updated (if cash payment)

- TEST-023-AUTH-004: Admin can approve large refund
  - Given Refund REF-001 with Status = Pending, Amount = $200
  - When Admin approves REF-001
  - Then refund Status = Approved
  - And invoice paid amount reduced by $200

- TEST-023-AUTH-005: Receptionist cannot approve large refund
  - Given Refund REF-001 with Status = Pending, Amount = $200
  - When Receptionist attempts to approve REF-001
  - Then request fails with 403 Forbidden
  - And error message: "You do not have permission to approve large refunds."

#### Financial Data Isolation (Money Precision Tests)
**Test Cases:**
- TEST-024-MONEY-001: Payment + refund + partial payment calculates correctly
  - Given Invoice with Total = $100.00
  - When $60.00 payment recorded
  - And $20.00 refund processed
  - And $40.00 payment recorded
  - Then invoice PaidAmount = $60.00 - $20.00 + $40.00 = $80.00
  - And invoice Status = PartiallyPaid
  - And balance due = $20.00

- TEST-024-MONEY-002: Cash drawer balance with complex transactions
  - Given Drawer opened with $500.00
  - When 5 cash payments: $123.45, $67.89, $234.56, $12.34, $89.01
  - And 3 expenses: $45.67, $123.00, $8.90
  - And 1 cash movement in: $200.00
  - And 1 cash movement out: $150.00
  - Then expected balance = $500.00 + $527.25 - $177.57 + $200.00 - $150.00 = $899.68
  - And no rounding errors

---

## 6. Requirements to Acceptance Criteria to Test Scenarios

### REQ-001: Patient Registration

**Acceptance Criteria:**
1. System shall validate required fields: firstName, lastName, dateOfBirth, gender, phone
2. System shall warn if phone number already exists (but allow duplicate)
3. System shall prevent future dates for dateOfBirth
4. System shall validate phone format (10-15 digits)
5. System shall auto-assign patient ID (pat_XXXXX)

**Test Scenarios:**
- SCENARIO-001: Happy path - Create valid patient
  - Given receptionist is logged in
  - When receptionist fills patient form with valid data
  - Then patient is created successfully
  - And patient appears in patient list
  
- SCENARIO-002: Validation errors
  - Given receptionist is logged in
  - When receptionist submits form with missing required fields
  - Then system displays validation errors
  - And patient is not created

- SCENARIO-003: Duplicate phone warning
  - Given existing patient with phone +201234567890
  - When receptionist creates new patient with same phone
  - Then system displays warning "Phone number already exists"
  - And receptionist can choose to continue or cancel

### REQ-005: Queue Management

**Acceptance Criteria:**
1. Queue shall display patients in token number order (ascending)
2. Queue shall show wait duration for each patient
3. Queue shall update in real-time (10-second polling)
4. Call next shall select next waiting token
5. Queue shall show color-coded status badges

**Test Scenarios:**
- SCENARIO-005: Call next patient
  - Given 3 patients in waiting status (tokens 1, 2, 3)
  - And 1 patient in called status (token 0)
  - When receptionist clicks "Call Next"
  - Then token 1 status changes to called
  - And token 0 status changes to in-progress
  - And queue display updates

- SCENARIO-006: Real-time queue updates
  - Given receptionist viewing queue dashboard
  - When doctor completes consultation in another tab
  - Then queue automatically updates within 10 seconds
  - And completed patient removed from active queue

### REQ-006: Consultation Workflow

**Acceptance Criteria:**
1. Doctor shall record vitals before assessment (optional but recommended)
2. Clinical notes shall autosave every 5 seconds
3. Diagnoses shall link to ICD-10 codes
4. Completing consultation shall create invoice automatically
5. Completed encounter shall be immutable

**Test Scenarios:**
- SCENARIO-007: Complete consultation end-to-end
  - Given doctor starts consultation for patient
  - When doctor records vitals
  - And writes clinical notes
  - And adds diagnosis (ICD-10: R51.9 - Headache)
  - And prescribes medication (Paracetamol 500mg)
  - And clicks "Complete"
  - Then encounter status = completed
  - And invoice created with consultation fee
  - And queue token status = completed
  - And encounter cannot be edited

- SCENARIO-008: Autosave recovery
  - Given doctor writing clinical notes
  - When browser crashes
  - And doctor reopens consultation page
  - Then draft notes are restored
  - And doctor can continue editing

### REQ-008: Cash Drawer Management

**Acceptance Criteria:**
1. Only one open drawer per branch at a time
2. Cash payments require open drawer
3. Closing drawer shall calculate expected balance
4. Discrepancy >$5 shall be flagged
5. Closed drawer shall be immutable

**Test Scenarios:**
- SCENARIO-009: Daily cash drawer cycle
  - Given morning shift starts
  - When receptionist opens drawer with $500 opening balance
  - And records 10 cash payments totaling $1,200
  - And records 3 expenses totaling $150
  - And closes drawer with $1,550 counted
  - Then expected balance = $500 + $1,200 - $150 = $1,550
  - And discrepancy = $0
  - And drawer status = closed

- SCENARIO-010: Cash drawer discrepancy
  - Given drawer opened with $500
  - When receptionist records payments totaling $1,000
  - And closes drawer with $1,480 counted (short $20)
  - Then expected = $1,500
  - And discrepancy = -$20 (flagged as short)
  - And summary shows warning

---

## 7. Architecture Decisions to Implementation

| ADR | Decision | Affected Components | Implementation Tasks |
|-----|----------|---------------------|----------------------|
| **ADR-001** | Selective CQRS | Application layer | Separate Commands and Queries in MediatR handlers |
| **ADR-002** | Simple Branch model | Branch entity, Authorization | Branch entity design, branch-level authorization checks |
| **ADR-003** | Enum-based identifiers | All entities | Use string prefix + GUID (e.g., pat_abc123) |
| **ADR-004** | Integrated Queue | Appointment, QueueToken | Create QueueToken on appointment creation |
| **ADR-005** | Simplified Billing | Invoice, Payment | 4-entity billing (defer multi-invoice payment to v2.0) |
| **ADR-006** | Concrete entities + JSON | Domain entities | Store structured data (allergies, chronic conditions) as JSON in TEXT columns |
| **ADR-007** | SQLite → PostgreSQL | Database, Infrastructure | SQLite v1.0, migration path v1.1 |
| **ADR-008** | Direct DbContext access | Repositories | No repository abstraction, use DbContext directly in handlers |

---

## 8. Test Coverage Matrix

| Component | Unit Tests | Integration Tests | E2E Tests | Coverage Target |
|-----------|------------|-------------------|-----------|-----------------|
| Domain Entities | Entity validation, business rules | - | - | ≥85% |
| Application Commands | All command handlers | - | - | 100% |
| Application Queries | All query handlers | - | - | 100% |
| Infrastructure (EF Core) | - | DbContext tests | - | ≥80% |
| API Endpoints | - | All endpoints | Critical paths | ≥80% |
| Frontend Components | Component rendering, validation | - | - | ≥70% |
| Frontend Hooks | API hooks (usePatients, etc.) | - | - | ≥70% |
| End-to-End Workflows | - | - | 4 critical paths | 100% critical paths |

**Critical E2E Test Paths:**
1. Patient → Appointment → Queue → Consultation → Billing (TEST-E2E-001)
2. Walk-in patient → Queue → Consultation → Payment (TEST-E2E-002)
3. Queue management: Add, call next, complete (TEST-E2E-003)
4. Cash drawer: Open, payments, expenses, close, reconcile (TEST-E2E-004)

---

## 9. Change Impact Analysis

**Example: Adding new field "Email" to Patient entity**

**Traceability Impact:**
1. **Domain Model (05-domain-model.md):** Update Patient entity definition
2. **Database Schema (07-database-design.md):** Add Email column to Patients table
3. **API Specification (11-backend-api-specification.md):** Add email field to PatientDto
4. **Frontend (10-frontend-specification.md):** Add email input to PatientForm
5. **Tests:** Add validation tests for email format

**Affected Tasks:**
- TASK-001-BE: Update CreatePatientCommand to include email
- TASK-001-FE: Update PatientForm component
- TASK-001-TEST: Add email validation tests
- TASK-002-BE: Include email in SearchPatientsQuery (optional)

**Migration Required:** Yes (database migration to add Email column)

---

## 10. Open Questions

### Q1: Traceability Tool
**Question:** Should we use a dedicated traceability tool (e.g., Jira with Zephyr, TestRail)?  
**Context:** Manual matrix in Markdown is simple but harder to maintain  
**Recommendation:** Start with Markdown, evaluate tool if team grows  
**Decision:** [PENDING]

### Q2: Test ID Naming Convention
**Question:** Should test IDs follow strict convention (TEST-XXX-LAYER-NNN)?  
**Recommendation:** Yes, enforces consistency, aids in reporting  
**Decision:** [PENDING]

### Q3: Automated Traceability Checks
**Question:** Should CI/CD enforce traceability (e.g., block PR if user story lacks linked task)?  
**Recommendation:** Manual for v1.0, automate in v1.1 if valuable  
**Decision:** [PENDING]

---

## 11. Related Documents

- [03-product-scope.md](./03-product-scope.md) - Requirements (features)
- [04-workflows.md](./04-workflows.md) - User workflows mapped to requirements
- [14-implementation-roadmap.md](./14-implementation-roadmap.md) - User stories and tasks
- [12-testing-strategy.md](./12-testing-strategy.md) - Test approach and cases

---

## 12. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial traceability matrix |

---

**Document Confidence:** HIGH  
**Evidence Base:** Requirements from scope document, user stories from roadmap, test strategy from testing document  
**Assumptions:** All requirements have at least one user story, all user stories have tests  
**Next Review:** After Sprint 1, verify traceability is maintained during implementation
