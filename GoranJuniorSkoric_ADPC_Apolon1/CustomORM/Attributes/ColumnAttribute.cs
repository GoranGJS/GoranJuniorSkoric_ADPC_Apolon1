namespace CustomORM.Attributes;


// Specifies the database column name and properties for a property

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    public string? Name { get; set; }
    public bool IsNullable { get; set; } = true;
    public int MaxLength { get; set; } = 0; // 0 means not specified
    public string? ColumnType { get; set; }

    public ColumnAttribute() { }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}
