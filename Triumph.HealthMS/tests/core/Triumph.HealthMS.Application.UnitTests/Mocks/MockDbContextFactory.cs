namespace Triumph.HealthMS.Application.UnitTests.Mocks;

/// <summary>
/// Factory for creating isolated in-memory TestAppDbContext instances per test class.
/// </summary>
public static class MockDbContextFactory
{
    public static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestAppDbContext(options);
    }
}

