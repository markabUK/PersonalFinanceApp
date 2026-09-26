using System;
using System.Collections.Generic;

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
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    
    // Dynamically categorized based on existing properties
    public string Category 
    {
        get
        {
            if (Type == TransactionType.PayInX) return "Pay in X";
            if (Recurrence == RecurrenceUnit.OneTime) return "One-off Payments";
            return Title; // Grouped by name for recurring items
        }
    }
    
    public DateTime EffectiveFromDate { get; set; } = DateTime.Today;
    public DateTime? EffectiveToDate { get; set; } 

    public TransactionType Type { get; set; }
    public RecurrenceUnit Recurrence { get; set; } = RecurrenceUnit.OneTime;
    public int Interval { get; set; } = 1; 

    public Dictionary<string, decimal> MonthlyAmountOverrides { get; set; } = new();

    public int TotalInstallments { get; set; } = 1;
    public int PaidInstallments { get; set; } = 0;
}