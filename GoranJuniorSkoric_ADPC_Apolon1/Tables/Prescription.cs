using CustomORM.Attributes;

namespace Tables;

[Table("prescriptions")]
public class Prescription
{
    [PrimaryKey(IsAutoIncrement = true)]
    [Column("id")]
    public int id { get; set; }

    [ForeignKey(typeof(Patient))]
    [Column("patient_id")]
    public int patient_id { get; set; }

    [Column("medication", MaxLength = 200)]
    public string medication { get; set; } = string.Empty;

    [Column("dosage", MaxLength = 100)]
    public string dosage { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime start_date { get; set; }

    [Column("end_date", IsNullable = true)]
    public DateTime? end_date { get; set; }

    [Column("doctor_name", MaxLength = 200)]
    public string doctor_name { get; set; } = string.Empty;

    [Column("instructions", MaxLength = 1000, IsNullable = true)]
    public string? instructions { get; set; }

    // Navigation property (not mapped to database)
    [NotMapped]
    public Patient? Patient { get; set; }

    [NotMapped]
    public bool IsActive => end_date == null || end_date > DateTime.Now;
}
