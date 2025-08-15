using System;
using System.Collections.Generic;

namespace FinanceManagementSystem
{
    // Core model: immutable record for a transaction
    public record Transaction(int Id, DateTime Date, decimal Amount, string Category);

    // Interface for transaction processors
    public interface ITransactionProcessor
    {
        void Process(Transaction transaction);
    }

    // Concrete processors with distinct behavior/messages
    public class BankTransferProcessor : ITransactionProcessor
    {
        public void Process(Transaction transaction)
        {
            if (transaction.Amount <= 0)
            {
                Console.WriteLine($"[BankTransfer] Invalid amount ({transaction.Amount}). Skipping processing.");
                return;
            }

            Console.WriteLine($"[BankTransfer] Processing bank transfer. Amount: {transaction.Amount:C}, Category: {transaction.Category}");
            // Additional domain logic could go here (logging, validation, settlement, etc.)
        }
    }

    public class MobileMoneyProcessor : ITransactionProcessor
    {
        public void Process(Transaction transaction)
        {
            if (transaction.Amount <= 0)
            {
                Console.WriteLine($"[MobileMoney] Invalid amount ({transaction.Amount}). Skipping processing.");
                return;
            }

            Console.WriteLine($"[MobileMoney] Processing mobile money payment. Amount: {transaction.Amount:C}, Category: {transaction.Category}");
            // Additional domain logic could go here
        }
    }

    public class CryptoWalletProcessor : ITransactionProcessor
    {
        public void Process(Transaction transaction)
        {
            if (transaction.Amount <= 0)
            {
                Console.WriteLine($"[CryptoWallet] Invalid amount ({transaction.Amount}). Skipping processing.");
                return;
            }

            Console.WriteLine($"[CryptoWallet] Processing crypto wallet transfer. Amount: {transaction.Amount:C}, Category: {transaction.Category}");
            // Additional domain logic could go here
        }
    }

    // Base Account class
    public class Account
    {
        public string AccountNumber { get; }
        public decimal Balance { get; protected set; }

        public Account(string accountNumber, decimal initialBalance)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new ArgumentException("Account number must be provided.", nameof(accountNumber));
            if (initialBalance < 0)
                throw new ArgumentException("Initial balance cannot be negative.", nameof(initialBalance));

            AccountNumber = accountNumber;
            Balance = initialBalance;
        }

        // Virtual by default: deducts the amount (no overdraft checks here)
        public virtual void ApplyTransaction(Transaction transaction)
        {
            if (transaction is null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction.Amount <= 0)
            {
                Console.WriteLine("Transaction amount must be greater than zero. Transaction ignored.");
                return;
            }

            Balance -= transaction.Amount;
            Console.WriteLine($"[Account] Applied transaction {transaction.Id}. New balance: {Balance:C}");
        }
    }

    // Sealed specialized account that prevents further inheritance
    public sealed class SavingsAccount : Account
    {
        public SavingsAccount(string accountNumber, decimal initialBalance)
            : base(accountNumber, initialBalance)
        {
        }

        // Override to enforce no overdrafts
        public override void ApplyTransaction(Transaction transaction)
        {
            if (transaction is null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction.Amount <= 0)
            {
                Console.WriteLine("[SavingsAccount] Transaction amount must be greater than zero. Transaction ignored.");
                return;
            }

            if (transaction.Amount > Balance)
            {
                Console.WriteLine("[SavingsAccount] Insufficient funds");
                return;
            }

            Balance -= transaction.Amount;
            Console.WriteLine($"[SavingsAccount] Transaction {transaction.Id} applied. Updated balance: {Balance:C}");
        }
    }

    // Main application orchestration
    public class FinanceApp
    {
        private readonly List<Transaction> _transactions = new();

        public void Run()
        {
            // i. Instantiate a SavingsAccount with an account number and initial balance (e.g., 1000)
            var account = new SavingsAccount("SA-001-2025", 1000m);
            Console.WriteLine($"Created SavingsAccount {account.AccountNumber} with balance {account.Balance:C}\n");

            // ii. Create three Transaction records with sample values (Groceries, Utilities, Entertainment)
            var t1 = new Transaction(1, DateTime.UtcNow, 150.75m, "Groceries");
            var t2 = new Transaction(2, DateTime.UtcNow, 300.00m, "Utilities");
            var t3 = new Transaction(3, DateTime.UtcNow, 600.50m, "Entertainment");

            // iii. Use the processors
            ITransactionProcessor processor1 = new MobileMoneyProcessor();
            ITransactionProcessor processor2 = new BankTransferProcessor();
            ITransactionProcessor processor3 = new CryptoWalletProcessor();

            // Process each transaction (prints processing messages)
            processor1.Process(t1); // MobileMoneyProcessor → Transaction 1
            processor2.Process(t2); // BankTransferProcessor → Transaction 2
            processor3.Process(t3); // CryptoWalletProcessor → Transaction 3
            Console.WriteLine();

            // iv. Apply each transaction to the SavingsAccount using ApplyTransaction
            account.ApplyTransaction(t1); // should succeed
            account.ApplyTransaction(t2); // should succeed or fail depending on remaining balance
            account.ApplyTransaction(t3); // may print "Insufficient funds" if funds are low
            Console.WriteLine();

            // v. Add all transactions to _transactions
            _transactions.Add(t1);
            _transactions.Add(t2);
            _transactions.Add(t3);

            // Summary
            Console.WriteLine("Transaction log:");
            foreach (var tx in _transactions)
            {
                Console.WriteLine($" - Id: {tx.Id}, Date: {tx.Date:u}, Amount: {tx.Amount:C}, Category: {tx.Category}");
            }

            Console.WriteLine($"\nFinal balance for account {account.AccountNumber}: {account.Balance:C}");
        }
    }

    class Program
    {
        static void Main()
        {
            var app = new FinanceApp();
            app.Run();

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
