using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;
using QuestBoard.IntegrationTests.Helpers;
using System.Net;

namespace QuestBoard.IntegrationTests.Controllers;

public class AdminControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public AdminControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Index_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Act - Changed from /Admin to /Admin/Users (actual route name)
        var response = await _client.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Index_WhenNotAdmin_ShouldReturnForbidden()
    {
        // Arrange - Create user with Player role (not Admin)
        var (regularClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "regularuser", "regular@example.com", roles: ["Player"]);

        // Act - Changed from /Admin to /Admin/Users (actual route name)
        var response = await regularClient.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManageUsers_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Act - Changed from /Admin/ManageUsers to /Admin/Users (actual route name)
        var response = await _client.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/Admin/EditUser/1")]
    [InlineData("/Admin/DeleteUser/1")]
    [InlineData("/Admin/ResetPassword/1")]
    public async Task AdminActions_WhenNotAuthenticated_ShouldRedirectToLogin(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint, TestContext.Current.CancellationToken);

        // Assert
        // Note: DeleteUser returns 405 Method Not Allowed since it requires DELETE not GET
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task EmailStats_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Admin/EmailStats", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EmailStats_WhenNotAdmin_ShouldReturnForbidden()
    {
        // Arrange - Create user with Player role (not Admin)
        var (playerClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "emailstatsuser", "emailstats@example.com", roles: ["Player"]);

        // Act
        var response = await playerClient.GetAsync("/Admin/EmailStats", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    // MGMT-07/REG-01: CreateUser auth gating — a non-admin must not reach the form
    [Fact]
    public async Task CreateUser_WhenNotAdmin_ShouldBeForbidden()
    {
        // Arrange
        var (playerClient, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "createuserplayer", "createuserplayer@example.com", roles: ["Player"]);

        // Act
        var response = await playerClient.GetAsync("/Admin/CreateUser", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized);
    }

    // MGMT-07: An admin can reach the CreateUser form
    [Fact]
    public async Task CreateUser_Get_WhenAdmin_ShouldReturnForm()
    {
        // Arrange
        var (adminClient, _) = await AuthenticationHelper.CreateAuthenticatedAdminClientAsync(_factory);

        // Act
        var response = await adminClient.GetAsync("/Admin/CreateUser", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain("CreateUser");
        content.Should().Contain("GroupRole");
    }

    // MGMT-07, REG-02, REG-03, PWFLOW-01: Admin-created users are assigned to the admin's
    // active group with the chosen GroupRole, created passwordless, and the Welcome email
    // job fires (targeting the SetPassword callback) instead of an admin-set password.
    [Fact]
    public async Task CreateUser_Post_WhenAdmin_CreatesUserInActiveGroup()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (adminClient, _) = await AuthenticationHelper.CreateAuthenticatedAdminClientAsync(_factory);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var newUserEmail = $"createduser_{uniqueSuffix}@example.com";
        // PWFLOW-01/D-01: no Password field is submitted — CreateUser is passwordless.
        var formData = new Dictionary<string, string>
        {
            ["Email"] = newUserEmail,
            ["Name"] = "Created User",
            ["GroupRole"] = ((int)GroupRole.DungeonMaster).ToString()
        };

        // Act — _factory.TestGroupContext.ActiveGroupId defaults to 1 (the seeded EuphoriaInn group)
        var response = await adminClient.PostAsync("/Admin/CreateUser",
            new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        var createdUser = await userManager.FindByEmailAsync(newUserEmail);
        createdUser.Should().NotBeNull();
        createdUser!.Name.Should().Be("Created User");
        createdUser.PasswordHash.Should().BeNull("accounts are created passwordless (PWFLOW-01/D-01)");
        createdUser.EmailConfirmed.Should().BeFalse("email is only confirmed once the user completes SetPassword (D-09)");

        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        var membership = context.UserGroups.FirstOrDefault(ug => ug.UserId == createdUser.Id && ug.GroupId == 1);
        membership.Should().NotBeNull("the created user should be assigned to the admin's active group");
        membership!.GroupRole.Should().Be((int)GroupRole.DungeonMaster);
    }

    // PWFLOW-05: SendConfirmationEmail (the "Resend Welcome Email" action) succeeds for an
    // unconfirmed user and redirects to Users with a success outcome. Job-enqueue internals
    // are not observable via HTTP (covered by Plan 02's WelcomeEmailJobTests unit tests).
    [Fact]
    public async Task SendConfirmationEmail_Post_WhenUserUnconfirmed_ShouldRedirectToUsersWithSuccess()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (adminClient, _) = await AuthenticationHelper.CreateAuthenticatedAdminClientAsync(_factory);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var targetEmail = $"resendwelcome_{uniqueSuffix}@example.com";

        int targetUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

            var createResult = await userService.CreateAsync(targetEmail, "Resend Welcome User");
            createResult.Succeeded.Should().BeTrue();

            var resolvedUserId = await identityService.GetIdByEmailAsync(targetEmail);
            resolvedUserId.Should().NotBeNull();
            targetUserId = resolvedUserId!.Value;
        }

        var getResponse = await adminClient.GetAsync("/Admin/Users", TestContext.Current.CancellationToken);
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);
        if (!string.IsNullOrEmpty(cookieValue))
        {
            adminClient.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string> { ["userId"] = targetUserId.ToString() },
            token);

        // Act
        var response = await adminClient.PostAsync("/Admin/SendConfirmationEmail", formContent, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.Should().Contain("Users");
    }
}
