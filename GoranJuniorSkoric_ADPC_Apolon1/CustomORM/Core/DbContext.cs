using CustomORM.Core;

namespace CustomORM.Core;


// Base database context class that provides connection access to repositories
public abstract class DbContext
{
    protected readonly ConnectionManager ConnectionManager;

    protected DbContext(string connectionString)
    {
        ConnectionManager = new ConnectionManager(connectionString);
    }

    // Get repository for the specified entity type (T)
    public Repository<T> Set<T>() where T : class, new()
    {
        return new Repository<T>(ConnectionManager);
    }

    // Ensures the database exists (creates it if it doesn't)
    public abstract Task EnsureDatabaseCreatedAsync();

}
