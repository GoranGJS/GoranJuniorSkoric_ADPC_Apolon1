using CustomORM.Core;
using Tables;
using Npgsql;

namespace Services;

// Service for managing checkup operations.
public class CheckupService
{
    private readonly Repository<Checkup> _repository;
    private readonly Repository<Patient> _patientRepository;
    private readonly Repository<CheckupTypeEntity> _checkupTypeRepository;

    // Initializes service with repositories
    public CheckupService(Repository<Checkup> repository, Repository<Patient> patientRepository, Repository<CheckupTypeEntity> checkupTypeRepository)
    {
        _repository = repository;
        _patientRepository = patientRepository;
        _checkupTypeRepository = checkupTypeRepository;
    }

    // Gets CheckupTypeEntity by enum value name.
    public async Task<CheckupTypeEntity?> GetCheckupTypeByNameAsync(string name)
    {
        var param = new NpgsqlParameter("@name", name);
        var types = await _checkupTypeRepository.WhereAsync("\"name\" = @name", new List<NpgsqlParameter> { param });
        return types.FirstOrDefault();
    }

    // Gets CheckupTypeEntity by id.
    public async Task<CheckupTypeEntity?> GetCheckupTypeByIdAsync(int id)
    {
        return await _checkupTypeRepository.FindAsync(id);
    }

    // Creates new checkup for patient
    // Validates checkup type exists before creating
    public async Task<Checkup> CreateCheckupAsync(int patientId, CheckupType checkupType, 
        DateTime checkupDate, string doctorName, string? results = null, string? notes = null)
    {
        // Find the CheckupTypeEntity by enum name
        var checkupTypeEntity = await GetCheckupTypeByNameAsync(checkupType.ToString());
        if (checkupTypeEntity == null)
        {
            throw new ArgumentException($"CheckupType '{checkupType}' not found in lookup table");
        }

        // Create checkup entity with validated checkup type
        var checkup = new Checkup
        {
            patient_id = patientId,
            checkup_type_id = checkupTypeEntity.id,
            checkup_date = checkupDate,
            doctor_name = doctorName,
            results = results,
            notes = notes
        };

        return await _repository.AddAsync(checkup);
    }

    // Gets a checkup by its primary key id
    public async Task<Checkup?> GetCheckupByIdAsync(int id)
    {
        return await _repository.FindAsync(id);
    }

    
    // Gets all checkups for specific patient using Lazy Loading.
    public async Task<List<Checkup>> GetCheckupsByPatientAsync(int patientId)
    {
        var param = new NpgsqlParameter("@patientId", patientId);
        var checkups = await _repository.WhereAsync("\"patient_id\" = @patientId", new List<NpgsqlParameter> { param });

        return checkups;
    }

    // Gets all checkups for specific type (ex: GP, BLOOD, XRAY)
    public async Task<List<Checkup>> GetCheckupsByTypeAsync(CheckupType checkupType)
    {
        // Find the CheckupTypeEntity by enum name
        var checkupTypeEntity = await GetCheckupTypeByNameAsync(checkupType.ToString());
        if (checkupTypeEntity == null)
        {
            return new List<Checkup>();
        }

        // Query checkups by checkup type id
        var param = new NpgsqlParameter("@checkupTypeId", checkupTypeEntity.id);
        return await _repository.WhereAsync("\"checkup_type_id\" = @checkupTypeId", new List<NpgsqlParameter> { param });
    }

    // Gets all checkups
    public async Task<List<Checkup>> GetAllCheckupsAsync()
    {
        return await _repository.GetAllAsync();
    }

    // Updates existing checkup
    public async Task UpdateCheckupAsync(Checkup checkup)
    {
        await _repository.UpdateAsync(checkup);
    }

    // Deletes checkup by id
    public async Task DeleteCheckupAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
