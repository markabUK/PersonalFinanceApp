using System;

namespace PersonalFinanceApp.Core.Models;

public class TransactionOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionItemId { get; set; }
    public string MonthKey { get; set; } = string.Empty; // "yyyy-MM"
    public decimal Amount { get; set; }
}