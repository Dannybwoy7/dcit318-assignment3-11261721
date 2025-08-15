using System;
using System.Collections.Generic;

namespace WarehouseInventorySystem
{
    // ---------- Marker interface ----------
    public interface IInventoryItem
    {
        int Id { get; }
        string Name { get; }
        int Quantity { get; set; }
    }

    // ---------- Product classes ----------
    public class ElectronicItem : IInventoryItem
    {
        public int Id { get; }
        public string Name { get; }
        public int Quantity { get; set; }
        public string Brand { get; }
        public int WarrantyMonths { get; }

        public ElectronicItem(int id, string name, int quantity, string brand, int warrantyMonths)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentException("Brand required", nameof(brand));
            if (warrantyMonths < 0) throw new ArgumentOutOfRangeException(nameof(warrantyMonths));

            Id = id;
            Name = name;
            Quantity = quantity;
            Brand = brand;
            WarrantyMonths = warrantyMonths;
        }

        public override string ToString()
            => $"ElectronicItem(Id={Id}, Name={Name}, Qty={Quantity}, Brand={Brand}, Warranty={WarrantyMonths}mo)";
    }

    public class GroceryItem : IInventoryItem
    {
        public int Id { get; }
        public string Name { get; }
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; }

        public GroceryItem(int id, string name, int quantity, DateTime expiryDate)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (expiryDate == default) throw new ArgumentException("Expiry date required", nameof(expiryDate));

            Id = id;
            Name = name;
            Quantity = quantity;
            ExpiryDate = expiryDate;
        }

        public override string ToString()
            => $"GroceryItem(Id={Id}, Name={Name}, Qty={Quantity}, Expiry={ExpiryDate:yyyy-MM-dd})";
    }

    // ---------- Custom exceptions ----------
    public class DuplicateItemException : Exception
    {
        public DuplicateItemException(string message) : base(message) { }
    }

    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException(string message) : base(message) { }
    }

    public class InvalidQuantityException : Exception
    {
        public InvalidQuantityException(string message) : base(message) { }
    }

    // ---------- Generic Inventory Repository ----------
    public class InventoryRepository<T> where T : IInventoryItem
    {
        private readonly Dictionary<int, T> _items = new();

        public void AddItem(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_items.ContainsKey(item.Id))
                throw new DuplicateItemException($"An item with ID {item.Id} already exists.");

            _items[item.Id] = item;
        }

        public T GetItemById(int id)
        {
            if (!_items.TryGetValue(id, out var item))
                throw new ItemNotFoundException($"No item found with ID {id}.");

            return item;
        }

        public void RemoveItem(int id)
        {
            if (!_items.Remove(id))
                throw new ItemNotFoundException($"Cannot remove: no item found with ID {id}.");
        }

        public List<T> GetAllItems()
        {
            // Return a copy list to avoid external mutation of the internal collection
            return new List<T>(_items.Values);
        }

        public void UpdateQuantity(int id, int newQuantity)
        {
            if (newQuantity < 0)
                throw new InvalidQuantityException("Quantity cannot be negative.");

            if (!_items.TryGetValue(id, out var item))
                throw new ItemNotFoundException($"No item found with ID {id}.");

            item.Quantity = newQuantity;
        }
    }

    // ---------- Warehouse manager ----------
    public class WareHouseManager
    {
        // Exposed so Main() can pass the repositories to generic methods (alternatively could provide wrapper methods)
        public InventoryRepository<ElectronicItem> _electronics { get; } = new();
        public InventoryRepository<GroceryItem> _groceries { get; } = new();

        // Seed 2-3 items of each type (handles duplicate errors gracefully)
        public void SeedData()
        {
            Console.WriteLine("Seeding data...\n");

            // Electronics
            AddItem(_electronics, new ElectronicItem(1, "Laptop", 10, "Dell", 24));
            AddItem(_electronics, new ElectronicItem(2, "Smartphone", 25, "Samsung", 12));
            AddItem(_electronics, new ElectronicItem(3, "Router", 5, "TP-Link", 18));

            // Groceries
            AddItem(_groceries, new GroceryItem(101, "Rice (5kg)", 50, DateTime.UtcNow.AddMonths(12)));
            AddItem(_groceries, new GroceryItem(102, "Milk (1L)", 30, DateTime.UtcNow.AddDays(14)));
            AddItem(_groceries, new GroceryItem(103, "Eggs (dozen)", 20, DateTime.UtcNow.AddDays(21)));

            Console.WriteLine();
        }

        // Generic print method
        public void PrintAllItems<T>(InventoryRepository<T> repo) where T : IInventoryItem
        {
            var items = repo.GetAllItems();
            Console.WriteLine($"Printing all {typeof(T).Name} items ({items.Count}):");
            foreach (var item in items)
            {
                Console.WriteLine(" - " + item);
            }
            Console.WriteLine();
        }

        // Add any item; catches DuplicateItemException and prints friendly message
        public void AddItem<T>(InventoryRepository<T> repo, T item) where T : IInventoryItem
        {
            try
            {
                repo.AddItem(item);
                Console.WriteLine($"Added item: {item}");
            }
            catch (DuplicateItemException dex)
            {
                Console.WriteLine($"[Error - Duplicate] {dex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error - AddItem] {ex.Message}");
            }
        }

        // Increase stock by quantity (validates item existence and non-negative computed quantity)
        public void IncreaseStock<T>(InventoryRepository<T> repo, int id, int quantityToAdd) where T : IInventoryItem
        {
            try
            {
                if (quantityToAdd < 0)
                    throw new InvalidQuantityException("Quantity to add must be non-negative.");

                var item = repo.GetItemById(id);
                int newQty = item.Quantity + quantityToAdd;
                repo.UpdateQuantity(id, newQty);
                Console.WriteLine($"Stock increased for ID {id}. New quantity: {newQty}");
            }
            catch (ItemNotFoundException inf)
            {
                Console.WriteLine($"[Error - Not Found] {inf.Message}");
            }
            catch (InvalidQuantityException iq)
            {
                Console.WriteLine($"[Error - Invalid Quantity] {iq.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error - IncreaseStock] {ex.Message}");
            }
        }

        // Remove an item by ID with friendly error handling
        public void RemoveItemById<T>(InventoryRepository<T> repo, int id) where T : IInventoryItem
        {
            try
            {
                repo.RemoveItem(id);
                Console.WriteLine($"Removed item with ID {id}");
            }
            catch (ItemNotFoundException inf)
            {
                Console.WriteLine($"[Error - Not Found] {inf.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error - RemoveItem] {ex.Message}");
            }
        }
    }

    // ---------- Program entry ----------
    internal class Program
    {
        private static void Main()
        {
            var manager = new WareHouseManager();

            // i. SeedData
            manager.SeedData();

            // ii & iii. Print all grocery items
            manager.PrintAllItems(manager._groceries);

            // iv. Print all electronic items
            manager.PrintAllItems(manager._electronics);

            // v. Try operations that cause exceptions

            Console.WriteLine("== Attempt to add a duplicate electronic item ==");
            // Attempt to add duplicate (ID 1 already present)
            var duplicateElectronic = new ElectronicItem(1, "Laptop Pro", 5, "Dell", 36);
            manager.AddItem(manager._electronics, duplicateElectronic);
            Console.WriteLine();

            Console.WriteLine("== Attempt to remove a non-existent grocery item ==");
            // Remove a non-existing ID (e.g., 999)
            manager.RemoveItemById(manager._groceries, 999);
            Console.WriteLine();

            Console.WriteLine("== Attempt to update with invalid quantity ==");
            // Attempt to set invalid negative quantity using IncreaseStock with negative add
            // We'll call IncreaseStock with negative to trigger InvalidQuantityException in method
            manager.IncreaseStock(manager._electronics, 2, -5);

            // Additionally, try repo.UpdateQuantity directly (simulate invalid update)
            Console.WriteLine("\n== Attempt direct invalid UpdateQuantity (should be caught) ==");
            try
            {
                manager._electronics.UpdateQuantity(2, -10); // negative -> InvalidQuantityException
            }
            catch (InvalidQuantityException iq)
            {
                Console.WriteLine($"[Caught in Main] {iq.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Caught in Main] Unexpected: {ex.Message}");
            }

            Console.WriteLine("\nFinal state of inventories:");
            manager.PrintAllItems(manager._electronics);
            manager.PrintAllItems(manager._groceries);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
