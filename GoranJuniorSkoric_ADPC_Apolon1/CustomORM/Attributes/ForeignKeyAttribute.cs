namespace CustomORM.Attributes;

// Specifies a foreign key relationship to another entity
// marks a property as a foreign key that references another entity/table.

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public Type ReferencedType { get; }
    public string? ReferencedProperty { get; set; }
    public string? ColumnName { get; set; }

    public ForeignKeyAttribute(Type referencedType)
    {
        ReferencedType = referencedType;
    }
}
