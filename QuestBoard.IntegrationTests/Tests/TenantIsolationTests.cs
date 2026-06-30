using QuestBoard.IntegrationTests.Helpers;

namespace QuestBoard.IntegrationTests.Tests;

/// <summary>
/// Cross-group tenant isolation tests.
/// Proves that the EF Core HasQueryFilter correctly scopes quests to the active group.
/// References: TENANT-03, D-03, D-05, D-10, D-11.
/// </summary>
public class TenantIsolationTests(WebApplicationFactoryBase factory) : IClassFixture<WebApplicationFactoryBase>
{
    /// <summary>
    /// A quest seeded with GroupId=2 must NOT appear in the response when the active group is 1.
    /// </summary>
    [Fact]
    public async Task GroupFilter_HidesQuestFromOtherGroup()
    {
        // Arrange — clean slate with roles and default Group 1 seeded
        await TestDataHelper.ClearDatabaseAsync(factory.Services);

        // Seed Group 2 and a DM user, then add a quest belonging to Group 2
        var dm = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "isolationdm1", "isolationdm1@example.com");

        await using var ctx = factory.Database.CreateContext(); // ActiveGroupId = null (sees all for seeding)
        ctx.Groups.Add(new GroupEntity { Id = 2, Name = "OtherGroup", CreatedAt = DateTime.UtcNow });
        ctx.Quests.Add(new QuestEntity
        {
            Title = "GroupTwoQuest",
            Description = "This quest belongs to Group 2 and must be hidden from Group 1 views.",
            GroupId = 2,
            DungeonMasterId = dm.Id,
            ChallengeRating = 3,
            TotalPlayerCount = 4,
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        // Act — request the home page (quest list) with the singleton stub scoped to Group 1
        factory.TestGroupContext.ActiveGroupId = 1;
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert — the Group-2 quest must not appear in the response body
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain("GroupTwoQuest");
    }

    /// <summary>
    /// A quest seeded with GroupId=1 MUST appear in the response when the active group is 1.
    /// </summary>
    [Fact]
    public async Task GroupFilter_ShowsQuestFromSameGroup()
    {
        // Arrange — clean slate with roles and default Group 1 seeded
        await TestDataHelper.ClearDatabaseAsync(factory.Services);

        var dm = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "isolationdm2", "isolationdm2@example.com");

        await using var ctx = factory.Database.CreateContext(); // ActiveGroupId = null (sees all for seeding)
        ctx.Quests.Add(new QuestEntity
        {
            Title = "GroupOneQuest",
            Description = "This quest belongs to Group 1 and must be visible for Group 1 views.",
            GroupId = 1,
            DungeonMasterId = dm.Id,
            ChallengeRating = 3,
            TotalPlayerCount = 4,
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        // Act — request the home page with the singleton stub scoped to Group 1
        factory.TestGroupContext.ActiveGroupId = 1;
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert — the Group-1 quest IS returned in the response body
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("GroupOneQuest");
    }

    /// <summary>
    /// When ActiveGroupId is null on the TestDatabase context (see-all semantics, D-05),
    /// DbContext.Quests.ToList() returns quests from all groups.
    /// </summary>
    [Fact]
    public async Task GroupFilter_NullGroupIdShowsAllGroups()
    {
        // Arrange — clean slate with roles and default Group 1 seeded
        await TestDataHelper.ClearDatabaseAsync(factory.Services);

        var dm = await AuthenticationHelper.CreateTestUserAsync(
            factory.Services, "isolationdm3", "isolationdm3@example.com");

        await using (var ctx = factory.Database.CreateContext()) // ActiveGroupId = null (sees all)
        {
            ctx.Groups.Add(new GroupEntity { Id = 2, Name = "OtherGroup", CreatedAt = DateTime.UtcNow });
            ctx.Quests.Add(new QuestEntity
            {
                Title = "GroupOneVisible",
                Description = "Quest in Group 1.",
                GroupId = 1,
                DungeonMasterId = dm.Id,
                ChallengeRating = 3,
                TotalPlayerCount = 4,
                CreatedAt = DateTime.UtcNow
            });
            ctx.Quests.Add(new QuestEntity
            {
                Title = "GroupTwoVisible",
                Description = "Quest in Group 2.",
                GroupId = 2,
                DungeonMasterId = dm.Id,
                ChallengeRating = 3,
                TotalPlayerCount = 4,
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        // Act — query via TestDatabase.CreateContext() which uses MutableGroupContext { ActiveGroupId = null }
        // This exercises the "null = see all" predicate in HasQueryFilter directly (D-05).
        await using var readCtx = factory.Database.CreateContext();
        var allQuests = readCtx.Quests.ToList();

        // Assert — both groups' quests are visible when ActiveGroupId is null
        allQuests.Should().Contain(q => q.Title == "GroupOneVisible",
            because: "null ActiveGroupId should return Group 1 quests");
        allQuests.Should().Contain(q => q.Title == "GroupTwoVisible",
            because: "null ActiveGroupId should return Group 2 quests");
    }
}
