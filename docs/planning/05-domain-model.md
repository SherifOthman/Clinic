# 05 - Domain Model & Entity Design

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 03-product-scope.md, 04-workflows.md, 06-architecture-decisions.md  
**Feeds Into:** 07-database-design.md, 11-backend-api-specification.md

---

## Purpose

Define all domain entities for ClinicCare with complete property specifications, relationships, constraints, and business rules. This is the authoritative source for entity design - implementation must match this document.

---

## Design Principles

1. **Evidence-Based:** Entity design informed by CARE reference model (40+ entities analyzed)
2. **Simplified for Context:** Hospital-grade complexity reduced to small clinic needs
3. **Audit-First:** All entities inherit audit fields (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
4. **Soft Delete:** No hard deletes; use IsDeleted flag for logical deletion
5. **Concrete + JSON:** Core queryable data in concrete columns, extensions in JSON (ADR-006)
6. **Branch-Aware:** Patient data global, operational data (appointments, queue) branch-specific (ADR-002)
7. **Type Safety:** Enums for fixed value sets (ADR-003)

---

## Entity Categories

### Foundation (6 entities)
- ApplicationUser
- ApplicationRole  
- Branch
- AuditLog
- SystemConfiguration
- UserSession

### Patient Management (4 entities)
- Patient
- PatientIdentifier
- PatientContact
- PatientAllergy

### Scheduling & Queue (3 entities)
- Appointment
- QueueEntry
- AppointmentType (lookup)

### Clinical (7 entities)
- Encounter
- Vitals
- Diagnosis
- Prescription
- MedicationRequest (synonym for Prescription)
- Attachment
- ClinicalNote (for SOAP notes)

### Billing (4 entities)
- Service
- Invoice
- InvoiceLineItem
- Payment

### Pharmacy (2 entities)
- Medication
- Stock (M6 - future)

### Reporting (Views/DTOs)
- RevenueReportDTO
- OperationalReportDTO
- No persistent entities

**Total: 26 entities for v1.0** (vs CARE's 40+ entities)

---

## Base Entities

### AuditableEntity (Abstract Base Class)

**Purpose:** Provides audit trail for all entities

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| Id | Guid | No | Primary key (GUID for distributed scenarios) |
| CreatedBy | string | No | User ID who created the record |
| CreatedDate | DateTime | No | UTC timestamp of creation |
| ModifiedBy | string | Yes | User ID who last modified (null if never modified) |
| ModifiedDate | DateTime | Yes | UTC timestamp of last modification |
| IsDeleted | bool | No | Soft delete flag (default false) |
| DeletedBy | string | Yes | User ID who deleted |
| DeletedDate | DateTime | Yes | UTC timestamp of deletion |

**C# Implementation:**
```csharp
public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
}
```

**Business Rules:**
- CreatedBy, CreatedDate set automatically on insert (EF Core SaveChanges intercept)
- ModifiedBy, ModifiedDate set automatically on update
- IsDeleted = true hides from default queries (global query filter)
- Hard deletes prohibited (no DELETE SQL allowed)

---

## 1. Foundation Entities

### 1.1 ApplicationUser

**Namespace:** Domain.Entities  
**Inherits From:** IdentityUser (ASP.NET Identity)  
**Purpose:** System users (Doctor, Receptionist, Admin)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Id | string | No | 450 | Primary key (from IdentityUser) |
| UserName | string | No | 256 | Login username (unique) |
| Email | string | No | 256 | Email address (unique) |
| FullName | string | No | 200 | Display name |
| IsActive | bool | No | - | Account active flag (default true) |
| DefaultBranchId | Guid | Yes | - | Default branch for Receptionist role |
| LicenseNumber | string | Yes | 100 | Medical license number (for Doctor role) |
| Signature | byte[] | Yes | - | Digital signature image (for prescriptions) |
| Preferences | string | Yes | - | JSON: UI preferences, language, etc. |

**Relationships:**
- **DefaultBranch:** Many-to-One with Branch
- **UserRoles:** Many-to-Many with ApplicationRole (via IdentityUserRole)
- **AssignedBranches:** Many-to-Many with Branch (via UserBranch join table)

**Indexes:**
- UserName (unique)
- Email (unique)
- IsActive (for filtering)

**Business Rules:**
- UserName must be unique, 3-50 characters, alphanumeric + underscore
- Email must be valid format
- FullName required, 2-200 characters
- LicenseNumber required for Doctor role
- Receptionist must have at least one assigned branch
- Doctor sees all branches by default (no assignment needed)

**Evidence:** CARE OrganizationUser entity

---

### 1.2 ApplicationRole

**Namespace:** Domain.Entities  
**Inherits From:** IdentityRole  
**Purpose:** Role-based access control

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| Id | string | No | Primary key (from IdentityRole) |
| Name | string | No | Role name (Doctor, Receptionist, Admin) |
| Description | string | Yes | Role description |

**Predefined Roles:**
- **Doctor:** Full clinical access, all branches, read-only admin
- **Receptionist:** Patient registration, scheduling, billing, assigned branches only
- **Admin:** Full system access, user management, configuration

**Business Rules:**
- Cannot delete predefined roles
- Cannot create custom roles in v1.0 (future enhancement)

**Evidence:** Standard RBAC pattern

---

### 1.3 Branch

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Clinic branches (multiple locations for same doctor)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Name | string | No | 100 | Branch name (e.g., "Main Clinic", "North Branch") |
| Code | string | Yes | 20 | Short code (e.g., "MAIN", "NTH") |
| AddressLine1 | string | No | 200 | Street address |
| AddressLine2 | string | Yes | 200 | Apt/Suite/Building |
| City | string | No | 100 | City |
| State | string | Yes | 100 | State/Province |
| PostalCode | string | Yes | 20 | Postal/ZIP code |
| Country | string | No | 100 | Country |
| Phone | string | Yes | 50 | Contact phone |
| Email | string | Yes | 200 | Contact email |
| IsActive | bool | No | - | Branch operational flag (default true) |

**Relationships:**
- **Appointments:** One-to-Many with Appointment
- **QueueEntries:** One-to-Many with QueueEntry
- **Encounters:** One-to-Many with Encounter
- **AssignedUsers:** Many-to-Many with ApplicationUser (Receptionists)

**Indexes:**
- Name (unique)
- Code (unique, if not null)
- IsActive (for filtering)

**Business Rules:**
- Name required, unique
- Address (Line1, City, Country) required
- At least one branch must exist (seed data: "Main Clinic")
- Cannot delete branch if it has active appointments/encounters (deactivate instead)
- Deactivated branch hidden from dropdown but history preserved

**Evidence:** CARE FacilityOrganization (simplified, no hierarchy)  
**Decision:** ADR-002 (Branch Model, Not Organization Hierarchy)

---

### 1.4 AuditLog

**Namespace:** Domain.Entities  
**Inherits From:** None (not auditable itself)  
**Purpose:** Detailed audit trail for sensitive operations

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Id | Guid | No | - | Primary key |
| UserId | string | No | 450 | User who performed action |
| EntityType | string | No | 200 | Entity name (e.g., "Patient", "Encounter") |
| EntityId | string | No | 100 | Entity primary key |
| Action | string | No | 50 | Action type (Create, Update, Delete, View) |
| Timestamp | DateTime | No | - | UTC timestamp |
| Changes | string | Yes | - | JSON: old value → new value |
| IPAddress | string | Yes | 50 | User's IP address |
| UserAgent | string | Yes | 500 | Browser/device info |

**Indexes:**
- UserId, Timestamp (for user activity reports)
- EntityType, EntityId (for entity history)
- Timestamp (for time-range queries)

**Business Rules:**
- Log creation automatic (EF Core interceptor or middleware)
- Logs immutable (no updates/deletes)
- Log sensitive operations: Create/Update/Delete Patient, Encounter, Invoice, Payment
- Log views for sensitive data: Patient PHI, Financial reports
- Retention policy: Keep 7 years (compliance requirement)

**Evidence:** HIPAA audit log requirement, CARE AuditableEntity pattern

---

### 1.5 SystemConfiguration

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Key-value store for system settings

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Key | string | No | 100 | Setting key (e.g., "ClinicName", "TaxRate") |
| Value | string | Yes | 1000 | Setting value (string, parse as needed) |
| DataType | string | No | 50 | Value type hint (String, Int, Decimal, Boolean, JSON) |
| Category | string | No | 50 | Setting category (General, Billing, Scheduling, etc.) |
| Description | string | Yes | 500 | Setting description (for admin UI) |

**Predefined Settings:**
- **ClinicName:** string, General
- **ClinicLogo:** string (base64 or file path), General
- **TaxRate:** decimal, Billing (default 0)
- **InvoiceNumberFormat:** string, Billing (default "INV-{YYYYMMDD}-{####}")
- **AppointmentSlotDuration:** int, Scheduling (default 30 minutes)
- **DefaultEncounterType:** string, Clinical (default "OutPatient")

**Indexes:**
- Key (unique)
- Category (for grouping in UI)

**Business Rules:**
- Key required, unique
- Admin role required to modify
- Changes audited
- Settings cached in memory, refresh on update

**Evidence:** Common pattern in configurable systems

---

## 2. Patient Management Entities

### 2.1 Patient

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Core patient demographics and medical summary

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| MedicalRecordNumber | string | No | 50 | Auto-generated unique MRN (format: MRN-YYYYMMDD-####) |
| FirstName | string | No | 100 | Given name |
| MiddleName | string | Yes | 100 | Middle name |
| LastName | string | No | 100 | Family name |
| DateOfBirth | DateOnly | No | - | Birth date (no time) |
| Gender | GenderType | No | - | Enum: Male, Female, Other |
| BloodType | BloodType | Yes | - | Enum: APositive, ANegative, BPositive, ... |
| Photo | byte[] | Yes | - | Patient photo (max 1MB) |
| MaritalStatus | MaritalStatus | Yes | - | Enum: Single, Married, Divorced, Widowed |
| Occupation | string | Yes | 200 | Profession |
| Notes | string | Yes | - | General notes (not clinical) |
| MedicalHistory | string | Yes | - | Chronic conditions, past surgeries, family history |
| IsActive | bool | No | - | Active patient flag (default true) |

**Relationships:**
- **Identifiers:** One-to-Many with PatientIdentifier
- **Contacts:** One-to-Many with PatientContact
- **Allergies:** One-to-Many with PatientAllergy
- **Encounters:** One-to-Many with Encounter
- **Appointments:** One-to-Many with Appointment
- **QueueEntries:** One-to-Many with QueueEntry
- **Invoices:** One-to-Many with Invoice

**Computed Properties (not stored):**
- **FullName:** $"{FirstName} {MiddleName} {LastName}".Trim()
- **Age:** Calculate from DateOfBirth to today (years)

**Indexes:**
- MedicalRecordNumber (unique)
- FirstName, LastName (composite, for name search)
- DateOfBirth (for DOB search, duplicate detection)
- IsActive (for filtering)

**Business Rules:**
- MedicalRecordNumber auto-generated, immutable, format: MRN-YYYYMMDD-#### (date + sequence)
- FirstName, LastName required, 1-100 characters
- DateOfBirth required, not in future, minimum age 0 (newborn), maximum age 150
- Gender required (default Male if not specified)
- Photo max size 1MB, accepted formats: JPG, PNG
- Duplicate detection: warn if same FirstName + LastName + DateOfBirth exists
- Cannot hard delete (soft delete only)

**Evidence:** CARE Patient entity (simplified)  
**Decision:** ADR-002 (Patient data global, not branch-specific)

---

### 2.2 PatientIdentifier

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Patient identification documents (passport, national ID, etc.)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| IdentifierType | PatientIdentifierType | No | - | Enum: Passport, NationalID, DriverLicense, Other |
| IdentifierValue | string | No | 100 | Document number |
| IssuingAuthority | string | Yes | 200 | Issuing country/agency |
| IssueDate | DateOnly | Yes | - | Document issue date |
| ExpiryDate | DateOnly | Yes | - | Document expiry date |
| IsPrimary | bool | No | - | Primary identifier flag (default false) |

**Relationships:**
- **Patient:** Many-to-One with Patient

**Indexes:**
- PatientId (foreign key)
- IdentifierType, IdentifierValue (composite unique)
- IsPrimary (for quick lookup)

**Business Rules:**
- IdentifierType, IdentifierValue required
- IdentifierValue unique per IdentifierType (e.g., two patients can't have same passport number)
- At least one identifier required per patient (enforced in application, not DB)
- Only one identifier can be IsPrimary per patient
- ExpiryDate must be after IssueDate (if both present)
- Warn if document expired (ExpiryDate < today)

**Evidence:** CARE PatientIdentifier entity  
**Decision:** ADR-003 (Enum-based identifier types, not config-driven)

**Enums:**
```csharp
public enum PatientIdentifierType
{
    Passport = 1,
    NationalID = 2,
    DriverLicense = 3,
    MilitaryID = 4,
    Other = 99
}
```

---

### 2.3 PatientContact

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Patient contact information and emergency contacts

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| ContactType | ContactType | No | - | Enum: Primary, Emergency, NextOfKin |
| Relationship | string | Yes | 100 | Relationship (for Emergency/NextOfKin: Spouse, Parent, etc.) |
| FullName | string | Yes | 200 | Contact person name (null for Primary = patient themself) |
| Phone | string | Yes | 50 | Phone number |
| Email | string | Yes | 200 | Email address |
| AddressLine1 | string | Yes | 200 | Street address |
| AddressLine2 | string | Yes | 200 | Apt/Suite |
| City | string | Yes | 100 | City |
| State | string | Yes | 100 | State/Province |
| PostalCode | string | Yes | 20 | Postal code |
| Country | string | Yes | 100 | Country |
| IsPrimary | bool | No | - | Primary contact flag (default false) |

**Relationships:**
- **Patient:** Many-to-One with Patient

**Indexes:**
- PatientId (foreign key)
- ContactType, IsPrimary (for quick lookup)

**Business Rules:**
- At least one contact with ContactType = Primary required per patient
- At least one Emergency contact recommended (warning if missing)
- Only one contact of each ContactType can be IsPrimary
- Phone OR Email required (at least one)
- FullName required for Emergency/NextOfKin contacts

**Enums:**
```csharp
public enum ContactType
{
    Primary = 1,      // Patient's own contact info
    Emergency = 2,    // Emergency contact person
    NextOfKin = 3     // Legal next of kin
}
```

**Evidence:** Standard practice in EMR systems

---

### 2.4 PatientAllergy

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Patient allergies and adverse reactions

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| Allergen | string | No | 200 | Substance (e.g., "Penicillin", "Peanuts") |
| AllergyType | AllergyType | No | - | Enum: Medication, Food, Environmental, Other |
| Severity | AllergySeverity | No | - | Enum: Mild, Moderate, Severe, Anaphylaxis |
| Reaction | string | Yes | 500 | Description of reaction (e.g., "Rash", "Difficulty breathing") |
| OnsetDate | DateOnly | Yes | - | When allergy first identified |
| Status | AllergyStatus | No | - | Enum: Active, Resolved, Suspected |

**Relationships:**
- **Patient:** Many-to-One with Patient

**Indexes:**
- PatientId (foreign key)
- AllergyType, Severity (for filtering critical allergies)

**Business Rules:**
- Allergen required, 1-200 characters
- Severity = Severe or Anaphylaxis → highlighted in red in UI
- Status = Resolved → greyed out but still visible
- Warn doctor if prescribing medication matching an active allergy

**Enums:**
```csharp
public enum AllergyType
{
    Medication = 1,
    Food = 2,
    Environmental = 3,
    Other = 99
}

public enum AllergySeverity
{
    Mild = 1,
    Moderate = 2,
    Severe = 3,
    Anaphylaxis = 4
}

public enum AllergyStatus
{
    Active = 1,
    Resolved = 2,
    Suspected = 3
}
```

**Evidence:** CARE (partial), standard EMR practice

---

## 3. Scheduling & Queue Entities

### 3.1 Appointment

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Pre-scheduled patient appointments

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| BranchId | Guid | No | - | Foreign key to Branch |
| DoctorId | string | No | - | Foreign key to ApplicationUser (Doctor role) |
| AppointmentDateTime | DateTime | No | - | Date and time of appointment |
| AppointmentTypeId | Guid | Yes | - | Foreign key to AppointmentType (optional) |
| ReasonForVisit | string | Yes | 500 | Patient's stated reason |
| Status | AppointmentStatus | No | - | Enum: Scheduled, Confirmed, CheckedIn, Completed, Cancelled, NoShow |
| CancellationReason | string | Yes | 500 | Reason if cancelled |
| Notes | string | Yes | 1000 | Internal notes (not visible to patient) |

**Relationships:**
- **Patient:** Many-to-One with Patient
- **Branch:** Many-to-One with Branch
- **Doctor:** Many-to-One with ApplicationUser
- **AppointmentType:** Many-to-One with AppointmentType (optional)
- **QueueEntry:** One-to-One with QueueEntry (created on check-in)
- **Encounter:** One-to-One with Encounter (created when doctor starts encounter)

**Indexes:**
- PatientId (foreign key)
- BranchId (foreign key)
- DoctorId (foreign key)
- AppointmentDateTime, BranchId (composite, for schedule view)
- Status (for filtering)

**Business Rules:**
- AppointmentDateTime must be in future (when created)
- Cannot book two appointments for same patient at same time
- Status workflow: Scheduled → Confirmed → CheckedIn → Completed
  - OR Scheduled → Cancelled
  - OR CheckedIn → NoShow (if patient leaves)
- Status = CheckedIn → automatically create QueueEntry
- Status = Completed → automatically create Encounter (or link if already created)
- CancellationReason required if Status = Cancelled
- Cannot modify completed appointment (only add notes)

**Enums:**
```csharp
public enum AppointmentStatus
{
    Scheduled = 1,
    Confirmed = 2,
    CheckedIn = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}
```

**Evidence:** CARE TokenBooking entity (simplified)  
**Decision:** ADR-004 (2-entity scheduling, not 7-entity)

---

### 3.2 QueueEntry

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Patient queue for doctor (walk-ins + checked-in appointments)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| BranchId | Guid | No | - | Foreign key to Branch |
| AppointmentId | Guid | Yes | - | Foreign key to Appointment (null if walk-in) |
| QueueNumber | int | No | - | Sequential number for the day (1, 2, 3...) |
| QueuedAt | DateTime | No | - | Timestamp when added to queue |
| CalledAt | DateTime | Yes | - | Timestamp when called by doctor |
| CompletedAt | DateTime | Yes | - | Timestamp when encounter completed |
| Status | QueueStatus | No | - | Enum: Waiting, InProgress, Completed, Cancelled |
| Priority | QueuePriority | No | - | Enum: Normal, High, Emergency |
| ReasonForVisit | string | Yes | 500 | Why patient is here |
| Notes | string | Yes | 1000 | Internal notes |

**Relationships:**
- **Patient:** Many-to-One with Patient
- **Branch:** Many-to-One with Branch
- **Appointment:** Many-to-One with Appointment (optional, null for walk-ins)
- **Encounter:** One-to-One with Encounter (created when doctor starts)

**Indexes:**
- PatientId (foreign key)
- BranchId, QueuedAt (composite, for branch queue view)
- Status, Priority (for filtering next patient)
- QueueNumber, BranchId, QueuedAt::Date (composite unique, ensure unique per branch per day)

**Business Rules:**
- QueueNumber auto-assigned (max + 1 for today at this branch)
- QueueNumber resets daily (per branch)
- Status workflow: Waiting → InProgress → Completed
  - OR Waiting → Cancelled (patient left)
- Priority = Emergency → moves to front of queue
- Only one patient InProgress at a time per doctor (validation)
- CompletedAt automatically set when encounter saved

**Enums:**
```csharp
public enum QueueStatus
{
    Waiting = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum QueuePriority
{
    Normal = 1,
    High = 2,
    Emergency = 3
}
```

**Evidence:** CARE Token/TokenQueue entities (simplified)  
**Decision:** ADR-004 (Integrated queue, not separate token system)

---

### 3.3 AppointmentType (Lookup)

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Predefined appointment types with default duration and service mapping

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Name | string | No | 100 | Type name (e.g., "General Consultation", "Follow-Up") |
| Code | string | Yes | 20 | Short code (e.g., "GEN", "FU") |
| Description | string | Yes | 500 | Description |
| DefaultDuration | int | No | - | Default duration in minutes (e.g., 30) |
| DefaultServiceId | Guid | Yes | - | Foreign key to Service (for auto-billing) |
| Color | string | Yes | 20 | Hex color for calendar display (e.g., "#4CAF50") |
| IsActive | bool | No | - | Active flag (default true) |

**Relationships:**
- **DefaultService:** Many-to-One with Service (optional)
- **Appointments:** One-to-Many with Appointment

**Indexes:**
- Name (unique)
- Code (unique if not null)
- IsActive (for filtering)

**Business Rules:**
- Name required, unique
- DefaultDuration must be multiple of slot duration (e.g., 15, 30, 45, 60)
- Deactivated types hidden from dropdown but history preserved

**Seed Data:**
- General Consultation (30 min, green)
- Follow-Up (15 min, blue)
- Emergency (15 min, red)

**Evidence:** Common practice, not in CARE

---

## 4. Clinical Entities

### 4.1 Encounter

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Clinical visit/consultation record

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| PatientId | Guid | No | - | Foreign key to Patient |
| BranchId | Guid | No | - | Foreign key to Branch |
| DoctorId | string | No | - | Foreign key to ApplicationUser (Doctor) |
| AppointmentId | Guid | Yes | - | Foreign key to Appointment (null if walk-in) |
| QueueEntryId | Guid | Yes | - | Foreign key to QueueEntry |
| EncounterNumber | string | No | 50 | Auto-generated unique number (format: ENC-YYYYMMDD-####) |
| EncounterDateTime | DateTime | No | - | Start date/time |
| CompletedAt | DateTime | Yes | - | Completion timestamp |
| EncounterType | EncounterType | No | - | Enum: OutPatient, Emergency, FollowUp, Consultation |
| Status | EncounterStatus | No | - | Enum: InProgress, Completed, Cancelled |
| ChiefComplaint | string | Yes | 500 | Primary reason for visit |
| Subjective | string | Yes | - | SOAP: Patient's description of symptoms |
| Objective | string | Yes | - | SOAP: Doctor's examination findings |
| Assessment | string | Yes | - | SOAP: Diagnosis/impression |
| Plan | string | Yes | - | SOAP: Treatment plan |

**Relationships:**
- **Patient:** Many-to-One with Patient
- **Branch:** Many-to-One with Branch
- **Doctor:** Many-to-One with ApplicationUser
- **Appointment:** Many-to-One with Appointment (optional)
- **QueueEntry:** Many-to-One with QueueEntry (optional)
- **Vitals:** One-to-One with Vitals (optional)
- **Diagnoses:** One-to-Many with Diagnosis
- **Prescriptions:** One-to-Many with Prescription
- **Attachments:** One-to-Many with Attachment
- **ClinicalNotes:** One-to-Many with ClinicalNote (for addendums)
- **Invoice:** One-to-One with Invoice (auto-generated)

**Indexes:**
- EncounterNumber (unique)
- PatientId (foreign key)
- BranchId (foreign key)
- DoctorId (foreign key)
- EncounterDateTime (for chronological queries)
- Status (for filtering)

**Business Rules:**
- EncounterNumber auto-generated, immutable, format: ENC-YYYYMMDD-####
- EncounterDateTime defaults to current timestamp
- Status = InProgress → editable
- Status = Completed → read-only, only addendums allowed
- CompletedAt automatically set when status changes to Completed
- At least one of (Vitals, Subjective, Objective, Assessment, Plan, Diagnoses) required before completion
- Completing encounter triggers:
  - QueueEntry status = Completed
  - Appointment status = Completed (if linked)
  - Invoice generation (if billing enabled)

**Enums:**
```csharp
public enum EncounterType
{
    OutPatient = 1,
    Emergency = 2,
    FollowUp = 3,
    Consultation = 4
}

public enum EncounterStatus
{
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
```

**Evidence:** CARE Encounter entity (simplified)

---

### 4.2 Vitals

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Physiological measurements

**Properties:**
| Property | Type | Nullable | Unit | Description |
|----------|------|----------|------|-------------|
| EncounterId | Guid | No | - | Foreign key to Encounter (one-to-one) |
| Height | decimal | Yes | cm | Height in centimeters |
| Weight | decimal | Yes | kg | Weight in kilograms |
| BMI | decimal | Yes | kg/m² | Auto-calculated from height/weight |
| Temperature | decimal | Yes | °C | Body temperature in Celsius |
| BloodPressureSystolic | int | Yes | mmHg | Systolic BP |
| BloodPressureDiastolic | int | Yes | mmHg | Diastolic BP |
| HeartRate | int | Yes | bpm | Pulse/heart rate |
| RespiratoryRate | int | Yes | breaths/min | Breathing rate |
| OxygenSaturation | decimal | Yes | % | SpO2 |
| Notes | string | Yes | - | Additional notes |

**Relationships:**
- **Encounter:** One-to-One with Encounter

**Computed Properties:**
- **BMI:** Weight(kg) / (Height(m))²
- **BMI Category:** Underweight (<18.5), Normal (18.5-24.9), Overweight (25-29.9), Obese (≥30)

**Indexes:**
- EncounterId (unique, one-to-one)

**Business Rules:**
- At least one vital required (height OR weight OR temperature OR BP OR heart rate)
- Height range: 30-250 cm (warn if outside)
- Weight range: 1-300 kg (warn if outside)
- Temperature range: 32-42°C (warn if outside normal 36.1-37.2°C)
- BP Systolic range: 50-250 mmHg (warn if >140 or <90)
- BP Diastolic range: 30-150 mmHg (warn if >90 or <60)
- Heart Rate range: 30-200 bpm (warn if outside 60-100)
- Respiratory Rate range: 5-60 breaths/min (warn if outside 12-20)
- Oxygen Saturation range: 50-100% (warn if <95%)
- BMI auto-calculated on save

**Evidence:** CARE Observation entity (vitals subset), standard practice

---

### 4.3 Diagnosis

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Clinical diagnosis with ICD-10 coding

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| EncounterId | Guid | No | - | Foreign key to Encounter |
| ICD10Code | string | Yes | 20 | ICD-10 code (e.g., "J00", "E11.9") |
| Description | string | No | 500 | Diagnosis description |
| DiagnosisType | DiagnosisType | No | - | Enum: Primary, Secondary, Comorbidity |
| Status | DiagnosisStatus | No | - | Enum: Active, Resolved, Ruled Out |
| OnsetDate | DateOnly | Yes | - | When condition started |
| Notes | string | Yes | 1000 | Additional notes |

**Relationships:**
- **Encounter:** Many-to-One with Encounter

**Indexes:**
- EncounterId (foreign key)
- ICD10Code (for reporting)
- DiagnosisType (for primary diagnosis queries)

**Business Rules:**
- Description required, 1-500 characters
- ICD10Code optional (free text diagnosis allowed)
- Only one Primary diagnosis per encounter (recommended)
- Status = Resolved → greyed out in patient history

**Enums:**
```csharp
public enum DiagnosisType
{
    Primary = 1,
    Secondary = 2,
    Comorbidity = 3
}

public enum DiagnosisStatus
{
    Active = 1,
    Resolved = 2,
    RuledOut = 3
}
```

**Evidence:** CARE Condition entity (simplified)

---

### 4.4 Prescription (MedicationRequest)

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Prescription/medication order

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| EncounterId | Guid | No | - | Foreign key to Encounter |
| MedicationId | Guid | Yes | - | Foreign key to Medication (null if free text) |
| MedicationName | string | No | 200 | Medication name (editable) |
| Dosage | string | No | 100 | Dosage (e.g., "500mg", "10ml") |
| Route | MedicationRoute | No | - | Enum: Oral, Topical, Injectable, Inhalation, etc. |
| Frequency | string | No | 100 | Frequency (e.g., "3 times daily", "Every 6 hours") |
| Duration | int | Yes | - | Duration in days |
| Quantity | decimal | Yes | - | Total quantity to dispense |
| Instructions | string | Yes | 500 | Special instructions (e.g., "Take with food") |
| Status | PrescriptionStatus | No | - | Enum: Active, Dispensed, Discontinued, Completed |

**Relationships:**
- **Encounter:** Many-to-One with Encounter
- **Medication:** Many-to-One with Medication (optional)

**Indexes:**
- EncounterId (foreign key)
- MedicationId (foreign key)
- Status (for filtering)

**Business Rules:**
- MedicationName required (from Medication OR free text)
- Dosage, Route, Frequency required
- Duration OR Quantity required (at least one)
- Status = Active → patient currently taking
- Status = Dispensed → medication given to patient (if in-house pharmacy)
- Status = Discontinued → doctor stopped this medication

**Enums:**
```csharp
public enum MedicationRoute
{
    Oral = 1,
    Topical = 2,
    Injectable = 3,
    Inhalation = 4,
    Rectal = 5,
    Other = 99
}

public enum PrescriptionStatus
{
    Active = 1,
    Dispensed = 2,
    Discontinued = 3,
    Completed = 4
}
```

**Evidence:** CARE MedicationRequest entity (simplified)

---

### 4.5 Attachment

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** File attachments (lab reports, images, documents)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| EncounterId | Guid | Yes | - | Foreign key to Encounter (optional, can be patient-level) |
| PatientId | Guid | No | - | Foreign key to Patient |
| FileName | string | No | 255 | Original file name |
| FileExtension | string | No | 10 | File extension (e.g., ".pdf", ".jpg") |
| FileSize | long | No | - | File size in bytes |
| MimeType | string | No | 100 | MIME type (e.g., "application/pdf") |
| StoragePath | string | No | 500 | File path on disk or cloud storage |
| AttachmentType | AttachmentType | No | - | Enum: LabReport, RadiologyReport, Document, Image, Other |
| Description | string | Yes | 500 | Description/notes |

**Relationships:**
- **Encounter:** Many-to-One with Encounter (optional)
- **Patient:** Many-to-One with Patient

**Indexes:**
- EncounterId (foreign key)
- PatientId (foreign key)
- AttachmentType (for filtering)

**Business Rules:**
- FileName required, sanitize for security
- FileSize max 10MB (configurable)
- Allowed file types: PDF, JPG, PNG, DOCX, XLSX (configurable)
- StoragePath format: `{BranchId}/{PatientId}/{Year}/{Month}/{FileName}_{Guid}.{Extension}`
- Virus scan before save (future enhancement)
- Encryption at rest (future enhancement)

**Enums:**
```csharp
public enum AttachmentType
{
    LabReport = 1,
    RadiologyReport = 2,
    Document = 3,
    Image = 4,
    Other = 99
}
```

**Evidence:** Common practice in EMR systems

---

### 4.6 ClinicalNote

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Additional notes and addendums to encounters

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| EncounterId | Guid | No | - | Foreign key to Encounter |
| NoteType | NoteType | No | - | Enum: Addendum, Correction, Progress, Other |
| Content | string | No | - | Note text (rich text/HTML) |

**Relationships:**
- **Encounter:** Many-to-One with Encounter

**Indexes:**
- EncounterId (foreign key)
- CreatedDate (for chronological order)

**Business Rules:**
- Content required, no max length (use TEXT column)
- NoteType = Addendum → displayed below original encounter SOAP notes
- NoteType = Correction → displayed with warning flag
- CreatedBy, CreatedDate immutable (cannot edit notes, only add new)

**Enums:**
```csharp
public enum NoteType
{
    Addendum = 1,
    Correction = 2,
    Progress = 3,
    Other = 99
}
```

**Evidence:** Medical record amendment requirements

---

## 5. Billing Entities

### 5.1 Service

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Service catalog (procedures, consultations, tests)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| Code | string | No | 20 | Service code (e.g., "CONS-GEN", "LAB-CBC") |
| Name | string | No | 200 | Service name (e.g., "General Consultation") |
| Description | string | Yes | 500 | Detailed description |
| Category | ServiceCategory | No | - | Enum: Consultation, Procedure, Lab, Imaging, Pharmacy, Other |
| DefaultPrice | decimal | No | - | Default price (editable per invoice line) |
| TaxRate | decimal | Yes | - | Tax rate override (null = use system default) |
| IsActive | bool | No | - | Active flag (default true) |

**Relationships:**
- **InvoiceLineItems:** One-to-Many with InvoiceLineItem

**Indexes:**
- Code (unique)
- Name (for search)
- Category (for filtering)
- IsActive (for filtering)

**Business Rules:**
- Code required, unique, uppercase, alphanumeric + dash
- Name required, 1-200 characters
- DefaultPrice ≥ 0
- TaxRate 0-100% (null = use system default)
- Deactivated services hidden from dropdown but history preserved

**Enums:**
```csharp
public enum ServiceCategory
{
    Consultation = 1,
    Procedure = 2,
    Lab = 3,
    Imaging = 4,
    Pharmacy = 5,
    Other = 99
}
```

**Seed Data:**
- CONS-GEN: General Consultation, $100
- CONS-FU: Follow-Up Consultation, $50
- PROC-SUTURE: Suture/Stitches, $150

**Evidence:** CARE ChargeItemDefinition (simplified)  
**Decision:** ADR-005 (Simplified billing, 4 entities)

---

### 5.2 Invoice

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Patient billing invoice

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| InvoiceNumber | string | No | 50 | Auto-generated (format: INV-YYYYMMDD-####) |
| PatientId | Guid | No | - | Foreign key to Patient |
| EncounterId | Guid | Yes | - | Foreign key to Encounter (null if manual invoice) |
| BranchId | Guid | No | - | Foreign key to Branch |
| InvoiceDate | DateTime | No | - | Invoice issue date |
| DueDate | DateTime | No | - | Payment due date |
| Status | InvoiceStatus | No | - | Enum: Draft, Issued, PartiallyPaid, Paid, Cancelled |
| Subtotal | decimal | No | - | Sum of line items before tax/discount |
| TaxAmount | decimal | No | - | Total tax |
| DiscountAmount | decimal | No | - | Total discount |
| TotalAmount | decimal | No | - | Subtotal + Tax - Discount |
| TotalPaid | decimal | No | - | Sum of payments |
| RemainingBalance | decimal | No | - | TotalAmount - TotalPaid |
| Notes | string | Yes | 1000 | Internal notes |

**Relationships:**
- **Patient:** Many-to-One with Patient
- **Encounter:** Many-to-One with Encounter (optional)
- **Branch:** Many-to-One with Branch
- **LineItems:** One-to-Many with InvoiceLineItem
- **Payments:** One-to-Many with Payment

**Indexes:**
- InvoiceNumber (unique)
- PatientId (foreign key)
- EncounterId (foreign key)
- InvoiceDate (for date range queries)
- Status (for filtering)

**Business Rules:**
- InvoiceNumber auto-generated, immutable
- Status = Draft → editable (line items, discount, etc.)
- Status = Issued → read-only line items, can record payments
- Status = Paid → fully paid (RemainingBalance = 0)
- Status = PartiallyPaid → some payment received (TotalPaid > 0, RemainingBalance > 0)
- Status = Cancelled → voided, no payments allowed
- Totals auto-calculated:
  - Subtotal = Sum(LineItems.Total)
  - TaxAmount = Subtotal × TaxRate (from SystemConfiguration)
  - TotalAmount = Subtotal + TaxAmount - DiscountAmount
  - RemainingBalance = TotalAmount - TotalPaid
- Status auto-updated when payment recorded:
  - If RemainingBalance = 0 → Status = Paid
  - If RemainingBalance > 0 AND TotalPaid > 0 → Status = PartiallyPaid

**Enums:**
```csharp
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}
```

**Evidence:** CARE Invoice entity (simplified)  
**Decision:** ADR-005 (Simplified billing)

---

### 5.3 InvoiceLineItem

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Individual items on an invoice

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| InvoiceId | Guid | No | - | Foreign key to Invoice |
| ServiceId | Guid | Yes | - | Foreign key to Service (null if free text) |
| Description | string | No | 500 | Item description (editable) |
| Quantity | decimal | No | - | Quantity (default 1) |
| UnitPrice | decimal | No | - | Price per unit |
| DiscountPercent | decimal | No | - | Discount % (0-100) |
| DiscountAmount | decimal | No | - | Discount $ |
| TotalPrice | decimal | No | - | (Quantity × UnitPrice) - DiscountAmount |

**Relationships:**
- **Invoice:** Many-to-One with Invoice
- **Service:** Many-to-One with Service (optional)

**Indexes:**
- InvoiceId (foreign key)
- ServiceId (foreign key)

**Business Rules:**
- Description required (from Service OR free text)
- Quantity ≥ 0 (default 1)
- UnitPrice ≥ 0 (from Service.DefaultPrice OR manual entry)
- DiscountPercent 0-100% (default 0)
- DiscountAmount = UnitPrice × Quantity × (DiscountPercent / 100)
- TotalPrice = (Quantity × UnitPrice) - DiscountAmount
- TotalPrice ≥ 0 (discount cannot exceed price)
- Cannot edit line items if Invoice.Status != Draft

**Evidence:** CARE ChargeItem (simplified)

---

### 5.4 Payment

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Payment/refund record

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| InvoiceId | Guid | No | - | Foreign key to Invoice |
| PaymentDate | DateTime | No | - | Payment timestamp |
| Amount | decimal | No | - | Payment amount (negative for refund) |
| PaymentMethod | PaymentMethod | No | - | Enum: Cash, Card, BankTransfer, Insurance, Other |
| Reference | string | Yes | 200 | Transaction reference (e.g., card last 4, check#) |
| Notes | string | Yes | 500 | Payment notes |
| IsRefund | bool | No | - | True if refund (Amount < 0) |
| RefundReason | string | Yes | 500 | Reason if refund |

**Relationships:**
- **Invoice:** Many-to-One with Invoice

**Indexes:**
- InvoiceId (foreign key)
- PaymentDate (for date range queries)
- PaymentMethod (for reports)

**Business Rules:**
- Amount ≠ 0 (must be non-zero)
- Amount > 0 → payment (IsRefund = false)
- Amount < 0 → refund (IsRefund = true)
- RefundReason required if IsRefund = true
- Payment triggers invoice status update (Paid/PartiallyPaid)
- Cannot delete payment (soft delete only)

**Enums:**
```csharp
public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Insurance = 4,
    Cheque = 5,
    Other = 99
}
```

**Evidence:** CARE PaymentReconciliation (simplified)

---

## 6. Pharmacy Entities

### 6.1 Medication

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Medication master data

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| GenericName | string | No | 200 | Generic drug name (e.g., "Amoxicillin") |
| BrandName | string | Yes | 200 | Brand name (e.g., "Amoxil") |
| Strength | string | No | 50 | Strength (e.g., "500mg") |
| Form | MedicationForm | No | - | Enum: Tablet, Capsule, Liquid, Injection, etc. |
| Manufacturer | string | Yes | 200 | Manufacturer name |
| NDC | string | Yes | 20 | National Drug Code (US) or equivalent |
| IsActive | bool | No | - | Active flag (default true) |

**Relationships:**
- **Prescriptions:** One-to-Many with Prescription
- **Stock:** One-to-Many with Stock (future)

**Indexes:**
- GenericName, Strength, Form (composite unique)
- BrandName (for search)
- IsActive (for filtering)

**Business Rules:**
- GenericName required, 1-200 characters
- Strength required (e.g., "500mg", "10ml")
- Form required
- Deactivated medications hidden from prescription dropdown but history preserved

**Enums:**
```csharp
public enum MedicationForm
{
    Tablet = 1,
    Capsule = 2,
    Liquid = 3,
    Syrup = 4,
    Suspension = 5,
    Injection = 6,
    Cream = 7,
    Ointment = 8,
    Drops = 9,
    Inhaler = 10,
    Patch = 11,
    Suppository = 12,
    Other = 99
}
```

**Evidence:** CARE MedicationRequest prerequisite, standard pharmacy practice

---

### 6.2 Stock (Future - M6)

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Medication inventory tracking

**Properties:** TBD (post-v1.0)

**Evidence:** Common in pharmacy management, not in CARE

---

## Entity Relationship Diagram (ERD)

```
Patient (1) ────┬── (*) PatientIdentifier
                ├── (*) PatientContact
                ├── (*) PatientAllergy
                ├── (*) Appointment
                ├── (*) QueueEntry
                ├── (*) Encounter
                ├── (*) Invoice
                └── (*) Attachment

Branch (1) ─────┬── (*) Appointment
                ├── (*) QueueEntry
                ├── (*) Encounter
                └── (*) Invoice

ApplicationUser (1) ──┬── (*) Appointment (Doctor)
                      └── (*) Encounter (Doctor)

Appointment (1) ─────┬── (1) QueueEntry (optional)
                     └── (1) Encounter (optional)

QueueEntry (1) ────── (1) Encounter (optional)

Encounter (1) ───┬── (1) Vitals (optional)
                 ├── (*) Diagnosis
                 ├── (*) Prescription
                 ├── (*) ClinicalNote
                 ├── (*) Attachment
                 └── (1) Invoice (auto-generated)

Invoice (1) ────┬── (*) InvoiceLineItem
                └── (*) Payment

Service (1) ────┬── (*) InvoiceLineItem
                └── (*) AppointmentType.DefaultService

Medication (1) ─── (*) Prescription
```

---

## Database Constraints Summary

### Unique Constraints
- Patient.MedicalRecordNumber
- ApplicationUser.UserName
- ApplicationUser.Email
- Branch.Name
- Branch.Code
- Appointment.PatientId + AppointmentDateTime (warning, not hard constraint)
- QueueEntry.QueueNumber + BranchId + QueuedAt::Date
- Encounter.EncounterNumber
- Invoice.InvoiceNumber
- Service.Code
- PatientIdentifier.IdentifierType + IdentifierValue
- Medication.GenericName + Strength + Form

### Check Constraints
- Vitals.Height > 0
- Vitals.Weight > 0
- Vitals.Temperature > 0
- Vitals.BloodPressureSystolic > 0
- Vitals.BloodPressureDiastolic > 0
- Vitals.HeartRate > 0
- Invoice.TotalAmount ≥ 0
- Invoice.TotalPaid ≥ 0
- Invoice.RemainingBalance ≥ 0
- Payment.Amount ≠ 0
- Service.DefaultPrice ≥ 0
- InvoiceLineItem.Quantity ≥ 0
- InvoiceLineItem.UnitPrice ≥ 0
- InvoiceLineItem.TotalPrice ≥ 0

### Foreign Key Constraints (All with ON DELETE RESTRICT)
- Patient → Branch (DefaultBranch, optional)
- Patient → ApplicationUser (CreatedBy, ModifiedBy)
- Appointment → Patient
- Appointment → Branch
- Appointment → ApplicationUser (Doctor)
- QueueEntry → Patient
- QueueEntry → Branch
- QueueEntry → Appointment (optional)
- Encounter → Patient
- Encounter → Branch
- Encounter → ApplicationUser (Doctor)
- Encounter → Appointment (optional)
- Encounter → QueueEntry (optional)
- Vitals → Encounter (one-to-one)
- Diagnosis → Encounter
- Prescription → Encounter
- Prescription → Medication (optional)
- Invoice → Patient
- Invoice → Encounter (optional)
- Invoice → Branch
- InvoiceLineItem → Invoice
- InvoiceLineItem → Service (optional)
- Payment → Invoice

---

## Implementation Notes

### EF Core Configuration

**Global Query Filters:**
```csharp
// Apply to all entities inheriting AuditableEntity
modelBuilder.Entity<AuditableEntity>()
    .HasQueryFilter(e => !e.IsDeleted);
```

**Audit Interceptor:**
```csharp
public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
{
    var entries = eventData.Context.ChangeTracker.Entries<AuditableEntity>();
    
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedBy = _currentUserService.UserId;
            entry.Entity.CreatedDate = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.ModifiedBy = _currentUserService.UserId;
            entry.Entity.ModifiedDate = DateTime.UtcNow;
        }
    }
    
    return result;
}
```

**Computed Properties:**
```csharp
// Invoice totals recalculated on save
modelBuilder.Entity<Invoice>()
    .Ignore(i => i.Subtotal)
    .Ignore(i => i.TaxAmount)
    .Ignore(i => i.TotalAmount)
    .Ignore(i => i.RemainingBalance);
    
// Calculated in application logic before save
```

**Value Conversions:**
```csharp
// DateOnly to DateTime conversion for SQLite
modelBuilder.Entity<Patient>()
    .Property(p => p.DateOfBirth)
    .HasConversion(
        d => d.ToDateTime(TimeOnly.MinValue),
        d => DateOnly.FromDateTime(d)
    );
```

---

## Cross-References

- **Workflows:** See `04-workflows.md` for entity lifecycle workflows
- **Architecture:** See `06-architecture-decisions.md` for design rationale
- **Database Schema:** See `07-database-design.md` for SQL DDL
- **API Contracts:** See `11-backend-api-specification.md` for DTOs
- **Implementation:** See `14-implementation-roadmap.md` for entity creation order

---

## Change Log

| Date | Version | Change | Author |
|------|---------|--------|--------|
| 2026-10-21 | 1.0 | Initial domain model design | Agent |

---

*This document is the single source of truth for entity design. Any deviation in implementation requires explicit approval and documentation of the change.*


---

## 5. Billing Entities (UPDATED)

**IMPORTANT UPDATE (2026-10-21):** Based on comparative research (OpenMRS Cashier, OpenEMR ar_session) documented in `08-financial-workflows.md`, the billing model has been expanded from 4 to 7 entities to support cash drawer management and expense tracking.

**Original 4 Entities:** Service, Invoice, InvoiceLineItem, Payment  
**Added Entities:** CashDrawer, Expense, CashMovement (optional)

### 5.5 CashDrawer

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Track cash drawer shifts with opening/closing balances and reconciliation

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| BranchId | Guid | No | Foreign key to Branch |
| UserId | string | No | Foreign key to ApplicationUser (cashier) |
| OpenedAt | DateTime | No | Shift start timestamp (UTC) |
| ClosedAt | DateTime | Yes | Shift end timestamp (null if still open) |
| OpeningBalance | decimal | No | Starting cash amount (Decimal 12,2) |
| ExpectedBalance | decimal | No | Calculated: Opening + CashIn - CashOut (Decimal 12,2) |
| ActualBalance | decimal | Yes | Counted cash at close (null if not yet closed) (Decimal 12,2) |
| Variance | decimal | Yes | ActualBalance - ExpectedBalance (Decimal 12,2) |
| VarianceReason | string | Yes | Explanation for variance (max 1000 chars) |
| Status | CashDrawerStatus | No | Enum: Open, Closed, Reconciled, Investigation |
| ManagerReviewedBy | string | Yes | FK to ApplicationUser (manager who reviewed) |
| ManagerReviewedAt | DateTime | Yes | Manager review timestamp |
| ManagerNotes | string | Yes | Manager comments on variance (max 1000 chars) |
| BankDepositAmount | decimal | Yes | Amount deposited to bank (Decimal 12,2) |
| BankDepositDate | DateOnly | Yes | Date of bank deposit |
| BankDepositReference | string | Yes | Bank deposit receipt number (max 100 chars) |

**Relationships:**
- **Branch:** Many-to-One with Branch
- **User:** Many-to-One with ApplicationUser (cashier)
- **ManagerReviewer:** Many-to-One with ApplicationUser (manager, optional)
- **Payments:** One-to-Many with Payment (cash payments only)
- **Expenses:** One-to-Many with Expense (cash expenses only)
- **CashMovements:** One-to-Many with CashMovement (optional, for v1.1+)

**Indexes:**
- BranchId, OpenedAt (for daily drawer lookup)
- UserId, Status (for active drawer queries: WHERE Status = Open)
- Status (for pending manager reviews: WHERE Status = Closed AND Variance > threshold)
- BranchId, ClosedAt (for historical queries)

**Business Rules:**
- Only one open drawer per UserId + BranchId at a time (unique constraint on UserId + BranchId + Status WHERE Status = Open)
- OpeningBalance ≥ 0 (check constraint)
- ExpectedBalance recalculated from related Payments and Expenses on each transaction
- ActualBalance set only when closing drawer (null while Status = Open)
- Variance auto-calculated: ActualBalance - ExpectedBalance
- Large variance (> tolerance threshold, configurable, default ±$5) requires manager review (Status = Closed, awaiting review)
- After manager review, Status = Reconciled (approved) OR Investigation (rejected)
- Status transitions: Open → Closed → Reconciled (or Investigation)
- Cannot modify drawer after Status = Reconciled (enforce in application)
- OpenedAt and ClosedAt in UTC, convert to branch timezone for display
- Variance tolerance configurable in SystemConfiguration

**Evidence:** OpenMRS Timesheet entity, OpenEMR ar_session table

**Enums:**
```csharp
public enum CashDrawerStatus
{
    Open = 1,          // Drawer is currently accepting transactions
    Closed = 2,        // Drawer counted and closed, awaiting reconciliation
    Reconciled = 3,    // Variance reviewed and approved by manager
    Investigation = 4  // Large variance flagged for investigation
}
```

**Calculation Formula (ExpectedBalance):**
```csharp
// Recalculated on every cash payment, refund, or expense
ExpectedBalance = OpeningBalance
                + Payments.Where(p => p.CashDrawerId == Id && p.PaymentMethod == Cash && p.Amount > 0).Sum(p => p.Amount)
                - Payments.Where(p => p.CashDrawerId == Id && p.PaymentMethod == Cash && p.Amount < 0).Sum(p => Math.Abs(p.Amount))
                - Expenses.Where(e => e.CashDrawerId == Id).Sum(e => e.Amount);
```

---

### 5.6 Expense

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Track clinic operating expenses (supplies, utilities, rent, etc.)

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| BranchId | Guid | No | - | Foreign key to Branch |
| CashDrawerId | Guid | Yes | - | FK to CashDrawer (if paid from drawer cash) |
| Category | ExpenseCategory | No | - | Enum: Supplies, Utilities, Rent, Salaries, Maintenance, etc. |
| Description | string | No | 500 | What was purchased/paid for |
| Amount | decimal | No | - | Expense amount (Decimal 12,2) |
| PaymentMethod | PaymentMethod | No | - | Cash, Card, BankTransfer, etc. (from Payment enum) |
| ReceiptNumber | string | Yes | 100 | Receipt or invoice number from vendor |
| ReceiptPath | string | Yes | 500 | File path to uploaded receipt image/PDF |
| PaidBy | string | No | - | FK to ApplicationUser (who recorded the expense) |
| PaidAt | DateTime | No | - | Payment date/time (UTC) |
| Notes | string | Yes | 1000 | Additional notes |
| ApprovedBy | string | Yes | - | FK to ApplicationUser (manager who approved, if required) |
| ApprovedAt | DateTime | Yes | - | Approval timestamp |

**Relationships:**
- **Branch:** Many-to-One with Branch
- **CashDrawer:** Many-to-One with CashDrawer (optional, cash expenses only)
- **PaidByUser:** Many-to-One with ApplicationUser
- **ApprovedByUser:** Many-to-One with ApplicationUser (optional)

**Indexes:**
- BranchId, PaidAt (for date range expense reports)
- Category (for category-based reports)
- CashDrawerId (for drawer reconciliation: WHERE CashDrawerId = X)
- ApprovedBy, ApprovedAt (for pending approvals: WHERE ApprovedBy IS NULL AND Amount > threshold)

**Business Rules:**
- Amount > 0 (check constraint)
- Category required
- Description required, 1-500 characters
- PaymentMethod = Cash → CashDrawerId required (FK constraint enforced in application)
- PaymentMethod ≠ Cash → CashDrawerId must be null
- Receipt upload encouraged but not mandatory (ReceiptPath optional)
- Large expenses (> threshold, configurable, default $500) require approval (ApprovedBy not null)
- Approval workflow: pending (ApprovedBy = null) → approved (ApprovedBy set)
- Cannot delete expense (soft delete only: IsDeleted flag)
- Cash expenses immediately deduct from CashDrawer.ExpectedBalance

**Evidence:** Common business requirement, not explicitly in CARE but in OpenMRS financial module

**Enums:**
```csharp
public enum ExpenseCategory
{
    Supplies = 1,       // Medical supplies, office supplies, consumables
    Utilities = 2,      // Electricity, water, gas, internet, phone
    Rent = 3,           // Facility rent or lease payments
    Salaries = 4,       // Staff salaries (if tracked in system vs external payroll)
    Maintenance = 5,    // Equipment maintenance, repairs, janitorial services
    Marketing = 6,      // Advertising, promotions, community outreach
    Insurance = 7,      // Liability, malpractice, property insurance
    Taxes = 8,          // Business taxes, regulatory fees
    Professional = 9,   // Legal fees, accounting fees, consulting
    Travel = 10,        // Staff travel, transportation
    Training = 11,      // Continuing education, certifications, conferences
    Equipment = 12,     // Medical equipment purchases (capital expenses)
    Other = 99          // Miscellaneous expenses not fitting other categories
}
```

**Seed Data (SystemConfiguration):**
- ExpenseApprovalThreshold: 500 (USD)
- ExpenseReceiptRequired: false (encouraged but not mandatory)

---

### 5.7 CashMovement (Optional - Defer to v1.1)

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Granular audit trail of all cash drawer transactions (optional for v1.0)

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| CashDrawerId | Guid | No | FK to CashDrawer |
| Type | CashMovementType | No | Enum: DrawerOpen, PaymentIn, Refund, Expense, DrawerClose, BankDeposit, Adjustment |
| Amount | decimal | No | Movement amount (positive = cash in, negative = cash out) (Decimal 12,2) |
| BalanceBefore | decimal | No | Drawer balance before this movement (Decimal 12,2) |
| BalanceAfter | decimal | No | Drawer balance after this movement (Decimal 12,2) |
| Reference | string | Yes | Invoice number, Expense ID, Payment ID, etc. (max 200 chars) |
| Notes | string | Yes | Additional details (max 1000 chars) |

**Relationships:**
- **CashDrawer:** Many-to-One with CashDrawer

**Indexes:**
- CashDrawerId, CreatedDate (for chronological audit trail)
- Type (for filtering by movement type)

**Business Rules:**
- Amount can be positive (cash in) or negative (cash out)
- BalanceBefore + Amount = BalanceAfter (check constraint or validation)
- Immutable once created (audit trail, no updates allowed)
- Automatic creation on every payment, refund, expense, drawer open/close
- CreatedBy always set (audit who recorded transaction)
- CreatedDate in UTC

**Enums:**
```csharp
public enum CashMovementType
{
    DrawerOpen = 1,       // Opening balance when drawer opened
    PaymentIn = 2,        // Cash payment received from patient
    Refund = 3,           // Cash refund given to patient
    Expense = 4,          // Cash expense paid
    DrawerClose = 5,      // Closing balance when drawer closed
    BankDeposit = 6,      // Cash deposited to bank (reduces drawer)
    Adjustment = 7,       // Manual correction by admin (requires approval)
    DrawerTransfer = 8    // Cash transferred between drawers (future)
}
```

**Decision:** Defer to v1.1. CashDrawer.ExpectedBalance can be calculated on-demand from Payment and Expense queries without this entity. Add only if granular audit trail becomes requirement or performance optimization needed.

**Evidence:** Derived from accounting best practices, not in reference systems

---

## Updated Payment Entity (Section 5.4 Amendments)

**Add to Payment entity properties:**

| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| CashDrawerId | Guid | Yes | FK to CashDrawer (cash payments only, null for non-cash) |
| AmountTendered | decimal | Yes | Amount customer gave (cash only) (Decimal 12,2) |
| AmountReturned | decimal | Yes | Change given to customer (cash only) (Decimal 12,2) |

**Updated Relationships:**
- **CashDrawer:** Many-to-One with CashDrawer (optional)

**Additional Indexes:**
- CashDrawerId (for drawer reconciliation queries)

**Updated Business Rules:**
- PaymentMethod = Cash → CashDrawerId required (enforce in application)
- PaymentMethod = Cash → AmountTendered ≥ Amount (customer must give enough)
- PaymentMethod = Cash → AmountReturned = AmountTendered - Amount (exact change)
- PaymentMethod ≠ Cash → CashDrawerId, AmountTendered, AmountReturned all null
- Non-cash payments (Card, BankTransfer, Insurance) do not affect drawer balance
- Cash refunds (Amount < 0) reduce drawer ExpectedBalance

**Evidence:** CARE PaymentReconciliation tendered_amount, returned_amount fields

---

## Entity Count Summary (Updated)

**Foundation:** 6 entities (no change)  
**Patient Management:** 4 entities (no change)  
**Scheduling & Queue:** 3 entities (no change)  
**Clinical:** 7 entities (no change)  
**Billing:** 7 entities (**updated from 4**)
  - Service
  - Invoice
  - InvoiceLineItem
  - Payment (updated with CashDrawer fields)
  - **CashDrawer** (NEW)
  - **Expense** (NEW)
  - CashMovement (optional, defer to v1.1)
**Pharmacy:** 2 entities (no change)

**Total for v1.0: 29 entities** (was 26, +3 for cash management)  
**Optional for v1.1: +1 CashMovement** (total 30)

---

## Updated ERD (Billing Entities)

```
Branch (1) ─────┬── (*) CashDrawer
                ├── (*) Expense
                └── (*) Invoice

ApplicationUser (1) ──┬── (*) CashDrawer.User (cashier)
                      ├── (*) CashDrawer.ManagerReviewer (optional)
                      ├── (*) Expense.PaidBy
                      └── (*) Expense.ApprovedBy (optional)

CashDrawer (1) ───┬── (*) Payment (cash only)
                  ├── (*) Expense (cash only)
                  └── (*) CashMovement (optional, v1.1+)

Invoice (1) ────┬── (*) InvoiceLineItem
                └── (*) Payment

Service (1) ──── (*) InvoiceLineItem

Payment (1) ──── (1) CashDrawer (optional, cash only)

Expense (1) ──── (1) CashDrawer (optional, cash only)
```

---

## Migration Notes

**Backward Compatibility:**
- Existing Payment records without CashDrawerId are valid (historical or non-cash)
- After cash drawer feature enabled, require CashDrawerId for new cash payments
- Existing Invoices unchanged, work with new Payment fields

**Migration Steps:**
1. Add CashDrawer table
2. Add Expense table
3. Add CashMovement table (optional)
4. Alter Payment table: add CashDrawerId, AmountTendered, AmountReturned columns (nullable)
5. Update Payment creation logic to populate new fields for cash payments
6. Add foreign key constraints
7. Add check constraints (ExpectedBalance calculation, variance tolerance)

---

## Testing Requirements

**Cash Drawer Tests:**
- Open drawer → verify Status = Open, ExpectedBalance = OpeningBalance
- Record cash payment → verify ExpectedBalance increases
- Record cash refund → verify ExpectedBalance decreases
- Record cash expense → verify ExpectedBalance decreases
- Close drawer with zero variance → Status = Closed, Variance = 0, no manager review
- Close drawer with small variance (< $5) → Status = Reconciled immediately
- Close drawer with large variance (> $5) → Status = Closed, awaiting manager review
- Manager approves variance → Status = Reconciled
- Manager rejects variance → Status = Investigation
- Cannot open two drawers for same user+branch
- Cannot record cash payment without open drawer

**Expense Tests:**
- Record cash expense → drawer balance decreases
- Record non-cash expense → drawer balance unchanged
- Large expense (> $500) → requires approval
- Small expense (< $500) → no approval needed
- Cannot record cash expense without open drawer

**Reconciliation Tests:**
- Daily reconciliation report accuracy
- Variance calculation correctness
- Multi-branch independent drawer tracking
- Shift summary report completeness

---

## Cross-References

- **Financial Workflows:** Complete workflows in `08-financial-workflows.md`
- **Database Design:** SQL DDL in `07-database-design.md`
- **API Specification:** Endpoints in `11-backend-api-specification.md`
- **Testing Strategy:** Test cases in `12-testing-strategy.md`

---

## Change Log

| Date | Version | Change | Rationale |
|------|---------|--------|-----------|
| 2026-10-21 | 1.1 | Added CashDrawer, Expense, CashMovement entities | Research findings from OpenMRS, OpenEMR show cash drawer management essential for financial integrity |
| 2026-10-21 | 1.1 | Updated Payment entity with CashDrawerId, AmountTendered, AmountReturned | CARE PaymentReconciliation pattern |
| 2026-10-21 | 1.0 | Initial domain model (26 entities) | Based on CARE research |

---

*Cash drawer management is critical for financial integrity in cash-based clinics. All patterns are evidence-based and tested in production systems (OpenMRS Cashier used by hundreds of facilities).*
