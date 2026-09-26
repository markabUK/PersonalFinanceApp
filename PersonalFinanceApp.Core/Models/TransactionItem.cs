using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PersonalFinanceApp.Core.Models;

public enum TransactionType
{
    Income,
    Expense,
    SavingTransfer,
    PayInX
}

public enum RecurrenceUnit
{
    OneTime,
    Days,
    Weeks,
    Months,
    Years
}

public class TransactionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    
    // Category property bound by the Desktop DataGrid
    public string Category 
    {
        get
        {
            if (Type == TransactionType.PayInX) return "Pay in X";
            if (Recurrence == RecurrenceUnit.OneTime) return "One-off Payments";
            return Title;
        }
    }
    
    public DateTime EffectiveFromDate { get; set; } = DateTime.Today;
    public DateTime? EffectiveToDate { get; set; } 

    public TransactionType Type { get; set; }
    public RecurrenceUnit Recurrence { get; set; } = RecurrenceUnit.OneTime;
    public int Interval { get; set; } = 1; 

    // Relational backing store for EF Core inside Core layer
    public List<TransactionOverride> OverrideEntities { get; set; } = new();

    [NotMapped]
    public Dictionary<string, decimal> MonthlyAmountOverrides
    {
        get => OverrideEntities.ToDictionary(o => o.MonthKey, o => o.Amount);
        set
        {
            OverrideEntities.Clear();
            if (value != null)
            {
                foreach (var kvp in value)
                {
                    OverrideEntities.Add(new TransactionOverride
                    {
                        TransactionItemId = Id,
                        MonthKey = kvp.Key,
                        Amount = kvp.Value
                    });
                }
            }
        }
    }

    public int TotalInstallments { get; set; } = 1;
    public int PaidInstallments { get; set; } = 0;
}