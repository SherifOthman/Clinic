# 12. Testing Strategy

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [10-frontend-specification.md](./10-frontend-specification.md), [11-backend-api-specification.md](./11-backend-api-specification.md), [14-implementation-roadmap.md](./14-implementation-roadmap.md)

---

## 1. Overview

### 1.1 Purpose
Define comprehensive testing approach across all layers (unit, integration, e2e, security, performance) to ensure ClinicCare meets quality, security, and reliability requirements.

### 1.2 Testing Philosophy
1. **Shift-Left:** Find bugs early in development lifecycle
2. **Automated First:** Automate repetitive tests, manual exploratory testing for edge cases
3. **Test Pyramid:** Many unit tests, fewer integration tests, minimal e2e tests
4. **Risk-Based:** Prioritize testing critical paths (patient safety, billing accuracy)
5. **Continuous:** Tests run on every commit (CI/CD pipeline)

### 1.3 Quality Gates
**Minimum Thresholds:**
- Code coverage: ≥80% for backend, ≥70% for frontend
- All critical path tests passing (blocker: cannot merge)
- Zero high/critical security vulnerabilities
- Performance: API response time <500ms (p95), UI interaction <100ms

---

## 2. Test Pyramid

```
        /\
       /e2e\       ~10 tests (Critical user journeys)
      /------\
     /  API  \     ~50 tests (Integration tests)
    /----------\
   / Unit Tests \  ~200 tests (Component/function tests)
  /--------------\
```

### 2.1 Distribution
| Test Level | Quantity | Execution Time | Run Frequency |
|------------|----------|----------------|---------------|
| Unit | ~200 | <30s | Every commit |
| Integration | ~50 | <2min | Every PR |
| E2E | ~10 | <5min | Before deployment |
| Manual | ~15 scenarios | ~1hr | Before release |

---

## 3. Unit Testing

### 3.1 Backend Unit Tests (xUnit, FluentAssertions, NSubstitute)

#### Test Structure
```csharp
// tests/Clinic.Application.Tests/Features/Patients/CreatePatientCommandTests.cs
public class CreatePatientCommandTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesPatient()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var handler = new CreatePatientCommandHandler(dbContext);
        var command = new CreatePatientCommand
        {
            FirstName = "Ahmed",
            LastName = "Hassan",
            DateOfBirth = new DateTime(1985, 3, 15),
            Gender = Gender.Male,
            Phone = "+201234567890"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FirstName.Should().Be("Ahmed");
        
        var patient = await dbContext.Patients.FindAsync(result.Value.Id);
        patient.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DuplicatePhone_ReturnsDuplicateError()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        await dbContext.Patients.AddAsync(new Patient
        {
            FirstName = "Existing",
            LastName = "Patient",
            Phone = "+201234567890"
        });
        await dbContext.SaveChangesAsync();

        var handler = new CreatePatientCommandHandler(dbContext);
        var command = new CreatePatientCommand
        {
            FirstName = "Ahmed",
            LastName = "Hassan",
            Phone = "+201234567890"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DUPLICATE_PHONE");
    }
}
```

#### Coverage Targets
**Application Layer (Commands/Queries):**
- All MediatR handlers: 100%
- Validation classes: 100%
- Business logic: ≥90%

**Domain Layer:**
- Entity methods: ≥85%
- Value objects: 100%

**Infrastructure Layer:**
- Repository pattern implementations: ≥80%
- External service wrappers: ≥75% (mock external dependencies)

#### Test Utilities
```csharp
// tests/Clinic.Application.Tests/Common/TestUtilities.cs
public static class TestUtilities
{
    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Patient CreateTestPatient(string? firstName = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = firstName ?? "Test",
            LastName = "Patient",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Gender = Gender.Male,
            Phone = $"+20{new Random().Next(1000000000, 1999999999)}"
        };
    }
}
```

### 3.2 Frontend Unit Tests (Vitest, React Testing Library)

#### Component Tests
```typescript
// features/patients/PatientForm.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PatientForm } from './PatientForm';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('PatientForm', () => {
  it('validates required fields on submit', async () => {
    render(<PatientForm onSubmit={vi.fn()} />, { wrapper: createWrapper() });
    
    const submitButton = screen.getByRole('button', { name: /save/i });
    await userEvent.click(submitButton);

    expect(screen.getByText(/first name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/last name is required/i)).toBeInTheDocument();
  });

  it('displays server errors', async () => {
    const mockSubmit = vi.fn().mockRejectedValue({
      errors: { phone: ['Phone number already exists'] },
    });

    render(<PatientForm onSubmit={mockSubmit} />, { wrapper: createWrapper() });
    
    await userEvent.type(screen.getByLabelText(/first name/i), 'Ahmed');
    await userEvent.type(screen.getByLabelText(/last name/i), 'Hassan');
    await userEvent.type(screen.getByLabelText(/phone/i), '+201234567890');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText(/phone number already exists/i)).toBeInTheDocument();
    });
  });
});
```

#### Hook Tests
```typescript
// features/patients/api.test.ts
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { usePatients, useCreatePatient } from './api';
import { rest } from 'msw';
import { setupServer } from 'msw/node';

const server = setupServer(
  rest.get('/api/v1/patients', (req, res, ctx) => {
    return res(ctx.json({
      data: [{ id: 'pat_123', firstName: 'Ahmed', lastName: 'Hassan' }],
      pagination: { page: 1, pageSize: 20, totalCount: 1 },
    }));
  }),
  rest.post('/api/v1/patients', (req, res, ctx) => {
    return res(ctx.status(201), ctx.json({
      id: 'pat_new',
      ...req.body,
    }));
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

const createWrapper = () => {
  const queryClient = new QueryClient();
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('usePatients', () => {
  it('fetches patients successfully', async () => {
    const { result } = renderHook(() => usePatients({ page: 1 }), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.data).toHaveLength(1);
    expect(result.current.data?.data[0].firstName).toBe('Ahmed');
  });
});
```

---

## 4. Integration Testing

### 4.1 Backend Integration Tests (WebApplicationFactory)

#### API Endpoint Tests
```csharp
// tests/Clinic.Api.Tests/Endpoints/PatientsEndpointsTests.cs
public class PatientsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;

    public PatientsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real database with in-memory
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();

        var scope = factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task POST_Patients_Returns201WithCreatedPatient()
    {
        // Arrange
        var request = new
        {
            firstName = "Ahmed",
            lastName = "Hassan",
            dateOfBirth = "1985-03-15",
            gender = "male",
            phone = "+201234567890"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var patient = await response.Content.ReadFromJsonAsync<PatientDto>();
        patient.FirstName.Should().Be("Ahmed");
        patient.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_Patients_RequiresAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Patients_WithValidToken_Returns200()
    {
        // Arrange
        var token = GenerateTestJwtToken("usr_123", "Doctor");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<PatientDto>>();
        result.Should().NotBeNull();
    }
}
```

#### Database Integration Tests
```csharp
// tests/Clinic.Infrastructure.Tests/Data/ApplicationDbContextTests.cs
public class ApplicationDbContextTests
{
    [Fact]
    public async Task SaveChanges_SetsAuditFields()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var patient = new Patient
        {
            FirstName = "Ahmed",
            LastName = "Hassan",
            DateOfBirth = new DateTime(1985, 3, 15),
            Gender = Gender.Male,
            Phone = "+201234567890"
        };

        // Act
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Assert
        patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        patient.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
```

### 4.2 Component Integration Tests (Frontend)
```typescript
// features/patients/PatientManagement.integration.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppProviders } from '@/test-utils';
import { PatientListPage } from './PatientListPage';
import { server } from '@/mocks/server';
import { rest } from 'msw';

describe('Patient Management Integration', () => {
  it('creates patient and displays in list', async () => {
    render(
      <AppProviders>
        <PatientListPage />
      </AppProviders>
    );

    // Open create form
    await userEvent.click(screen.getByRole('button', { name: /new patient/i }));

    // Fill form
    await userEvent.type(screen.getByLabelText(/first name/i), 'Ahmed');
    await userEvent.type(screen.getByLabelText(/last name/i), 'Hassan');
    await userEvent.type(screen.getByLabelText(/phone/i), '+201234567890');

    // Submit
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    // Verify patient appears in list
    await waitFor(() => {
      expect(screen.getByText('Ahmed Hassan')).toBeInTheDocument();
    });
  });
});
```

---

## 5. End-to-End Testing

### 5.1 E2E Test Suite (Playwright)

#### Critical User Journeys
```typescript
// tests/e2e/patient-appointment-billing.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Critical Path: Patient → Appointment → Billing', () => {
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto('/login');
    await page.fill('input[name="email"]', 'doctor@clinic.com');
    await page.fill('input[name="password"]', 'TestPassword123!');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/dashboard');
  });

  test('complete patient journey from registration to payment', async ({ page }) => {
    // Step 1: Create patient
    await page.click('a[href="/patients"]');
    await page.click('button:has-text("New Patient")');
    
    const patientName = `Test Patient ${Date.now()}`;
    await page.fill('input[name="firstName"]', patientName.split(' ')[0]);
    await page.fill('input[name="lastName"]', patientName.split(' ')[1] + ' ' + patientName.split(' ')[2]);
    await page.fill('input[name="dateOfBirth"]', '1990-01-15');
    await page.selectOption('select[name="gender"]', 'male');
    await page.fill('input[name="phone"]', `+201${Math.floor(Math.random() * 1000000000)}`);
    await page.click('button:has-text("Save")');

    await expect(page.locator(`text=${patientName}`)).toBeVisible();

    // Step 2: Schedule appointment
    await page.click('button:has-text("Schedule Appointment")');
    await page.selectOption('select[name="branchId"]', { index: 0 });
    
    // Select tomorrow's date
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    await page.fill('input[name="scheduledAt"]', tomorrow.toISOString().slice(0, 16));
    
    await page.fill('textarea[name="reason"]', 'Routine checkup');
    await page.click('button:has-text("Schedule")');

    await expect(page.locator('text=Appointment scheduled successfully')).toBeVisible();

    // Step 3: Start consultation (requires appointment date to be today - skip for now)
    // For demo, go directly to billing

    // Step 4: Create invoice
    await page.click('a[href="/billing"]');
    await page.click('button:has-text("New Invoice")');
    
    await page.fill('input[name="patient-search"]', patientName);
    await page.click(`text=${patientName}`);
    
    await page.fill('input[name="items[0].description"]', 'General Consultation');
    await page.fill('input[name="items[0].unitPrice"]', '150');
    await page.click('button:has-text("Create Invoice")');

    await expect(page.locator('text=Invoice created')).toBeVisible();

    // Step 5: Record payment
    await page.click('button:has-text("Record Payment")');
    await page.fill('input[name="amount"]', '150');
    await page.selectOption('select[name="method"]', 'cash');
    await page.click('button:has-text("Submit Payment")');

    await expect(page.locator('text=Payment recorded')).toBeVisible();
    await expect(page.locator('text=Paid')).toBeVisible();
  });
});
```

#### Queue Workflow Test
```typescript
// tests/e2e/queue-management.spec.ts
test('queue workflow: add patient, call next, complete', async ({ page }) => {
  await page.goto('/queue');
  
  // Add patient to queue
  await page.click('button:has-text("Add to Queue")');
  await page.fill('input[name="patient-search"]', 'Ahmed Hassan');
  await page.click('text=Ahmed Hassan');
  await page.click('button:has-text("Add")');

  // Verify patient in queue
  await expect(page.locator('text=Ahmed Hassan').first()).toBeVisible();

  // Call next patient
  await page.click('button:has-text("Call Next")');
  await expect(page.locator('[data-status="called"]')).toBeVisible();

  // Start consultation
  await page.click('button:has-text("Start Consultation")');
  await expect(page).toHaveURL(/\/consultation\/.+/);

  // Complete consultation
  await page.fill('textarea[name="notes"]', 'Patient doing well, no issues');
  await page.click('button:has-text("Complete")');

  // Verify queue updated
  await page.goto('/queue');
  await expect(page.locator('text=Ahmed Hassan')).not.toBeVisible();
});
```

### 5.2 Visual Regression Tests
```typescript
// tests/visual/components.spec.ts
import { test, expect } from '@playwright/test';

test('PatientCard visual snapshot', async ({ page }) => {
  await page.goto('/storybook/?path=/story/patients-patientcard--default');
  await expect(page).toHaveScreenshot('patient-card-default.png', {
    maxDiffPixels: 100,
  });
});

test('InvoiceForm visual snapshot', async ({ page }) => {
  await page.goto('/storybook/?path=/story/billing-invoiceform--empty');
  await expect(page).toHaveScreenshot('invoice-form-empty.png');
});
```

---

## 6. Security Testing

### 6.1 Authentication Tests
```csharp
// tests/Clinic.Api.Tests/Security/AuthenticationTests.cs
[Fact]
public async Task AccessProtectedEndpoint_WithoutToken_Returns401()
{
    var response = await _client.GetAsync("/api/v1/patients");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task AccessProtectedEndpoint_WithExpiredToken_Returns401()
{
    var expiredToken = GenerateExpiredJwtToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", expiredToken);

    var response = await _client.GetAsync("/api/v1/patients");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### 6.2 Authorization Tests
```csharp
// tests/Clinic.Api.Tests/Security/AuthorizationTests.cs
[Fact]
public async Task CreateBranch_AsReceptionist_Returns403()
{
    var token = GenerateTestJwtToken("usr_rec1", "Receptionist");
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    var request = new { name = "New Branch", address = "123 St" };
    var response = await _client.PostAsJsonAsync("/api/v1/branches", request);

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task AccessPatient_FromDifferentBranch_Returns403()
{
    // User has access to branch_main only
    var token = GenerateTestJwtToken("usr_doc1", "Doctor", branches: new[] { "branch_main" });
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Patient belongs to branch_downtown
    var patientId = await CreatePatientInBranch("branch_downtown");

    var response = await _client.GetAsync($"/api/v1/patients/{patientId}");

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### 6.3 Input Validation Tests
```csharp
[Theory]
[InlineData("'; DROP TABLE Patients; --", "SQL injection attempt")]
[InlineData("<script>alert('xss')</script>", "XSS attempt")]
[InlineData("../../../etc/passwd", "Path traversal attempt")]
public async Task CreatePatient_WithMaliciousInput_ReturnsValidationError(
    string maliciousInput, string testName)
{
    var request = new
    {
        firstName = maliciousInput,
        lastName = "Test",
        phone = "+201234567890"
    };

    var response = await _client.PostAsJsonAsync("/api/v1/patients", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

### 6.4 OWASP Top 10 Tests
```markdown
| OWASP Risk | Test Coverage | Status |
|------------|---------------|--------|
| A01: Broken Access Control | ✅ Authorization tests, branch-level access | Covered |
| A02: Cryptographic Failures | ✅ Passwords hashed (BCrypt), JWT signed | Covered |
| A03: Injection | ✅ Parameterized queries (EF Core), input validation | Covered |
| A04: Insecure Design | ⚠️ Threat model review (manual) | Partial |
| A05: Security Misconfiguration | ✅ HTTPS enforced, CORS configured | Covered |
| A06: Vulnerable Components | ✅ Dependabot alerts, `dotnet list package --vulnerable` | Automated |
| A07: ID & Authentication Failures | ✅ JWT expiry, rate limiting, password policy | Covered |
| A08: Software & Data Integrity | ✅ Audit logs, checksums (future P2) | Partial |
| A09: Logging & Monitoring Failures | ✅ Structured logging (Serilog), audit trail | Covered |
| A10: SSRF | ⚠️ No external HTTP calls in v1.0 | N/A |
```

---

## 7. Performance Testing

### 7.1 Load Testing (k6)
```javascript
// tests/load/api-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 10 },  // Ramp-up to 10 users
    { duration: '3m', target: 10 },  // Stay at 10 users
    { duration: '1m', target: 20 },  // Ramp-up to 20 users
    { duration: '3m', target: 20 },  // Stay at 20 users
    { duration: '1m', target: 0 },   // Ramp-down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests < 500ms
    http_req_failed: ['rate<0.01'],   // <1% failure rate
  },
};

const BASE_URL = 'http://localhost:5000/api/v1';
const TOKEN = __ENV.AUTH_TOKEN;

export default function () {
  const headers = { Authorization: `Bearer ${TOKEN}` };

  // List patients
  let res = http.get(`${BASE_URL}/patients`, { headers });
  check(res, {
    'patients list status 200': (r) => r.status === 200,
    'patients list response time < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(1);

  // Get patient details
  const patientId = 'pat_123'; // Known test patient
  res = http.get(`${BASE_URL}/patients/${patientId}`, { headers });
  check(res, {
    'patient details status 200': (r) => r.status === 200,
    'patient details response time < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(2);
}
```

### 7.2 Performance Benchmarks
| Endpoint | Method | Target (p95) | Acceptable (p95) | Max (p99) |
|----------|--------|--------------|------------------|-----------|
| GET /patients | Paginated | <300ms | <500ms | <1s |
| GET /patients/:id | Single | <200ms | <300ms | <500ms |
| POST /patients | Create | <400ms | <600ms | <1s |
| GET /queue/:branchId | Real-time | <200ms | <300ms | <500ms |
| POST /invoices | Create | <500ms | <800ms | <1.5s |
| POST /invoices/:id/payments | Payment | <600ms | <1s | <2s |

### 7.3 Database Query Performance
```csharp
// tests/Clinic.Infrastructure.Tests/Performance/QueryPerformanceTests.cs
[Fact]
public async Task GetPatients_WithPagination_CompletesWithin100ms()
{
    // Arrange: Seed 1000 patients
    var dbContext = CreateTestDbContext();
    var patients = Enumerable.Range(1, 1000)
        .Select(i => TestUtilities.CreateTestPatient($"Patient{i}"))
        .ToList();
    await dbContext.Patients.AddRangeAsync(patients);
    await dbContext.SaveChangesAsync();

    var stopwatch = Stopwatch.StartNew();

    // Act
    var query = new GetPatientsQuery { Page = 1, PageSize = 20 };
    var handler = new GetPatientsQueryHandler(dbContext);
    var result = await handler.Handle(query, CancellationToken.None);

    stopwatch.Stop();

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Data.Should().HaveCount(20);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
}
```

---

## 8. Accessibility Testing

### 8.1 Automated Accessibility Tests (jest-axe)
```typescript
// tests/a11y/forms.test.tsx
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { PatientForm } from '@/features/patients/PatientForm';

expect.extend(toHaveNoViolations);

describe('Accessibility', () => {
  it('PatientForm has no accessibility violations', async () => {
    const { container } = render(<PatientForm />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('QueueDashboard has no accessibility violations', async () => {
    const { container } = render(<QueueDashboard branchId="branch_main" />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

### 8.2 Keyboard Navigation Tests
```typescript
// tests/a11y/keyboard-navigation.spec.ts
import { test, expect } from '@playwright/test';

test('full keyboard navigation through patient form', async ({ page }) => {
  await page.goto('/patients/new');

  // Tab through all form fields
  await page.keyboard.press('Tab'); // First name field
  await expect(page.locator('input[name="firstName"]')).toBeFocused();

  await page.keyboard.press('Tab'); // Last name field
  await expect(page.locator('input[name="lastName"]')).toBeFocused();

  await page.keyboard.press('Tab'); // Date of birth field
  await expect(page.locator('input[name="dateOfBirth"]')).toBeFocused();

  // ... continue through all fields

  await page.keyboard.press('Tab'); // Save button
  await expect(page.locator('button:has-text("Save")')).toBeFocused();

  // Submit with Enter
  await page.keyboard.press('Enter');
  await expect(page.locator('text=Validation error')).toBeVisible();
});
```

### 8.3 Screen Reader Testing (Manual)
```markdown
## Manual Screen Reader Test Checklist
- [ ] Login form announced correctly
- [ ] Patient list items have meaningful labels
- [ ] Form validation errors read aloud
- [ ] Dynamic queue updates announced (live region)
- [ ] Modal dialogs trap focus correctly
- [ ] Landmarks (header, nav, main, aside) identified
- [ ] Tables have proper headers
- [ ] Error messages associated with form fields
```

---

## 9. Test Data Management

### 9.1 Test Data Seeding
```csharp
// tests/Clinic.Application.Tests/Common/TestDataSeeder.cs
public static class TestDataSeeder
{
    public static async Task SeedTestData(ApplicationDbContext context)
    {
        // Branches
        var mainBranch = new Branch
        {
            Id = "branch_test_main",
            Name = "Test Main Clinic",
            Address = "123 Test St",
            Phone = "+201111111111",
            IsActive = true
        };
        context.Branches.Add(mainBranch);

        // Users
        var doctorUser = new ApplicationUser
        {
            Id = "usr_test_doctor",
            Email = "doctor@test.com",
            FullName = "Dr. Test Doctor"
        };
        context.Users.Add(doctorUser);

        // Patients
        var patients = Enumerable.Range(1, 50)
            .Select(i => new Patient
            {
                Id = $"pat_test_{i}",
                FirstName = $"Patient{i}",
                LastName = "Test",
                DateOfBirth = DateTime.Now.AddYears(-30 - i),
                Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                Phone = $"+2011{i:D8}"
            })
            .ToList();
        context.Patients.AddRange(patients);

        await context.SaveChangesAsync();
    }
}
```

### 9.2 Test Data Cleanup
```csharp
// tests/Clinic.Api.Tests/Common/DatabaseFixture.cs
public class DatabaseFixture : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;

    public async Task InitializeAsync()
    {
        await TestDataSeeder.SeedTestData(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
```

---

## 10. Continuous Integration

### 10.1 CI Pipeline (GitHub Actions)
```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  backend-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: ./backend
      
      - name: Build
        run: dotnet build --no-restore
        working-directory: ./backend
      
      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
        working-directory: ./backend
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: ./backend/**/coverage.cobertura.xml

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend
      
      - name: Run linter
        run: npm run lint
        working-directory: ./frontend
      
      - name: Run unit tests
        run: npm run test:coverage
        working-directory: ./frontend
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: ./frontend/coverage/coverage-final.json

  e2e-tests:
    needs: [backend-tests, frontend-tests]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Start backend
        run: |
          cd backend/src/Clinic.Api
          dotnet run &
          sleep 10
      
      - name: Start frontend
        run: |
          cd frontend
          npm ci
          npm run build
          npm run preview &
          sleep 5
      
      - name: Run Playwright tests
        run: |
          cd frontend
          npx playwright install --with-deps
          npx playwright test
      
      - name: Upload Playwright report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: frontend/playwright-report/
```

### 10.2 Quality Gates
```yaml
# Enforce quality gates in PR checks
# PR cannot merge unless:
- All unit tests passing (backend + frontend)
- Code coverage ≥80% (backend), ≥70% (frontend)
- No linting errors
- No high/critical security vulnerabilities (Dependabot)
- E2E critical path tests passing
- Performance benchmarks not regressed by >10%
```

---

## 11. Test Coverage Reporting

### 11.1 Backend Coverage (Coverlet)
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**Report Generation:**
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

### 11.2 Frontend Coverage (Vitest)
```json
// vitest.config.ts
export default defineConfig({
  test: {
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'lcov'],
      exclude: [
        'node_modules/',
        'tests/',
        '**/*.config.ts',
        '**/types/**',
      ],
      thresholds: {
        statements: 70,
        branches: 70,
        functions: 70,
        lines: 70,
      },
    },
  },
});
```

---

## 12. Manual Testing

### 12.1 Exploratory Testing Charters

**Charter 1: Patient Management Edge Cases**
- Duration: 30 minutes
- Scope: Create, edit, search patients
- Focus: Edge cases (special characters, long names, missing fields)
- Risk Areas: Duplicate detection, search accuracy

**Charter 2: Queue Management Under Load**
- Duration: 45 minutes
- Scope: Queue operations (add, call next, skip, no-show)
- Focus: Concurrent operations, race conditions
- Risk Areas: Token ordering, status transitions

**Charter 3: Billing & Payment Workflows**
- Duration: 60 minutes
- Scope: Invoice creation, payments, refunds
- Focus: Financial accuracy, edge cases (overpayment, zero amount)
- Risk Areas: Cash drawer reconciliation, balance calculations

### 12.2 User Acceptance Testing (UAT)

**UAT Test Scenarios:**
1. **Reception Workflow:** Register 10 walk-in patients, verify all appear in queue
2. **Doctor Workflow:** Complete 5 consultations, verify notes saved
3. **Billing Workflow:** Create 10 invoices, record payments (mix of cash/card)
4. **Cash Drawer Workflow:** Open drawer, record 20 payments + 3 expenses, close with reconciliation
5. **Multi-Branch:** Switch branches, verify data isolation

**UAT Success Criteria:**
- ✅ Zero data loss
- ✅ Zero incorrect calculations
- ✅ <2 usability issues per workflow
- ✅ Average task completion time within 20% of expected

---

## 13. Defect Management

### 13.1 Bug Severity Levels

| Severity | Description | SLA | Example |
|----------|-------------|-----|---------|
| **Critical** | System crash, data loss, security breach | Fix within 24h | Database connection failure |
| **High** | Core feature broken, workaround exists | Fix within 3 days | Invoice calculation incorrect |
| **Medium** | Minor feature broken, non-blocking | Fix within 1 week | Search autocomplete not working |
| **Low** | Cosmetic issue, usability improvement | Fix within 2 weeks | Button alignment off by 2px |

### 13.2 Bug Report Template
```markdown
**Title:** [Component] Brief description

**Severity:** Critical | High | Medium | Low

**Environment:**
- Version: 1.0.0
- Browser: Chrome 130.0
- OS: Windows 11

**Steps to Reproduce:**
1. Navigate to /patients
2. Click "New Patient"
3. Fill form with test data
4. Click "Save"

**Expected Result:**
Patient created and appears in list

**Actual Result:**
Error toast: "Phone number already exists" (but phone is unique)

**Screenshots/Logs:**
[Attach screenshot]

**Impact:**
Cannot create new patients, blocking reception workflow
```

---

## 14. Test Metrics & Reporting

### 14.1 Key Metrics
- **Test Pass Rate:** (Passed / Total) × 100%
- **Code Coverage:** % of lines executed during tests
- **Defect Density:** Defects / KLOC (thousands of lines of code)
- **Mean Time To Repair (MTTR):** Average time to fix bugs
- **Test Execution Time:** Total time for test suite run

### 14.2 Dashboard (Example)
```
╔══════════════════════════════════════════════════════════╗
║               ClinicCare Test Dashboard                  ║
╠══════════════════════════════════════════════════════════╣
║ Last Run: 2026-10-21 14:30 UTC                          ║
║                                                          ║
║ Backend Tests                                            ║
║   Unit:        197 passed, 0 failed  ✅ 100%            ║
║   Integration:  48 passed, 0 failed  ✅ 100%            ║
║   Coverage:     84.2% (target: ≥80%) ✅                 ║
║                                                          ║
║ Frontend Tests                                           ║
║   Unit:        143 passed, 2 failed  ⚠️ 98.6%           ║
║   Integration:  12 passed, 0 failed  ✅ 100%            ║
║   Coverage:     72.5% (target: ≥70%) ✅                 ║
║                                                          ║
║ E2E Tests                                                ║
║   Critical:      8 passed, 0 failed  ✅ 100%            ║
║   Extended:      5 passed, 1 failed  ⚠️ 83.3%           ║
║                                                          ║
║ Security                                                 ║
║   Vulnerabilities: 0 high, 1 medium  ⚠️                 ║
║   OWASP Scans:     Passed            ✅                 ║
║                                                          ║
║ Performance                                              ║
║   API p95:         320ms (target: <500ms) ✅            ║
║   UI interaction:  85ms (target: <100ms)  ✅            ║
╚══════════════════════════════════════════════════════════╝
```

---

## 15. Open Questions

### Q1: Mock vs Real Database for Integration Tests
**Question:** Should integration tests use in-memory database or real SQLite?  
**Options:**
- A) In-memory (fast, isolated, but may behave differently from production)
- B) Real SQLite file (accurate, slower)
- C) Docker PostgreSQL (production-like, requires Docker)

**Recommendation:** Option A (in-memory) for unit tests, Option B (SQLite file) for integration tests  
**Decision:** [PENDING]

### Q2: Visual Regression Test Frequency
**Question:** How often should visual regression tests run?  
**Context:** Playwright screenshot tests can be flaky, slow  
**Recommendation:** Run on demand (manual trigger) or nightly, not on every commit  
**Decision:** [PENDING]

### Q3: Load Testing Targets
**Question:** What concurrent user count should load tests target?  
**Context:** Single doctor, small clinic - likely <5 concurrent users  
**Recommendation:** Test up to 20 concurrent users (safety margin)  
**Decision:** [PENDING]

---

## 16. Related Documents

- [10-frontend-specification.md](./10-frontend-specification.md) - Frontend components to test
- [11-backend-api-specification.md](./11-backend-api-specification.md) - API endpoints to test
- [14-implementation-roadmap.md](./14-implementation-roadmap.md) - Testing milestones

---

## 17. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial testing strategy |

---

**Document Confidence:** HIGH  
**Evidence Base:** Industry best practices (Test Pyramid, OWASP), ASP.NET Core testing patterns, Playwright documentation  
**Assumptions:** CI/CD pipeline exists, developers write tests alongside features  
**Next Review:** After first sprint, adjust coverage targets based on actual development velocity
