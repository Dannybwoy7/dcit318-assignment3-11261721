using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthcareSystem
{
    // Generic repository for entity management
    // Constrained to class so we can return null for GetById
    public class Repository<T> where T : class
    {
        private readonly List<T> items = new();

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            items.Add(item);
        }

        public List<T> GetAll()
        {
            // return a copy to preserve encapsulation
            return new List<T>(items);
        }

        // Return first match or null
        public T? GetById(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return items.FirstOrDefault(predicate);
        }

        // Remove the first item matching predicate, return true if removed
        public bool Remove(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var item = items.FirstOrDefault(predicate);
            if (item == null) return false;
            return items.Remove(item);
        }
    }

    // Patient class
    public class Patient
    {
        public int Id { get; }
        public string Name { get; }
        public int Age { get; }
        public string Gender { get; }

        public Patient(int id, string name, int age, string gender)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
            if (age < 0) throw new ArgumentOutOfRangeException(nameof(age), "Age cannot be negative");
            if (string.IsNullOrWhiteSpace(gender)) throw new ArgumentException("Gender required", nameof(gender));

            Id = id;
            Name = name;
            Age = age;
            Gender = gender;
        }

        public override string ToString() => $"Patient(Id={Id}, Name={Name}, Age={Age}, Gender={Gender})";
    }

    // Prescription class
    public class Prescription
    {
        public int Id { get; }
        public int PatientId { get; }
        public string MedicationName { get; }
        public DateTime DateIssued { get; }

        public Prescription(int id, int patientId, string medicationName, DateTime dateIssued)
        {
            if (string.IsNullOrWhiteSpace(medicationName)) throw new ArgumentException("Medication name required", nameof(medicationName));

            Id = id;
            PatientId = patientId;
            MedicationName = medicationName;
            DateIssued = dateIssued;
        }

        public override string ToString() => $"Prescription(Id={Id}, PatientId={PatientId}, Medication={MedicationName}, Date={DateIssued:yyyy-MM-dd})";
    }

    // Main application class
    public class HealthSystemApp
    {
        private readonly Repository<Patient> _patientRepo = new();
        private readonly Repository<Prescription> _prescriptionRepo = new();
        private readonly Dictionary<int, List<Prescription>> _prescriptionMap = new();

        // Seed sample patients and prescriptions
        public void SeedData()
        {
            // Add 2-3 patients
            _patientRepo.Add(new Patient(1, "Alice Johnson", 34, "Female"));
            _patientRepo.Add(new Patient(2, "Bob Mensah", 45, "Male"));
            _patientRepo.Add(new Patient(3, "Clara Osei", 29, "Female"));

            // Add 4-5 prescriptions (ensure valid PatientIds)
            _prescriptionRepo.Add(new Prescription(1, 1, "Amoxicillin 500mg", DateTime.UtcNow.AddDays(-10)));
            _prescriptionRepo.Add(new Prescription(2, 1, "Paracetamol 500mg", DateTime.UtcNow.AddDays(-3)));
            _prescriptionRepo.Add(new Prescription(3, 2, "Lisinopril 10mg", DateTime.UtcNow.AddDays(-30)));
            _prescriptionRepo.Add(new Prescription(4, 3, "Metformin 500mg", DateTime.UtcNow.AddDays(-7)));
            _prescriptionRepo.Add(new Prescription(5, 2, "Atorvastatin 20mg", DateTime.UtcNow.AddDays(-1)));
        }

        // Build dictionary mapping PatientId -> List<Prescription>
        public void BuildPrescriptionMap()
        {
            _prescriptionMap.Clear();

            foreach (var pres in _prescriptionRepo.GetAll())
            {
                if (!_prescriptionMap.TryGetValue(pres.PatientId, out var list))
                {
                    list = new List<Prescription>();
                    _prescriptionMap[pres.PatientId] = list;
                }
                list.Add(pres);
            }
        }

        // Retrieve prescriptions by patient id (returns empty list if none found)
        public List<Prescription> GetPrescriptionsByPatientId(int patientId)
        {
            if (_prescriptionMap.TryGetValue(patientId, out var list))
            {
                // return a copy to protect internal state
                return new List<Prescription>(list);
            }
            return new List<Prescription>();
        }

        // Print all patients stored in repository
        public void PrintAllPatients()
        {
            var patients = _patientRepo.GetAll();
            Console.WriteLine("Patients:");
            foreach (var p in patients)
            {
                Console.WriteLine($" - {p}");
            }
        }

        // Print prescriptions for a specific patient using the map
        public void PrintPrescriptionsForPatient(int patientId)
        {
            var patient = _patientRepo.GetById(p => p.Id == patientId);
            if (patient == null)
            {
                Console.WriteLine($"No patient found with Id {patientId}");
                return;
            }

            var prescriptions = GetPrescriptionsByPatientId(patientId);
            Console.WriteLine($"\nPrescriptions for {patient.Name} (Id={patient.Id}):");

            if (!prescriptions.Any())
            {
                Console.WriteLine(" - (no prescriptions found)");
                return;
            }

            foreach (var pres in prescriptions.OrderByDescending(p => p.DateIssued))
            {
                Console.WriteLine($" - {pres}");
            }
        }
    }

    // Program entry
    public static class Program
    {
        public static void Main()
        {
            var app = new HealthSystemApp();

            // i. Seed data
            app.SeedData();

            // ii. Build prescription dictionary (grouping)
            app.BuildPrescriptionMap();

            // iii. Print all patients
            app.PrintAllPatients();

            // iv. Select one PatientId and print all prescriptions for that patient
            // (we pick PatientId = 2 as an example)
            int selectedPatientId = 2;
            app.PrintPrescriptionsForPatient(selectedPatientId);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
