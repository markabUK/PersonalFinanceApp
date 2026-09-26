using System;
using System.Collections.Generic;

namespace PersonalFinanceApp.Core.Models;

public class AppState
{
    public List<Account> Accounts { get; set; } = new();
    public int FilterYear { get; set; } = DateTime.Today.Year;
    public int FilterMonth { get; set; } = DateTime.Today.Month;
}