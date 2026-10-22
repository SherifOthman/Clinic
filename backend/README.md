# ClinicCare Backend

## Running Tests

### Run All Tests
```bash
cd backend
dotnet test Clinic.slnx
```

### Run Specific Test Project
```bash
# Unit tests
dotnet test src/Clinic.Application.Tests

# Integration tests
dotnet test src/Clinic.Api.Tests
```

### Run Tests with Coverage
```bash
dotnet test Clinic.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### Watch Mode (Auto-run on file changes)
```bash
dotnet watch test --project src/Clinic.Application.Tests
```

## Test Structure

- **Clinic.Application.Tests**: Unit tests for domain entities, commands, queries
- **Clinic.Api.Tests**: Integration tests for API endpoints using WebApplicationFactory

## Coverage Targets

- **Current Baseline**: Established with initial 9 tests (TASK-000-INFRA)
- **Sprint 1 Target**: 30% (foundation tests)
- **Sprint 2 Target**: 50%
- **Sprint 4 Target**: 80% (enforced in CI/CD)

Coverage will be gradually increased as features are implemented.

## Building the Solution

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build Clinic.slnx

# Build specific configuration
dotnet build Clinic.slnx --configuration Release
```

## Project Structure

```
backend/
├── Clinic.slnx                   # Solution file
├── src/
│   ├── Clinic.Api/              # ASP.NET Core Web API
│   ├── Clinic.Application/      # Application logic (MediatR handlers)
│   ├── Clinic.Domain/           # Domain entities
│   ├── Clinic.Infrastructure/   # EF Core, Identity, JWT
│   ├── Clinic.Application.Tests/ # Unit tests
│   └── Clinic.Api.Tests/        # Integration tests
```

## Test Commands Reference

| Command | Description |
|---------|-------------|
| `dotnet test` | Run all tests in current directory |
| `dotnet test Clinic.slnx` | Run all tests in solution |
| `dotnet test --verbosity normal` | Run tests with detailed output |
| `dotnet test --filter "FullyQualifiedName~ApplicationUser"` | Run specific tests |
| `dotnet watch test` | Run tests in watch mode |

## Troubleshooting

### Tests fail with "Database is locked"
This happens when SQLite database file is in use. Integration tests use in-memory SQLite to avoid this issue.

### Tests fail with "Cannot access a disposed object"
Ensure `TestWebApplicationFactory` is properly implemented with `IAsyncLifetime` for test data seeding.

### Coverage reports not generated
Run with explicit collection: `dotnet test --collect:"XPlat Code Coverage"`
