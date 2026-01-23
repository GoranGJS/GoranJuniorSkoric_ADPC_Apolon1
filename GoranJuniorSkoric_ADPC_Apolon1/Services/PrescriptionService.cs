using CustomORM.Core;
using Tables;
using Npgsql;

namespace Services;

// Service for managing prescription operations.
public class PrescriptionService
{
    private readonly Repository<Prescription> _repository;
    private readonly Repository<Patient> _patientRepository;

    // Initializes service with repositories
    public PrescriptionService(Repository<Prescription> repository, Repository<Patient> patientRepository)
    {
        _repository = repository;
        _patientRepository = patientRepository;
    }

    // Creates new prescription
    public async Task<Prescription> CreatePrescriptionAsync(int patientId, string medication, 
        string dosage, DateTime startDate, string doctorName, DateTime? endDate = null, string? instructions = null)
    {
        var prescription = new Prescription
        {
            patient_id = patientId,
            medication = medication,
            dosage = dosage,
            start_date = startDate,
            end_date = endDate,
            doctor_name = doctorName,
            instructions = instructions
        };

        return await _repository.AddAsync(prescription);
    }

    // Gets prescription by id
    public async Task<Prescription?> GetPrescriptionByIdAsync(int id)
    {
        return await _repository.FindAsync(id);
    }

   
    // Gets all prescriptions for specific patient using Lazy Loading.
    public async Task<List<Prescription>> GetPrescriptionsByPatientAsync(int patientId)
    {
        var param = new NpgsqlParameter("@patientId", patientId);
        var prescriptions = await _repository.WhereAsync("\"patient_id\" = @patientId", new List<NpgsqlParameter> { param });

        return prescriptions;
    }

    // Gets all active prescriptions
    public async Task<List<Prescription>> GetActivePrescriptionsAsync()
    {
        var allPrescriptions = await _repository.GetAllAsync();
        return allPrescriptions.Where(p => p.IsActive).ToList();
    }

    // Gets all active prescriptions for specific patient
    public async Task<List<Prescription>> GetActivePrescriptionsByPatientAsync(int patientId)
    {
        var prescriptions = await GetPrescriptionsByPatientAsync(patientId);
        return prescriptions.Where(p => p.IsActive).ToList();
    }

    // Updates existing prescription
    public async Task UpdatePrescriptionAsync(Prescription prescription)
    {
        await _repository.UpdateAsync(prescription);
    }

    // Deletes prescription by id
    public async Task DeletePrescriptionAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
