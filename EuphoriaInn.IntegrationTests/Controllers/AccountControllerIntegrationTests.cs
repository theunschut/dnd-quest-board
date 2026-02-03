using EuphoriaInn.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class AccountControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public AccountControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Login_Get_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Login");
    }

    [Fact]
    public async Task Register_Get_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Account/Register");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Register");
    }

    [Fact]
    public async Task Register_Post_WithValidData_ShouldCreateUser()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // First, GET the register page to obtain the anti-forgery token
        var getResponse = await _client.GetAsync("/Account/Register");
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);

        // Set the anti-forgery cookie
        if (!string.IsNullOrEmpty(cookieValue))
        {
            _client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string>
            {
                ["Name"] = "New User",
                ["Email"] = "newuser@example.com",
                ["Password"] = "NewUser123!",
                ["ConfirmPassword"] = "NewUser123!"
            },
            token);

        // Act
        var response = await _client.PostAsync("/Account/Register", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Verify user was created
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        var user = await userManager.FindByEmailAsync("newuser@example.com");
        user.Should().NotBeNull();
        user!.Email.Should().Be("newuser@example.com");
        user!.Name.Should().Be("New User");
    }

    [Fact]
    public async Task Register_Post_WithMismatchedPasswords_ShouldReturnError()
    {
        // Arrange
        // First, GET the register page to obtain the anti-forgery token
        var getResponse = await _client.GetAsync("/Account/Register");
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);

        // Set the anti-forgery cookie
        if (!string.IsNullOrEmpty(cookieValue))
        {
            _client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string>
            {
                ["Name"] = "Test User 2",
                ["Email"] = "test2@example.com",
                ["Password"] = "Test123!",
                ["ConfirmPassword"] = "DifferentPassword123!"
            },
            token);

        // Act
        var response = await _client.PostAsync("/Account/Register", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password", "");
    }

    [Fact]
    public async Task Profile_Get_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Account/Profile");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_Get_WhenAuthenticated_ShouldReturnUserProfile()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var password = "ProfilePass123!";
        var (client, user) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "profileuser", "profile@example.com", password, "Profile User");

        // Act
        var response = await client.GetAsync("/Account/Profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(user.Email);
    }

    [Fact]
    public async Task Logout_Post_ShouldRedirectToHome()
    {
        // Arrange
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // First, GET a page to obtain the anti-forgery token (use the home page or profile)
        var getResponse = await client.GetAsync("/Account/Profile");
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);

        // Set the anti-forgery cookie
        if (!string.IsNullOrEmpty(cookieValue))
        {
            client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string>(),
            token);

        // Act
        var response = await client.PostAsync("/Account/Logout", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.OK);
    }
}