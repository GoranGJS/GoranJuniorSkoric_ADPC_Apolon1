using CustomORM.Attributes;

namespace Tables;

[Table("checkups")]
public class Checkup
{
    [PrimaryKey(IsAutoIncrement = true)]
    [Column("id")]
    public int id { get; set; }

    [ForeignKey(typeof(Patient))]
    [Column("patient_id")]
    public int patient_id { get; set; }

    [ForeignKey(typeof(CheckupTypeEntity))]
    [Column("checkup_type_id")]
    public int checkup_type_id { get; set; }

    [Column("checkup_date")]
    public DateTime checkup_date { get; set; }

    [Column("results", MaxLength = 5000, IsNullable = true)]
    public string? results { get; set; }

    [Column("notes", MaxLength = 2000, IsNullable = true)]
    public string? notes { get; set; }

    [Column("doctor_name", MaxLength = 200)]
    public string doctor_name { get; set; } = string.Empty;

    // Navigation properties (not mapped to database)
    // Allows access to related entities through object references
    [NotMapped]
    public Patient? Patient { get; set; }

    [NotMapped]
    public CheckupTypeEntity? CheckupType { get; set; }
}
