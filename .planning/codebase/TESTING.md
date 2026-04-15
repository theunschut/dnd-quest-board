# Testing Patterns

**Analysis Date:** 2026-04-15

## Test Framework

**Runner:**
- xUnit 2.5.3
- Config: `xunit.runner.json` (present in both test projects — parallelization disabled)

**Assertion Library:**
- FluentAssertions 8.8.0 — used in all test files

**Mocking (Unit Tests only):**
- NSubstitute 5.3.0 — available in `EuphoriaInn.UnitTests`

**Run Commands:**
```bash
# Run all tests (from repo root)
dotnet test

# Run specific project
dotnet test EuphoriaInn.UnitTests
dotnet test EuphoriaInn.IntegrationTests

# Run with coverage (coverlet.collector is included)
dotnet test --collect:"XPlat Code Coverage"
```

## Test Projects

| Project | Path | Type |
|---|---|---|
| `EuphoriaInn.UnitTests` | `EuphoriaInn.UnitTests/` | Unit tests — domain models and view models |
| `EuphoriaInn.IntegrationTests` | `EuphoriaInn.IntegrationTests/` | Integration tests — full HTTP request/response via `WebApplicationFactory` |

Both projects target `net8.0`, have `ImplicitUsings` enabled, and `Nullable` enabled.

## Test File Organization

**Unit Tests:**
```
EuphoriaInn.UnitTests/
├── Helpers/
│   └── DateHelperTests.cs
├── Models/
│   └── QuestModelTests.cs
└── ViewModels/
    └── CreateQuestViewModelTests.cs   (class is named QuestViewModelTests inside)
```

**Integration Tests:**
```
EuphoriaInn.IntegrationTests/
├── Controllers/
│   ├── AccountControllerIntegrationTests.cs
│   ├── AdminControllerIntegrationTests.cs
│   ├── CalendarControllerIntegrationTests.cs
│   ├── GuildMembersControllerIntegrationTests.cs
│   ├── HomeControllerIntegrationTests.cs
│   ├── QuestControllerIntegrationTests_Comprehensive.cs
│   ├── QuestLogControllerIntegrationTests.cs
│   └── ShopControllerIntegrationTests.cs
├── Helpers/
│   ├── AntiForgeryHelper.cs
│   ├── AuthenticationHelper.cs
│   ├── CookieAuthenticationHelper.cs
│   ├── TestAuthSelectorMiddleware.cs
│   ├── TestDatabase.cs
│   └── TestDataHelper.cs
├── GlobalUsings.cs
└── WebApplicationFactoryBase.cs
```

**Naming:**
- Unit test files: `{Subject}Tests.cs`
- Integration test files: `{Controller}IntegrationTests.cs` or `{Controller}IntegrationTests_Comprehensive.cs`
- Test methods: `{Method}_{Scenario}_{ExpectedOutcome}` — e.g., `Create_Get_WhenNotAuthenticated_ShouldRedirectToLogin`

## Test Structure

**Unit test suite organization:**
```csharp
public class QuestModelTests
{
    [Fact]
    public void Quest_ShouldInitializeWithDefaultValues()
    {
        // Act
        var quest = new Quest();

        // Assert
        quest.Id.Should().Be(0);
        quest.IsFinalized.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Quest_ShouldAcceptValidChallengeRatings(int rating)
    {
        // Arrange
        var quest = new Quest();

        // Act
        quest.ChallengeRating = rating;

        // Assert
        quest.ChallengeRating.Should().Be(rating);
    }
}
```

**Integration test suite organization:**
```csharp
public class QuestControllerIntegrationTests_Comprehensive : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public QuestControllerIntegrationTests_Comprehensive(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Details_WithValidQuestId_ShouldReturnQuestDetails()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var dm = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "questdm", "questdm@example.com");
        var quest = await TestDataHelper.CreateTestQuestAsync(_factory.Services, dm.Id, "Adventure Quest", "Epic adventure");

        // Act
        var response = await _client.GetAsync($"/Quest/Details/{quest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Adventure Quest");
    }
}
```

**Patterns:**
- Arrange/Act/Assert structure with comment labels in all test methods
- `IClassFixture<WebApplicationFactoryBase>` for all integration test classes
- Call `TestDataHelper.ClearDatabaseAsync(_factory.Services)` at the start of tests that create data
- Use `_factory.CreateNonRedirectingClient()` for testing authorization redirects (most controller tests)
- Use `_factory.CreateClient()` when redirect following is acceptable

## Mocking

**Framework:** NSubstitute 5.3.0 (available in unit test project, not used in integration tests)

**Integration test approach:** No mocking — tests run against a real SQLite in-memory database (not EF Core's in-memory provider), and the full ASP.NET Core pipeline via `WebApplicationFactory<Program>`.

**Authentication mocking in integration tests:**
Custom `TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>` decodes a custom `Authorization: Test {userId}:{userName}:{email}:{roles}` header and builds a `ClaimsPrincipal`. The handler is registered and made default in `WebApplicationFactoryBase.ConfigureWebHost`:
```csharp
services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
```
Source: `EuphoriaInn.IntegrationTests/Helpers/AuthenticationHelper.cs`

**Anti-forgery mocking:** `TestAntiforgeryDecorator` wraps the real `IAntiforgery` but always returns `true` from `IsRequestValidAsync` and completes `ValidateRequestAsync` without error. This enables POST tests without requiring real CSRF tokens. Source: `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs`.

**What NOT to mock:** The database — integration tests use a real SQLite connection that persists for the lifetime of the `WebApplicationFactoryBase` instance.

## Fixtures and Factories

**WebApplicationFactoryBase:**
- Source: `EuphoriaInn.IntegrationTests/WebApplicationFactoryBase.cs`
- Replaces all EF Core SQL Server services with SQLite in-memory using a shared `TestDatabase` connection
- Creates a unique database name per factory instance: `QuestBoardTest_{Guid}`
- Exposes `ResetDatabase()` and `CreateNonRedirectingClient()`

**TestDatabase:**
- Source: `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs`
- Wraps a `SqliteConnection("DataSource=:memory:")` that stays open for the factory's lifetime
- Exposes `Connection` for use in `WebApplicationFactoryBase` to ensure app and test share the same connection
- `Reset()` calls `EnsureDeleted` + `EnsureCreated`

**TestDataHelper (static factory methods):**
Source: `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs`

```csharp
// Creates an entity directly via EF context
await TestDataHelper.CreateTestQuestAsync(services, dm.Id, "Title", "Description");
await TestDataHelper.CreatePlayerSignupAsync(services, quest.Id, player.Id);
await TestDataHelper.CreateProposedDateAsync(services, quest.Id, DateTime.Today.AddDays(7));
await TestDataHelper.CreateShopItemAsync(services, dmId);
await TestDataHelper.CreateTestCharacterAsync(services, ownerId);

// Database lifecycle
await TestDataHelper.ClearDatabaseAsync(services);  // EnsureDeleted + EnsureCreated + seed roles
await TestDataHelper.SeedRolesAsync(services);       // Creates Admin, DungeonMaster, Player roles
```

**AuthenticationHelper (static factory methods):**
Source: `EuphoriaInn.IntegrationTests/Helpers/AuthenticationHelper.cs`

```csharp
// Create user only
var user = await AuthenticationHelper.CreateTestUserAsync(services, "username", "email@example.com");

// Create authenticated HTTP client + user
var (client, user) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(factory);
var (client, dm) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(factory);
var (client, admin) = await AuthenticationHelper.CreateAuthenticatedAdminClientAsync(factory);
```

All `CreateTestUserAsync` calls append a `Guid`-based suffix to username and email to prevent conflicts between parallel-unsafe tests.

**Location:**
- Shared helpers: `EuphoriaInn.IntegrationTests/Helpers/`
- No separate fixtures directory — test data is seeded inline via `TestDataHelper`

## Coverage

**Requirements:** None enforced — no coverage thresholds configured.

**Collector:** `coverlet.collector` version 6.0.0 is present in both test projects. Coverage can be collected but is not checked in CI.

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Output in TestResults/{guid}/coverage.cobertura.xml
```

## Test Types

**Unit Tests (`EuphoriaInn.UnitTests`):**
- Scope: Domain model property initialization, default value assertions, view model property assertions
- No service logic tested — current unit tests only exercise plain model/view model construction and property assignment
- No mocks used in practice despite NSubstitute being available
- Files: `Models/QuestModelTests.cs`, `ViewModels/CreateQuestViewModelTests.cs`, `Helpers/DateHelperTests.cs`

**Integration Tests (`EuphoriaInn.IntegrationTests`):**
- Scope: Full HTTP request-response cycle testing all controllers
- Verifies HTTP status codes, response HTML content, redirect behavior, and database side effects
- Tests authentication/authorization boundary conditions (unauthenticated, wrong role, non-owner)
- Directly queries the `QuestBoardContext` after actions to verify database state changes

**E2E Tests:** Not present.

## Common Patterns

**Async Testing:**
```csharp
[Fact]
public async Task Create_Get_WhenAuthenticatedAsDM_ShouldReturnCreateForm()
{
    // Arrange
    await TestDataHelper.ClearDatabaseAsync(_factory.Services);
    var (client, _) = await AuthenticationHelper.CreateAuthenticatedDMClientAsync(_factory);

    // Act
    var response = await client.GetAsync("/Quest/Create");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**Status code verification with multiple acceptable codes (for redirects):**
```csharp
response.StatusCode.Should().BeOneOf(
    HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
```

**Database state verification after action:**
```csharp
using var scope = _factory.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
var signup = await context.PlayerSignups
    .FirstOrDefaultAsync(s => s.QuestId == quest.Id && s.PlayerId == player.Id);
signup.Should().NotBeNull();
```

**POST form submission with anti-forgery:**
```csharp
var getResponse = await _client.GetAsync("/Account/Register");
var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);
var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
    new Dictionary<string, string> { ["Name"] = "Test User", ... },
    token);
var response = await _client.PostAsync("/Account/Register", formContent);
```

**Parallelization:** Both test projects disable parallelization (`parallelizeAssembly: false`, `parallelizeTestCollections: false`) to prevent database conflicts between tests sharing the same `WebApplicationFactoryBase` instance via `IClassFixture`.

---

*Testing analysis: 2026-04-15*
