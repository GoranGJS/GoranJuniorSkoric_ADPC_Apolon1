using CustomORM.Core;
using Npgsql;

namespace Migrations;

// Manages applying and reverting migrations.
public class MigrationRunner
{
    private readonly ConnectionManager _connectionManager;

    public MigrationRunner(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    // Creates the __Migrations table if it doesn't exist.
    public async Task EnsureMigrationsTableExistsAsync()
    {
        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ""__Migrations"" (
                    ""Id"" VARCHAR(255) PRIMARY KEY,
                    ""Description"" VARCHAR(500),
                    ""AppliedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                );";

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
            return Task.CompletedTask;
        });
    }

    // Gets all applied migrations (Id)
    public async Task<List<string>> GetAppliedMigrationsAsync()
    {
        await EnsureMigrationsTableExistsAsync();

        return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
        {
            var sql = "SELECT \"Id\" FROM \"__Migrations\" ORDER BY \"AppliedAt\";";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var migrations = new List<string>();
            while (await reader.ReadAsync())
            {
                migrations.Add(reader.GetString(0));
            }
            return migrations;
        });
    }

    // Applies a migration (UpAsync)
    public async Task ApplyMigrationAsync(Migration migration)
    {
        await EnsureMigrationsTableExistsAsync();

        var applied = await GetAppliedMigrationsAsync();
        if (applied.Contains(migration.Id))
        {
            Console.WriteLine($"Migration {migration.Id} already applied. Skipping.");
            return;
        }

        
        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            // Start transaction
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Generate SQL from migration (C# to SQL translation)
                var builder = new MigrationBuilder();
                await migration.UpAsync(builder);
                var sql = builder.GetUpSql();

                // Execute migration SQL (creates tables, adds columns, etc.)
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                await command.ExecuteNonQueryAsync();

                // Record migration in tracking table to prevent re-application
                var recordSql = "INSERT INTO \"__Migrations\" (\"Id\", \"Description\") VALUES (@id, @description);";
                await using var recordCommand = new NpgsqlCommand(recordSql, connection, transaction);
                recordCommand.Parameters.AddWithValue("@id", migration.Id);
                recordCommand.Parameters.AddWithValue("@description", migration.Description);
                await recordCommand.ExecuteNonQueryAsync();

                // Commit transaction if successful
                await transaction.CommitAsync();
                Console.WriteLine($"Applied migration: {migration.Id} - {migration.Description}");
            }
            catch
            {
                // Rollback transaction on any error to maintain database consistency
                await transaction.RollbackAsync();
                throw;
            }
            return Task.CompletedTask;
        });
    }

    // Reverts a migration (DownAsync)
    public async Task RevertMigrationAsync(Migration migration)
    {
        await EnsureMigrationsTableExistsAsync();

        var applied = await GetAppliedMigrationsAsync();
        if (!applied.Contains(migration.Id))
        {
            Console.WriteLine($"Migration {migration.Id} not applied. Cannot revert.");
            return;
        }

        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            // Start transaction
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Generate rollback SQL from migration (C# to SQL translation)
                var builder = new MigrationBuilder();
                await migration.DownAsync(builder);
                var sql = builder.GetDownSql();

                // Execute rollback SQL (drops tables, removes columns, etc.)
                if (!string.IsNullOrEmpty(sql))
                {
                    await using var command = new NpgsqlCommand(sql, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                }

                // Remove migration record from tracking table
                var recordSql = "DELETE FROM \"__Migrations\" WHERE \"Id\" = @id;";
                await using var recordCommand = new NpgsqlCommand(recordSql, connection, transaction);
                recordCommand.Parameters.AddWithValue("@id", migration.Id);
                await recordCommand.ExecuteNonQueryAsync();

                // Commit transaction if successful
                await transaction.CommitAsync();
                Console.WriteLine($"Reverted migration: {migration.Id} - {migration.Description}");
            }
            catch
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                throw;
            }
            return Task.CompletedTask;
        });
    }

    // Applies all pending migrations (ApplyMigrationAsync)
    public async Task ApplyAllMigrationsAsync(List<Migration> migrations)
    {
        var applied = await GetAppliedMigrationsAsync();
        var pending = migrations.Where(m => !applied.Contains(m.Id))
            .OrderBy(m => m.Id)
            .ToList();

        foreach (var migration in pending)
        {
            await ApplyMigrationAsync(migration);
        }
    }

    /// <summary>
    /// Removes a migration record from __Migrations so it can be re-applied.
    /// Use when the DB was recreated or tables were dropped but __Migrations still marks the migration as applied.
    /// </summary>
    public async Task RemoveMigrationRecordAsync(string migrationId)
    {
        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            var sql = "DELETE FROM \"__Migrations\" WHERE \"Id\" = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", migrationId);
            await command.ExecuteNonQueryAsync();
            return Task.CompletedTask;
        });
    }
}
