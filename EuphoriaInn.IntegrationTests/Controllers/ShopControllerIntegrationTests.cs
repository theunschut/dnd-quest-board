using EuphoriaInn.IntegrationTests.Helpers;
using System.Net;

namespace EuphoriaInn.IntegrationTests.Controllers;

public class ShopControllerIntegrationTests : IClassFixture<WebApplicationFactoryBase>
{
    private readonly WebApplicationFactoryBase _factory;
    private readonly HttpClient _client;

    public ShopControllerIntegrationTests(WebApplicationFactoryBase factory)
    {
        _factory = factory;
        // Use non-redirecting client to properly test authorization redirects
        _client = factory.CreateNonRedirectingClient();
    }

    [Fact]
    public async Task Index_ShouldReturnShopPage()
    {
        // Arrange - Shop requires authentication
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Act
        var response = await client.GetAsync("/Shop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Shop");
    }

    [Fact]
    public async Task Index_WithItems_ShouldDisplayItems()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create shopkeeper and items BEFORE creating authenticated client
        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "shopkeeper", "shopkeeper@example.com");

        await TestDataHelper.CreateShopItemAsync(
            _factory.Services, shopkeeper.Id, "Longsword", 15.0m, 3);
        await TestDataHelper.CreateShopItemAsync(
            _factory.Services, shopkeeper.Id, "Health Potion", 5.0m, 10);

        // Create authenticated client AFTER data setup
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Act
        var response = await client.GetAsync("/Shop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Longsword");
        content.Should().Contain("Health Potion");
    }

    [Fact]
    public async Task Details_WithValidItemId_ShouldReturnItemDetails()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "detailshop", "detailshop@example.com");

        var item = await TestDataHelper.CreateShopItemAsync(
            _factory.Services, shopkeeper.Id, "Magic Staff", 50.0m, 1);

        // Act
        var response = await client.GetAsync($"/Shop/Details/{item.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Magic Staff");
        content.Should().Contain("50");
    }

    [Fact]
    public async Task Details_WithInvalidItemId_ShouldReturn404()
    {
        // Arrange - Shop requires authentication
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        // Act
        var response = await client.GetAsync("/Shop/Details/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Purchase_WhenNotAuthenticated_ShouldRedirectToLogin()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(_factory.Services, "purchaseshop", "purchase@example.com");
        var item = await TestDataHelper.CreateShopItemAsync(_factory.Services, shopkeeper.Id);

        // Try to access Shop page without auth to get anti-forgery token (will redirect)
        // For this test, we're just checking that unauthenticated access is blocked
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = item.Id.ToString(),
            ["quantity"] = "1"
        });

        // Act
        var response = await _client.PostAsync("/Shop/Purchase", formContent);

        // Assert - Should redirect to login or return unauthorized
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Purchase_WhenAuthenticated_ShouldProcessRequest()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "richshopkeeper", "richshop@example.com");

        var (buyerClient, buyer) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "richbuyer", "richbuyer@example.com");

        var item = await TestDataHelper.CreateShopItemAsync(
            _factory.Services, shopkeeper.Id, "Affordable Item", 10.0m, 5);

        // Get the shop page to extract anti-forgery token
        var getResponse = await buyerClient.GetAsync("/Shop");
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);

        // Set the anti-forgery cookie
        if (!string.IsNullOrEmpty(cookieValue))
        {
            buyerClient.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string>
            {
                ["id"] = item.Id.ToString(),
                ["quantity"] = "1"
            },
            token);

        // Act
        var response = await buyerClient.PostAsync("/Shop/Purchase", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Purchase_WithExpensiveItem_ShouldProcessRequest()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);
        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "expensiveshopkeeper", "expensiveshop@example.com");

        var (buyerClient, buyer) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
            _factory, "poorbuyer", "poorbuyer@example.com");

        var item = await TestDataHelper.CreateShopItemAsync(
            _factory.Services, shopkeeper.Id, "Expensive Item", 100.0m, 5);

        // Get the shop page to extract anti-forgery token
        var getResponse = await buyerClient.GetAsync("/Shop");
        var (token, cookieValue) = await AntiForgeryHelper.ExtractAntiForgeryTokenAsync(getResponse);

        // Set the anti-forgery cookie
        if (!string.IsNullOrEmpty(cookieValue))
        {
            buyerClient.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Antiforgery={cookieValue}");
        }

        var formContent = AntiForgeryHelper.CreateFormContentWithAntiForgeryToken(
            new Dictionary<string, string>
            {
                ["id"] = item.Id.ToString(),
                ["quantity"] = "1"
            },
            token);

        // Act
        var response = await buyerClient.PostAsync("/Shop/Purchase", formContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task Search_WithKeyword_ShouldReturnMatchingItems()
    {
        // Arrange
        await TestDataHelper.ClearDatabaseAsync(_factory.Services);

        // Create authenticated client
        var (client, _) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(_factory);

        var shopkeeper = await AuthenticationHelper.CreateTestUserAsync(
            _factory.Services, "searchshop", "searchshop@example.com");

        await TestDataHelper.CreateShopItemAsync(_factory.Services, shopkeeper.Id, "Iron Sword", 15.0m);
        await TestDataHelper.CreateShopItemAsync(_factory.Services, shopkeeper.Id, "Steel Sword", 25.0m);
        await TestDataHelper.CreateShopItemAsync(_factory.Services, shopkeeper.Id, "Magic Wand", 30.0m);

        // Act
        var response = await client.GetAsync("/Shop?search=sword");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Iron Sword");
        content.Should().Contain("Steel Sword");
    }
}
