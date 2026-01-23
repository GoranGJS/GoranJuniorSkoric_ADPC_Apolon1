using CustomORM.Core;
using Tables;
using Npgsql;

namespace GoranJuniorSkoric_ADPC_Apolon1;

// Database context for the medical system.
public class MedicalDbContext : DbContext
{
    // Initializes database context with connection string
    public MedicalDbContext(string connectionString) : base(connectionString)
    {
    }

    // Repositories for each entity type
    public Repository<CheckupTypeEntity> CheckupTypes => Set<CheckupTypeEntity>();
    public Repository<Patient> Patients => Set<Patient>();
    public Repository<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public Repository<Checkup> Checkups => Set<Checkup>();
    public Repository<Prescription> Prescriptions => Set<Prescription>();

    // Ensures database exists (will be handled by migrations)
    public override async Task EnsureDatabaseCreatedAsync()
    {
        // This will be handled by migrations
        await Task.CompletedTask;
    }
}
