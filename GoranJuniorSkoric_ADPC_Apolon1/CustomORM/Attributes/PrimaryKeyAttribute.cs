namespace CustomORM.Attributes;

// Marks a property as the primary key of the table. 
// IsAutoIncrement makes the database auto-generate the ID

[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyAttribute : Attribute
{
    public bool IsAutoIncrement { get; set; } = true;

    public PrimaryKeyAttribute() { }
}
