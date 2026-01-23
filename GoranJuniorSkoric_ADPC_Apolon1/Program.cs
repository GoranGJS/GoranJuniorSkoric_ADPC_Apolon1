using CustomORM.Core;
using Tables;
using GoranJuniorSkoric_ADPC_Apolon1;
using Migrations;
using Npgsql;
using Services;

namespace GoranJuniorSkoric_ADPC_Apolon1;

class Program
{
    private static MedicalDbContext? _dbContext;
    private static PatientService? _patientService;
    private static CheckupService? _checkupService;
    private static PrescriptionService? _prescriptionService;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Apolon Medical System");
        Console.WriteLine();

        // Get connection string
        var connectionString = GetConnectionString();
        _dbContext = new MedicalDbContext(connectionString);

        // Run migrations
        await RunMigrationsAsync(connectionString);

        // Seed CheckupType lookup table
        await SeedCheckupTypesAsync();

        // Initialize services
        _patientService = new PatientService(_dbContext.Patients);
        _checkupService = new CheckupService(_dbContext.Checkups, _dbContext.Patients, _dbContext.CheckupTypes);
        _prescriptionService = new PrescriptionService(_dbContext.Prescriptions, _dbContext.Patients);

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n--- Main Menu ---");
            Console.WriteLine("1. Patient Management");
            Console.WriteLine("2. Checkup Management");
            Console.WriteLine("3. Prescription Management");
            Console.WriteLine("4. View Reports");
            Console.WriteLine("5. Exit");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await PatientMenuAsync();
                    break;
                case "2":
                    await CheckupMenuAsync();
                    break;
                case "3":
                    await PrescriptionMenuAsync();
                    break;
                case "4":
                    await ReportsMenuAsync();
                    break;
                case "5":
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }

        Console.WriteLine("\nThank you for using Apolon Medical System!");
    }

    static string GetConnectionString()
    {
        Console.Write("Enter PostgreSQL connection string (or press Enter for default): ");
        var input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            // Default connection string
            return "Host=localhost;Port=5432;Database=apolon_medical;Username=postgres;Password=postgres";
        }
        
        return input;
    }

    static async Task RunMigrationsAsync(string connectionString)
    {
        // Extract database name from connection string and create it if it doesn't exist
        await EnsureDatabaseExistsAsync(connectionString);

        Console.WriteLine("\nRunning migrations...");
        var connectionManager = new ConnectionManager(connectionString);
        var migrationRunner = new MigrationRunner(connectionManager);

        var migrations = new List<Migration>
        {
            new InitialMigration()
        };

        await migrationRunner.ApplyAllMigrationsAsync(migrations);
        Console.WriteLine("Migrations completed.\n");
    }

    static async Task SeedCheckupTypesAsync()
    {
        var checkupTypes = _dbContext!.CheckupTypes;
        var existingTypes = await checkupTypes.GetAllAsync();

        if (existingTypes.Count > 0)
        {
            Console.WriteLine("CheckupTypes already seeded.\n");
            return;
        }

        Console.WriteLine("Seeding CheckupTypes...");
        var enumValues = Enum.GetValues<CheckupType>();
        foreach (var enumValue in enumValues)
        {
            var checkupTypeEntity = new CheckupTypeEntity
            {
                name = enumValue.ToString()
            };
            await checkupTypes.AddAsync(checkupTypeEntity);
        }
        Console.WriteLine($"Seeded {enumValues.Length} checkup types.\n");
    }

    static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        // Parse connection string to get database name
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        
        if (string.IsNullOrEmpty(databaseName))
        {
            Console.WriteLine("Warning: No database name in connection string.");
            return;
        }

        // Connect to postgres database to create the target database
        builder.Database = "postgres";
        var postgresConnectionString = builder.ConnectionString;

        try
        {
            await using var connection = new Npgsql.NpgsqlConnection(postgresConnectionString);
            await connection.OpenAsync();

            // Check if database exists
            var checkDbSql = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}';";
            await using var checkCommand = new Npgsql.NpgsqlCommand(checkDbSql, connection);
            var exists = await checkCommand.ExecuteScalarAsync();

            if (exists == null)
            {
                // Create database
                var createDbSql = $"CREATE DATABASE \"{databaseName}\";";
                await using var createCommand = new Npgsql.NpgsqlCommand(createDbSql, connection);
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"Created database: {databaseName}");
            }
            else
            {
                Console.WriteLine($"Database '{databaseName}' already exists");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ensuring database exists: {ex.Message}");
            throw;
        }
    }

    static async Task PatientMenuAsync()
    {
        while (true)
        {
            Console.WriteLine("\n=== Patient Management ===");
            Console.WriteLine("1. Create Patient");
            Console.WriteLine("2. View All Patients");
            Console.WriteLine("3. Search Patients");
            Console.WriteLine("4. View Patient Details");
            Console.WriteLine("5. Update Patient");
            Console.WriteLine("6. Delete Patient");
            Console.WriteLine("7. Back to Main Menu");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await CreatePatientAsync();
                    break;
                case "2":
                    await ViewAllPatientsAsync();
                    break;
                case "3":
                    await SearchPatientsAsync();
                    break;
                case "4":
                    await ViewPatientDetailsAsync();
                    break;
                case "5":
                    await UpdatePatientAsync();
                    break;
                case "6":
                    await DeletePatientAsync();
                    break;
                case "7":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static async Task CreatePatientAsync()
    {
        Console.Write("First Name: ");
        var firstName = Console.ReadLine() ?? "";
        Console.Write("Last Name: ");
        var lastName = Console.ReadLine() ?? "";
        Console.Write("Date of Birth (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out var dob))
        {
            Console.WriteLine("Invalid date format.");
            return;
        }
        Console.Write("Gender: ");
        var gender = Console.ReadLine() ?? "";
        Console.Write("Phone Number (optional): ");
        var phone = Console.ReadLine();
        Console.Write("Email (optional): ");
        var email = Console.ReadLine();
        Console.Write("Address (optional): ");
        var address = Console.ReadLine();

        var patient = await _patientService!.CreatePatientAsync(firstName, lastName, dob, gender, phone, email, address);
        Console.WriteLine($"\nPatient created with ID: {patient.id}");
    }

    static async Task ViewAllPatientsAsync()
    {
        var patients = await _patientService!.GetAllPatientsAsync();
        Console.WriteLine($"\nTotal Patients: {patients.Count}\n");
        foreach (var p in patients)
        {
            Console.WriteLine($"ID: {p.id}, Name: {p.FullName}, Age: {p.Age}, Gender: {p.gender}");
        }
    }

    static async Task SearchPatientsAsync()
    {
        Console.Write("Enter search term: ");
        var term = Console.ReadLine() ?? "";
        var patients = await _patientService!.SearchPatientsByNameAsync(term);
        Console.WriteLine($"\nFound {patients.Count} patient(s):\n");
        foreach (var p in patients)
        {
            Console.WriteLine($"ID: {p.id}, Name: {p.FullName}, Age: {p.Age}");
        }
    }

    static async Task ViewPatientDetailsAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var patient = await _patientService!.GetPatientByIdAsync(id);
        if (patient == null)
        {
            Console.WriteLine("Patient not found.");
            return;
        }

        Console.WriteLine($"\n=== Patient Details ===");
        Console.WriteLine($"ID: {patient.id}");
        Console.WriteLine($"Name: {patient.FullName}");
        Console.WriteLine($"Date of Birth: {patient.date_of_birth:yyyy-MM-dd}");
        Console.WriteLine($"Age: {patient.Age}");
        Console.WriteLine($"Gender: {patient.gender}");
        Console.WriteLine($"Phone: {patient.phone_number ?? "N/A"}");
        Console.WriteLine($"Email: {patient.email ?? "N/A"}");
        Console.WriteLine($"Address: {patient.address ?? "N/A"}");
    }

    static async Task UpdatePatientAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var patient = await _patientService!.GetPatientByIdAsync(id);
        if (patient == null)
        {
            Console.WriteLine("Patient not found.");
            return;
        }

        Console.Write($"First Name ({patient.first_name}): ");
        var firstName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(firstName)) patient.first_name = firstName;

        Console.Write($"Last Name ({patient.last_name}): ");
        var lastName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(lastName)) patient.last_name = lastName;

        Console.Write($"Phone ({patient.phone_number ?? "N/A"}): ");
        var phone = Console.ReadLine();
        if (phone != null) patient.phone_number = phone;

        await _patientService.UpdatePatientAsync(patient);
        Console.WriteLine("Patient updated.");
    }

    static async Task DeletePatientAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Are you sure? (yes/no): ");
        if (Console.ReadLine()?.ToLower() != "yes")
        {
            return;
        }

        await _patientService!.DeletePatientAsync(id);
        Console.WriteLine("Patient deleted.");
    }

    static async Task CheckupMenuAsync()
    {
        while (true)
        {
            Console.WriteLine("\n=== Checkup Management ===");
            Console.WriteLine("1. Create Checkup");
            Console.WriteLine("2. View All Checkups");
            Console.WriteLine("3. View Checkups by Patient");
            Console.WriteLine("4. View Checkups by Type");
            Console.WriteLine("5. Back to Main Menu");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await CreateCheckupAsync();
                    break;
                case "2":
                    await ViewAllCheckupsAsync();
                    break;
                case "3":
                    await ViewCheckupsByPatientAsync();
                    break;
                case "4":
                    await ViewCheckupsByTypeAsync();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static async Task CreateCheckupAsync()
    {
        Console.Write("Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var patientId))
        {
            Console.WriteLine("Invalid Patient ID.");
            return;
        }

        Console.WriteLine("Checkup Types: GP, BLOOD, XRAY, CT, MRI, ULTRA, EKG, ECHO, EYE, DERM, DENTA, MAMMO, EEG");
        Console.Write("Checkup Type: ");
        if (!Enum.TryParse<CheckupType>(Console.ReadLine(), true, out var type))
        {
            Console.WriteLine("Invalid checkup type.");
            return;
        }

        Console.Write("Checkup Date (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out var date))
        {
            Console.WriteLine("Invalid date format.");
            return;
        }

        Console.Write("Doctor Name: ");
        var doctor = Console.ReadLine() ?? "";
        Console.Write("Results (optional): ");
        var results = Console.ReadLine();
        Console.Write("Notes (optional): ");
        var notes = Console.ReadLine();

        var checkup = await _checkupService!.CreateCheckupAsync(patientId, type, date, doctor, results, notes);
        Console.WriteLine($"\nCheckup created with ID: {checkup.id}");
    }

    static async Task ViewAllCheckupsAsync()
    {
        var checkups = await _checkupService!.GetAllCheckupsAsync();
        Console.WriteLine($"\nTotal Checkups: {checkups.Count}\n");
        foreach (var c in checkups)
        {
            var checkupType = c.CheckupType?.name ?? "Unknown";
            Console.WriteLine($"ID: {c.id}, Patient ID: {c.patient_id}, Type: {checkupType}, Date: {c.checkup_date:yyyy-MM-dd}");
        }
    }

    static async Task ViewCheckupsByPatientAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var patientId))
        {
            Console.WriteLine("Invalid Patient ID.");
            return;
        }

        // Using lazy loading - only checkup data is loaded, not Patient data
        var checkups = await _checkupService!.GetCheckupsByPatientAsync(patientId);
        Console.WriteLine($"\nFound {checkups.Count} checkup(s) for Patient ID {patientId}:\n");
        foreach (var c in checkups)
        {
            // Load checkup type if needed
            var checkupType = c.CheckupType?.name ?? "Unknown";
            if (c.CheckupType == null && c.checkup_type_id > 0)
            {
                var typeEntity = await _dbContext!.CheckupTypes.FindAsync(c.checkup_type_id);
                checkupType = typeEntity?.name ?? "Unknown";
            }
            Console.WriteLine($"ID: {c.id}, Type: {checkupType}, Date: {c.checkup_date:yyyy-MM-dd}, Doctor: {c.doctor_name}");
        }
    }

    static async Task ViewCheckupsByTypeAsync()
    {
        Console.WriteLine("Checkup Types: GP, BLOOD, XRAY, CT, MRI, ULTRA, EKG, ECHO, EYE, DERM, DENTA, MAMMO, EEG");
        Console.Write("Checkup Type: ");
        if (!Enum.TryParse<CheckupType>(Console.ReadLine(), true, out var type))
        {
            Console.WriteLine("Invalid checkup type.");
            return;
        }

        var checkups = await _checkupService!.GetCheckupsByTypeAsync(type);
        Console.WriteLine($"\nFound {checkups.Count} checkup(s) of type {type}:\n");
        foreach (var c in checkups)
        {
            Console.WriteLine($"ID: {c.id}, Patient ID: {c.patient_id}, Date: {c.checkup_date:yyyy-MM-dd}");
        }
    }

    static async Task PrescriptionMenuAsync()
    {
        while (true)
        {
            Console.WriteLine("\n=== Prescription Management ===");
            Console.WriteLine("1. Create Prescription");
            Console.WriteLine("2. View All Prescriptions");
            Console.WriteLine("3. View Prescriptions by Patient");
            Console.WriteLine("4. View Active Prescriptions");
            Console.WriteLine("5. Back to Main Menu");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await CreatePrescriptionAsync();
                    break;
                case "2":
                    await ViewAllPrescriptionsAsync();
                    break;
                case "3":
                    await ViewPrescriptionsByPatientAsync();
                    break;
                case "4":
                    await ViewActivePrescriptionsAsync();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static async Task CreatePrescriptionAsync()
    {
        Console.Write("Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var patientId))
        {
            Console.WriteLine("Invalid Patient ID.");
            return;
        }

        Console.Write("Medication: ");
        var medication = Console.ReadLine() ?? "";
        Console.Write("Dosage: ");
        var dosage = Console.ReadLine() ?? "";
        Console.Write("Start Date (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out var startDate))
        {
            Console.WriteLine("Invalid date format.");
            return;
        }

        Console.Write("End Date (YYYY-MM-DD, optional): ");
        DateTime? endDate = null;
        var endDateInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(endDateInput) && DateTime.TryParse(endDateInput, out var parsed))
        {
            endDate = parsed;
        }

        Console.Write("Doctor Name: ");
        var doctor = Console.ReadLine() ?? "";
        Console.Write("Instructions (optional): ");
        var instructions = Console.ReadLine();

        var prescription = await _prescriptionService!.CreatePrescriptionAsync(
            patientId, medication, dosage, startDate, doctor, endDate, instructions);
        Console.WriteLine($"\nPrescription created with ID: {prescription.id}");
    }

    static async Task ViewAllPrescriptionsAsync()
    {
        var allPrescriptions = await _dbContext!.Prescriptions.GetAllAsync();
        Console.WriteLine($"\nTotal Prescriptions: {allPrescriptions.Count}\n");
        foreach (var p in allPrescriptions)
        {
            Console.WriteLine($"ID: {p.id}, Patient ID: {p.patient_id}, Medication: {p.medication}, Active: {p.IsActive}");
        }
    }

    static async Task ViewPrescriptionsByPatientAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var patientId))
        {
            Console.WriteLine("Invalid Patient ID.");
            return;
        }

        // Using lazy loading - only prescription data is loaded, not Patient data
        var prescriptions = await _prescriptionService!.GetPrescriptionsByPatientAsync(patientId);
        Console.WriteLine($"\nFound {prescriptions.Count} prescription(s) for Patient ID {patientId}:\n");
        foreach (var p in prescriptions)
        {
            Console.WriteLine($"ID: {p.id}, Medication: {p.medication}, Dosage: {p.dosage}, Active: {p.IsActive}");
        }
    }

    static async Task ViewActivePrescriptionsAsync()
    {
        var prescriptions = await _prescriptionService!.GetActivePrescriptionsAsync();
        Console.WriteLine($"\nActive Prescriptions: {prescriptions.Count}\n");
        foreach (var p in prescriptions)
        {
            Console.WriteLine($"ID: {p.id}, Patient ID: {p.patient_id}, Medication: {p.medication}, Dosage: {p.dosage}");
        }
    }

    static async Task ReportsMenuAsync()
    {
        Console.WriteLine("\n=== Reports ===");
        Console.WriteLine("1. Patient Summary (with checkups and prescriptions)");
        Console.WriteLine("2. Back to Main Menu");
        Console.Write("Select an option: ");

        var choice = Console.ReadLine();

        if (choice == "1")
        {
            await PatientSummaryAsync();
        }
    }

    static async Task PatientSummaryAsync()
    {
        Console.Write("Enter Patient ID: ");
        if (!int.TryParse(Console.ReadLine(), out var patientId))
        {
            Console.WriteLine("Invalid Patient ID.");
            return;
        }

        var patient = await _patientService!.GetPatientByIdAsync(patientId);
        if (patient == null)
        {
            Console.WriteLine("Patient not found.");
            return;
        }

        // Using lazy loading - only checkup and prescription data is loaded, not Patient data
        // This is more efficient as we already have the patient object above
        var checkups = await _checkupService!.GetCheckupsByPatientAsync(patientId);
        var prescriptions = await _prescriptionService!.GetPrescriptionsByPatientAsync(patientId);

        Console.WriteLine($"\n=== Patient Summary ===");
        Console.WriteLine($"Patient: {patient.FullName} (ID: {patient.id})");
        Console.WriteLine($"Age: {patient.Age}, Gender: {patient.gender}");
        Console.WriteLine($"\nCheckups: {checkups.Count}");
        foreach (var c in checkups)
        {
            // Load checkup type if needed
            var checkupType = c.CheckupType?.name ?? "Unknown";
            if (c.CheckupType == null && c.checkup_type_id > 0)
            {
                var typeEntity = await _dbContext!.CheckupTypes.FindAsync(c.checkup_type_id);
                checkupType = typeEntity?.name ?? "Unknown";
            }
            Console.WriteLine($"  - {checkupType} on {c.checkup_date:yyyy-MM-dd} by {c.doctor_name}");
        }
        Console.WriteLine($"\nPrescriptions: {prescriptions.Count}");
        foreach (var p in prescriptions)
        {
            Console.WriteLine($"  - {p.medication} ({p.dosage}) - {(p.IsActive ? "Active" : "Inactive")}");
        }
    }
}
