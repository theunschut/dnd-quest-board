namespace EuphoriaInn.IntegrationTests.Helpers;

public class TestDatabase : IDisposable
{
    public string DatabaseName { get; }
    public string ConnectionString { get; }

    public TestDatabase(string databaseName)
    {
        DatabaseName = databaseName;
        ConnectionString = $"server=.;database={databaseName};Integrated Security=true;TrustServerCertificate=True";

        // Create database
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public QuestBoardContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QuestBoardContext>()
            .UseSqlServer(ConnectionString)
            .EnableSensitiveDataLogging()
            .Options;

        return new QuestBoardContext(options);
    }

    public void Reset()
    {
        // Drop and recreate the database for a clean slate
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

    public void Dispose()
    {
        // Drop database after tests
        try
        {
            using var context = CreateContext();
            context.Database.EnsureDeleted();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
