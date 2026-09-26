using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Core.Models;
using System;
using System.IO;

namespace PersonalFinanceApp.Core.Services;

internal class FinanceDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TransactionItem> Transactions => Set<TransactionItem>();
    public DbSet<TransactionOverride> TransactionOverrides => Set<TransactionOverride>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PersonalFinanceApp"
        );
        Directory.CreateDirectory(folder);
        string dbPath = Path.Combine(folder, "finance.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>()
            .HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransactionItem>()
            .HasMany(t => t.OverrideEntities)
            .WithOne()
            .HasForeignKey(o => o.TransactionItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}