# TASK-000-INFRA: Test Infrastructure Setup

**Task ID:** TASK-000-INFRA  
**Sprint:** Sprint 1, Week 1, Day 1  
**Type:** Infrastructure / Foundation  
**Priority:** P0 (BLOCKER - blocks all feature work)  
**Dependencies:** None (can start immediately)  
**Estimate:** 1 day (8 hours)  
**Status:** 🔵 Ready to Start

---

## Context

Before implementing any clinical features in Sprint 1 Week 2, establish complete test infrastructure, CI/CD pipeline, and code coverage enforcement. This foundation ensures quality gates are in place from the first line of feature code.

**Why This is P0:**
- Without tests, no way to verify feature correctness
- Without CI/CD, manual verification required for every change
- Without coverage enforcement, test gaps go unnoticed
- Test infrastructure complexity increases if retrofitted later

**Evidence:**
- Industry best practice: tests-first development
- 08-effort-estimation.md: Test writing estimated at 20% of implementation effort
- 12-testing-strategy.md: Requires ≥80% backend, ≥70% frontend coverage

---

## Objectives

1. **Backend Test Projects:** xUnit test infrastructure for Application and API layers
2. **Frontend Test Setup:** Vitest + React Testing Library + jest-axe for component/integration tests
3. **Sample Tests:** Demonstrate testing patterns (unit, integration, authorization)
4. **CI/CD Pipeline:** GitHub Actions workflow running tests on every commit
5. **Coverage Enforcement:** Fail builds if coverage drops below thresholds

---

## Scope

### In Scope
- ✅ Create test projects with proper dependencies
- ✅ Configure test runners (xUnit, Vitest)
- ✅ Write sample tests demonstrating patterns
- ✅ Setup GitHub Actions CI/CD workflow
- ✅ Configure code coverage reporting (Coverlet, Vitest coverage)
- ✅ Enforce coverage thresholds in CI

### Out of Scope
- ❌ Feature implementation (Sprint 1 Week 2)
- ❌ E2E test infrastructure (Playwright/Cypress, defer to TASK-004)
- ❌ Performance test infrastructure (defer to Sprint 4)
- ❌ Load testing (defer to post-v1.0)

---

## Technical Requirements

### Backend Test Infrastructure

#### 1. Test Projects to Create

**Clinic.Application.Tests** (Unit Tests)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.*" />
    <PackageReference Include="coverlet.collector" Version="6.0.*" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Clinic.Application\Clinic.Application.csproj" />
  </ItemGroup>
</Project>
```

**Clinic.Api.Tests** (Integration Tests)
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.*" />
    <PackageReference Include="coverlet.collector" Version="6.0.*" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Clinic.Api\Clinic.Api.csproj" />
  </ItemGroup>
</Project>
```

#### 2. Sample Tests to Write

**ApplicationUserTests.cs** (Unit Test Example)
```csharp
using Clinic.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Clinic.Application.Tests.Domain;

public class ApplicationUserTests
{
    [Fact]
    public void User_WithValidData_ShouldBeCreated()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = "Ahmed",
            LastName = "Hassan",
            Email = "ahmed.hassan@clinic.com",
            UserName = "ahmed.hassan"
        };

        // Assert
        user.FirstName.Should().Be("Ahmed");
        user.FullName.Should().Be("Ahmed Hassan");
        user.IsActive.Should().BeTrue(); // Default value
    }

    [Theory]
    [InlineData("", "Hassan")]
    [InlineData("Ahmed", "")]
    [InlineData(null, "Hassan")]
    [InlineData("Ahmed", null)]
    public void FullName_WithInvalidNames_ShouldHandleGracefully(string firstName, string lastName)
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().NotBeNullOrWhiteSpace();
    }
}
```

**AuthControllerTests.cs** (Integration Test Example)
```csharp
using System.Net;
using System.Net.Http.Json;
using Clinic.Application.Features.Auth.Commands.Login;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Clinic.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var command = new LoginCommand
        {
            Username = "admin",
            Password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        // Arrange
        var command = new LoginCommand
        {
            Username = "invalid",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Frontend Test Infrastructure

#### 1. Dependencies to Add

Update `frontend/package.json`:
```json
{
  "devDependencies": {
    "@testing-library/react": "^16.0.0",
    "@testing-library/jest-dom": "^6.5.0",
    "@testing-library/user-event": "^14.5.0",
    "@vitest/ui": "^2.1.0",
    "vitest": "^2.1.0",
    "jsdom": "^25.0.0",
    "jest-axe": "^9.0.0",
    "@vitest/coverage-v8": "^2.1.0"
  }
}
```

#### 2. Vitest Configuration

Create `frontend/vitest.config.ts`:
```typescript
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/tests/setup.ts',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'lcov'],
      exclude: [
        'node_modules/',
        'src/tests/',
        '**/*.d.ts',
        '**/*.config.*',
        '**/mockData',
        'src/main.tsx',
      ],
      thresholds: {
        lines: 70,
        functions: 70,
        branches: 70,
        statements: 70,
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
```

#### 3. Test Setup File

Create `frontend/src/tests/setup.ts`:
```typescript
import '@testing-library/jest-dom';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock window.matchMedia (required for some components)
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});
```

#### 4. Sample Tests to Write

**LoginForm.test.tsx** (Component Test Example)
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { LoginForm } from '@/features/auth/components/LoginForm';
import { BrowserRouter } from 'react-router-dom';

describe('LoginForm', () => {
  const mockOnSubmit = vi.fn();

  const renderLoginForm = () => {
    return render(
      <BrowserRouter>
        <LoginForm onSubmit={mockOnSubmit} />
      </BrowserRouter>
    );
  };

  it('should render login form fields', () => {
    renderLoginForm();

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('should show validation errors for empty fields', async () => {
    const user = userEvent.setup();
    renderLoginForm();

    const submitButton = screen.getByRole('button', { name: /sign in/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/username is required/i)).toBeInTheDocument();
      expect(screen.getByText(/password is required/i)).toBeInTheDocument();
    });
  });

  it('should call onSubmit with form data when valid', async () => {
    const user = userEvent.setup();
    renderLoginForm();

    await user.type(screen.getByLabelText(/username/i), 'testuser');
    await user.type(screen.getByLabelText(/password/i), 'Password@123');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith({
        username: 'testuser',
        password: 'Password@123',
      });
    });
  });

  it('should be accessible', async () => {
    const { container } = renderLoginForm();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

### CI/CD Pipeline

#### GitHub Actions Workflow

Create `.github/workflows/ci.yml`:
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    
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
      
      - name: Run tests with coverage
        run: |
          dotnet test \
            --no-build \
            --verbosity normal \
            /p:CollectCoverage=true \
            /p:CoverletOutputFormat=opencover \
            /p:Threshold=80 \
            /p:ThresholdType=line \
            /p:ThresholdStat=total
        working-directory: ./backend
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: ./backend/src/Clinic.Application.Tests/coverage.opencover.xml,./backend/src/Clinic.Api.Tests/coverage.opencover.xml
          flags: backend
          name: backend-coverage

  frontend-tests:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: './frontend/package-lock.json'
      
      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend
      
      - name: Run tests with coverage
        run: npm run test:coverage
        working-directory: ./frontend
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: ./frontend/coverage/lcov.info
          flags: frontend
          name: frontend-coverage
      
      - name: Lint
        run: npm run lint
        working-directory: ./frontend
      
      - name: Type check
        run: npm run type-check
        working-directory: ./frontend

  build-docker:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    if: github.ref == 'refs/heads/main'
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Build Docker images
        run: docker-compose build
```

---

## Acceptance Criteria

### Backend
- [ ] Clinic.Application.Tests project created with xUnit + FluentAssertions + Moq
- [ ] Clinic.Api.Tests project created with WebApplicationFactory
- [ ] At least 2 sample unit tests written (ApplicationUserTests)
- [ ] At least 2 sample integration tests written (AuthControllerTests)
- [ ] `dotnet test` runs successfully from backend/ directory
- [ ] Coverage report generated (Coverlet)
- [ ] Coverage threshold enforced (≥80% lines)

### Frontend
- [ ] Vitest + Testing Library configured
- [ ] `vitest.config.ts` created with coverage thresholds (≥70%)
- [ ] Test setup file created (`tests/setup.ts`)
- [ ] At least 1 sample component test written (LoginForm.test.tsx)
- [ ] jest-axe configured for accessibility testing
- [ ] `npm run test` passes
- [ ] `npm run test:coverage` generates report with ≥70% coverage

### CI/CD
- [ ] `.github/workflows/ci.yml` created
- [ ] Backend tests run on every push/PR
- [ ] Frontend tests run on every push/PR
- [ ] Build fails if coverage drops below thresholds
- [ ] Coverage reports uploaded to Codecov (optional but recommended)
- [ ] Docker build step runs after tests pass (main branch only)

### Documentation
- [ ] README.md updated with test commands
- [ ] Test patterns documented (unit vs integration vs e2e)
- [ ] Coverage requirements documented

---

## Test Commands

After implementation, developers should be able to run:

### Backend
```bash
# Run all tests
cd backend
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test src/Clinic.Application.Tests

# Run tests in watch mode
dotnet watch test --project src/Clinic.Application.Tests
```

### Frontend
```bash
# Run all tests
cd frontend
npm run test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage

# Run tests with UI
npm run test:ui
```

---

## Success Metrics

- ✅ CI pipeline green (all tests passing)
- ✅ Backend coverage ≥80% (enforced)
- ✅ Frontend coverage ≥70% (enforced)
- ✅ Sample tests demonstrate patterns for future features
- ✅ Documentation complete (README.md, test conventions)

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Coverage thresholds too strict | Medium | Medium | Start at 70% backend/60% frontend, increase gradually |
| Integration tests flaky | Low | High | Use in-memory database, reset state between tests |
| CI pipeline slow (>5 minutes) | Medium | Medium | Parallelize jobs, cache dependencies |
| Developers skip tests locally | Medium | High | Pre-commit hook to run tests, fast test feedback |

---

## Dependencies

**Blocks:**
- TASK-001-BE (CreatePatientCommand) - needs Application.Tests project
- TASK-001-FE (PatientForm) - needs frontend test setup
- All Sprint 1 Week 2 features - need test infrastructure

**Depends On:**
- None (can start immediately)

---

## Related Documents

- [12-testing-strategy.md](./12-testing-strategy.md) - Overall testing approach
- [08-effort-estimation.md](./08-effort-estimation.md) - Test effort estimates
- [15-traceability-matrix.md](./15-traceability-matrix.md) - Test cases mapped to requirements
- [14-implementation-roadmap.md](./14-implementation-roadmap.md) - Sprint 1 Week 1 tasks

---

## Implementation Notes

### Backend Test Project Structure
```
backend/
  src/
    Clinic.Application.Tests/
      Domain/
        ApplicationUserTests.cs
      Features/
        Auth/
          LoginCommandHandlerTests.cs
      Common/
        TestFixtures/
          TestUserFixture.cs
      Clinic.Application.Tests.csproj
    
    Clinic.Api.Tests/
      Controllers/
        AuthControllerTests.cs
      Integration/
        WebApplicationFactoryExtensions.cs
      Clinic.Api.Tests.csproj
```

### Frontend Test Project Structure
```
frontend/
  src/
    tests/
      setup.ts
      utils/
        test-utils.tsx
        mock-data/
    features/
      auth/
        components/
          LoginForm.test.tsx
```

---

## Status: Ready to Implement

This task is fully specified and ready to begin. All dependencies (NuGet packages, npm packages) are well-established with stable versions. Sample code provided for copy-paste implementation.

**Estimated Completion:** 1 day (8 hours)  
**Confidence:** HIGH (straightforward infrastructure setup)

