using CustomORM.Attributes;
using CustomORM.Core;
using System.Reflection;
using System.Text;

namespace Migrations;

// Builds SQL statements for migrations using a fluent API
//TLDR: Takes C# and converts into SQL statements
public class MigrationBuilder
{
    private readonly StringBuilder _sqlBuilder = new();
    private readonly List<string> _revertSqlBuilder = new();

    // Creates a new table from an entity type (ex: Patient)
    public MigrationBuilder CreateTable<T>()
    {
        // Get metadata for the entity type
        var metadata = EntityMapper.GetMetadata<T>();
        var sql = new StringBuilder();
        sql.Append($"CREATE TABLE IF NOT EXISTS \"{metadata.TableName}\" (\n");

        var columns = new List<string>();

        foreach (var prop in metadata.Columns)
        {
            var columnName = metadata.GetColumnName(prop);
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>();

            var columnDef = $"  \"{columnName}\" ";

            // Determine column type
            var columnType = colAttr?.ColumnType ?? GetPostgresType(prop.PropertyType);
            columnDef += columnType;

            // Primary key and auto-increment
            if (pkAttr != null)
            {
                if (pkAttr.IsAutoIncrement && prop.PropertyType == typeof(int))
                {
                    columnDef = columnDef.Replace(columnType, "SERIAL");
                }
                columnDef += " PRIMARY KEY";
            }

            // Check column nullable
            if (colAttr != null && !colAttr.IsNullable && pkAttr == null)
            {
                columnDef += " NOT NULL";
            }

            // Check max length
            if (colAttr != null && colAttr.MaxLength > 0 && columnType.Contains("VARCHAR"))
            {
                columnDef = columnDef.Replace("VARCHAR", $"VARCHAR({colAttr.MaxLength})");
            }

            columns.Add(columnDef);
        }

        sql.Append(string.Join(",\n", columns));
        sql.Append("\n);");

        _sqlBuilder.AppendLine(sql.ToString());

        // Revert: drop table
        _revertSqlBuilder.Add($"DROP TABLE IF EXISTS \"{metadata.TableName}\";");

        return this;
    }

    // Creates a foreign key constraint (ex: MedicalRecord.patient_id -> Patient.id)
    public MigrationBuilder CreateForeignKey<T>(string propertyName, Type referencedType, string referencedProperty = "Id")
    {
        
        var metadata = EntityMapper.GetMetadata<T>();
        var prop = typeof(T).GetProperty(propertyName);
        if (prop == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on {typeof(T).Name}");
        }

        var columnName = metadata.GetColumnName(prop);
        var referencedMetadata = EntityMapper.GetMetadata(referencedType);
        var referencedColumn = referencedMetadata.GetColumnName(referencedMetadata.PrimaryKey);

        var fkName = $"fk_{metadata.TableName}_{columnName}";
        var sql = $"ALTER TABLE \"{metadata.TableName}\" " +
                  $"ADD CONSTRAINT \"{fkName}\" " +
                  $"FOREIGN KEY (\"{columnName}\") " +
                  $"REFERENCES \"{referencedMetadata.TableName}\" (\"{referencedColumn}\");";

        _sqlBuilder.AppendLine(sql);

        // Revert: drop foreign key
        _revertSqlBuilder.Add($"ALTER TABLE \"{metadata.TableName}\" DROP CONSTRAINT IF EXISTS \"{fkName}\";");

        return this;
    }

    // Creates an index (ex: Patient.id)
    public MigrationBuilder CreateIndex<T>(string propertyName, string? indexName = null)
    {
        var metadata = EntityMapper.GetMetadata<T>();
        var prop = typeof(T).GetProperty(propertyName);
        if (prop == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on {typeof(T).Name}");
        }

        var columnName = metadata.GetColumnName(prop);
        var idxName = indexName ?? $"ix_{metadata.TableName}_{columnName}";

        var sql = $"CREATE INDEX IF NOT EXISTS \"{idxName}\" ON \"{metadata.TableName}\" (\"{columnName}\");";
        _sqlBuilder.AppendLine(sql);

        // Revert: drop index
        _revertSqlBuilder.Add($"DROP INDEX IF EXISTS \"{idxName}\";");

        return this;
    }

    // Adds a column to an existing table.
    public MigrationBuilder AddColumn<T>(string propertyName)
    {
        var metadata = EntityMapper.GetMetadata<T>();
        var prop = typeof(T).GetProperty(propertyName);
        if (prop == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on {typeof(T).Name}");
        }

        var columnName = metadata.GetColumnName(prop);
        var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
        var columnType = colAttr?.ColumnType ?? GetPostgresType(prop.PropertyType);

        if (colAttr != null && colAttr.MaxLength > 0 && columnType.Contains("VARCHAR"))
        {
            columnType = columnType.Replace("VARCHAR", $"VARCHAR({colAttr.MaxLength})");
        }

        var nullable = colAttr?.IsNullable != false ? "" : " NOT NULL";
        var sql = $"ALTER TABLE \"{metadata.TableName}\" ADD COLUMN \"{columnName}\" {columnType}{nullable};";

        _sqlBuilder.AppendLine(sql);

        // Revert: drop column
        _revertSqlBuilder.Add($"ALTER TABLE \"{metadata.TableName}\" DROP COLUMN IF EXISTS \"{columnName}\";");

        return this;
    }

    // Drops a table.
    public MigrationBuilder DropTable<T>()
    {
        var metadata = EntityMapper.GetMetadata<T>();
        var sql = $"DROP TABLE IF EXISTS \"{metadata.TableName}\";";
        _sqlBuilder.AppendLine(sql);
        return this;
    }

    // Gets the SQL for applying the migration.
    public string GetUpSql() => _sqlBuilder.ToString();

    // Gets the SQL for reverting the migration.
    public string GetDownSql() => string.Join("\n", _revertSqlBuilder);

    // Maps C# types to PostgreSQL types.
    private static string GetPostgresType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.Name switch
        {
            "Int32" => "INTEGER",
            "Int64" => "BIGINT",
            "String" => "VARCHAR",
            "DateTime" => "TIMESTAMP",
            "Boolean" => "BOOLEAN",
            "Decimal" => "DECIMAL",
            "Double" => "DOUBLE PRECISION",
            "Single" => "REAL",
            _ => "TEXT"
        };
    }
}
