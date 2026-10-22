# 03 - Product Scope & Feature Prioritization

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 01-repository-audit.md, 02-reference-research.md  
**Feeds Into:** 04-workflows.md, 05-domain-model.md, 14-implementation-roadmap.md

---

## Purpose

Define the comprehensive feature set for ClinicCare with clear prioritization (P0/P1/P2/P3), success criteria, and scope boundaries. Establishes what we are building, what we are NOT building, and why.

---

## Product Vision

**ClinicCare** is a clinic management system for a **single doctor** operating **one or more branches**. It manages the complete patient lifecycle from registration through diagnosis, treatment, and billing, with patient records shared across branches while appointments and queues remain branch-specific.

### Core Principles

1. **Simplicity First:** Avoid hospital-grade complexity; optimize for single-doctor workflow
2. **Evidence-Based:** Features justified by reference research (CARE, OpenEMR, etc.)
3. **Progressive Disclosure:** Essential features in v1.0, advanced features in later versions
4. **Branch-Aware:** Patient data global, operational data (appointments, queue) branch-specific
5. **Internet-Independent:** Runs on local network without internet dependency; SQLite-based with LAN access

### Explicit Non-Goals

- ❌ Multi-tenancy (one installation = one doctor)
- ❌ Organization hierarchy (flat Branch model only)
- ❌ Hospital-grade scheduling (no resource pooling, complex availability)
- ❌ Insurance claim submission (manual filing only)
- ❌ Laboratory integration (external lab results as attachments)
- ❌ Imaging integration (PACS, DICOM)
- ❌ Pharmacy dispensing (prescription printing only, no stock management in v1.0)
- ❌ Telemedicine (future consideration)
- ❌ Patient portal (future consideration)
- ❌ Mobile apps (future consideration)

---

## Priority Levels

- **P0 (Critical):** Blocking release; core workflows cannot function without these
- **P1 (High):** Major features; significantly degrades experience if missing
- **P2 (Medium):** Nice-to-have; improves efficiency but not essential
- **P3 (Low):** Future consideration; can be added post-v1.0

---

## Feature Breakdown

### 1. Foundation & Infrastructure (P0)

#### 1.1 Internet-Independent Operation
**Priority:** P0  
**Evidence:** User directive, care_clinic desktop deployment model  
**Milestone:** M1 (Sprint 1, Week 1)

**Features:**
- Application runs on local server/desktop (Docker + SQLite)
- Multiple clinic devices access via LAN (http://192.168.1.x:5000)
- No internet connection required for operation
- No cloud services, no external API dependencies
- Static IP configuration for server
- LAN-accessible web interface (not client-side offline)

**Success Criteria:**
- Server accessible from multiple LAN devices
- Application works without internet connection
- CORS configured for LAN subnet
- Firewall allows LAN access to port 5000

**Architecture:**
```
Clinic LAN (192.168.1.0/24)
  ├─ Server PC (192.168.1.100:5000) - Docker + SQLite
  ├─ Reception Desktop (192.168.1.101) - Browser client
  └─ Doctor Laptop (192.168.1.102) - Browser client
  
NO INTERNET REQUIRED
```

**Status:** Architecture supports this, configuration needed Sprint 1

#### 1.2 Authentication & Authorization
**Priority:** P0  
**Evidence:** Every reference system (CARE, OpenEMR, OpenMRS)  
**Milestone:** M0 (DONE)

**Features:**
- ✅ Local authentication (username/password)
- ✅ JWT token-based sessions
- ✅ Role-based access control (Doctor, Receptionist, future roles)
- ✅ Password hashing (ASP.NET Identity)

**Success Criteria:**
- User can log in with credentials
- JWT token issued and validated
- Role-based endpoint protection

**Status:** COMPLETE in Milestone 0

#### 1.3 Multi-Branch Support
**Priority:** P0  
**Evidence:** User directive, CARE FacilityOrganization  
**Milestone:** M1

**Features:**
- Branch entity (Id, Name, Address, Phone, IsActive)
- Branch selection at login for Receptionist role
- Doctor sees all branches by default
- Patient records global, appointments/queue branch-specific

**Success Criteria:**
- Admin can create/edit/deactivate branches
- Receptionist logs into specific branch
- Appointments filtered by branch
- Patient search works across all branches

**Acceptance Tests:**
- Create 2 branches (Main Clinic, North Branch)
- Register same patient at both branches
- Book appointment at North Branch only
- Verify Main Clinic doesn't see North Branch appointment
- Verify both branches see patient in search

#### 1.4 Audit Logging
**Priority:** P0  
**Evidence:** CARE AuditableEntity, HIPAA compliance requirement  
**Milestone:** M1

**Features:**
- Base AuditableEntity (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
- Automatic audit trail for all entities
- Soft delete (IsDeleted flag, no hard deletes)

**Success Criteria:**
- All entities inherit audit fields
- EF Core SaveChanges automatically populates audit fields
- Deleted records remain queryable with IsDeleted filter

**Acceptance Tests:**
- Create patient → verify CreatedBy/CreatedDate
- Update patient → verify ModifiedBy/ModifiedDate
- Delete patient → verify IsDeleted=true, record still exists

---

### 2. Patient Management (P0)

#### 2.1 Patient Registration
**Priority:** P0  
**Evidence:** CARE Patient entity, every EMR system  
**Milestone:** M1

**Features:**
- Demographics (Name, DOB, Gender, BloodType)
- Contact (Address, Phone, Email, Emergency Contact)
- Identifiers (Passport, National ID, Medical Record Number)
- Photo upload (optional)
- Allergies (free text, future: coded)
- Medical History (free text, future: structured)

**Success Criteria:**
- Receptionist can register new patient in <2 minutes
- Required fields: Name, DOB, Gender, at least one identifier
- Duplicate detection (name + DOB warning)
- Patient assigned unique MRN automatically

**Acceptance Tests:**
- Register patient with passport number
- Register patient with national ID
- Try duplicate name + DOB → warning shown
- Verify auto-generated MRN (format: MRN-YYYYMMDD-####)

#### 2.2 Patient Search
**Priority:** P0  
**Evidence:** Universal requirement  
**Milestone:** M1

**Features:**
- Search by name (partial match, case-insensitive)
- Search by MRN (exact match)
- Search by identifier (passport, national ID)
- Search by phone number
- Results across all branches
- Recent patients quick list (per user)

**Success Criteria:**
- Search returns results in <1 second for 10,000 patients
- Partial name matches (e.g., "john" finds "John Smith", "Johnny Doe")
- Case-insensitive

**Acceptance Tests:**
- Search "john" → finds 3 patients
- Search "MRN-20261020-0001" → finds exact patient
- Search phone "555-1234" → finds patient with that number

#### 2.3 Patient Profile
**Priority:** P0  
**Evidence:** CARE Patient detail view  
**Milestone:** M1

**Features:**
- View/edit demographics
- View/edit contact information
- View/edit identifiers
- View encounter history (list)
- View appointment history
- View billing history (total owed, last payment)

**Success Criteria:**
- Profile loads in <500ms
- Edit mode with validation
- Changes saved to audit log

**Acceptance Tests:**
- Open patient profile
- Edit phone number → save → verify audit log
- View encounter history → sorted by date descending

---

### 3. Scheduling & Queue (P0)

#### 3.1 Appointment Booking
**Priority:** P0  
**Evidence:** CARE TokenBooking, OpenEMR appointments  
**Milestone:** M2

**Features:**
- Book appointment (Patient, Branch, DateTime, ReasonForVisit)
- View daily/weekly calendar
- Appointment status (Scheduled, Confirmed, CheckedIn, Completed, Cancelled, NoShow)
- Conflict detection (same time slot)
- Receptionist books for any branch, Doctor sees all branches

**Success Criteria:**
- Book appointment in <30 seconds
- Conflict warning if double-booked
- Calendar view shows all appointments for selected branch/date

**Acceptance Tests:**
- Book appointment for tomorrow 10:00 AM
- Try book another patient same time → conflict warning
- Change appointment status to Confirmed
- Mark appointment as NoShow

#### 3.2 Walk-In Queue
**Priority:** P0  
**Evidence:** CARE Token/TokenQueue, common in clinics  
**Milestone:** M2

**Features:**
- Add patient to queue (walk-in or checked-in appointment)
- Queue number assigned automatically (1, 2, 3...)
- Queue status (Waiting, InProgress, Completed, Cancelled)
- Call next patient button
- Queue display (for waiting room TV)

**Success Criteria:**
- Add patient to queue in <10 seconds
- Queue number visible to patient
- Doctor sees queue in order
- "Call Next" advances queue

**Acceptance Tests:**
- Add 3 walk-in patients to queue
- Verify queue numbers 1, 2, 3
- Doctor calls patient #1 → status InProgress
- Complete patient #1 → call next → patient #2 InProgress

#### 3.3 Daily Schedule View
**Priority:** P1  
**Evidence:** Standard feature  
**Milestone:** M2

**Features:**
- Timeline view (8 AM - 8 PM)
- Drag-and-drop reschedule
- Color-coded by status
- Show appointment + queue in same view

**Success Criteria:**
- Clear visual distinction between appointments and queue
- Drag appointment to new time → conflict check → save

**Acceptance Tests:**
- View schedule for today
- Drag 10 AM appointment to 2 PM
- Verify conflict if 2 PM already booked

---

### 4. Clinical Documentation (P0)

#### 4.1 Encounter Creation
**Priority:** P0  
**Evidence:** CARE Encounter, OpenEMR encounters  
**Milestone:** M3

**Features:**
- Start encounter from appointment or queue
- Encounter type (OutPatient, Emergency, FollowUp)
- SOAP notes (Subjective, Objective, Assessment, Plan)
- Vitals (Height, Weight, BP, Temp, Pulse, SpO2)
- Diagnosis (ICD-10 code + description)
- Treatment plan (free text)

**Success Criteria:**
- Doctor starts encounter from queue in <5 seconds
- SOAP notes support rich text (bold, bullets)
- Vitals saved with units (kg, cm, mmHg, °C, bpm, %)

**Acceptance Tests:**
- Start encounter for patient in queue
- Enter vitals: Weight 70kg, BP 120/80, Temp 37°C
- Enter SOAP notes
- Add diagnosis: J00 (Acute nasopharyngitis - common cold)
- Save encounter → verify in patient history

#### 4.2 Prescription Writing
**Priority:** P0  
**Evidence:** CARE MedicationRequest, universal requirement  
**Milestone:** M3

**Features:**
- Medication search (generic name, brand name)
- Dosage, frequency, duration
- Instructions (free text)
- Prescription printing (PDF)

**Success Criteria:**
- Search medication by name (autocomplete)
- Generate prescription PDF with clinic letterhead
- Include doctor signature (image upload)

**Acceptance Tests:**
- During encounter, add prescription
- Search "amoxicillin" → select from list
- Set dosage: 500mg, frequency: 3x daily, duration: 7 days
- Instructions: "Take with food"
- Print prescription → verify PDF generated

#### 4.3 Encounter History
**Priority:** P1  
**Evidence:** Continuity of care requirement  
**Milestone:** M3

**Features:**
- List all encounters for patient
- Filter by date range, encounter type
- View encounter details (read-only after completion)
- Amend encounter (add addendum, not edit original)

**Success Criteria:**
- Load last 10 encounters in <500ms
- Full encounter history paginated (20 per page)

**Acceptance Tests:**
- View patient with 15 encounters
- Filter to last 30 days → see 5 encounters
- Open encounter from 2 months ago → read-only
- Add addendum to old encounter → saved with audit log

---

### 5. Billing & Payments (P0)

#### 5.1 Service Catalog
**Priority:** P0  
**Evidence:** CARE ChargeItemDefinition  
**Milestone:** M4

**Features:**
- Service definition (Code, Name, Description, DefaultPrice, Category)
- Service categories (Consultation, Procedure, Lab, Imaging, Pharmacy)
- Price editing (admin only)
- Active/inactive flag

**Success Criteria:**
- Admin can add/edit services
- Services searchable by name/code
- Price changes audited

**Acceptance Tests:**
- Add service: "General Consultation", 100 USD
- Add service: "Blood Test - CBC", 50 USD
- Edit consultation price to 120 USD → verify audit log

#### 5.2 Invoice Generation
**Priority:** P0  
**Evidence:** CARE Invoice, universal requirement  
**Milestone:** M4

**Features:**
- Auto-generate invoice from encounter
- Manual invoice creation (for services without encounter)
- Invoice line items (Service, Quantity, Price, Discount, Tax)
- Invoice status (Draft, Issued, Paid, PartiallyPaid, Cancelled)
- Invoice printing (PDF)

**Success Criteria:**
- Generate invoice from encounter in <10 seconds
- Line items editable before issue
- PDF includes clinic details, patient details, line items, total

**Acceptance Tests:**
- Complete encounter with consultation + prescription
- Generate invoice → verify 2 line items
- Add discount 10% to consultation
- Issue invoice → status changes to Issued
- Print invoice → verify PDF

#### 5.3 Payment Recording
**Priority:** P0  
**Evidence:** CARE PaymentReconciliation  
**Milestone:** M4

**Features:**
- Record payment (Amount, Method, Reference, Date)
- Payment methods (Cash, Card, BankTransfer, Insurance, Other)
- Partial payments (invoice remains PartiallyPaid)
- Receipt printing (PDF)
- Refund recording (negative payment)

**Success Criteria:**
- Record payment in <15 seconds
- Auto-calculate balance (invoice total - payments)
- Receipt includes payment details, balance

**Acceptance Tests:**
- Invoice total: 150 USD
- Record payment: 100 USD Cash
- Verify invoice status → PartiallyPaid
- Record payment: 50 USD Card
- Verify invoice status → Paid
- Print receipt → verify total paid 150 USD

#### 5.4 Financial Reports
**Priority:** P1  
**Evidence:** Business requirement  
**Milestone:** M4

**Features:**
- Daily revenue report (by payment method)
- Outstanding invoices report
- Service revenue breakdown
- Export to Excel

**Success Criteria:**
- Generate report for date range
- Export to Excel in <5 seconds

**Acceptance Tests:**
- Generate report for last 7 days
- Verify total cash: 500 USD, total card: 300 USD
- Export to Excel → open file → verify data matches

---

### 6. Inventory & Pharmacy (P2)

#### 6.1 Medication Master
**Priority:** P1  
**Evidence:** CARE MedicationRequest prerequisite  
**Milestone:** M5

**Features:**
- Medication entity (GenericName, BrandName, Strength, Form, Manufacturer)
- Medication search (autocomplete)
- Active/inactive flag

**Success Criteria:**
- Search returns results in <500ms
- Supports 5,000+ medications

**Acceptance Tests:**
- Add medication: Amoxicillin 500mg Capsule
- Search "amoxi" → finds medication
- Mark inactive → doesn't appear in prescription search

#### 6.2 Stock Management
**Priority:** P2  
**Evidence:** CARE doesn't have it, but common feature  
**Milestone:** M6 (future)

**Features:**
- Stock tracking (per branch)
- Stock movements (Purchase, Sale, Adjustment, Transfer)
- Low stock alerts
- Expiry date tracking

**Success Criteria:**
- Add stock: 100 units Amoxicillin, expiry 2027-12-31
- Dispense 10 units → stock balance 90
- Low stock alert when <20 units

**Acceptance Tests:**
- TBD (post-v1.0)

---

### 7. Reporting & Analytics (P1)

#### 7.1 Operational Reports
**Priority:** P1  
**Evidence:** Business requirement  
**Milestone:** M5

**Features:**
- Daily patient count (appointments + walk-ins)
- Appointment no-show rate
- Average wait time
- Top 10 diagnoses
- Export to PDF/Excel

**Success Criteria:**
- Generate report for date range in <10 seconds
- Data accurate (matches manual count)

**Acceptance Tests:**
- Generate report for last 30 days
- Verify patient count matches encounter count
- Verify no-show rate = (NoShow appointments / Total appointments) * 100

#### 7.2 Clinical Analytics
**Priority:** P2  
**Evidence:** Quality improvement  
**Milestone:** M6 (future)

**Features:**
- Diagnosis distribution (ICD-10 codes)
- Prescription patterns (top medications)
- Patient demographics breakdown
- Encounter type distribution

**Success Criteria:**
- TBD (post-v1.0)

**Acceptance Tests:**
- TBD (post-v1.0)

---

### 8. System Administration (P1)

#### 8.1 User Management
**Priority:** P1  
**Evidence:** Multi-user system requirement  
**Milestone:** M1

**Features:**
- Create/edit/deactivate users
- Assign roles (Doctor, Receptionist, Admin)
- Assign branches (for Receptionist)
- Change password
- View audit log for user actions

**Success Criteria:**
- Admin can manage users
- Role changes take effect immediately
- Deactivated users cannot log in

**Acceptance Tests:**
- Create receptionist user for North Branch
- Log in as receptionist → verify only North Branch visible
- Deactivate user → verify login fails

#### 8.2 System Configuration
**Priority:** P2  
**Evidence:** Customization requirement  
**Milestone:** M5

**Features:**
- Clinic settings (Name, Logo, Address, Phone, Email)
- Appointment slot duration (default 30 min, configurable)
- Invoice numbering format
- Prescription template
- Backup/restore database

**Success Criteria:**
- Changes saved and applied immediately
- Backup creates SQLite file copy
- Restore overwrites current database

**Acceptance Tests:**
- Change clinic name → verify appears on invoices
- Change appointment slot to 15 min → verify calendar grid updates
- Backup database → verify .db file created
- Restore backup → verify data restored

---

### 9. Localization & Accessibility (P1)

#### 9.1 Multi-Language Support
**Priority:** P1  
**Evidence:** M0 implementation (i18n)  
**Milestone:** M0 (DONE - framework), M1-M5 (translations)

**Features:**
- ✅ English (default)
- Arabic (RTL support)
- French (future)

**Success Criteria:**
- Language switch without reload
- RTL layout for Arabic
- 100% translation coverage for included languages

**Acceptance Tests:**
- Switch to Arabic → verify RTL layout
- Verify all labels translated (no English fallback)

#### 9.2 Accessibility (WCAG 2.1 AA)
**Priority:** P1  
**Evidence:** Legal requirement in many jurisdictions  
**Milestone:** M1-M5 (ongoing)

**Features:**
- Keyboard navigation (tab order, shortcuts)
- Screen reader support (ARIA labels)
- Color contrast (minimum 4.5:1)
- Focus indicators
- Error messages (clear, actionable)

**Success Criteria:**
- Pass WCAG 2.1 AA automated tests
- Keyboard-only navigation functional
- Screen reader announces all interactive elements

**Acceptance Tests:**
- Navigate entire app with keyboard only
- Use NVDA/JAWS to verify screen reader support
- Run axe DevTools → zero critical violations

---

## Feature Dependencies

```
M0 (DONE): Auth + Foundation
│
├─ M1: Branch + Patient + Audit
│  │
│  ├─ M2: Scheduling + Queue
│  │  │
│  │  └─ M3: Encounters + Prescriptions
│  │     │
│  │     └─ M4: Billing + Payments
│  │        │
│  │        └─ M5: Reports + Medication Master + Config
│  │           │
│  │           └─ M6: Stock Management + Advanced Analytics (future)
│
└─ (ongoing): Localization + Accessibility
```

---

## Success Metrics

### v1.0 Release Criteria (P0 + P1 features)

**Functional:**
- ✅ Doctor can manage patient from registration → encounter → prescription → billing
- ✅ Receptionist can book appointments, manage queue, record payments
- ✅ Admin can configure clinic, manage users, generate reports
- ✅ All P0 features implemented and tested
- ✅ 80%+ P1 features implemented

**Non-Functional:**
- ✅ System handles 50 patients/day without performance issues
- ✅ Database stores 10,000 patients + 50,000 encounters
- ✅ Response times: Search <1s, Reports <10s, Forms <500ms
- ✅ Zero data loss (audit trail complete)
- ✅ Backup/restore tested and documented

**Quality:**
- ✅ 80%+ code coverage (unit + integration tests)
- ✅ Zero critical security vulnerabilities
- ✅ WCAG 2.1 AA compliance
- ✅ User documentation complete (user guide, admin guide)
- ✅ Deployment guide complete

---

## Scope Control

### Change Request Process

1. **Proposal:** New feature suggested with justification
2. **Impact Analysis:** Estimate effort, dependencies, risks
3. **Priority Assignment:** P0/P1/P2/P3 based on criteria
4. **Decision:** Accept (add to roadmap), Defer (post-v1.0), Reject (out of scope)
5. **Documentation:** Update this document + `19-decision-log.md`

### In-Scope Criteria

Feature must satisfy ALL:
- Supports core clinical workflow (registration → encounter → billing)
- Feasible with single-doctor, multi-branch model
- Evidence from reference research OR explicit user requirement
- ROI justifies development effort

### Out-of-Scope Criteria

Feature rejected if ANY:
- Requires multi-tenancy
- Requires hospital-grade complexity
- Violates "simplicity first" principle
- Better served by external tool (e.g., accounting software)

---

## Traceability

- **User Directives:** Master directive (initial message), "no multi-tenancy", "shared patient records", "branch-specific appointments"
- **Reference Evidence:** CARE entities documented in `01-repository-audit.md`
- **Architecture Decisions:** ADRs in `06-architecture-decisions.md`
- **Workflows:** Detailed in `04-workflows.md`
- **Domain Model:** Entities in `05-domain-model.md`
- **Implementation:** Roadmap in `14-implementation-roadmap.md`

---

## Open Questions

See `16-open-questions.md` for detailed discussion, key questions:

1. **ICD-10 Coding:** Embed full code list (20,000+ codes) or external API?
2. **Prescription Drug Database:** Embed or external?
3. **Insurance Claims:** Manual filing only, or add claim submission?
4. **Lab Integration:** HL7/FHIR or attachments only?
5. **Imaging:** DICOM support or attachments only?

---

## Review & Approval

| Stakeholder | Role | Status | Date | Comments |
|-------------|------|--------|------|----------|
| User | Product Owner | 🟡 PENDING | - | Awaiting review |
| Agent | Architect | 🟢 APPROVED | 2026-10-21 | Evidence-based, traceable |

---

*This document is the contract between user expectations and implementation reality. Changes require explicit approval.*
