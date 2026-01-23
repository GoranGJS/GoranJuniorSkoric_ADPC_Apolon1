using CustomORM.Attributes;

namespace Tables;

// Lookup table entity for checkup types.
[Table("checkup_types")]
public class CheckupTypeEntity
{
    [PrimaryKey(IsAutoIncrement = true)]
    [Column("id")]
    public int id { get; set; }

    [Column("name", MaxLength = 50, IsNullable = false)]
    public string name { get; set; } = string.Empty;
}
