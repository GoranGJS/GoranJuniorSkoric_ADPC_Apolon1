using CustomORM.Attributes;
using CustomORM.Core;
using Npgsql;
using System.Data;
using System.Reflection;

namespace CustomORM.Core;

// Provides CRUD operations for entities using the custom ORM
public class Repository<T> where T : class, new()
{
    private readonly ConnectionManager _connectionManager;
    private readonly EntityMetadata _metadata;
    private readonly QueryBuilder _queryBuilder;

    public Repository(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _metadata = EntityMapper.GetMetadata<T>();
        _queryBuilder = new QueryBuilder(_metadata);
    }

    // Finds an entity by its primary key
    public async Task<T?> FindAsync(object id)
    {
        return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
        {
            var (sql, parameters) = _queryBuilder.BuildSelectById(id);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToEntity(reader);
            }
            return null;
        });
    }

    // Finds all entities matching a WHERE clause
    public async Task<List<T>> WhereAsync(string whereClause, List<NpgsqlParameter>? parameters = null)
    {
        return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
        {
            var (sql, queryParams) = _queryBuilder.BuildSelect(whereClause, parameters);

            await using var command = new NpgsqlCommand(sql, connection);
            if (queryParams != null)
            {
                command.Parameters.AddRange(queryParams.ToArray());
            }

            var results = new List<T>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapToEntity(reader));
            }
            return results;
        });
    }

    // Gets all entities
    public async Task<List<T>> GetAllAsync()
    {
        return await WhereAsync(null!);
    }

    // Adds a new entity to the database
    public async Task<T> AddAsync(T entity)
    {
        return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
        {
            var (sql, parameters) = _queryBuilder.BuildInsert(entity);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var newId = await command.ExecuteScalarAsync();
            
            // Set the primary key if it was auto-generated
            var pkAttr = _metadata.PrimaryKey.GetCustomAttribute<PrimaryKeyAttribute>();
            if (pkAttr != null && pkAttr.IsAutoIncrement)
            {
                _metadata.PrimaryKey.SetValue(entity, Convert.ChangeType(newId, _metadata.PrimaryKey.PropertyType));
            }

            return entity;
        });
    }

    // Updates an existing entity in the database
    public async Task UpdateAsync(T entity)
    {
        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            var (sql, parameters) = _queryBuilder.BuildUpdate(entity);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            await command.ExecuteNonQueryAsync();
            return Task.CompletedTask;
        });
    }

    // Deletes an entity by its primary key
    public async Task DeleteAsync(object id)
    {
        await _connectionManager.ExecuteWithConnectionAsync(async (NpgsqlConnection connection) =>
        {
            var (sql, parameters) = _queryBuilder.BuildDelete(id);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            await command.ExecuteNonQueryAsync();
            return Task.CompletedTask;
        });
    }

    // Maps a database row to an entity instance using reflection
    private T MapToEntity(NpgsqlDataReader reader)
    {
        var entity = new T();

        foreach (var prop in _metadata.Columns)
        {
            var columnName = _metadata.GetColumnName(prop);
            
            try
            {
                var value = reader[columnName];
                if (value != DBNull.Value)
                {
                    // Handle nullable types
                    var propType = prop.PropertyType;
                    var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
                    
                    var convertedValue = Convert.ChangeType(value, underlyingType);
                    prop.SetValue(entity, convertedValue);
                }
            }
            catch
            {
                // Column not found or type mismatch - skip
            }
        }

        return entity;
    }
}
