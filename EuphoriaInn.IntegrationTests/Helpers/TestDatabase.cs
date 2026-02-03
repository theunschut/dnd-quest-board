using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.IntegrationTests.Helpers;

public class TestDatabase : IDisposable
{
    public string DatabaseName { get; }
    private readonly DbContextOptions<QuestBoardContext> _options;
    private readonly SqliteConnection _connection;

    // Expose the connection so it can be reused in the web application factory
    public SqliteConnection Connection => _connection;

    public TestDatabase(string databaseName)
    {
        DatabaseName = databaseName;

        // Create and open a SQLite in-memory connection
        // IMPORTANT: Connection must stay open for the lifetime of the database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Use SQLite in-memory database for tests (works in CI/CD environments)
        // SQLite provides real SQL behavior unlike EF Core's InMemory provider
        _options = new DbContextOptionsBuilder<QuestBoardContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        // Initialize database schema
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public QuestBoardContext CreateContext()
    {
        return new QuestBoardContext(_options);
    }

    public void Reset()
    {
        // Clear all data from the SQLite in-memory database
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
        // Close the connection, which destroys the in-memory database
        _connection?.Close();
        _connection?.Dispose();
    }
}
