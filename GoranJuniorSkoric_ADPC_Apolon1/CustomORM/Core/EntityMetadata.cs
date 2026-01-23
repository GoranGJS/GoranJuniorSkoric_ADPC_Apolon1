using CustomORM.Attributes;
using System.Reflection;
using System.Text;

namespace CustomORM.Core;

// Contains metadata about an entity type extracted using reflection

// reads entity class (like Patient) and extracts database mapping information from attributes
public class EntityMetadata
{
    public Type EntityType { get; }
    public string TableName { get; }
    public PropertyInfo PrimaryKey { get; }
    public List<PropertyInfo> Columns { get; }
    public Dictionary<PropertyInfo, ForeignKeyAttribute> ForeignKeys { get; }
    public Dictionary<PropertyInfo, ColumnAttribute> ColumnAttributes { get; }

    // Constructor: extracts all metadata from the entity class
    public EntityMetadata(Type entityType)
    {
        EntityType = entityType;
        
        // Get table name from Table attribute or use class name
        var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
        var rawTableName = tableAttr?.Name ?? entityType.Name;
        TableName = ConvertToSnakeCase(rawTableName);

        // Find primary key
        PrimaryKey = entityType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
            ?? throw new InvalidOperationException($"Entity {entityType.Name} must have a property marked with [PrimaryKey]");

        // Get all mapped properties (exclude NotMapped)
        Columns = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
            .ToList();

        // Extract foreign keys and column attributes
        ForeignKeys = new Dictionary<PropertyInfo, ForeignKeyAttribute>();
        ColumnAttributes = new Dictionary<PropertyInfo, ColumnAttribute>();

        foreach (var prop in Columns)
        {
            // Check if property has ForeignKey
            var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
            if (fkAttr != null)
            {
                ForeignKeys[prop] = fkAttr;
            }

            // Check if property has Column
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (colAttr != null)
            {
                ColumnAttributes[prop] = colAttr;
            }
        }
    }

    
    // Returns the name from Column attribute if true, otherwise converts property name to snake_case
    public string GetColumnName(PropertyInfo property)
    {
        if (ColumnAttributes.TryGetValue(property, out var colAttr) && !string.IsNullOrEmpty(colAttr.Name))
        {
            return ConvertToSnakeCase(colAttr.Name);
        }
        return ConvertToSnakeCase(property.Name);
    }

    // Converts PascalCase/camelCase to snake_case
    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
