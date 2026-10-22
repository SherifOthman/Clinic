# 11. Backend API Specification

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [05-domain-model.md](./05-domain-model.md), [07-database-design.md](./07-database-design.md), [09-security-authorization.md](./09-security-authorization.md), [10-frontend-specification.md](./10-frontend-specification.md)

---

## 1. Overview

### 1.1 Purpose
Define RESTful API contracts, request/response formats, authentication, and error handling for ClinicCare's ASP.NET Core backend.

### 1.2 Technology Stack
- ASP.NET Core 9.0 Minimal APIs
- MediatR for CQRS pattern
- FluentValidation for request validation
- Entity Framework Core 9.0 with SQLite
- JWT Bearer authentication
- Swagger/OpenAPI documentation

### 1.3 API Design Principles
1. **Resource-Oriented:** URLs represent resources, not actions
2. **Stateless:** No server-side session state
3. **Consistent:** Standard HTTP verbs and status codes
4. **Versioned:** API version in URL (`/api/v1/...`)
5. **Filterable:** Query parameters for pagination, search, sort

### 1.4 Base URL
```
Development: http://localhost:5000/api/v1
Production:  https://clinic.example.com/api/v1
```

---

## 2. Authentication & Authorization

### 2.1 JWT Authentication
**Endpoint:** `POST /api/v1/auth/login`

**Request:**
```json
{
  "email": "doctor@clinic.com",
  "password": "SecurePass123!",
  "remember": false
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6...",
  "expiresAt": "2026-10-22T10:00:00Z",
  "user": {
    "id": "usr_123",
    "email": "doctor@clinic.com",
    "fullName": "Dr. Ahmed Hassan",
    "role": "Doctor",
    "branches": ["branch_main", "branch_downtown"]
  }
}
```

**Error (401 Unauthorized):**
```json
{
  "message": "Invalid email or password",
  "code": "INVALID_CREDENTIALS"
}
```

### 2.2 Token Refresh
**Endpoint:** `POST /api/v1/auth/refresh`

**Request:**
```json
{
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Response (200 OK):** Same as login response

### 2.3 Authorization Header
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

All endpoints except `/auth/login` and `/auth/refresh` require this header.

### 2.4 JWT Claims
```json
{
  "sub": "usr_123",              // User ID
  "email": "doctor@clinic.com",
  "role": "Doctor",              // Single role (Admin, Doctor, Receptionist)
  "branches": ["branch_main"],   // Array of branch IDs user can access
  "iat": 1729501200,             // Issued at
  "exp": 1729587600              // Expiration (24 hours)
}
```

---

## 3. Common Patterns

### 3.1 Pagination
**Query Parameters:**
```
GET /api/v1/patients?page=1&pageSize=20
```

**Response Envelope:**
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 157,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

### 3.2 Filtering & Search
```
GET /api/v1/patients?search=john&gender=male&minAge=18
GET /api/v1/invoices?status=pending&fromDate=2026-10-01&toDate=2026-10-31
```

### 3.3 Sorting
```
GET /api/v1/patients?sortBy=lastName&sortOrder=asc
```
Valid `sortOrder`: `asc` | `desc`

### 3.4 Field Selection (Sparse Fieldsets)
```
GET /api/v1/patients/pat_123?fields=id,firstName,lastName,phone
```

### 3.5 Timestamps
All timestamps in ISO 8601 format with UTC timezone:
```
"createdAt": "2026-10-21T14:30:00Z"
```

### 3.6 Error Response Format
```json
{
  "message": "Validation failed",
  "code": "VALIDATION_ERROR",
  "errors": {
    "email": ["Email is required", "Invalid email format"],
    "phone": ["Phone number must be 10-15 digits"]
  },
  "traceId": "00-abc123-def456-00"
}
```

**Standard Error Codes:**
- `VALIDATION_ERROR` - Request validation failed
- `UNAUTHORIZED` - Missing or invalid token
- `FORBIDDEN` - Insufficient permissions
- `NOT_FOUND` - Resource not found
- `CONFLICT` - Resource conflict (duplicate, concurrent edit)
- `INTERNAL_ERROR` - Unexpected server error

---

## 4. API Endpoints

### 4.1 Patients Module

#### List Patients
```
GET /api/v1/patients
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 20, max: 100) |
| `search` | string | Search by name, phone, nationalId |
| `gender` | string | Filter by gender (`male`, `female`, `other`) |
| `minAge` | int | Minimum age filter |
| `maxAge` | int | Maximum age filter |
| `sortBy` | string | Sort field (`firstName`, `lastName`, `createdAt`) |
| `sortOrder` | string | Sort order (`asc`, `desc`) |

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "pat_123",
      "firstName": "Ahmed",
      "lastName": "Hassan",
      "dateOfBirth": "1985-03-15",
      "age": 41,
      "gender": "male",
      "phone": "+201234567890",
      "nationalId": "28503150101234",
      "hasActiveEncounter": false,
      "lastVisitDate": "2026-09-15T10:30:00Z",
      "createdAt": "2025-01-10T08:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

#### Search Patients (Autocomplete)
```
GET /api/v1/patients/search?q=ahmed&limit=10
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "pat_123",
      "fullName": "Ahmed Hassan",
      "phone": "+201234567890",
      "dateOfBirth": "1985-03-15"
    }
  ]
}
```

#### Get Patient by ID
```
GET /api/v1/patients/{patientId}
```

**Response (200 OK):**
```json
{
  "id": "pat_123",
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "dateOfBirth": "1985-03-15",
  "age": 41,
  "gender": "male",
  "phone": "+201234567890",
  "email": "ahmed@example.com",
  "address": "123 Main St, Cairo",
  "nationalId": "28503150101234",
  "bloodGroup": "O+",
  "allergies": ["Penicillin", "Peanuts"],
  "chronicConditions": ["Hypertension"],
  "emergencyContact": {
    "name": "Fatima Hassan",
    "phone": "+201987654321",
    "relationship": "Wife"
  },
  "createdAt": "2025-01-10T08:00:00Z",
  "updatedAt": "2026-10-21T14:30:00Z"
}
```

#### Create Patient
```
POST /api/v1/patients
```

**Request:**
```json
{
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "dateOfBirth": "1985-03-15",
  "gender": "male",
  "phone": "+201234567890",
  "email": "ahmed@example.com",
  "address": "123 Main St, Cairo",
  "nationalId": "28503150101234",
  "bloodGroup": "O+",
  "allergies": ["Penicillin"],
  "chronicConditions": ["Hypertension"],
  "emergencyContact": {
    "name": "Fatima Hassan",
    "phone": "+201987654321",
    "relationship": "Wife"
  }
}
```

**Response (201 Created):**
```json
{
  "id": "pat_123",
  "firstName": "Ahmed",
  "lastName": "Hassan",
  ...
}
```
**Location Header:** `/api/v1/patients/pat_123`

**Validation Rules:**
- `firstName`: Required, 1-100 characters
- `lastName`: Required, 1-100 characters
- `dateOfBirth`: Required, cannot be future date
- `gender`: Required, enum (`male`, `female`, `other`)
- `phone`: Required, regex `^\+?[0-9]{10,15}$`
- `email`: Optional, valid email format
- `nationalId`: Optional, unique if provided

#### Update Patient
```
PUT /api/v1/patients/{patientId}
```

**Request:** Same as Create Patient  
**Response (200 OK):** Updated patient object

#### Get Patient Visit History
```
GET /api/v1/patients/{patientId}/visits
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "encounterId": "enc_456",
      "visitDate": "2026-10-15T10:30:00Z",
      "type": "scheduled",
      "chiefComplaint": "Routine checkup",
      "diagnosis": "Hypertension, controlled",
      "branch": {
        "id": "branch_main",
        "name": "Main Clinic"
      },
      "totalCharge": 150.00,
      "isPaid": true
    }
  ],
  "pagination": { ... }
}
```

---

### 4.2 Appointments Module

#### List Appointments
```
GET /api/v1/appointments
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `branchId` | string | Filter by branch (required if user has multi-branch access) |
| `fromDate` | date | Start date (ISO 8601) |
| `toDate` | date | End date (ISO 8601) |
| `status` | string | Filter by status (`scheduled`, `completed`, `cancelled`, `no-show`) |
| `patientId` | string | Filter by patient |

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "apt_789",
      "patient": {
        "id": "pat_123",
        "fullName": "Ahmed Hassan",
        "phone": "+201234567890"
      },
      "branch": {
        "id": "branch_main",
        "name": "Main Clinic"
      },
      "scheduledAt": "2026-10-22T09:00:00Z",
      "type": "scheduled",
      "status": "scheduled",
      "reason": "Follow-up checkup",
      "queuePosition": 3,
      "createdAt": "2026-10-20T15:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

#### Create Appointment
```
POST /api/v1/appointments
```

**Request:**
```json
{
  "patientId": "pat_123",
  "branchId": "branch_main",
  "scheduledAt": "2026-10-22T09:00:00Z",
  "type": "scheduled",
  "reason": "Follow-up checkup",
  "notes": "Patient requested morning slot"
}
```

**Response (201 Created):**
```json
{
  "id": "apt_789",
  "patient": { ... },
  "branch": { ... },
  "scheduledAt": "2026-10-22T09:00:00Z",
  "queueToken": {
    "id": "tok_101",
    "number": 15,
    "status": "waiting"
  },
  ...
}
```

**Business Rules:**
- Creates both Appointment and QueueToken
- Appointment time must be future
- Check for conflicts (same patient, overlapping time) - return 409 Conflict

#### Cancel Appointment
```
PATCH /api/v1/appointments/{appointmentId}/cancel
```

**Request:**
```json
{
  "reason": "Patient requested reschedule"
}
```

**Response (200 OK):** Updated appointment with `status: "cancelled"`

---

### 4.3 Queue Management Module

#### Get Queue Status
```
GET /api/v1/queue/{branchId}
```

**Response (200 OK):**
```json
{
  "branchId": "branch_main",
  "date": "2026-10-21",
  "tokens": [
    {
      "id": "tok_101",
      "number": 15,
      "patient": {
        "id": "pat_123",
        "fullName": "Ahmed Hassan"
      },
      "appointment": {
        "id": "apt_789",
        "scheduledAt": "2026-10-21T09:00:00Z"
      },
      "status": "waiting",
      "calledAt": null,
      "completedAt": null,
      "waitDuration": "00:15:30",
      "position": 2
    },
    {
      "id": "tok_102",
      "number": 16,
      "status": "in-progress",
      "calledAt": "2026-10-21T09:00:00Z",
      "waitDuration": "00:10:00"
    }
  ],
  "statistics": {
    "waiting": 5,
    "inProgress": 1,
    "completed": 12,
    "noShow": 1,
    "averageWaitTime": "00:18:45"
  }
}
```

#### Call Next Patient
```
POST /api/v1/queue/{branchId}/call-next
```

**Response (200 OK):**
```json
{
  "token": {
    "id": "tok_101",
    "number": 15,
    "patient": { ... },
    "status": "called",
    "calledAt": "2026-10-21T09:15:00Z"
  }
}
```

**Business Rules:**
- Calls next token in `waiting` status
- Updates previous `called` token to `in-progress`
- Returns 404 if no waiting tokens

#### Mark Token Status
```
PATCH /api/v1/queue/tokens/{tokenId}/status
```

**Request:**
```json
{
  "status": "no-show",
  "notes": "Patient did not respond to call"
}
```

**Response (200 OK):** Updated token object

**Valid Transitions:**
- `waiting` → `called` (via call-next)
- `called` → `in-progress` (auto when next called)
- `in-progress` → `completed` (via complete encounter)
- `waiting|called` → `no-show` (manual)

---

### 4.4 Consultation Module

#### Start Consultation (Create Encounter)
```
POST /api/v1/encounters
```

**Request:**
```json
{
  "patientId": "pat_123",
  "appointmentId": "apt_789",
  "branchId": "branch_main",
  "type": "scheduled",
  "chiefComplaint": "Headache for 3 days"
}
```

**Response (201 Created):**
```json
{
  "id": "enc_456",
  "patient": { ... },
  "appointment": { ... },
  "branch": { ... },
  "type": "scheduled",
  "status": "in-progress",
  "chiefComplaint": "Headache for 3 days",
  "startTime": "2026-10-21T09:15:00Z",
  "createdBy": {
    "id": "usr_doc1",
    "fullName": "Dr. Ahmed Hassan"
  }
}
```

**Business Rules:**
- Marks QueueToken as `in-progress`
- Only one active encounter per patient at a time
- Only Doctor role can create encounters

#### Update Encounter Notes
```
PATCH /api/v1/encounters/{encounterId}/notes
```

**Request:**
```json
{
  "subjective": "Patient reports headache...",
  "objective": "BP: 120/80, Temp: 37.2°C...",
  "assessment": "Tension headache...",
  "plan": "Rest, hydration, paracetamol..."
}
```

**Response (200 OK):** Updated encounter

**Autosave:** Frontend sends PATCH every 5 seconds during consultation

#### Record Vitals
```
POST /api/v1/encounters/{encounterId}/vitals
```

**Request:**
```json
{
  "bloodPressureSystolic": 120,
  "bloodPressureDiastolic": 80,
  "heartRate": 72,
  "temperature": 37.2,
  "weight": 75.5,
  "height": 175,
  "oxygenSaturation": 98,
  "notes": "Patient resting"
}
```

**Response (201 Created):** Created Observation object

#### Add Diagnosis
```
POST /api/v1/encounters/{encounterId}/diagnoses
```

**Request:**
```json
{
  "icd10Code": "R51.9",
  "description": "Headache, unspecified",
  "isPrimary": true,
  "notes": "Tension-type headache"
}
```

**Response (201 Created):** Created Condition object

#### Create Medication Order
```
POST /api/v1/encounters/{encounterId}/medications
```

**Request:**
```json
{
  "medicationName": "Paracetamol 500mg",
  "dosage": "500mg",
  "frequency": "Every 6 hours",
  "duration": "3 days",
  "quantity": 12,
  "instructions": "Take with food",
  "notes": "If no improvement in 3 days, return"
}
```

**Response (201 Created):** Created MedicationRequest object

#### Complete Encounter
```
POST /api/v1/encounters/{encounterId}/complete
```

**Request:**
```json
{
  "followUpDate": "2026-10-28",
  "followUpNotes": "Check if headache resolved"
}
```

**Response (200 OK):**
```json
{
  "id": "enc_456",
  "status": "completed",
  "endTime": "2026-10-21T09:45:00Z",
  "duration": "00:30:00",
  "invoice": {
    "id": "inv_999",
    "totalAmount": 150.00,
    "status": "pending"
  }
}
```

**Business Rules:**
- Marks QueueToken as `completed`
- Automatically creates Invoice with default consultation fee
- Encounter cannot be edited after completion (immutable)

---

### 4.5 Billing Module

#### List Invoices
```
GET /api/v1/invoices
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `patientId` | string | Filter by patient |
| `status` | string | `pending`, `partial`, `paid`, `cancelled` |
| `fromDate` | date | Invoice date range start |
| `toDate` | date | Invoice date range end |
| `branchId` | string | Filter by branch |

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "inv_999",
      "invoiceNumber": "INV-2026-001234",
      "patient": {
        "id": "pat_123",
        "fullName": "Ahmed Hassan"
      },
      "encounter": {
        "id": "enc_456",
        "date": "2026-10-21"
      },
      "items": [
        {
          "description": "General Consultation",
          "quantity": 1,
          "unitPrice": 150.00,
          "totalPrice": 150.00
        }
      ],
      "subtotal": 150.00,
      "discount": 0.00,
      "tax": 0.00,
      "totalAmount": 150.00,
      "paidAmount": 0.00,
      "balanceDue": 150.00,
      "status": "pending",
      "issuedAt": "2026-10-21T09:45:00Z",
      "dueDate": "2026-10-28T00:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

#### Get Invoice by ID
```
GET /api/v1/invoices/{invoiceId}
```

**Response (200 OK):** Full invoice object with payment history

#### Create Invoice
```
POST /api/v1/invoices
```

**Request:**
```json
{
  "patientId": "pat_123",
  "encounterId": "enc_456",
  "branchId": "branch_main",
  "items": [
    {
      "description": "General Consultation",
      "quantity": 1,
      "unitPrice": 150.00,
      "taxRate": 0.00
    },
    {
      "description": "Lab Test - CBC",
      "quantity": 1,
      "unitPrice": 80.00,
      "taxRate": 0.00
    }
  ],
  "discount": {
    "type": "fixed",
    "value": 20.00
  },
  "dueDate": "2026-10-28",
  "notes": "Follow-up in 7 days"
}
```

**Response (201 Created):** Created invoice object

**Validation:**
- At least one item required
- `unitPrice` must be positive
- `taxRate` between 0-100
- `discount.value` must be positive

#### Record Payment
```
POST /api/v1/invoices/{invoiceId}/payments
```

**Request:**
```json
{
  "amount": 150.00,
  "method": "cash",
  "cashDrawerId": "drawer_555",
  "reference": "CASH-2026-001",
  "notes": "Paid in full"
}
```

**Response (201 Created):**
```json
{
  "id": "pay_777",
  "invoice": {
    "id": "inv_999",
    "totalAmount": 150.00,
    "paidAmount": 150.00,
    "balanceDue": 0.00,
    "status": "paid"
  },
  "amount": 150.00,
  "method": "cash",
  "cashDrawer": {
    "id": "drawer_555"
  },
  "paidAt": "2026-10-21T10:00:00Z",
  "receivedBy": {
    "id": "usr_rec1",
    "fullName": "Sara Ali"
  }
}
```

**Payment Methods:**
- `cash` - Requires `cashDrawerId`
- `card` - Optional `reference` (transaction ID)
- `insurance` - Optional `reference` (claim number)

**Business Rules:**
- Payment amount cannot exceed `balanceDue`
- Updates invoice status: `pending` → `partial` → `paid`
- If `method=cash`, updates CashDrawer balance

---

### 4.6 Cash Drawer Module

#### Open Cash Drawer
```
POST /api/v1/cash-drawers/open
```

**Request:**
```json
{
  "branchId": "branch_main",
  "openingBalance": 500.00,
  "notes": "Morning shift, 2026-10-21"
}
```

**Response (201 Created):**
```json
{
  "id": "drawer_555",
  "branch": {
    "id": "branch_main",
    "name": "Main Clinic"
  },
  "openedAt": "2026-10-21T08:00:00Z",
  "openedBy": {
    "id": "usr_rec1",
    "fullName": "Sara Ali"
  },
  "openingBalance": 500.00,
  "currentBalance": 500.00,
  "status": "open"
}
```

**Business Rules:**
- Only one open drawer per branch at a time
- Returns 409 Conflict if drawer already open

#### Get Active Cash Drawer
```
GET /api/v1/cash-drawers/active?branchId=branch_main
```

**Response (200 OK):** Cash drawer object  
**Response (404 Not Found):** No active drawer

#### Record Expense
```
POST /api/v1/cash-drawers/{drawerId}/expenses
```

**Request:**
```json
{
  "amount": 50.00,
  "category": "supplies",
  "description": "Office supplies - printer paper",
  "notes": "Receipt #12345"
}
```

**Response (201 Created):**
```json
{
  "id": "exp_888",
  "cashDrawer": {
    "id": "drawer_555",
    "currentBalance": 450.00
  },
  "amount": 50.00,
  "category": "supplies",
  "description": "Office supplies - printer paper",
  "recordedAt": "2026-10-21T11:00:00Z",
  "recordedBy": {
    "id": "usr_rec1",
    "fullName": "Sara Ali"
  }
}
```

**Expense Categories:**
- `supplies` - Office/medical supplies
- `utilities` - Electricity, water, internet
- `maintenance` - Repairs, cleaning
- `salaries` - Staff payments
- `other` - Miscellaneous

#### Close Cash Drawer
```
POST /api/v1/cash-drawers/{drawerId}/close
```

**Request:**
```json
{
  "closingBalance": 1200.00,
  "notes": "End of morning shift"
}
```

**Response (200 OK):**
```json
{
  "id": "drawer_555",
  "status": "closed",
  "closedAt": "2026-10-21T14:00:00Z",
  "closedBy": {
    "id": "usr_rec1",
    "fullName": "Sara Ali"
  },
  "openingBalance": 500.00,
  "closingBalance": 1200.00,
  "expectedBalance": 1180.00,
  "discrepancy": 20.00,
  "summary": {
    "totalPayments": 750.00,
    "totalExpenses": 70.00,
    "netChange": 680.00
  }
}
```

**Business Rules:**
- Calculates expected balance: `openingBalance + totalPayments - totalExpenses`
- Flags discrepancy if `|closingBalance - expectedBalance| > 5.00`
- Drawer cannot be reopened after closing

#### Get Cash Drawer History
```
GET /api/v1/cash-drawers?branchId=branch_main&fromDate=2026-10-01&toDate=2026-10-31
```

**Response (200 OK):** List of cash drawers with pagination

---

### 4.7 Settings & Administration Module

#### List Users
```
GET /api/v1/users
```

**Permission:** Admin only

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "usr_doc1",
      "email": "doctor@clinic.com",
      "fullName": "Dr. Ahmed Hassan",
      "role": "Doctor",
      "branches": ["branch_main", "branch_downtown"],
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ]
}
```

#### Create User
```
POST /api/v1/users
```

**Permission:** Admin only

**Request:**
```json
{
  "email": "newuser@clinic.com",
  "password": "SecurePass123!",
  "fullName": "Dr. Sara Ali",
  "role": "Doctor",
  "branches": ["branch_main"]
}
```

**Response (201 Created):** Created user object

**Password Requirements:**
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

#### List Branches
```
GET /api/v1/branches
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "branch_main",
      "name": "Main Clinic",
      "address": "123 Main Street, Cairo",
      "phone": "+20123456789",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ]
}
```

#### Create Branch
```
POST /api/v1/branches
```

**Permission:** Admin only

**Request:**
```json
{
  "name": "Downtown Branch",
  "address": "456 Downtown Blvd, Cairo",
  "phone": "+20198765432"
}
```

**Response (201 Created):** Created branch object

---

## 5. Error Handling

### 5.1 HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, PATCH requests |
| 201 | Created | Successful POST requests |
| 204 | No Content | Successful DELETE requests |
| 400 | Bad Request | Malformed request, validation errors |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | Authenticated but insufficient permissions |
| 404 | Not Found | Resource does not exist |
| 409 | Conflict | Resource conflict (duplicate, concurrent edit) |
| 422 | Unprocessable Entity | Business rule violation |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected server error |

### 5.2 Validation Error Example
```json
{
  "message": "Validation failed",
  "code": "VALIDATION_ERROR",
  "errors": {
    "email": [
      "Email is required",
      "Email must be in valid format"
    ],
    "phone": [
      "Phone number must be between 10-15 digits"
    ],
    "dateOfBirth": [
      "Date of birth cannot be in the future"
    ]
  },
  "traceId": "00-abc123-def456-00"
}
```

### 5.3 Business Rule Violation Example
```json
{
  "message": "Cannot create appointment: Patient already has an active encounter",
  "code": "ACTIVE_ENCOUNTER_EXISTS",
  "details": {
    "encounterId": "enc_456",
    "branchId": "branch_main"
  },
  "traceId": "00-def789-ghi012-00"
}
```

### 5.4 Not Found Example
```json
{
  "message": "Patient not found",
  "code": "PATIENT_NOT_FOUND",
  "details": {
    "patientId": "pat_invalid"
  },
  "traceId": "00-xyz789-abc123-00"
}
```

---

## 6. API Versioning

### 6.1 URL Versioning
```
/api/v1/patients
/api/v2/patients  (future)
```

### 6.2 Deprecation Policy
- Deprecated endpoints return `Deprecated: true` header
- Minimum 6 months notice before removal
- Sunset date in `Sunset` header (RFC 8594)

---

## 7. Rate Limiting

### 7.1 Rate Limits
- Anonymous: 10 requests/minute
- Authenticated: 100 requests/minute
- Admin: 200 requests/minute

### 7.2 Rate Limit Headers
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1729501260
```

### 7.3 Rate Limit Exceeded Response (429)
```json
{
  "message": "Rate limit exceeded. Please try again in 42 seconds.",
  "code": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 42
}
```

---

## 8. OpenAPI/Swagger Documentation

### 8.1 Swagger UI
```
Development: http://localhost:5000/swagger
Production:  https://clinic.example.com/swagger (disabled in production)
```

### 8.2 OpenAPI Spec
```
GET /swagger/v1/swagger.json
```

### 8.3 Code Generation
OpenAPI spec enables client generation:
```bash
# Generate TypeScript client
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g typescript-axios \
  -o frontend/src/lib/api-client
```

---

## 9. Performance & Caching

### 9.1 Response Caching
```
Cache-Control: private, max-age=300  (5 minutes)
ETag: "abc123def456"
```

**Cached Endpoints:**
- `GET /api/v1/branches` - 30 minutes
- `GET /api/v1/users` - 10 minutes
- `GET /api/v1/patients/{id}` - 5 minutes

### 9.2 Conditional Requests
```
# Client sends
If-None-Match: "abc123def456"

# Server responds
304 Not Modified (if unchanged)
200 OK (if changed, with new ETag)
```

### 9.3 Pagination Performance
- Default `pageSize`: 20
- Maximum `pageSize`: 100
- Cursor-based pagination for large datasets (P2 feature)

---

## 10. Security Considerations

### 10.1 HTTPS Only
All API requests must use HTTPS in production. HTTP requests redirected to HTTPS.

### 10.2 CORS Configuration
```csharp
// Development
AllowedOrigins: ["http://localhost:5173"]

// Production
AllowedOrigins: ["https://clinic.example.com"]
AllowCredentials: true
AllowedMethods: ["GET", "POST", "PUT", "PATCH", "DELETE"]
AllowedHeaders: ["Authorization", "Content-Type"]
```

### 10.3 SQL Injection Prevention
- All database queries use parameterized queries via EF Core
- No raw SQL with string interpolation

### 10.4 Sensitive Data Handling
- Passwords never returned in API responses
- PII (national ID, phone) only accessible with proper permissions
- Audit logs for all data access (Admin role)

### 10.5 Input Validation
- FluentValidation rules on all commands
- Maximum request body size: 10MB
- File uploads (P2): Max 5MB per file, allowed types: PDF, JPG, PNG

---

## 11. Testing Endpoints

### 11.1 Health Check
```
GET /api/health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy"
  },
  "timestamp": "2026-10-21T14:30:00Z"
}
```

### 11.2 Version Info
```
GET /api/version
```

**Response (200 OK):**
```json
{
  "version": "1.0.0",
  "commit": "ef9617d",
  "buildDate": "2026-10-15T10:00:00Z"
}
```

---

## 12. Open Questions

### Q1: Real-time Updates
**Question:** Should queue status use WebSockets or Server-Sent Events (SSE)?  
**Context:** Frontend needs real-time queue updates  
**Options:**
- A) WebSocket (bidirectional, requires SignalR)
- B) SSE (unidirectional, simpler for read-only updates)
- C) Polling (simplest, higher latency)

**Recommendation:** Option C (polling every 10s) for v1.0, upgrade to SSE in v1.1  
**Decision:** [PENDING]

### Q2: File Upload Endpoints
**Question:** How to handle file uploads (patient documents, prescriptions)?  
**Context:** P2 feature, not critical for v1.0  
**Recommendation:** Multipart form-data upload, store in `wwwroot/uploads/`, reference in database  
**Decision:** [DEFERRED TO v1.1]

### Q3: Bulk Operations
**Question:** Should API support bulk create/update/delete?  
**Context:** E.g., importing patients from CSV  
**Recommendation:** Add `POST /api/v1/patients/bulk` endpoint in v1.1 if needed  
**Decision:** [DEFERRED]

### Q4: GraphQL Alternative
**Question:** Should we provide GraphQL endpoint alongside REST?  
**Context:** Frontend has varying data needs (over-fetching vs under-fetching)  
**Recommendation:** REST for v1.0 (simpler), evaluate GraphQL in v2.0 if needed  
**Decision:** [DEFERRED]

---

## 13. Related Documents

- [05-domain-model.md](./05-domain-model.md) - Entities mapped to DTOs
- [07-database-design.md](./07-database-design.md) - Database schema backing API
- [09-security-authorization.md](./09-security-authorization.md) - Permission checks in API
- [10-frontend-specification.md](./10-frontend-specification.md) - Frontend consuming this API
- [12-testing-strategy.md](./12-testing-strategy.md) - API testing approach

---

## 14. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial API specification |

---

**Document Confidence:** HIGH  
**Evidence Base:** ASP.NET Core Minimal API patterns, CARE backend structure, RESTful best practices  
**Assumptions:** JWT authentication, no OAuth/OIDC, single-tenant (no tenant ID in URLs)  
**Next Review:** After frontend implementation begins, validate API contracts meet frontend needs
