namespace CustomORM.Attributes;

// Indicates that a property should not be mapped to a database column
// Prevents ORM from trying to map navigation(C# objects) and computed properties to database columns

[AttributeUsage(AttributeTargets.Property)]
public class NotMappedAttribute : Attribute
{
}
