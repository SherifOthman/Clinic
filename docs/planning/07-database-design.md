# 07 - Database Design & Schema

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 05-domain-model.md, 08-financial-workflows.md  
**Feeds Into:** 14-implementation-roadmap.md, 12-testing-strategy.md

---

## Purpose

Define the complete database schema for ClinicCare including table definitions, relationships, indexes, constraints, and migration strategy. This is the implementation blueprint for the 29-entity domain model.

---

## Database Technology

**Primary:** SQLite (v1.0)  
**Migration Path:** PostgreSQL (v1.1+)

**Rationale:** See ADR-007 in `06-architecture-decisions.md`
- SQLite: Simple deployment, single file, no server, perfect for single-doctor clinic
- PostgreSQL: Migration path when scale demands (multiple concurrent users, advanced features)

---

## Schema Overview

**Total Tables:** 29 entities + 4 join tables = 33 tables

**Categories:**
1. Foundation: 6 tables (User, Role, Branch, AuditLog, SystemConfiguration, UserSession)
2. Patient Management: 4 tables (Patient, PatientIdentifier, PatientContact, PatientAllergy)
3. Scheduling & Queue: 3 tables (Appointment, QueueEntry, AppointmentType)
4. Clinical: 7 tables (Encounter, Vitals, Diagnosis, Prescription, Attachment, ClinicalNote, Medication)
5. Billing: 7 tables (Service, Invoice, InvoiceLineItem, Payment, CashDrawer, Expense, CashMovement)
6. Join Tables: 4 tables (UserBranch, EncounterOrganization, etc.)

---

## Naming Conventions

- **Tables:** PascalCase singular (e.g., `Patient`, `CashDrawer`)
- **Columns:** PascalCase (e.g., `FirstName`, `DateOfBirth`)
- **Primary Keys:** `Id` (Guid/string)
- **Foreign Keys:** `{EntityName}Id` (e.g., `PatientId`, `BranchId`)
- **Indexes:** `IX_{TableName}_{Column(s)}` (e.g., `IX_Patient_MedicalRecordNumber`)
- **Unique Constraints:** `UQ_{TableName}_{Column(s)}`
- **Check Constraints:** `CK_{TableName}_{Column}__{Rule}`

---

## Common Columns (All Tables)

Every table inheriting from `AuditableEntity` includes:

```sql
Id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
CreatedBy         NVARCHAR(450) NOT NULL,
CreatedDate       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
ModifiedBy        NVARCHAR(450) NULL,
ModifiedDate      DATETIME2 NULL,
IsDeleted         BIT NOT NULL DEFAULT 0,
DeletedBy         NVARCHAR(450) NULL,
DeletedDate       DATETIME2 NULL
```

**SQLite Equivalent:**
```sql
Id                TEXT PRIMARY KEY,
CreatedBy         TEXT NOT NULL,
CreatedDate       TEXT NOT NULL DEFAULT (datetime('now')),
ModifiedBy        TEXT,
ModifiedDate      TEXT,
IsDeleted         INTEGER NOT NULL DEFAULT 0,
DeletedBy         TEXT,
DeletedDate       TEXT
```

**Note:** SQLite uses TEXT for GUID (stored as string), TEXT for DATETIME (ISO 8601 format)

---

## 1. Foundation Tables

### 1.1 AspNetUsers (Identity Framework)

**Purpose:** User accounts (from ASP.NET Identity)

```sql
CREATE TABLE AspNetUsers (
    Id                      NVARCHAR(450) PRIMARY KEY,
    UserName                NVARCHAR(256) NOT NULL,
    NormalizedUserName      NVARCHAR(256) NOT NULL,
    Email                   NVARCHAR(256) NOT NULL,
    NormalizedEmail         NVARCHAR(256) NOT NULL,
    EmailConfirmed          BIT NOT NULL DEFAULT 0,
    PasswordHash            NVARCHAR(MAX) NULL,
    SecurityStamp           NVARCHAR(MAX) NULL,
    ConcurrencyStamp        NVARCHAR(MAX) NULL,
    PhoneNumber             NVARCHAR(50) NULL,
    PhoneNumberConfirmed    BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled        BIT NOT NULL DEFAULT 0,
    LockoutEnd              DATETIMEOFFSET NULL,
    LockoutEnabled          BIT NOT NULL DEFAULT 1,
    AccessFailedCount       INT NOT NULL DEFAULT 0,
    -- ClinicCare extensions
    FullName                NVARCHAR(200) NOT NULL,
    IsActive                BIT NOT NULL DEFAULT 1,
    DefaultBranchId         UNIQUEIDENTIFIER NULL,
    LicenseNumber           NVARCHAR(100) NULL,
    Signature               VARBINARY(MAX) NULL,
    Preferences             NVARCHAR(MAX) NULL, -- JSON
    CONSTRAINT FK_AspNetUsers_Branch FOREIGN KEY (DefaultBranchId) REFERENCES Branch(Id)
);

CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON AspNetUsers(NormalizedUserName);
CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedEmail ON AspNetUsers(NormalizedEmail);
CREATE INDEX IX_AspNetUsers_IsActive ON AspNetUsers(IsActive) WHERE IsActive = 1;
```

### 1.2 AspNetRoles (Identity Framework)

```sql
CREATE TABLE AspNetRoles (
    Id                  NVARCHAR(450) PRIMARY KEY,
    Name                NVARCHAR(256) NOT NULL,
    NormalizedName      NVARCHAR(256) NOT NULL,
    ConcurrencyStamp    NVARCHAR(MAX) NULL,
    Description         NVARCHAR(500) NULL
);

CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON AspNetRoles(NormalizedName);
```

### 1.3 Branch

```sql
CREATE TABLE Branch (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name                NVARCHAR(100) NOT NULL,
    Code                NVARCHAR(20) NULL,
    AddressLine1        NVARCHAR(200) NOT NULL,
    AddressLine2        NVARCHAR(200) NULL,
    City                NVARCHAR(100) NOT NULL,
    State               NVARCHAR(100) NULL,
    PostalCode          NVARCHAR(20) NULL,
    Country             NVARCHAR(100) NOT NULL,
    Phone               NVARCHAR(50) NULL,
    Email               NVARCHAR(200) NULL,
    IsActive            BIT NOT NULL DEFAULT 1,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT UQ_Branch_Name UNIQUE (Name),
    CONSTRAINT UQ_Branch_Code UNIQUE (Code) WHERE Code IS NOT NULL
);

CREATE INDEX IX_Branch_IsActive ON Branch(IsActive) WHERE IsActive = 1;
```

### 1.4 AuditLog

```sql
CREATE TABLE AuditLog (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId              NVARCHAR(450) NOT NULL,
    EntityType          NVARCHAR(200) NOT NULL,
    EntityId            NVARCHAR(100) NOT NULL,
    Action              NVARCHAR(50) NOT NULL, -- Create, Update, Delete, View
    Timestamp           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Changes             NVARCHAR(MAX) NULL, -- JSON: {field: {old, new}}
    IPAddress           NVARCHAR(50) NULL,
    UserAgent           NVARCHAR(500) NULL,
    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_AuditLog_UserId_Timestamp ON AuditLog(UserId, Timestamp);
CREATE INDEX IX_AuditLog_EntityType_EntityId ON AuditLog(EntityType, EntityId);
CREATE INDEX IX_AuditLog_Timestamp ON AuditLog(Timestamp);
```

### 1.5 SystemConfiguration

```sql
CREATE TABLE SystemConfiguration (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Key]               NVARCHAR(100) NOT NULL,
    Value               NVARCHAR(1000) NULL,
    DataType            NVARCHAR(50) NOT NULL, -- String, Int, Decimal, Boolean, JSON
    Category            NVARCHAR(50) NOT NULL, -- General, Billing, Scheduling, Clinical
    Description         NVARCHAR(500) NULL,
    -- Audit fields (omitted for brevity - same as above)
    CONSTRAINT UQ_SystemConfiguration_Key UNIQUE ([Key])
);

CREATE INDEX IX_SystemConfiguration_Category ON SystemConfiguration(Category);
```

**Seed Data:**
```sql
INSERT INTO SystemConfiguration ([Key], Value, DataType, Category, Description) VALUES
('ClinicName', 'ClinicCare Demo', 'String', 'General', 'Clinic display name'),
('TaxRate', '0', 'Decimal', 'Billing', 'Default tax rate (0-100)'),
('InvoiceNumberFormat', 'INV-{YYYYMMDD}-{####}', 'String', 'Billing', 'Invoice number template'),
('AppointmentSlotDuration', '30', 'Int', 'Scheduling', 'Default appointment slot (minutes)'),
('CashDrawerVarianceTolerance', '5', 'Decimal', 'Billing', 'Cash drawer variance tolerance (USD)'),
('ExpenseApprovalThreshold', '500', 'Decimal', 'Billing', 'Expense approval threshold (USD)'),
('RefundApprovalThreshold', '100', 'Decimal', 'Billing', 'Refund approval threshold (USD)');
```

---

## 2. Patient Management Tables

### 2.1 Patient

```sql
CREATE TABLE Patient (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MedicalRecordNumber     NVARCHAR(50) NOT NULL,
    FirstName               NVARCHAR(100) NOT NULL,
    MiddleName              NVARCHAR(100) NULL,
    LastName                NVARCHAR(100) NOT NULL,
    DateOfBirth             DATE NOT NULL,
    Gender                  INT NOT NULL, -- 1=Male, 2=Female, 3=Other
    BloodType               INT NULL, -- 1=APositive, 2=ANegative, etc.
    Photo                   VARBINARY(MAX) NULL,
    MaritalStatus           INT NULL, -- 1=Single, 2=Married, 3=Divorced, 4=Widowed
    Occupation              NVARCHAR(200) NULL,
    Notes                   NVARCHAR(MAX) NULL,
    MedicalHistory          NVARCHAR(MAX) NULL,
    IsActive                BIT NOT NULL DEFAULT 1,
    -- Audit fields
    CreatedBy               NVARCHAR(450) NOT NULL,
    CreatedDate             DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy              NVARCHAR(450) NULL,
    ModifiedDate            DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    DeletedBy               NVARCHAR(450) NULL,
    DeletedDate             DATETIME2 NULL,
    CONSTRAINT UQ_Patient_MRN UNIQUE (MedicalRecordNumber),
    CONSTRAINT CK_Patient_DateOfBirth CHECK (DateOfBirth <= GETDATE()),
    CONSTRAINT CK_Patient_Gender CHECK (Gender IN (1, 2, 3)),
    CONSTRAINT CK_Patient_BloodType CHECK (BloodType BETWEEN 1 AND 8 OR BloodType IS NULL)
);

CREATE INDEX IX_Patient_FirstName_LastName ON Patient(FirstName, LastName);
CREATE INDEX IX_Patient_DateOfBirth ON Patient(DateOfBirth);
CREATE INDEX IX_Patient_IsActive ON Patient(IsActive) WHERE IsActive = 1;
```

### 2.2 PatientIdentifier

```sql
CREATE TABLE PatientIdentifier (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    IdentifierType      INT NOT NULL, -- 1=Passport, 2=NationalID, 3=DriverLicense, 4=MilitaryID, 99=Other
    IdentifierValue     NVARCHAR(100) NOT NULL,
    IssuingAuthority    NVARCHAR(200) NULL,
    IssueDate           DATE NULL,
    ExpiryDate          DATE NULL,
    IsPrimary           BIT NOT NULL DEFAULT 0,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_PatientIdentifier_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_PatientIdentifier_Type_Value UNIQUE (IdentifierType, IdentifierValue),
    CONSTRAINT CK_PatientIdentifier_ExpiryAfterIssue CHECK (ExpiryDate IS NULL OR IssueDate IS NULL OR ExpiryDate > IssueDate)
);

CREATE INDEX IX_PatientIdentifier_PatientId ON PatientIdentifier(PatientId);
CREATE INDEX IX_PatientIdentifier_IsPrimary ON PatientIdentifier(IsPrimary) WHERE IsPrimary = 1;
```

### 2.3 PatientContact

```sql
CREATE TABLE PatientContact (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    ContactType         INT NOT NULL, -- 1=Primary, 2=Emergency, 3=NextOfKin
    Relationship        NVARCHAR(100) NULL,
    FullName            NVARCHAR(200) NULL,
    Phone               NVARCHAR(50) NULL,
    Email               NVARCHAR(200) NULL,
    AddressLine1        NVARCHAR(200) NULL,
    AddressLine2        NVARCHAR(200) NULL,
    City                NVARCHAR(100) NULL,
    State               NVARCHAR(100) NULL,
    PostalCode          NVARCHAR(20) NULL,
    Country             NVARCHAR(100) NULL,
    IsPrimary           BIT NOT NULL DEFAULT 0,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_PatientContact_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id) ON DELETE CASCADE,
    CONSTRAINT CK_PatientContact_PhoneOrEmail CHECK (Phone IS NOT NULL OR Email IS NOT NULL)
);

CREATE INDEX IX_PatientContact_PatientId ON PatientContact(PatientId);
CREATE INDEX IX_PatientContact_Type_Primary ON PatientContact(ContactType, IsPrimary);
```

### 2.4 PatientAllergy

```sql
CREATE TABLE PatientAllergy (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    Allergen            NVARCHAR(200) NOT NULL,
    AllergyType         INT NOT NULL, -- 1=Medication, 2=Food, 3=Environmental, 99=Other
    Severity            INT NOT NULL, -- 1=Mild, 2=Moderate, 3=Severe, 4=Anaphylaxis
    Reaction            NVARCHAR(500) NULL,
    OnsetDate           DATE NULL,
    Status              INT NOT NULL DEFAULT 1, -- 1=Active, 2=Resolved, 3=Suspected
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_PatientAllergy_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PatientAllergy_PatientId ON PatientAllergy(PatientId);
CREATE INDEX IX_PatientAllergy_Severity ON PatientAllergy(AllergyType, Severity);
```

---

## 3. Scheduling & Queue Tables

### 3.1 Appointment

```sql
CREATE TABLE Appointment (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId               UNIQUEIDENTIFIER NOT NULL,
    BranchId                UNIQUEIDENTIFIER NOT NULL,
    DoctorId                NVARCHAR(450) NOT NULL,
    AppointmentDateTime     DATETIME2 NOT NULL,
    AppointmentTypeId       UNIQUEIDENTIFIER NULL,
    ReasonForVisit          NVARCHAR(500) NULL,
    Status                  INT NOT NULL DEFAULT 1, -- 1=Scheduled, 2=Confirmed, 3=CheckedIn, 4=Completed, 5=Cancelled, 6=NoShow
    CancellationReason      NVARCHAR(500) NULL,
    Notes                   NVARCHAR(1000) NULL,
    -- Audit fields
    CreatedBy               NVARCHAR(450) NOT NULL,
    CreatedDate             DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy              NVARCHAR(450) NULL,
    ModifiedDate            DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    DeletedBy               NVARCHAR(450) NULL,
    DeletedDate             DATETIME2 NULL,
    CONSTRAINT FK_Appointment_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id),
    CONSTRAINT FK_Appointment_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT FK_Appointment_Doctor FOREIGN KEY (DoctorId) REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_Appointment_Type FOREIGN KEY (AppointmentTypeId) REFERENCES AppointmentType(Id)
);

CREATE INDEX IX_Appointment_PatientId ON Appointment(PatientId);
CREATE INDEX IX_Appointment_BranchId_DateTime ON Appointment(BranchId, AppointmentDateTime);
CREATE INDEX IX_Appointment_DoctorId ON Appointment(DoctorId);
CREATE INDEX IX_Appointment_Status ON Appointment(Status);
```

### 3.2 QueueEntry

```sql
CREATE TABLE QueueEntry (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    BranchId            UNIQUEIDENTIFIER NOT NULL,
    AppointmentId       UNIQUEIDENTIFIER NULL,
    QueueNumber         INT NOT NULL,
    QueuedAt            DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CalledAt            DATETIME2 NULL,
    CompletedAt         DATETIME2 NULL,
    Status              INT NOT NULL DEFAULT 1, -- 1=Waiting, 2=InProgress, 3=Completed, 4=Cancelled
    Priority            INT NOT NULL DEFAULT 1, -- 1=Normal, 2=High, 3=Emergency
    ReasonForVisit      NVARCHAR(500) NULL,
    Notes               NVARCHAR(1000) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_QueueEntry_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id),
    CONSTRAINT FK_QueueEntry_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT FK_QueueEntry_Appointment FOREIGN KEY (AppointmentId) REFERENCES Appointment(Id),
    CONSTRAINT UQ_QueueEntry_Number_Branch_Date UNIQUE (QueueNumber, BranchId, CAST(QueuedAt AS DATE))
);

CREATE INDEX IX_QueueEntry_PatientId ON QueueEntry(PatientId);
CREATE INDEX IX_QueueEntry_Branch_QueuedAt ON QueueEntry(BranchId, QueuedAt);
CREATE INDEX IX_QueueEntry_Status_Priority ON QueueEntry(Status, Priority);
```

### 3.3 AppointmentType

```sql
CREATE TABLE AppointmentType (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name                NVARCHAR(100) NOT NULL,
    Code                NVARCHAR(20) NULL,
    Description         NVARCHAR(500) NULL,
    DefaultDuration     INT NOT NULL DEFAULT 30, -- Minutes
    DefaultServiceId    UNIQUEIDENTIFIER NULL,
    Color               NVARCHAR(20) NULL, -- Hex color for calendar
    IsActive            BIT NOT NULL DEFAULT 1,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT UQ_AppointmentType_Name UNIQUE (Name),
    CONSTRAINT UQ_AppointmentType_Code UNIQUE (Code) WHERE Code IS NOT NULL,
    CONSTRAINT FK_AppointmentType_Service FOREIGN KEY (DefaultServiceId) REFERENCES Service(Id)
);

CREATE INDEX IX_AppointmentType_IsActive ON AppointmentType(IsActive) WHERE IsActive = 1;
```

**Seed Data:**
```sql
INSERT INTO AppointmentType (Id, Name, Code, DefaultDuration, Color, IsActive) VALUES
(NEWID(), 'General Consultation', 'GEN', 30, '#4CAF50', 1),
(NEWID(), 'Follow-Up', 'FU', 15, '#2196F3', 1),
(NEWID(), 'Emergency', 'EMG', 15, '#F44336', 1);
```

---

## 4. Clinical Tables

### 4.1 Encounter

```sql
CREATE TABLE Encounter (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId               UNIQUEIDENTIFIER NOT NULL,
    BranchId                UNIQUEIDENTIFIER NOT NULL,
    DoctorId                NVARCHAR(450) NOT NULL,
    AppointmentId           UNIQUEIDENTIFIER NULL,
    QueueEntryId            UNIQUEIDENTIFIER NULL,
    EncounterNumber         NVARCHAR(50) NOT NULL,
    EncounterDateTime       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt             DATETIME2 NULL,
    EncounterType           INT NOT NULL, -- 1=OutPatient, 2=Emergency, 3=FollowUp, 4=Consultation
    Status                  INT NOT NULL DEFAULT 1, -- 1=InProgress, 2=Completed, 3=Cancelled
    ChiefComplaint          NVARCHAR(500) NULL,
    Subjective              NVARCHAR(MAX) NULL, -- SOAP: Subjective
    Objective               NVARCHAR(MAX) NULL, -- SOAP: Objective
    Assessment              NVARCHAR(MAX) NULL, -- SOAP: Assessment
    Plan                    NVARCHAR(MAX) NULL, -- SOAP: Plan
    -- Audit fields
    CreatedBy               NVARCHAR(450) NOT NULL,
    CreatedDate             DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy              NVARCHAR(450) NULL,
    ModifiedDate            DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    DeletedBy               NVARCHAR(450) NULL,
    DeletedDate             DATETIME2 NULL,
    CONSTRAINT FK_Encounter_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id),
    CONSTRAINT FK_Encounter_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT FK_Encounter_Doctor FOREIGN KEY (DoctorId) REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_Encounter_Appointment FOREIGN KEY (AppointmentId) REFERENCES Appointment(Id),
    CONSTRAINT FK_Encounter_Queue FOREIGN KEY (QueueEntryId) REFERENCES QueueEntry(Id),
    CONSTRAINT UQ_Encounter_Number UNIQUE (EncounterNumber)
);

CREATE INDEX IX_Encounter_PatientId ON Encounter(PatientId);
CREATE INDEX IX_Encounter_BranchId ON Encounter(BranchId);
CREATE INDEX IX_Encounter_DoctorId ON Encounter(DoctorId);
CREATE INDEX IX_Encounter_DateTime ON Encounter(EncounterDateTime);
CREATE INDEX IX_Encounter_Status ON Encounter(Status);
```

### 4.2 Vitals

```sql
CREATE TABLE Vitals (
    Id                          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EncounterId                 UNIQUEIDENTIFIER NOT NULL,
    Height                      DECIMAL(5,2) NULL, -- cm
    Weight                      DECIMAL(6,2) NULL, -- kg
    BMI                         DECIMAL(5,2) NULL, -- kg/m²
    Temperature                 DECIMAL(4,1) NULL, -- °C
    BloodPressureSystolic       INT NULL, -- mmHg
    BloodPressureDiastolic      INT NULL, -- mmHg
    HeartRate                   INT NULL, -- bpm
    RespiratoryRate             INT NULL, -- breaths/min
    OxygenSaturation            DECIMAL(5,2) NULL, -- %
    Notes                       NVARCHAR(MAX) NULL,
    -- Audit fields
    CreatedBy                   NVARCHAR(450) NOT NULL,
    CreatedDate                 DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy                  NVARCHAR(450) NULL,
    ModifiedDate                DATETIME2 NULL,
    IsDeleted                   BIT NOT NULL DEFAULT 0,
    DeletedBy                   NVARCHAR(450) NULL,
    DeletedDate                 DATETIME2 NULL,
    CONSTRAINT FK_Vitals_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Vitals_EncounterId UNIQUE (EncounterId), -- One-to-one
    CONSTRAINT CK_Vitals_Height CHECK (Height BETWEEN 30 AND 250 OR Height IS NULL),
    CONSTRAINT CK_Vitals_Weight CHECK (Weight BETWEEN 1 AND 300 OR Weight IS NULL),
    CONSTRAINT CK_Vitals_Temp CHECK (Temperature BETWEEN 32 AND 42 OR Temperature IS NULL),
    CONSTRAINT CK_Vitals_BP_Systolic CHECK (BloodPressureSystolic BETWEEN 50 AND 250 OR BloodPressureSystolic IS NULL),
    CONSTRAINT CK_Vitals_BP_Diastolic CHECK (BloodPressureDiastolic BETWEEN 30 AND 150 OR BloodPressureDiastolic IS NULL),
    CONSTRAINT CK_Vitals_HeartRate CHECK (HeartRate BETWEEN 30 AND 200 OR HeartRate IS NULL),
    CONSTRAINT CK_Vitals_RespRate CHECK (RespiratoryRate BETWEEN 5 AND 60 OR RespiratoryRate IS NULL),
    CONSTRAINT CK_Vitals_SpO2 CHECK (OxygenSaturation BETWEEN 50 AND 100 OR OxygenSaturation IS NULL)
);

CREATE UNIQUE INDEX IX_Vitals_EncounterId ON Vitals(EncounterId);
```

### 4.3 Diagnosis

```sql
CREATE TABLE Diagnosis (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EncounterId         UNIQUEIDENTIFIER NOT NULL,
    ICD10Code           NVARCHAR(20) NULL,
    Description         NVARCHAR(500) NOT NULL,
    DiagnosisType       INT NOT NULL, -- 1=Primary, 2=Secondary, 3=Comorbidity
    Status              INT NOT NULL DEFAULT 1, -- 1=Active, 2=Resolved, 3=RuledOut
    OnsetDate           DATE NULL,
    Notes               NVARCHAR(1000) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Diagnosis_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Diagnosis_EncounterId ON Diagnosis(EncounterId);
CREATE INDEX IX_Diagnosis_ICD10Code ON Diagnosis(ICD10Code);
CREATE INDEX IX_Diagnosis_Type ON Diagnosis(DiagnosisType);
```

### 4.4 Prescription

```sql
CREATE TABLE Prescription (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EncounterId         UNIQUEIDENTIFIER NOT NULL,
    MedicationId        UNIQUEIDENTIFIER NULL,
    MedicationName      NVARCHAR(200) NOT NULL,
    Dosage              NVARCHAR(100) NOT NULL,
    Route               INT NOT NULL, -- 1=Oral, 2=Topical, 3=Injectable, 4=Inhalation, 5=Rectal, 99=Other
    Frequency           NVARCHAR(100) NOT NULL,
    Duration            INT NULL, -- Days
    Quantity            DECIMAL(10,2) NULL,
    Instructions        NVARCHAR(500) NULL,
    Status              INT NOT NULL DEFAULT 1, -- 1=Active, 2=Dispensed, 3=Discontinued, 4=Completed
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Prescription_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Prescription_Medication FOREIGN KEY (MedicationId) REFERENCES Medication(Id)
);

CREATE INDEX IX_Prescription_EncounterId ON Prescription(EncounterId);
CREATE INDEX IX_Prescription_MedicationId ON Prescription(MedicationId);
CREATE INDEX IX_Prescription_Status ON Prescription(Status);
```

### 4.5 Medication

```sql
CREATE TABLE Medication (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GenericName         NVARCHAR(200) NOT NULL,
    BrandName           NVARCHAR(200) NULL,
    Strength            NVARCHAR(50) NOT NULL,
    Form                INT NOT NULL, -- 1=Tablet, 2=Capsule, 3=Liquid, 4=Syrup, 5=Suspension, 6=Injection, etc.
    Manufacturer        NVARCHAR(200) NULL,
    NDC                 NVARCHAR(20) NULL, -- National Drug Code or equivalent
    IsActive            BIT NOT NULL DEFAULT 1,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT UQ_Medication_Generic_Strength_Form UNIQUE (GenericName, Strength, Form)
);

CREATE INDEX IX_Medication_GenericName ON Medication(GenericName);
CREATE INDEX IX_Medication_BrandName ON Medication(BrandName) WHERE BrandName IS NOT NULL;
CREATE INDEX IX_Medication_IsActive ON Medication(IsActive) WHERE IsActive = 1;
```

### 4.6 Attachment

```sql
CREATE TABLE Attachment (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EncounterId         UNIQUEIDENTIFIER NULL,
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    FileName            NVARCHAR(255) NOT NULL,
    FileExtension       NVARCHAR(10) NOT NULL,
    FileSize            BIGINT NOT NULL,
    MimeType            NVARCHAR(100) NOT NULL,
    StoragePath         NVARCHAR(500) NOT NULL,
    AttachmentType      INT NOT NULL, -- 1=LabReport, 2=RadiologyReport, 3=Document, 4=Image, 99=Other
    Description         NVARCHAR(500) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Attachment_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Attachment_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Attachment_FileSize CHECK (FileSize > 0 AND FileSize <= 10485760) -- Max 10MB
);

CREATE INDEX IX_Attachment_EncounterId ON Attachment(EncounterId);
CREATE INDEX IX_Attachment_PatientId ON Attachment(PatientId);
CREATE INDEX IX_Attachment_Type ON Attachment(AttachmentType);
```

### 4.7 ClinicalNote

```sql
CREATE TABLE ClinicalNote (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EncounterId         UNIQUEIDENTIFIER NOT NULL,
    NoteType            INT NOT NULL, -- 1=Addendum, 2=Correction, 3=Progress, 99=Other
    Content             NVARCHAR(MAX) NOT NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_ClinicalNote_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id) ON DELETE CASCADE
);

CREATE INDEX IX_ClinicalNote_EncounterId ON ClinicalNote(EncounterId);
CREATE INDEX IX_ClinicalNote_CreatedDate ON ClinicalNote(CreatedDate);
```

---

## 5. Billing Tables

### 5.1 Service

```sql
CREATE TABLE Service (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code                NVARCHAR(20) NOT NULL,
    Name                NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(500) NULL,
    Category            INT NOT NULL, -- 1=Consultation, 2=Procedure, 3=Lab, 4=Imaging, 5=Pharmacy, 99=Other
    DefaultPrice        DECIMAL(12,2) NOT NULL,
    TaxRate             DECIMAL(5,2) NULL, -- Override system default tax rate
    IsActive            BIT NOT NULL DEFAULT 1,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT UQ_Service_Code UNIQUE (Code),
    CONSTRAINT CK_Service_DefaultPrice CHECK (DefaultPrice >= 0)
);

CREATE INDEX IX_Service_Name ON Service(Name);
CREATE INDEX IX_Service_Category ON Service(Category);
CREATE INDEX IX_Service_IsActive ON Service(IsActive) WHERE IsActive = 1;
```

**Seed Data:**
```sql
INSERT INTO Service (Id, Code, Name, Category, DefaultPrice, IsActive) VALUES
(NEWID(), 'CONS-GEN', 'General Consultation', 1, 100.00, 1),
(NEWID(), 'CONS-FU', 'Follow-Up Consultation', 1, 50.00, 1),
(NEWID(), 'PROC-SUTURE', 'Suture/Stitches', 2, 150.00, 1);
```

### 5.2 Invoice

```sql
CREATE TABLE Invoice (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InvoiceNumber       NVARCHAR(50) NOT NULL,
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    EncounterId         UNIQUEIDENTIFIER NULL,
    BranchId            UNIQUEIDENTIFIER NOT NULL,
    InvoiceDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DueDate             DATETIME2 NOT NULL,
    Status              INT NOT NULL DEFAULT 1, -- 1=Draft, 2=Issued, 3=PartiallyPaid, 4=Paid, 5=Cancelled
    Subtotal            DECIMAL(12,2) NOT NULL,
    TaxAmount           DECIMAL(12,2) NOT NULL,
    DiscountAmount      DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount         DECIMAL(12,2) NOT NULL,
    TotalPaid           DECIMAL(12,2) NOT NULL DEFAULT 0,
    RemainingBalance    DECIMAL(12,2) NOT NULL,
    Notes               NVARCHAR(1000) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Invoice_Patient FOREIGN KEY (PatientId) REFERENCES Patient(Id),
    CONSTRAINT FK_Invoice_Encounter FOREIGN KEY (EncounterId) REFERENCES Encounter(Id),
    CONSTRAINT FK_Invoice_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT UQ_Invoice_Number UNIQUE (InvoiceNumber),
    CONSTRAINT CK_Invoice_Amounts CHECK (TotalAmount = Subtotal + TaxAmount - DiscountAmount),
    CONSTRAINT CK_Invoice_Balance CHECK (RemainingBalance = TotalAmount - TotalPaid)
);

CREATE INDEX IX_Invoice_PatientId ON Invoice(PatientId);
CREATE INDEX IX_Invoice_EncounterId ON Invoice(EncounterId);
CREATE INDEX IX_Invoice_InvoiceDate ON Invoice(InvoiceDate);
CREATE INDEX IX_Invoice_Status ON Invoice(Status);
```

### 5.3 InvoiceLineItem

```sql
CREATE TABLE InvoiceLineItem (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InvoiceId           UNIQUEIDENTIFIER NOT NULL,
    ServiceId           UNIQUEIDENTIFIER NULL,
    Description         NVARCHAR(500) NOT NULL,
    Quantity            DECIMAL(10,2) NOT NULL DEFAULT 1,
    UnitPrice           DECIMAL(12,2) NOT NULL,
    DiscountPercent     DECIMAL(5,2) NOT NULL DEFAULT 0,
    DiscountAmount      DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalPrice          DECIMAL(12,2) NOT NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_InvoiceLineItem_Invoice FOREIGN KEY (InvoiceId) REFERENCES Invoice(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceLineItem_Service FOREIGN KEY (ServiceId) REFERENCES Service(Id),
    CONSTRAINT CK_InvoiceLineItem_Quantity CHECK (Quantity >= 0),
    CONSTRAINT CK_InvoiceLineItem_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_InvoiceLineItem_TotalPrice CHECK (TotalPrice >= 0)
);

CREATE INDEX IX_InvoiceLineItem_InvoiceId ON InvoiceLineItem(InvoiceId);
CREATE INDEX IX_InvoiceLineItem_ServiceId ON InvoiceLineItem(ServiceId);
```

### 5.4 Payment

```sql
CREATE TABLE Payment (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InvoiceId           UNIQUEIDENTIFIER NOT NULL,
    CashDrawerId        UNIQUEIDENTIFIER NULL, -- For cash payments only
    PaymentDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Amount              DECIMAL(12,2) NOT NULL,
    AmountTendered      DECIMAL(12,2) NULL, -- Cash only
    AmountReturned      DECIMAL(12,2) NULL, -- Cash only (change)
    PaymentMethod       INT NOT NULL, -- 1=Cash, 2=Card, 3=BankTransfer, 4=Insurance, 5=Cheque, 99=Other
    Reference           NVARCHAR(200) NULL, -- Card last 4, check#, transaction ID
    Notes               NVARCHAR(500) NULL,
    IsRefund            BIT NOT NULL DEFAULT 0,
    RefundReason        NVARCHAR(500) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Payment_Invoice FOREIGN KEY (InvoiceId) REFERENCES Invoice(Id),
    CONSTRAINT FK_Payment_CashDrawer FOREIGN KEY (CashDrawerId) REFERENCES CashDrawer(Id),
    CONSTRAINT CK_Payment_Amount CHECK (Amount <> 0),
    CONSTRAINT CK_Payment_Refund_Reason CHECK (IsRefund = 0 OR RefundReason IS NOT NULL),
    CONSTRAINT CK_Payment_Cash_Tendered CHECK (PaymentMethod <> 1 OR AmountTendered >= ABS(Amount))
);

CREATE INDEX IX_Payment_InvoiceId ON Payment(InvoiceId);
CREATE INDEX IX_Payment_CashDrawerId ON Payment(CashDrawerId);
CREATE INDEX IX_Payment_PaymentDate ON Payment(PaymentDate);
CREATE INDEX IX_Payment_PaymentMethod ON Payment(PaymentMethod);
```

### 5.5 CashDrawer

```sql
CREATE TABLE CashDrawer (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BranchId                UNIQUEIDENTIFIER NOT NULL,
    UserId                  NVARCHAR(450) NOT NULL, -- Cashier
    OpenedAt                DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ClosedAt                DATETIME2 NULL,
    OpeningBalance          DECIMAL(12,2) NOT NULL,
    ExpectedBalance         DECIMAL(12,2) NOT NULL,
    ActualBalance           DECIMAL(12,2) NULL,
    Variance                DECIMAL(12,2) NULL,
    VarianceReason          NVARCHAR(1000) NULL,
    Status                  INT NOT NULL DEFAULT 1, -- 1=Open, 2=Closed, 3=Reconciled, 4=Investigation
    ManagerReviewedBy       NVARCHAR(450) NULL,
    ManagerReviewedAt       DATETIME2 NULL,
    ManagerNotes            NVARCHAR(1000) NULL,
    BankDepositAmount       DECIMAL(12,2) NULL,
    BankDepositDate         DATE NULL,
    BankDepositReference    NVARCHAR(100) NULL,
    -- Audit fields
    CreatedBy               NVARCHAR(450) NOT NULL,
    CreatedDate             DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy              NVARCHAR(450) NULL,
    ModifiedDate            DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    DeletedBy               NVARCHAR(450) NULL,
    DeletedDate             DATETIME2 NULL,
    CONSTRAINT FK_CashDrawer_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT FK_CashDrawer_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_CashDrawer_ManagerReviewer FOREIGN KEY (ManagerReviewedBy) REFERENCES AspNetUsers(Id),
    CONSTRAINT CK_CashDrawer_OpeningBalance CHECK (OpeningBalance >= 0),
    CONSTRAINT CK_CashDrawer_Variance CHECK (Variance = ActualBalance - ExpectedBalance OR Variance IS NULL)
);

CREATE INDEX IX_CashDrawer_BranchId_OpenedAt ON CashDrawer(BranchId, OpenedAt);
CREATE INDEX IX_CashDrawer_UserId_Status ON CashDrawer(UserId, Status);
CREATE INDEX IX_CashDrawer_Status ON CashDrawer(Status);
CREATE UNIQUE INDEX IX_CashDrawer_User_Branch_Open ON CashDrawer(UserId, BranchId) WHERE Status = 1; -- Only one open drawer per user+branch
```

### 5.6 Expense

```sql
CREATE TABLE Expense (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BranchId            UNIQUEIDENTIFIER NOT NULL,
    CashDrawerId        UNIQUEIDENTIFIER NULL, -- For cash expenses only
    Category            INT NOT NULL, -- 1=Supplies, 2=Utilities, 3=Rent, 4=Salaries, 5=Maintenance, etc.
    Description         NVARCHAR(500) NOT NULL,
    Amount              DECIMAL(12,2) NOT NULL,
    PaymentMethod       INT NOT NULL, -- 1=Cash, 2=Card, 3=BankTransfer, 99=Other
    ReceiptNumber       NVARCHAR(100) NULL,
    ReceiptPath         NVARCHAR(500) NULL, -- File path to uploaded receipt
    PaidBy              NVARCHAR(450) NOT NULL,
    PaidAt              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Notes               NVARCHAR(1000) NULL,
    ApprovedBy          NVARCHAR(450) NULL, -- Manager approval for large expenses
    ApprovedAt          DATETIME2 NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_Expense_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id),
    CONSTRAINT FK_Expense_CashDrawer FOREIGN KEY (CashDrawerId) REFERENCES CashDrawer(Id),
    CONSTRAINT FK_Expense_PaidBy FOREIGN KEY (PaidBy) REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_Expense_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES AspNetUsers(Id),
    CONSTRAINT CK_Expense_Amount CHECK (Amount > 0)
);

CREATE INDEX IX_Expense_BranchId_PaidAt ON Expense(BranchId, PaidAt);
CREATE INDEX IX_Expense_Category ON Expense(Category);
CREATE INDEX IX_Expense_CashDrawerId ON Expense(CashDrawerId);
CREATE INDEX IX_Expense_ApprovedBy_ApprovedAt ON Expense(ApprovedBy, ApprovedAt);
```

### 5.7 CashMovement (Optional)

```sql
CREATE TABLE CashMovement (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CashDrawerId        UNIQUEIDENTIFIER NOT NULL,
    Type                INT NOT NULL, -- 1=DrawerOpen, 2=PaymentIn, 3=Refund, 4=Expense, 5=DrawerClose, 6=BankDeposit, 7=Adjustment
    Amount              DECIMAL(12,2) NOT NULL, -- Positive = in, Negative = out
    BalanceBefore       DECIMAL(12,2) NOT NULL,
    BalanceAfter        DECIMAL(12,2) NOT NULL,
    Reference           NVARCHAR(200) NULL,
    Notes               NVARCHAR(1000) NULL,
    -- Audit fields
    CreatedBy           NVARCHAR(450) NOT NULL,
    CreatedDate         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy          NVARCHAR(450) NULL,
    ModifiedDate        DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    DeletedBy           NVARCHAR(450) NULL,
    DeletedDate         DATETIME2 NULL,
    CONSTRAINT FK_CashMovement_CashDrawer FOREIGN KEY (CashDrawerId) REFERENCES CashDrawer(Id) ON DELETE CASCADE,
    CONSTRAINT CK_CashMovement_Balance CHECK (BalanceAfter = BalanceBefore + Amount)
);

CREATE INDEX IX_CashMovement_CashDrawerId_CreatedDate ON CashMovement(CashDrawerId, CreatedDate);
CREATE INDEX IX_CashMovement_Type ON CashMovement(Type);
```

---

## 6. Join Tables

### 6.1 UserBranch (Many-to-Many)

```sql
CREATE TABLE UserBranch (
    UserId      NVARCHAR(450) NOT NULL,
    BranchId    UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_UserBranch PRIMARY KEY (UserId, BranchId),
    CONSTRAINT FK_UserBranch_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserBranch_Branch FOREIGN KEY (BranchId) REFERENCES Branch(Id) ON DELETE CASCADE
);
```

---

## Migration Strategy

### Phase 1: Foundation (M1)
1. AspNetUsers, AspNetRoles (Identity tables - auto-created)
2. Branch
3. AuditLog
4. SystemConfiguration
5. UserBranch

### Phase 2: Patient (M1)
6. Patient
7. PatientIdentifier
8. PatientContact
9. PatientAllergy

### Phase 3: Scheduling (M2)
10. AppointmentType
11. Appointment
12. QueueEntry

### Phase 4: Clinical (M3)
13. Medication
14. Encounter
15. Vitals
16. Diagnosis
17. Prescription
18. Attachment
19. ClinicalNote

### Phase 5: Billing (M4)
20. Service
21. Invoice
22. InvoiceLineItem
23. Payment
24. CashDrawer
25. Expense
26. CashMovement (optional, M6)

### EF Core Migration Commands

```powershell
# Create migration
dotnet ef migrations add M1_Foundation --project src/Clinic.Infrastructure --startup-project src/Clinic.Api

# Apply migration
dotnet ef database update --project src/Clinic.Infrastructure --startup-project src/Clinic.Api

# Generate SQL script
dotnet ef migrations script --project src/Clinic.Infrastructure --startup-project src/Clinic.Api --output migrations/M1_Foundation.sql
```

---

## Data Seeding

**Required Seed Data:**
1. Default Admin user (first run)
2. Default roles (Doctor, Receptionist, Admin)
3. Default branch ("Main Clinic")
4. System configuration values
5. Default appointment types
6. Default services

**Seed Execution:** DbInitializer class in Infrastructure layer

---

## Performance Considerations

### Indexes Priority

**Critical (query performance):**
- Patient.MedicalRecordNumber (unique, frequent lookups)
- Appointment.BranchId_DateTime (schedule view)
- QueueEntry.Branch_QueuedAt (queue list)
- Encounter.PatientId, EncounterId (patient history)
- Invoice.PatientId, InvoiceDate (billing history)
- CashDrawer.UserId_Status (active drawer lookup)

**Important (reporting):**
- Payment.PaymentDate, PaymentMethod (financial reports)
- Expense.BranchId_PaidAt (expense reports)
- AuditLog.UserId_Timestamp, EntityType_EntityId (audit queries)

### Query Optimization

**Global Query Filter (EF Core):**
```csharp
modelBuilder.Entity<AuditableEntity>()
    .HasQueryFilter(e => !e.IsDeleted);
```

**Caching Candidates:**
- SystemConfiguration (rarely changes)
- Branch list (small, rarely changes)
- AppointmentType list (small, rarely changes)
- Service list (changes infrequently)

---

## Backup & Recovery

### SQLite Backup

**Simple file copy:**
```powershell
# Backup
Copy-Item "data/clinic.db" "backups/clinic_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"

# Restore
Copy-Item "backups/clinic_20261021_120000.db" "data/clinic.db"
```

**Incremental backup:** Not natively supported in SQLite, copy full file

### PostgreSQL Backup (v1.1+)

```bash
# Backup
pg_dump -h localhost -U cliniccare -d cliniccare_db > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore
psql -h localhost -U cliniccare -d cliniccare_db < backup_20261021_120000.sql
```

---

## SQLite to PostgreSQL Migration

**Data Types Mapping:**

| SQLite | PostgreSQL | Notes |
|--------|------------|-------|
| TEXT (GUID) | UUID | Convert string to UUID |
| TEXT (DateTime) | TIMESTAMP | ISO 8601 → PostgreSQL timestamp |
| INTEGER | INT or BIGINT | Depends on range |
| REAL | DECIMAL(p,s) | Specify precision/scale |
| BLOB | BYTEA | Binary data |

**Migration Script Template:**
```sql
-- PostgreSQL schema
CREATE TABLE Patient (
    Id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    MedicalRecordNumber VARCHAR(50) NOT NULL UNIQUE,
    FirstName       VARCHAR(100) NOT NULL,
    -- ... (converted types)
    DateOfBirth     DATE NOT NULL,
    CreatedDate     TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Data migration (using tool like pgloader or custom script)
```

---

## Constraints Summary

**Check Constraints:** 20+ (vitals ranges, amounts > 0, variance calculation, etc.)  
**Foreign Keys:** 40+ (all relationships)  
**Unique Constraints:** 15+ (MRN, invoice number, branch name, etc.)  
**Indexes:** 50+ (performance-critical queries)

---

## Testing Data Generators

**For integration tests, generate:**
- 100 patients with realistic data
- 500 appointments (past/future)
- 200 encounters with vitals/diagnoses/prescriptions
- 300 invoices with payments (various statuses)
- 50 cash drawers (mix of open/closed/reconciled)

**Use libraries:**
- Bogus (C# fake data generator)
- AutoFixture (test data builder)

---

## Cross-References

- **Domain Model:** Entity definitions in `05-domain-model.md`
- **Workflows:** Business rules in `04-workflows.md`, `08-financial-workflows.md`
- **API Spec:** DTOs and validation in `11-backend-api-specification.md`
- **Testing:** Schema validation tests in `12-testing-strategy.md`

---

## Open Questions

See `16-open-questions.md`:

**Q1:** Use GUID vs INT for primary keys? (Decision: GUID for distributed scenarios, easier merging)

**Q2:** DateTime vs DateTimeOffset? (Decision: DateTime with explicit UTC conversion, simpler for SQLite)

**Q3:** NVARCHAR vs VARCHAR? (Decision: NVARCHAR for Unicode support, international names)

---

*This schema is the canonical database design for ClinicCare. All migrations must match this specification exactly. Any deviation requires explicit approval and documentation.*
