using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PersonalFinanceApp.Core.Services;

public class StorageService
{
    private static string GetAppDataFolder()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PersonalFinanceApp"
        );
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static AppState LoadState()
    {
        using var db = new FinanceDbContext();
        db.Database.EnsureCreated();

        string folder = GetAppDataFolder();
        string jsonPath = Path.Combine(folder, "app_state.json");

        // Migrate legacy JSON data into SQLite if it exists and database is currently empty
        if (File.Exists(jsonPath) && !db.Accounts.Any())
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var legacyState = JsonSerializer.Deserialize<AppState>(jsonContent);

                if (legacyState != null && legacyState.Accounts.Any())
                {
                    foreach (var acc in legacyState.Accounts)
                    {
                        db.Accounts.Add(acc);
                    }
                    db.SaveChanges();
                }

                string backupPath = Path.Combine(folder, "app_state.json.bak");
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(jsonPath, backupPath);
            }
            catch
            {
                // Fallback gracefully if migration fails
            }
        }

        var accounts = db.Accounts
            .Include(a => a.Transactions)
                .ThenInclude(t => t.OverrideEntities)
            .ToList();

        return new AppState
        {
            Accounts = accounts,
            FilterYear = DateTime.Today.Year,
            FilterMonth = DateTime.Today.Month
        };
    }

    public static void SaveState(AppState state)
    {
        using var db = new FinanceDbContext();
        db.Database.EnsureCreated();

        // Load existing database state to identify deletions
        var existingAccountIds = db.Accounts.Select(a => a.Id).ToList();
        var incomingAccountIds = state.Accounts.Select(a => a.Id).ToList();

        // 1. Remove accounts deleted in UI
        var accountsToRemove = db.Accounts.Where(a => !incomingAccountIds.Contains(a.Id)).ToList();
        if (accountsToRemove.Any())
        {
            db.Accounts.RemoveRange(accountsToRemove);
        }

        // 2. Synchronize each account using EntityState tracking
        foreach (var account in state.Accounts)
        {
            if (account.Id == Guid.Empty) account.Id = Guid.NewGuid();

            var dbAccount = db.Accounts
                .Include(a => a.Transactions)
                    .ThenInclude(t => t.OverrideEntities)
                .FirstOrDefault(a => a.Id == account.Id);

            if (dbAccount == null)
            {
                // New Account
                db.Accounts.Add(account);
            }
            else
            {
                // Update Account metadata
                dbAccount.Name = account.Name;
                dbAccount.Type = account.Type;
                dbAccount.StartingBalance = account.StartingBalance;
                dbAccount.StartingBalanceDate = account.StartingBalanceDate;
                dbAccount.PayCycleStartDay = account.PayCycleStartDay;

                var incomingTxIds = account.Transactions.Select(t => t.Id).ToList();

                // Remove deleted transactions for this account
                var txsToRemove = dbAccount.Transactions.Where(t => !incomingTxIds.Contains(t.Id)).ToList();
                if (txsToRemove.Any())
                {
                    db.Transactions.RemoveRange(txsToRemove);
                }

                // Update or Add transactions
                foreach (var tx in account.Transactions)
                {
                    if (tx.Id == Guid.Empty) tx.Id = Guid.NewGuid();
                    tx.AccountId = dbAccount.Id;

                    var dbTx = dbAccount.Transactions.FirstOrDefault(t => t.Id == tx.Id);

                    if (dbTx == null)
                    {
                        // New Transaction
                        db.Transactions.Add(tx);
                    }
                    else
                    {
                        // Update Existing Transaction
                        dbTx.Title = tx.Title;
                        dbTx.Amount = tx.Amount;
                        dbTx.EffectiveFromDate = tx.EffectiveFromDate;
                        dbTx.EffectiveToDate = tx.EffectiveToDate;
                        dbTx.Type = tx.Type;
                        dbTx.Recurrence = tx.Recurrence;
                        dbTx.Interval = tx.Interval;

                        // Reconcile Overrides safely
                        db.TransactionOverrides.RemoveRange(dbTx.OverrideEntities);
                        foreach (var ov in tx.OverrideEntities)
                        {
                            if (ov.Id == Guid.Empty) ov.Id = Guid.NewGuid();
                            ov.TransactionItemId = dbTx.Id;
                            db.TransactionOverrides.Add(ov);
                        }
                    }
                }
            }
        }

        db.SaveChanges();
    }
}