using System;
using System.Collections.Generic;

namespace PersonalFinanceApp.Core.Models;

public enum AccountType
{
    Current,
    Savings
}

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal StartingBalance { get; set; }
    public DateTime StartingBalanceDate { get; set; } = DateTime.Today;
    public int PayCycleStartDay { get; set; } = 1; 
    public List<TransactionItem> Transactions { get; set; } = new();
}