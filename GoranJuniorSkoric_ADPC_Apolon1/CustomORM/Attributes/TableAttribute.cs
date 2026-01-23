namespace CustomORM.Attributes;

// Specifies the database table name for a class
// If not specified, the class name is used as the table name

[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}
