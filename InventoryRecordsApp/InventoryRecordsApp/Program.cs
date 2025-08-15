using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InventoryRecordsApp
{
    //Files which any program will read from is located in the same folder as its executable(.exe).
    //Per the project folder:
    //dcit318-assignment3-11261721\InventoryRecordsApp\InventoryRecordsApp\bin\Debug\net8.0\inventory.json

    // Marker interface for inventory entities
    public interface IInventoryEntity
    {
        int Id { get; }
    }

    // Immutable record representing an inventory item
    public record InventoryItem(int Id, string Name, int Quantity, DateTime DateAdded) : IInventoryEntity
    {
        // Make a nicer string for display
        public override string ToString() =>
            $"{Id}: {Name} (Qty: {Quantity}) Added: {DateAdded:yyyy-MM-dd}";
    }

    // Generic inventory logger
    public class InventoryLogger<T> where T : IInventoryEntity
    {
        private readonly List<T> _log = new();
        private readonly string _filePath;

        public InventoryLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));

            _filePath = filePath;
        }

        // Add item (prevent duplicate Ids)
        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_log.Any(x => x.Id == item.Id))
                throw new InvalidOperationException($"An item with Id {item.Id} already exists in the log.");

            _log.Add(item);
        }

        // Return a copy of all items
        public List<T> GetAll() => new(_log);

        // Persist to file as JSON
        public void SaveToFile()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };

                // Use FileStream + using
                using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(fs, _log, options);
            }
            catch (UnauthorizedAccessException uex)
            {
                Console.WriteLine($"Permission error while saving file: {uex.Message}");
                throw;
            }
            catch (IOException ioex)
            {
                Console.WriteLine($"I/O error while saving file: {ioex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while saving file: {ex.Message}");
                throw;
            }
        }

        // Load from file (replaces current _log contents)
        public void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    Console.WriteLine($"Data file not found: {_filePath}");
                    _log.Clear();
                    return;
                }

                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var items = JsonSerializer.Deserialize<List<T>>(fs, options);
                _log.Clear();

                if (items != null)
                {
                    _log.AddRange(items);
                }
            }
            catch (JsonException jex)
            {
                Console.WriteLine($"Data format error while loading file: {jex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException uex)
            {
                Console.WriteLine($"Permission error while loading file: {uex.Message}");
                throw;
            }
            catch (IOException ioex)
            {
                Console.WriteLine($"I/O error while loading file: {ioex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while loading file: {ex.Message}");
                throw;
            }
        }

        // Clear the log (helper for simulating new sessions)
        public void Clear() => _log.Clear();
    }

    // Integration class
    public class InventoryApp
    {
        private readonly InventoryLogger<InventoryItem> _logger;

        public InventoryApp(string filePath)
        {
            _logger = new InventoryLogger<InventoryItem>(filePath);
        }

        public void SeedSampleData()
        {
            // Create a few InventoryItem records (immutable)
            var now = DateTime.UtcNow.Date;
            try
            {
                _logger.Add(new InventoryItem(1, "USB-C Cable", 25, now.AddDays(-10)));
                _logger.Add(new InventoryItem(2, "Wireless Mouse", 15, now.AddDays(-5)));
                _logger.Add(new InventoryItem(3, "Keyboard - Mechanical", 8, now.AddDays(-2)));
                _logger.Add(new InventoryItem(4, "27\" Monitor", 6, now.AddDays(-1)));
                _logger.Add(new InventoryItem(5, "Laptop Stand", 12, now));
            }
            catch (InvalidOperationException inv)
            {
                Console.WriteLine($"Warning when seeding: {inv.Message}");
            }
        }

        public void SaveData()
        {
            Console.WriteLine("Saving data to disk...");
            _logger.SaveToFile();
            Console.WriteLine("Save complete.");
        }

        public void LoadData()
        {
            Console.WriteLine("Loading data from disk...");
            _logger.LoadFromFile();
            Console.WriteLine("Load complete.");
        }

        public void PrintAllItems()
        {
            var items = _logger.GetAll();
            if (items.Count == 0)
            {
                Console.WriteLine("(no items)");
                return;
            }

            Console.WriteLine("Inventory items:");
            foreach (var it in items)
            {
                Console.WriteLine(" - " + it);
            }
        }

        // Expose a way to clear in-memory log (simulate restart)
        public void ClearMemory() => _logger.Clear();
    }

    // Program entry
    public static class Program
    {
        public static void Main()
        {
            // file path next to the running exe
            string exeFolder = AppContext.BaseDirectory;
            string fileName = "inventory.json";
            string filePath = Path.Combine(exeFolder, fileName);

            Console.WriteLine("Executable folder: " + exeFolder);
            Console.WriteLine("Data file: " + filePath);
            Console.WriteLine();

            var app = new InventoryApp(filePath);

            // 1) Seed
            app.SeedSampleData();

            // 2) Save to disk
            try
            {
                app.SaveData();
            }
            catch
            {
                Console.WriteLine("Failed to save data. Aborting.");
                return;
            }

            // 3) Clear memory and simulate new session
            app.ClearMemory();
            Console.WriteLine("\nCleared in-memory data to simulate a new session.\n");

            // 4) Load from disk
            try
            {
                app.LoadData();
            }
            catch
            {
                Console.WriteLine("Failed to load data. Aborting.");
                return;
            }

            // 5) Print to confirm recovery
            Console.WriteLine();
            app.PrintAllItems();

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
