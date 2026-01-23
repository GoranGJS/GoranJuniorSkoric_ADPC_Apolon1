using CustomORM.Core;
using Npgsql;
using System.Reflection;
using System.Text;

namespace CustomORM.Core;

// Builds SQL queries from entity metadata and objects
public class QueryBuilder
{
    private readonly EntityMetadata _metadata;

    public QueryBuilder(EntityMetadata metadata)
    {
        _metadata = metadata;
    }

    // Builds a SELECT query with optional WHERE clause.
    public (string sql, List<NpgsqlParameter> parameters) BuildSelect(string? whereClause = null, List<NpgsqlParameter>? parameters = null)
    {
        var sql = new StringBuilder();
        sql.Append($"SELECT * FROM \"{_metadata.TableName}\"");

        if (!string.IsNullOrEmpty(whereClause))
        {
            sql.Append($" WHERE {whereClause}");
        }

        return (sql.ToString(), parameters ?? new List<NpgsqlParameter>());
    }

    // Builds a SELECT query by primary key.
    public (string sql, List<NpgsqlParameter> parameters) BuildSelectById(object id)
    {
        var pkColumn = _metadata.GetColumnName(_metadata.PrimaryKey);
        var param = new NpgsqlParameter("@id", id);
        
        return BuildSelect($"\"{pkColumn}\" = @id", new List<NpgsqlParameter> { param });
    }

    // Builds an INSERT query.
    public (string sql, List<NpgsqlParameter> parameters) BuildInsert(object entity)
    {
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        foreach (var prop in _metadata.Columns)
        {
            // Skip primary key if it's auto-increment
            var pkAttr = prop.GetCustomAttribute<Attributes.PrimaryKeyAttribute>();
            if (pkAttr != null && pkAttr.IsAutoIncrement)
            {
                continue;
            }

            var columnName = _metadata.GetColumnName(prop);
            var value = prop.GetValue(entity);

            columns.Add($"\"{columnName}\"");
            values.Add($"@{columnName}");
            parameters.Add(new NpgsqlParameter($"@{columnName}", value ?? DBNull.Value));
        }

        var sql = $"INSERT INTO \"{_metadata.TableName}\" ({string.Join(", ", columns)}) " +
                  $"VALUES ({string.Join(", ", values)}) RETURNING \"{_metadata.GetColumnName(_metadata.PrimaryKey)}\"";

        return (sql, parameters);
    }

    // Builds an UPDATE query.
    public (string sql, List<NpgsqlParameter> parameters) BuildUpdate(object entity)
    {
        var setClauses = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        foreach (var prop in _metadata.Columns)
        {
            // Skip primary key in SET clause
            if (prop == _metadata.PrimaryKey)
            {
                continue;
            }

            var columnName = _metadata.GetColumnName(prop);
            var value = prop.GetValue(entity);

            setClauses.Add($"\"{columnName}\" = @{columnName}");
            parameters.Add(new NpgsqlParameter($"@{columnName}", value ?? DBNull.Value));
        }

        var pkColumn = _metadata.GetColumnName(_metadata.PrimaryKey);
        var pkValue = _metadata.PrimaryKey.GetValue(entity);
        parameters.Add(new NpgsqlParameter("@pkId", pkValue));

        var sql = $"UPDATE \"{_metadata.TableName}\" SET {string.Join(", ", setClauses)} " +
                  $"WHERE \"{pkColumn}\" = @pkId";

        return (sql, parameters);
    }

    // Builds a DELETE query by primary key.
    public (string sql, List<NpgsqlParameter> parameters) BuildDelete(object id)
    {
        var pkColumn = _metadata.GetColumnName(_metadata.PrimaryKey);
        var param = new NpgsqlParameter("@id", id);

        var sql = $"DELETE FROM \"{_metadata.TableName}\" WHERE \"{pkColumn}\" = @id";

        return (sql, new List<NpgsqlParameter> { param });
    }

}
