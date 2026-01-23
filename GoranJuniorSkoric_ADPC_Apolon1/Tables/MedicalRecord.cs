using CustomORM.Attributes;

namespace Tables;

[Table("medical_records")]
public class MedicalRecord
{
    [PrimaryKey(IsAutoIncrement = true)]
    [Column("id")]
    public int id { get; set; }

    [ForeignKey(typeof(Patient))]
    [Column("patient_id")]
    public int patient_id { get; set; }

    [Column("record_date")]
    public DateTime record_date { get; set; }

    [Column("diagnosis", MaxLength = 1000)]
    public string diagnosis { get; set; } = string.Empty;

    [Column("notes", MaxLength = 5000, IsNullable = true)]
    public string? notes { get; set; }

    [Column("doctor_name", MaxLength = 200)]
    public string doctor_name { get; set; } = string.Empty;

    // Navigation property (not mapped to database)
    [NotMapped]
    public Patient? Patient { get; set; }
}
