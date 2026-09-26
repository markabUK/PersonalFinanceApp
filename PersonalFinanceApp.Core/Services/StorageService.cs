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

                // Move legacy file to a backup so it doesn't re-import on subsequent boots
                string backupPath = Path.Combine(folder, "app_state.json.bak");
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(jsonPath, backupPath);
            }
            catch
            {
                // Fallback gracefully if migration encounters malformed data
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
        
        foreach (var account in state.Accounts)
        {
            var existingAcc = db.Accounts
                .Include(a => a.Transactions)
                    .ThenInclude(t => t.OverrideEntities)
                .FirstOrDefault(a => a.Id == account.Id);

            if (existingAcc == null)
            {
                db.Accounts.Add(account);
            }
            else
            {
                existingAcc.Name = account.Name;
                existingAcc.Type = account.Type;
                existingAcc.StartingBalance = account.StartingBalance;
                existingAcc.StartingBalanceDate = account.StartingBalanceDate;
                existingAcc.PayCycleStartDay = account.PayCycleStartDay;

                var incomingTxIds = account.Transactions.Select(t => t.Id).ToList();
                var toRemove = existingAcc.Transactions.Where(t => !incomingTxIds.Contains(t.Id)).ToList();
                db.Transactions.RemoveRange(toRemove);

                foreach (var tx in account.Transactions)
                {
                    var existingTx = existingAcc.Transactions.FirstOrDefault(t => t.Id == tx.Id);
                    if (existingTx == null)
                    {
                        tx.AccountId = account.Id;
                        existingAcc.Transactions.Add(tx);
                    }
                    else
                    {
                        existingTx.Title = tx.Title;
                        existingTx.Amount = tx.Amount;
                        existingTx.EffectiveFromDate = tx.EffectiveFromDate;
                        existingTx.EffectiveToDate = tx.EffectiveToDate;
                        existingTx.Type = tx.Type;
                        existingTx.Recurrence = tx.Recurrence;
                        existingTx.Interval = tx.Interval;

                        db.TransactionOverrides.RemoveRange(existingTx.OverrideEntities);
                        existingTx.OverrideEntities = tx.OverrideEntities.Select(o => new TransactionOverride
                        {
                            Id = o.Id,
                            TransactionItemId = existingTx.Id,
                            MonthKey = o.MonthKey,
                            Amount = o.Amount
                        }).ToList();
                    }
                }
            }
        }

        var incomingAccIds = state.Accounts.Select(a => a.Id).ToList();
        var accountsToRemove = db.Accounts.Where(a => !incomingAccIds.Contains(a.Id)).ToList();
        db.Accounts.RemoveRange(accountsToRemove);

        db.SaveChanges();
    }
}