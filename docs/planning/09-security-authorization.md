# 09 - Security & Authorization Model

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 03-product-scope.md, 05-domain-model.md, 08-financial-workflows.md  
**Feeds Into:** 11-backend-api-specification.md, 12-testing-strategy.md

---

## Purpose

Define the complete security and authorization model for ClinicCare including authentication, role-based access control (RBAC), branch-level authorization, data access rules, audit logging, and security best practices.

---

## Security Principles

1. **Principle of Least Privilege:** Users have only the permissions necessary for their role
2. **Defense in Depth:** Multiple layers of security (authentication, authorization, validation, audit)
3. **Secure by Default:** All endpoints protected unless explicitly public
4. **Audit Everything:** All sensitive operations logged with who/what/when
5. **Fail Secure:** On error, deny access rather than grant
6. **Data Minimization:** Users see only data relevant to their role and branch

---

## 1. Authentication

### 1.1 Local Authentication (v1.0)

**Technology:** ASP.NET Core Identity + JWT Tokens

**Flow:**
1. User submits credentials (username/password)
2. Server validates against database (PasswordHash)
3. Server generates JWT token with claims (userId, roles, branches)
4. Client stores token (HttpOnly cookie OR localStorage)
5. Client includes token in Authorization header for subsequent requests
6. Server validates token on each request

**Password Policy:**
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number
- At least 1 special character
- Password history: cannot reuse last 3 passwords

**Token Configuration:**
```json
{
  "Jwt": {
    "Secret": "[256-bit secret, env variable]",
    "Issuer": "ClinicCare",
    "Audience": "ClinicCare-Client",
    "ExpirationMinutes": 480,  // 8 hours
    "RefreshTokenExpirationDays": 7
  }
}
```

**JWT Claims:**
```json
{
  "sub": "user-guid",
  "name": "Dr. John Smith",
  "email": "john.smith@clinic.com",
  "roles": ["Doctor"],
  "branches": ["branch-guid-1", "branch-guid-2"],
  "default_branch": "branch-guid-1",
  "iat": 1634567890,
  "exp": 1634596690
}
```

### 1.2 Session Management

- **Session Duration:** 8 hours (configurable)
- **Idle Timeout:** 30 minutes of inactivity → re-authentication required
- **Concurrent Sessions:** Allowed (same user, multiple devices)
- **Logout:** Invalidate token (client-side delete + server-side blacklist for revocation)

### 1.3 Future Enhancements (v1.1+)

- **SSO (Single Sign-On):** SAML 2.0 or OAuth 2.0
- **MFA (Multi-Factor Authentication):** SMS, Email, TOTP
- **Biometric:** Fingerprint for mobile apps

---

## 2. Role-Based Access Control (RBAC)

### 2.1 Predefined Roles

#### Doctor Role

**Permissions:**
- ✅ View all patients (all branches)
- ✅ Create/update patient records
- ✅ View all appointments (all branches)
- ✅ Create/update/cancel appointments
- ✅ View queue (all branches)
- ✅ Call next patient, manage queue
- ✅ Create/complete encounters
- ✅ View/create diagnoses, prescriptions, clinical notes
- ✅ View invoices (all branches)
- ❌ Create/edit invoices directly (auto-generated from encounter)
- ❌ Record payments (Receptionist role)
- ❌ Open/close cash drawer (Receptionist role)
- ❌ Manage users (Admin role)
- ✅ Approve large refunds (> threshold)
- ✅ Review cash drawer variances
- ✅ View all reports

**Access Scope:**
- **Patient Data:** All branches
- **Appointments/Queue:** All branches
- **Encounters:** All branches
- **Billing:** View only (all branches)

#### Receptionist Role

**Permissions:**
- ✅ View patients (assigned branches only)
- ✅ Create/update patient records (assigned branches only)
- ✅ View/create/update/cancel appointments (assigned branches only)
- ✅ Add patients to queue (assigned branches only)
- ✅ View encounter list (cannot view clinical details)
- ❌ Create/edit encounters (Doctor role)
- ✅ View/create/edit invoices (assigned branches only)
- ✅ Record payments (cash, card, etc.) (assigned branches only)
- ✅ Open/close cash drawer (assigned branches only)
- ✅ Record expenses (cash drawer) (assigned branches only)
- ✅ Request refunds (< threshold, auto-approved; > threshold, requires Doctor/Admin approval)
- ✅ View financial reports (assigned branches only)
- ❌ Manage users (Admin role)
- ❌ Manage branches (Admin role)
- ❌ Manage system configuration (Admin role)

**Access Scope:**
- **Patient Data:** All branches (search/view), assigned branches (create/update)
- **Appointments/Queue:** Assigned branches only
- **Billing:** Assigned branches only
- **Cash Drawer:** Own drawer only

#### Admin Role

**Permissions:**
- ✅ All Doctor permissions
- ✅ All Receptionist permissions (all branches)
- ✅ Manage users (create, edit, deactivate, assign roles, assign branches)
- ✅ Manage branches (create, edit, deactivate)
- ✅ Manage system configuration
- ✅ Manage services (create, edit, pricing)
- ✅ Manage appointment types
- ✅ Approve large refunds
- ✅ Review/approve cash drawer variances
- ✅ View all audit logs
- ✅ Access all reports

**Access Scope:**
- **Everything:** All branches, all data

### 2.2 Permission Matrix

| Feature | Doctor | Receptionist | Admin |
|---------|--------|--------------|-------|
| **Patient Management** | | | |
| View patient (all branches) | ✅ | Search only | ✅ |
| View patient (assigned branches) | ✅ | ✅ | ✅ |
| Create/edit patient | ✅ | Assigned branches | ✅ |
| Delete patient (soft) | ❌ | ❌ | ✅ |
| **Scheduling** | | | |
| View appointments (all branches) | ✅ | ❌ | ✅ |
| View appointments (assigned branches) | ✅ | ✅ | ✅ |
| Create/edit appointment | ✅ | Assigned branches | ✅ |
| Cancel appointment | ✅ | Assigned branches | ✅ |
| **Queue** | | | |
| View queue (all branches) | ✅ | ❌ | ✅ |
| View queue (assigned branches) | ✅ | ✅ | ✅ |
| Add to queue | ✅ | Assigned branches | ✅ |
| Call next patient | ✅ | ❌ | ✅ |
| **Clinical** | | | |
| Create/complete encounter | ✅ | ❌ | ✅ |
| View encounter details | ✅ | Summary only | ✅ |
| Add vitals/diagnosis/prescription | ✅ | ❌ | ✅ |
| Add clinical notes/addendum | ✅ | ❌ | ✅ |
| Upload attachments | ✅ | ❌ | ✅ |
| **Billing** | | | |
| View invoices (all branches) | ✅ | ❌ | ✅ |
| View invoices (assigned branches) | ✅ | ✅ | ✅ |
| Create/edit invoice | Auto-generated | ✅ (assigned) | ✅ |
| Record payment | ❌ | ✅ (assigned) | ✅ |
| Process refund (< threshold) | ❌ | ✅ (assigned) | ✅ |
| Approve refund (> threshold) | ✅ | ❌ | ✅ |
| **Cash Drawer** | | | |
| Open/close drawer | ❌ | Own drawer | ✅ |
| Record cash payment | ❌ | Own drawer | ✅ |
| Record expense | ❌ | Own drawer | ✅ |
| Review variance | ✅ | ❌ | ✅ |
| **Administration** | | | |
| Manage users | ❌ | ❌ | ✅ |
| Manage branches | ❌ | ❌ | ✅ |
| Manage services | ❌ | ❌ | ✅ |
| Manage system config | ❌ | ❌ | ✅ |
| **Reports** | | | |
| View clinical reports | ✅ | ❌ | ✅ |
| View financial reports (all) | ✅ | ❌ | ✅ |
| View financial reports (assigned) | ❌ | ✅ | ✅ |
| View audit logs | ❌ | ❌ | ✅ |

---

## 3. Branch-Level Authorization

### 3.1 Branch Assignment

**UserBranch Table:**
```sql
CREATE TABLE UserBranch (
    UserId      NVARCHAR(450) NOT NULL,
    BranchId    UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (UserId, BranchId)
);
```

**Assignment Rules:**
- **Doctor:** No explicit branch assignment (sees all branches)
- **Receptionist:** MUST be assigned to at least one branch
- **Admin:** No explicit branch assignment (sees all branches)

### 3.2 Branch Filtering Logic

**Patient Search (Receptionist):**
```csharp
// Can search all patients, but create/update restricted to assigned branches
var patients = await _context.Patients
    .Where(p => !p.IsDeleted)
    .ToListAsync(); // Search all

// Create/Update check
if (!await _userService.HasAccessToBranch(branchId))
    throw new UnauthorizedException("Access denied to this branch");
```

**Appointment Queries (Receptionist):**
```csharp
var appointments = await _context.Appointments
    .Where(a => !a.IsDeleted 
             && _userBranches.Contains(a.BranchId)) // Filter by assigned branches
    .ToListAsync();
```

**Cash Drawer (Receptionist):**
```csharp
var drawer = await _context.CashDrawers
    .Where(d => d.UserId == currentUserId 
             && d.Status == CashDrawerStatus.Open)
    .FirstOrDefaultAsync(); // Own drawer only
```

### 3.3 Branch Context Selection

**Frontend Flow:**
1. Receptionist logs in
2. If assigned to multiple branches → show branch selector
3. Receptionist selects active branch for session
4. All operations default to selected branch
5. Selected branch stored in JWT claim or session

**API Enforcement:**
```csharp
// Middleware: Validate branch access
if (User.IsInRole("Receptionist"))
{
    var requestedBranchId = context.Request.Headers["X-Branch-Id"];
    if (!await UserHasAccessToBranch(User.GetUserId(), requestedBranchId))
        return Unauthorized("Access denied to this branch");
}
```

---

## 4. Data Access Rules

### 4.1 Sensitive Data Protection

**Protected Fields (encrypted at rest):**
- Patient.Photo (binary, not encrypted but access-controlled)
- AspNetUsers.PasswordHash (hashed, not encrypted)
- Payment.Reference (card last 4 digits) (not sensitive enough to encrypt, but audit logged)

**PHI (Protected Health Information) - HIPAA:**
- All Patient entity fields
- All Encounter entity fields
- Vitals, Diagnosis, Prescription, ClinicalNote, Attachment

**Access Control:**
- PHI access logged in AuditLog with Action = "View"
- Patient summary (name, MRN, DOB) not logged (too frequent)
- Full clinical details (SOAP notes, prescriptions) logged

### 4.2 Query-Level Authorization

**EF Core Global Query Filters:**

```csharp
// Apply soft delete filter to all entities
modelBuilder.Entity<AuditableEntity>()
    .HasQueryFilter(e => !e.IsDeleted);

// Apply branch filter for Receptionist role
if (User.IsInRole("Receptionist"))
{
    modelBuilder.Entity<Appointment>()
        .HasQueryFilter(a => _userBranches.Contains(a.BranchId));
    
    modelBuilder.Entity<QueueEntry>()
        .HasQueryFilter(q => _userBranches.Contains(q.BranchId));
    
    modelBuilder.Entity<Invoice>()
        .HasQueryFilter(i => _userBranches.Contains(i.BranchId));
}
```

### 4.3 Row-Level Security (Future: PostgreSQL)

**PostgreSQL RLS Policies:**

```sql
-- Enable RLS on Appointment table
ALTER TABLE Appointment ENABLE ROW LEVEL SECURITY;

-- Policy: Receptionist sees only assigned branches
CREATE POLICY receptionist_appointment_access ON Appointment
    FOR ALL
    TO receptionist_role
    USING (BranchId IN (
        SELECT BranchId FROM UserBranch WHERE UserId = current_user_id()
    ));

-- Policy: Doctor sees all
CREATE POLICY doctor_appointment_access ON Appointment
    FOR ALL
    TO doctor_role
    USING (true);
```

**Note:** Not applicable to SQLite (no RLS support). Enforce in application layer.

---

## 5. API Endpoint Authorization

### 5.1 Authorization Attributes

**ASP.NET Core Authorization:**

```csharp
// Public endpoint (no auth required)
[AllowAnonymous]
public async Task<IActionResult> Login([FromBody] LoginCommand command)

// Authenticated only
[Authorize]
public async Task<IActionResult> GetCurrentUser()

// Role-based
[Authorize(Roles = "Doctor,Admin")]
public async Task<IActionResult> CompleteEncounter([FromBody] CompleteEncounterCommand command)

// Policy-based (custom)
[Authorize(Policy = "CanManageFinances")]
public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentCommand command)
```

### 5.2 Authorization Policies

**Define in Program.cs:**

```csharp
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("RequireDoctor", policy => 
        policy.RequireRole("Doctor", "Admin"));
    
    options.AddPolicy("RequireReceptionist", policy => 
        policy.RequireRole("Receptionist", "Admin"));
    
    // Custom policies
    options.AddPolicy("CanManageFinances", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Receptionist") || 
            context.User.IsInRole("Admin")));
    
    options.AddPolicy("CanApproveLargeRefund", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Doctor") || 
            context.User.IsInRole("Admin")));
    
    options.AddPolicy("CanAccessAllBranches", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Doctor") || 
            context.User.IsInRole("Admin")));
});
```

### 5.3 Endpoint Authorization Matrix

| Endpoint | Method | Authorization |
|----------|--------|---------------|
| /api/auth/login | POST | [AllowAnonymous] |
| /api/auth/logout | POST | [Authorize] |
| /api/auth/me | GET | [Authorize] |
| /api/patients | GET | [Authorize] |
| /api/patients | POST | [Authorize(Roles="Doctor,Receptionist,Admin")] |
| /api/patients/{id} | GET | [Authorize] |
| /api/patients/{id} | PUT | [Authorize(Roles="Doctor,Receptionist,Admin")] + Branch check |
| /api/appointments | GET | [Authorize] + Branch filter |
| /api/appointments | POST | [Authorize(Roles="Doctor,Receptionist,Admin")] + Branch check |
| /api/queue/{branchId} | GET | [Authorize] + Branch check |
| /api/queue | POST | [Authorize(Roles="Doctor,Receptionist,Admin")] + Branch check |
| /api/encounters | POST | [Authorize(Roles="Doctor,Admin")] |
| /api/encounters/{id}/complete | PUT | [Authorize(Roles="Doctor,Admin")] |
| /api/invoices | GET | [Authorize] + Branch filter |
| /api/invoices | POST | [Authorize(Roles="Receptionist,Admin")] + Branch check |
| /api/payments | POST | [Authorize(Roles="Receptionist,Admin")] + Drawer check |
| /api/cash-drawer/open | POST | [Authorize(Roles="Receptionist,Admin")] + Branch check |
| /api/cash-drawer/close | POST | [Authorize(Roles="Receptionist,Admin")] + Own drawer only |
| /api/expenses | POST | [Authorize(Roles="Receptionist,Doctor,Admin")] + Drawer check (cash only) |
| /api/refunds | POST | [Authorize(Roles="Receptionist,Admin")] + Threshold check |
| /api/refunds/{id}/approve | PUT | [Authorize(Policy="CanApproveLargeRefund")] |
| /api/reports/financial | GET | [Authorize] + Branch filter |
| /api/users | GET | [Authorize(Roles="Admin")] |
| /api/users | POST | [Authorize(Roles="Admin")] |
| /api/branches | GET | [Authorize] |
| /api/branches | POST | [Authorize(Roles="Admin")] |

---

## 6. Audit Logging

### 6.1 Audit Events

**Logged Operations:**

| Entity | Actions | Logged Fields |
|--------|---------|---------------|
| Patient | Create, Update, Delete, View (clinical details) | All fields (old → new) |
| Encounter | Create, Update, Complete, View | SOAP notes, diagnoses, prescriptions |
| Invoice | Create, Update, Issue, Cancel | Amounts, line items |
| Payment | Create, Delete (refund) | Amount, method, drawer |
| CashDrawer | Open, Close, Approve/Reject Variance | Balances, variance |
| Expense | Create, Approve | Amount, category |
| AspNetUsers | Create, Update, Deactivate, PasswordChange | Role changes, branch assignments |
| Prescription | Create, Update, Discontinue | Medication, dosage |

**AuditLog Record:**
```json
{
  "id": "guid",
  "userId": "user-guid",
  "entityType": "Patient",
  "entityId": "patient-guid",
  "action": "Update",
  "timestamp": "2026-10-21T14:30:00Z",
  "changes": {
    "Phone": { "old": "555-1234", "new": "555-5678" },
    "Email": { "old": "old@example.com", "new": "new@example.com" }
  },
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0..."
}
```

### 6.2 Audit Log Retention

- **Duration:** 7 years (HIPAA requirement)
- **Storage:** AuditLog table (PostgreSQL recommended for long-term)
- **Archival:** Move logs older than 2 years to archive database
- **Immutability:** AuditLog records cannot be updated or deleted (enforce in application)

### 6.3 Audit Log Access

- **Who Can View:** Admin only
- **Search Filters:** UserId, EntityType, EntityId, Date Range, Action
- **Export:** Excel, CSV (for compliance audits)

---

## 7. Security Best Practices

### 7.1 Input Validation

**All API inputs validated:**
- **FluentValidation:** Model validation before processing
- **SQL Injection Prevention:** EF Core parameterized queries (never string concatenation)
- **XSS Prevention:** HTML encoding on output (React handles this)
- **Path Traversal Prevention:** Validate file paths, no `../` allowed
- **File Upload Validation:** File size, MIME type whitelist, virus scan (future)

### 7.2 HTTPS/TLS

- **Enforce HTTPS:** All API requests over HTTPS only (Redirect HTTP → HTTPS)
- **HSTS:** Strict-Transport-Security header
- **Certificate:** Use valid TLS certificate (Let's Encrypt for production)

### 7.3 CORS Policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("https://clinic.example.com", "http://localhost:5173") // React dev server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});
```

### 7.4 Rate Limiting

**Prevent brute force attacks:**

```csharp
// Login endpoint: 5 attempts per minute per IP
[RateLimit(5, TimeSpan.FromMinutes(1))]
public async Task<IActionResult> Login([FromBody] LoginCommand command)

// General API: 100 requests per minute per user
[RateLimit(100, TimeSpan.FromMinutes(1))]
```

### 7.5 Password Storage

- **Hashing:** ASP.NET Identity uses PBKDF2 with random salt (default)
- **Never Store Plain Text:** Passwords never stored unhashed
- **Password Reset:** Time-limited token (15 minutes expiry)

### 7.6 Secrets Management

**Environment Variables:**
```bash
# .env (development only, NOT committed to Git)
JWT_SECRET=your-256-bit-secret-here-change-in-production
DB_CONNECTION_STRING=Data Source=data/clinic.db
```

**Production:**
- Use Azure Key Vault, AWS Secrets Manager, or equivalent
- Never commit secrets to Git
- Rotate JWT secret periodically

---

## 8. Compliance Requirements

### 8.1 HIPAA (US - if applicable)

**Technical Safeguards:**
- ✅ Access Control: Role-based authorization
- ✅ Audit Controls: Comprehensive audit logging
- ✅ Integrity Controls: Checksums for file uploads (future)
- ✅ Transmission Security: HTTPS/TLS
- ✅ Encryption at Rest: SQLite file-level encryption (optional)

**Administrative Safeguards:**
- User training (outside scope of this system)
- Risk assessment (outside scope)

**Physical Safeguards:**
- Workstation security (user responsibility)
- Device encryption (user responsibility)

### 8.2 GDPR (EU - if applicable)

**Data Subject Rights:**
- ✅ Right to Access: Export patient data
- ✅ Right to Rectification: Update patient records
- ✅ Right to Erasure: Soft delete (IsDeleted flag, cannot hard delete for audit)
- ✅ Right to Data Portability: Export patient data as JSON/CSV
- ✅ Right to be Informed: Privacy policy (outside scope of this system)

**Consent Management:**
- Implicit consent for clinical care
- Explicit consent for marketing (not applicable in v1.0)

### 8.3 Other Jurisdictions

- Implement country-specific compliance as needed
- Consult local healthcare regulations

---

## 9. Security Testing

### 9.1 Unit Tests

- Role-based authorization tests (Doctor can access X, Receptionist cannot)
- Branch-level authorization tests (Receptionist sees only assigned branches)
- JWT token validation tests

### 9.2 Integration Tests

- End-to-end authorization flows
- Audit log creation verification
- Unauthorized access attempts (expect 401/403)

### 9.3 Penetration Testing

- Hire external security firm for production deployment
- Test for OWASP Top 10 vulnerabilities
- SQL injection, XSS, CSRF attempts

---

## 10. Incident Response

### 10.1 Security Breach Protocol

1. **Detection:** Automated alerts for suspicious activity (multiple failed logins, unusual data access patterns)
2. **Containment:** Disable compromised user accounts
3. **Investigation:** Review audit logs, identify scope of breach
4. **Notification:** Notify affected users (if PHI exposed), comply with breach notification laws
5. **Remediation:** Patch vulnerabilities, reset passwords, rotate secrets
6. **Documentation:** Document incident, lessons learned

### 10.2 Monitoring & Alerts

**Alert Triggers:**
- 10+ failed login attempts from same IP within 1 hour
- User role change (Admin should review)
- Large data export (> 1000 patient records)
- Cash drawer variance > $100
- Expense > $1000 without approval
- Database connection errors (potential attack)

**Tools:**
- Application Insights (Azure) or equivalent
- Custom alert rules in logging framework (Serilog)

---

## 5.5 Branch Data Isolation Rules

### 5.5.1 Entities with Branch Scope

**Global Entities (No Branch Filter):**
- Patient (shared across all branches)
- ApplicationUser (shared across all branches)
- Service (price list, shared across all branches)
- AppointmentType (shared across all branches)

**Branch-Scoped Entities (Filter by BranchId):**
- Appointment
- QueueEntry
- Encounter
- Invoice
- Payment
- CashDrawer
- Expense
- CashMovement

### 5.5.2 Branch Filter Implementation

**GetAppointmentsQuery (Receptionist):**
```csharp
public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, List<AppointmentDto>>
{
    public async Task<List<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var userBranchIds = await _currentUserService.GetUserBranchIdsAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        IQueryable<Appointment> query = _context.Appointments
            .Where(a => !a.IsDeleted);

        // Apply branch filter for Receptionist role
        if (userRole == "Receptionist")
        {
            query = query.Where(a => userBranchIds.Contains(a.BranchId));
        }

        // Doctor and Admin see all branches
        return await query
            .OrderBy(a => a.StartTime)
            .ProjectTo<AppointmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
```

**CreateInvoiceCommand (Receptionist):**
```csharp
public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var userBranchIds = await _currentUserService.GetUserBranchIdsAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        // Validate branch access for Receptionist
        if (userRole == "Receptionist" && !userBranchIds.Contains(request.BranchId))
        {
            throw new UnauthorizedAccessException("You do not have access to create invoices for this branch.");
        }

        // Proceed with invoice creation...
        var invoice = new Invoice
        {
            BranchId = request.BranchId,
            PatientId = request.PatientId,
            // ... other fields
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
```

**GetQueueStatusQuery (Receptionist):**
```csharp
public class GetQueueStatusQueryHandler : IRequestHandler<GetQueueStatusQuery, List<QueueTokenDto>>
{
    public async Task<List<QueueTokenDto>> Handle(GetQueueStatusQuery request, CancellationToken cancellationToken)
    {
        var userBranchIds = await _currentUserService.GetUserBranchIdsAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        IQueryable<QueueEntry> query = _context.QueueEntries
            .Include(q => q.Patient)
            .Include(q => q.Appointment)
            .Where(q => !q.IsDeleted && q.Date == DateTime.UtcNow.Date);

        // Apply branch filter for Receptionist
        if (userRole == "Receptionist")
        {
            query = query.Where(q => userBranchIds.Contains(q.BranchId));
        }

        return await query
            .OrderBy(q => q.TokenNumber)
            .ProjectTo<QueueTokenDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
```

### 5.5.3 Branch Access Validation

**ICurrentUserService Implementation:**
```csharp
public interface ICurrentUserService
{
    Task<string> GetUserIdAsync();
    Task<string> GetUserRoleAsync();
    Task<List<Guid>> GetUserBranchIdsAsync();
    Task<bool> HasAccessToBranchAsync(Guid branchId);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;

    public async Task<List<Guid>> GetUserBranchIdsAsync()
    {
        var userId = GetUserIdAsync().Result;
        var role = GetUserRoleAsync().Result;

        // Doctor and Admin have access to all branches
        if (role == "Doctor" || role == "Admin")
        {
            return await _context.Branches
                .Where(b => !b.IsDeleted)
                .Select(b => b.Id)
                .ToListAsync();
        }

        // Receptionist has access only to assigned branches
        return await _context.UserBranches
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BranchId)
            .ToListAsync();
    }

    public async Task<bool> HasAccessToBranchAsync(Guid branchId)
    {
        var userBranchIds = await GetUserBranchIdsAsync();
        return userBranchIds.Contains(branchId);
    }
}
```

---

## 5.6 Financial Authorization Rules

### 5.6.1 Payment Authorization

**Who Can Record Payments:**
- **Receptionist:** Can record payments ONLY if they have an open cash drawer (for cash payments) OR for their assigned branches (for card/insurance payments)
- **Admin:** Can record payments for any branch, any payment method

**RecordPaymentCommand Authorization:**
```csharp
public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Guid>
{
    public async Task<Guid> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();
        var userBranchIds = await _currentUserService.GetUserBranchIdsAsync();

        // Get the invoice to determine its branch
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);
        
        if (invoice == null)
            throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        // Validate branch access
        if (userRole == "Receptionist" && !userBranchIds.Contains(invoice.BranchId))
        {
            throw new UnauthorizedAccessException("You do not have access to record payments for this branch.");
        }

        // For cash payments, validate open cash drawer
        if (request.PaymentMethod == PaymentMethod.Cash)
        {
            var openDrawer = await _context.CashDrawers
                .Where(d => d.UserId == userId
                         && d.BranchId == invoice.BranchId
                         && d.Status == CashDrawerStatus.Open)
                .FirstOrDefaultAsync(cancellationToken);

            if (openDrawer == null)
            {
                throw new BusinessRuleViolationException("You must have an open cash drawer to record cash payments.");
            }

            // Update cash drawer balance
            openDrawer.CurrentBalance += request.Amount;
        }

        // Create payment record
        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReceivedBy = userId,
            PaymentDate = DateTime.UtcNow,
            CashDrawerId = openDrawer?.Id
        };

        _context.Payments.Add(payment);

        // Update invoice status
        invoice.PaidAmount += request.Amount;
        if (invoice.PaidAmount >= invoice.TotalAmount)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.PaidAmount > 0)
            invoice.Status = InvoiceStatus.PartiallyPaid;

        await _context.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
```

### 5.6.2 Cash Drawer Authorization

**Who Can Open/Close Cash Drawer:**
- **Receptionist:** Can open/close cash drawer ONLY for branches they are assigned to
- **Admin:** Can open/close cash drawer for any branch

**Business Rules:**
- One receptionist can have only ONE open cash drawer at a time
- One branch can have multiple open cash drawers (different receptionists)
- Cannot close drawer if not owned by current user (unless Admin)

**OpenCashDrawerCommand Authorization:**
```csharp
public class OpenCashDrawerCommandHandler : IRequestHandler<OpenCashDrawerCommand, Guid>
{
    public async Task<Guid> Handle(OpenCashDrawerCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();
        var userBranchIds = await _currentUserService.GetUserBranchIdsAsync();

        // Validate branch access
        if (userRole == "Receptionist" && !userBranchIds.Contains(request.BranchId))
        {
            throw new UnauthorizedAccessException("You do not have access to open cash drawers for this branch.");
        }

        // Check if user already has an open drawer
        var existingOpenDrawer = await _context.CashDrawers
            .Where(d => d.UserId == userId && d.Status == CashDrawerStatus.Open)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOpenDrawer != null)
        {
            throw new BusinessRuleViolationException("You already have an open cash drawer. Please close it before opening a new one.");
        }

        // Create new cash drawer
        var drawer = new CashDrawer
        {
            BranchId = request.BranchId,
            UserId = userId,
            OpeningBalance = request.OpeningBalance,
            CurrentBalance = request.OpeningBalance,
            OpenedAt = DateTime.UtcNow,
            Status = CashDrawerStatus.Open
        };

        _context.CashDrawers.Add(drawer);
        await _context.SaveChangesAsync(cancellationToken);

        return drawer.Id;
    }
}
```

**CloseCashDrawerCommand Authorization:**
```csharp
public class CloseCashDrawerCommandHandler : IRequestHandler<CloseCashDrawerCommand, Unit>
{
    public async Task<Unit> Handle(CloseCashDrawerCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        var drawer = await _context.CashDrawers
            .FirstOrDefaultAsync(d => d.Id == request.DrawerId, cancellationToken);

        if (drawer == null)
            throw new NotFoundException(nameof(CashDrawer), request.DrawerId);

        // Validate ownership (Receptionist can only close own drawer)
        if (userRole == "Receptionist" && drawer.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only close your own cash drawer.");
        }

        if (drawer.Status == CashDrawerStatus.Closed)
        {
            throw new BusinessRuleViolationException("This cash drawer is already closed.");
        }

        // Calculate expected balance
        var expectedBalance = drawer.OpeningBalance
            + await _context.Payments
                .Where(p => p.CashDrawerId == drawer.Id && p.PaymentMethod == PaymentMethod.Cash)
                .SumAsync(p => p.Amount, cancellationToken)
            - await _context.Expenses
                .Where(e => e.CashDrawerId == drawer.Id)
                .SumAsync(e => e.Amount, cancellationToken)
            + await _context.CashMovements
                .Where(m => m.ToCashDrawerId == drawer.Id)
                .SumAsync(m => m.Amount, cancellationToken)
            - await _context.CashMovements
                .Where(m => m.FromCashDrawerId == drawer.Id)
                .SumAsync(m => m.Amount, cancellationToken);

        drawer.ClosingBalance = request.ActualClosingBalance;
        drawer.ExpectedBalance = expectedBalance;
        drawer.Discrepancy = request.ActualClosingBalance - expectedBalance;
        drawer.ClosedAt = DateTime.UtcNow;
        drawer.Status = CashDrawerStatus.Closed;
        drawer.Notes = request.Notes;

        // If discrepancy > threshold, flag for review
        var discrepancyThreshold = 5.00m; // $5.00
        if (Math.Abs(drawer.Discrepancy) > discrepancyThreshold)
        {
            drawer.RequiresApproval = true;
            // TODO: Send notification to Doctor/Admin
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

### 5.6.3 Expense Authorization

**Who Can Record Expenses:**
- **Receptionist:** Can record expenses ONLY from their own open cash drawer
- **Doctor:** Can record expenses with approval workflow (expense created with Pending status, requires Admin approval)
- **Admin:** Can record expenses from any cash drawer OR create non-cash expenses (e.g., bank transfer)

**RecordExpenseCommand Authorization:**
```csharp
public class RecordExpenseCommandHandler : IRequestHandler<RecordExpenseCommand, Guid>
{
    public async Task<Guid> Handle(RecordExpenseCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        // For cash expenses, validate open drawer ownership
        if (request.PaymentMethod == ExpensePaymentMethod.Cash)
        {
            var drawer = await _context.CashDrawers
                .FirstOrDefaultAsync(d => d.Id == request.CashDrawerId, cancellationToken);

            if (drawer == null)
                throw new NotFoundException(nameof(CashDrawer), request.CashDrawerId.Value);

            if (drawer.Status != CashDrawerStatus.Open)
                throw new BusinessRuleViolationException("Cash drawer is not open.");

            // Receptionist can only record from own drawer
            if (userRole == "Receptionist" && drawer.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only record expenses from your own cash drawer.");
            }

            // Update drawer balance
            drawer.CurrentBalance -= request.Amount;
        }

        // Create expense
        var expense = new Expense
        {
            BranchId = request.BranchId,
            Amount = request.Amount,
            Category = request.Category,
            Description = request.Description,
            PaymentMethod = request.PaymentMethod,
            CashDrawerId = request.CashDrawerId,
            RecordedBy = userId,
            ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow,
            Status = (userRole == "Doctor") ? ExpenseStatus.Pending : ExpenseStatus.Approved
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        // If Doctor creates expense, send approval notification
        if (userRole == "Doctor")
        {
            // TODO: Notify Admin for approval
        }

        return expense.Id;
    }
}
```

### 5.6.4 Refund Authorization

**Refund Approval Thresholds:**
- **Auto-Approved (Receptionist):** Refund amount ≤ $50.00
- **Requires Approval (Doctor/Admin):** Refund amount > $50.00

**ProcessRefundCommand Authorization:**
```csharp
public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, Guid>
{
    public async Task<Guid> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();
        var userRole = await _currentUserService.GetUserRoleAsync();

        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
            throw new NotFoundException(nameof(Payment), request.PaymentId);

        // Validate refund amount
        if (request.RefundAmount > payment.Amount)
        {
            throw new BusinessRuleViolationException("Refund amount cannot exceed original payment amount.");
        }

        // Check approval threshold
        var refundThreshold = 50.00m;
        var requiresApproval = request.RefundAmount > refundThreshold;

        // Receptionist cannot auto-approve large refunds
        if (userRole == "Receptionist" && requiresApproval)
        {
            // Create refund request with Pending status
            var refundRequest = new Refund
            {
                PaymentId = request.PaymentId,
                Amount = request.RefundAmount,
                Reason = request.Reason,
                RequestedBy = userId,
                RequestedAt = DateTime.UtcNow,
                Status = RefundStatus.Pending
            };

            _context.Refunds.Add(refundRequest);
            await _context.SaveChangesAsync(cancellationToken);

            // TODO: Notify Doctor/Admin for approval
            return refundRequest.Id;
        }

        // Auto-approve for small amounts or Admin/Doctor role
        var refund = new Refund
        {
            PaymentId = request.PaymentId,
            Amount = request.RefundAmount,
            Reason = request.Reason,
            RequestedBy = userId,
            RequestedAt = DateTime.UtcNow,
            ApprovedBy = userId,
            ApprovedAt = DateTime.UtcNow,
            Status = RefundStatus.Approved
        };

        _context.Refunds.Add(refund);

        // Update invoice paid amount
        payment.Invoice.PaidAmount -= request.RefundAmount;
        if (payment.Invoice.PaidAmount < payment.Invoice.TotalAmount)
            payment.Invoice.Status = InvoiceStatus.PartiallyPaid;
        else if (payment.Invoice.PaidAmount == 0)
            payment.Invoice.Status = InvoiceStatus.Pending;

        // If cash payment, update cash drawer balance
        if (payment.PaymentMethod == PaymentMethod.Cash && payment.CashDrawerId.HasValue)
        {
            var drawer = await _context.CashDrawers
                .FirstOrDefaultAsync(d => d.Id == payment.CashDrawerId.Value, cancellationToken);

            if (drawer != null && drawer.Status == CashDrawerStatus.Open)
            {
                drawer.CurrentBalance -= request.RefundAmount;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return refund.Id;
    }
}
```

**ApproveRefundCommand (Doctor/Admin only):**
```csharp
[Authorize(Policy = "CanApproveLargeRefund")]
public class ApproveRefundCommandHandler : IRequestHandler<ApproveRefundCommand, Unit>
{
    public async Task<Unit> Handle(ApproveRefundCommand request, CancellationToken cancellationToken)
    {
        var userId = await _currentUserService.GetUserIdAsync();

        var refund = await _context.Refunds
            .Include(r => r.Payment)
            .ThenInclude(p => p.Invoice)
            .FirstOrDefaultAsync(r => r.Id == request.RefundId, cancellationToken);

        if (refund == null)
            throw new NotFoundException(nameof(Refund), request.RefundId);

        if (refund.Status != RefundStatus.Pending)
        {
            throw new BusinessRuleViolationException("This refund has already been processed.");
        }

        refund.ApprovedBy = userId;
        refund.ApprovedAt = DateTime.UtcNow;
        refund.Status = RefundStatus.Approved;

        // Update invoice and cash drawer (same logic as auto-approved refunds)
        // ...

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

---

## Cross-References

- **Domain Model:** User roles in `05-domain-model.md`
- **Workflows:** Authorization requirements in `04-workflows.md`, `08-financial-workflows.md`
- **API Spec:** Endpoint authorization in `11-backend-api-specification.md`
- **Testing:** Security tests in `12-testing-strategy.md`

---

## Open Questions

See `16-open-questions.md`:

**Q1:** Implement RLS (Row-Level Security) in application or database? (Decision: Application for SQLite, database for PostgreSQL v1.1+)

**Q2:** SSO integration priority? (Decision: Defer to v1.1 based on customer demand)

**Q3:** MFA enforcement? (Decision: Optional in v1.0, mandatory for Admin role in v1.1)

**Q4:** Data encryption at rest? (Decision: File-level OS encryption for SQLite, column-level encryption for highly sensitive fields in v1.1)

---

*This security model is based on industry best practices and HIPAA Technical Safeguards. Implementation must include regular security reviews and updates as threats evolve.*
