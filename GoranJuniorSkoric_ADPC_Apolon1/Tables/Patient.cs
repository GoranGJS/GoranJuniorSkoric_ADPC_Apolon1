using CustomORM.Attributes;

namespace Tables;

[Table("patients")]
public class Patient
{
    [PrimaryKey(IsAutoIncrement = true)]
    [Column("id")]
    public int id { get; set; }

    [Column("first_name", MaxLength = 100)]
    public string first_name { get; set; } = string.Empty;

    [Column("last_name", MaxLength = 100)]
    public string last_name { get; set; } = string.Empty;

    [Column("date_of_birth")]
    public DateTime date_of_birth { get; set; }

    [Column("gender", MaxLength = 10)]
    public string gender { get; set; } = string.Empty;

    [Column("phone_number", MaxLength = 20, IsNullable = true)]
    public string? phone_number { get; set; }

    [Column("email", MaxLength = 255, IsNullable = true)]
    public string? email { get; set; }

    [Column("address", MaxLength = 500, IsNullable = true)]
    public string? address { get; set; }

    [NotMapped]
    public string FullName => $"{first_name} {last_name}";

    [NotMapped]
    public int Age => DateTime.Now.Year - date_of_birth.Year - (DateTime.Now.DayOfYear < date_of_birth.DayOfYear ? 1 : 0);
}
