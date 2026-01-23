using Tables;

namespace Migrations;


// Initial migration sets up the initial database schema for the medical system
public class InitialMigration : Migration
{
    public override string Id => "20250117000000_Initial";
    public override string Description => "Create initial tables: checkup_types, patients, medical_records, checkups, prescriptions";

    public override async Task UpAsync(MigrationBuilder builder)
    {
        // Create CheckupTypeEntity lookup table first
        builder.CreateTable<CheckupTypeEntity>();

        // Create Patients table
        builder.CreateTable<Patient>();

        // Create MedicalRecords table
        builder.CreateTable<MedicalRecord>()
            .CreateForeignKey<MedicalRecord>(nameof(MedicalRecord.patient_id), typeof(Patient));

        // Create Checkups table
        builder.CreateTable<Checkup>()
            .CreateForeignKey<Checkup>(nameof(Checkup.patient_id), typeof(Patient))
            .CreateForeignKey<Checkup>(nameof(Checkup.checkup_type_id), typeof(CheckupTypeEntity))
            .CreateIndex<Checkup>(nameof(Checkup.patient_id))
            .CreateIndex<Checkup>(nameof(Checkup.checkup_type_id));

        // Create Prescriptions table
        builder.CreateTable<Prescription>()
            .CreateForeignKey<Prescription>(nameof(Prescription.patient_id), typeof(Patient))
            .CreateIndex<Prescription>(nameof(Prescription.patient_id))
            .CreateIndex<Prescription>(nameof(Prescription.start_date));

        await Task.CompletedTask;
    }

// DownAsync method reverses the migration by dropping all tables in reverse order
    public override async Task DownAsync(MigrationBuilder builder)
    {
        // Drop in reverse order (respecting foreign key constraints)
        builder.DropTable<Prescription>();
        builder.DropTable<Checkup>();
        builder.DropTable<MedicalRecord>();
        builder.DropTable<Patient>();
        builder.DropTable<CheckupTypeEntity>();

        await Task.CompletedTask;
    }
}
