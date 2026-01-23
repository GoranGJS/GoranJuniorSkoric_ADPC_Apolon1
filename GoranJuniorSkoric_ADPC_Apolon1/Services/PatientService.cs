using CustomORM.Core;
using Tables;
using Npgsql;

namespace Services;

// Service for managing patient operations.
public class PatientService
{
    private readonly Repository<Patient> _repository;

    // Initializes service with repository
    public PatientService(Repository<Patient> repository)
    {
        _repository = repository;
    }

    // Creates new patient
    public async Task<Patient> CreatePatientAsync(string firstName, string lastName, DateTime dateOfBirth, 
        string gender, string? phoneNumber = null, string? email = null, string? address = null)
    {
        var patient = new Patient
        {
            first_name = firstName,
            last_name = lastName,
            date_of_birth = dateOfBirth,
            gender = gender,
            phone_number = phoneNumber,
            email = email,
            address = address
        };

        return await _repository.AddAsync(patient);
    }

    // Gets patient by id
    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _repository.FindAsync(id);
    }

    // Gets all patients
    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        return await _repository.GetAllAsync();
    }

    // Searches patients by name
    public async Task<List<Patient>> SearchPatientsByNameAsync(string name)
    {
        var firstNameParam = new NpgsqlParameter("@firstName", $"%{name}%");
        var lastNameParam = new NpgsqlParameter("@lastName", $"%{name}%");
        
        return await _repository.WhereAsync(
            "\"first_name\" ILIKE @firstName OR \"last_name\" ILIKE @lastName",
            new List<NpgsqlParameter> { firstNameParam, lastNameParam }
        );
    }

    // Updates existing patient
    public async Task UpdatePatientAsync(Patient patient)
    {
        await _repository.UpdateAsync(patient);
    }

    // Deletes patient by id
    public async Task DeletePatientAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
