namespace EuphoriaInn.IntegrationTests.Helpers;

public class TestDatabase : IDisposable
{
    public string DatabaseName { get; }
    private readonly DbContextOptions<QuestBoardContext> _options;

    public TestDatabase(string databaseName)
    {
        DatabaseName = databaseName;

        _options = new DbContextOptionsBuilder<QuestBoardContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public QuestBoardContext CreateContext()
    {
        return new QuestBoardContext(_options);
    }

    public void Reset()
    {
        try
        {
            using var context = CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        catch
        {
            // Ignore errors
        }
    }

    public void Dispose() { }
}
