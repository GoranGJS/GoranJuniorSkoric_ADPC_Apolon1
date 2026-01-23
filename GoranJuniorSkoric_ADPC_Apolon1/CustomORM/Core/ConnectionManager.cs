using Npgsql;

namespace CustomORM.Core;

// Manages database connections with connection pooling
// Connection pooling reuses existing database connections instead of creating a new one for each request.
public class ConnectionManager
{
    private readonly string _connectionString;

    public ConnectionManager(string connectionString)
    {
        _connectionString = connectionString;
    }


    // Creates a new database connection
    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);  // Npgsql handles pooling automatically
    }


    // Executes action with connection, opens/auto-closes (non-blocking async)
    public async Task<T> ExecuteWithConnectionAsync<T>(Func<NpgsqlConnection, Task<T>> action)
    {
        await using var connection = CreateConnection();//create
        await connection.OpenAsync(); //open
        return await action(connection); //use
        //await using auto closes
    }


}
