using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinanceApp.Core.Models;
using PersonalFinanceApp.Core.Services;

namespace PersonalFinanceApp.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private AppState _state;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveAccount))]
    private Account? _activeAccount;

    public bool HasActiveAccount => ActiveAccount != null;

    [ObservableProperty]
    private string _activeViewTitle = "Select or Add an Account";

    [ObservableProperty]
    private decimal _displayedBalance;

    [ObservableProperty]
    private decimal _projectedEndBalance;

    [ObservableProperty]
    private decimal _periodIncome;

    [ObservableProperty]
    private decimal _periodExpenses;

    [ObservableProperty]
    private decimal _oneOffExpenses;

    [ObservableProperty]
    private decimal _payInXExpenses;

    [ObservableProperty]
    private double _incomeBarWidth = 150;

    [ObservableProperty]
    private double _expenseBarWidth = 150;

    [ObservableProperty]
    private string _currentMonthDisplay = string.Empty;

    [ObservableProperty]
    private string _projectedEndDateDisplay = string.Empty;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<TransactionItem> FilteredTransactions { get; } = new();

    public MainViewModel(AppState state)
    {
        _state = state;

        foreach (var acc in _state.Accounts)
        {
            Accounts.Add(acc);
        }

        if (Accounts.Count > 0)
        {
            SwitchAccount(Accounts[0]);
        }
        else
        {
            ActiveViewTitle = "No Accounts Configured";
        }

        UpdateMonthDisplay();
    }

    [RelayCommand]
    private void SwitchAccount(Account? account)
    {
        if (account == null) return;
        ActiveAccount = account;
        ActiveViewTitle = $"{account.Type}: {account.Name}";
        RefreshActiveView();
        UpdateMonthDisplay();
    }

    public void AddAccount(Account account)
    {
        _state.Accounts.Add(account);
        Accounts.Add(account);
        StorageService.SaveState(_state);
        SwitchAccount(account);
    }

    public void UpdateActiveAccountSettings()
    {
        if (ActiveAccount == null) return;
        StorageService.SaveState(_state);
        ActiveViewTitle = $"{ActiveAccount.Type}: {ActiveAccount.Name}";
        RefreshActiveView();
        UpdateMonthDisplay();
    }

    [RelayCommand]
    private void DeleteActiveAccount()
    {
        if (ActiveAccount == null) return;

        _state.Accounts.Remove(ActiveAccount);
        Accounts.Remove(ActiveAccount);
        StorageService.SaveState(_state);

        if (Accounts.Count > 0)
        {
            SwitchAccount(Accounts[0]);
        }
        else
        {
            ActiveAccount = null;
            ActiveViewTitle = "No Accounts Configured";
            FilteredTransactions.Clear();
            DisplayedBalance = 0;
            ProjectedEndBalance = 0;
            PeriodIncome = 0;
            PeriodExpenses = 0;
            OneOffExpenses = 0;
            PayInXExpenses = 0;
        }
    }

    public void RefreshActiveView()
    {
        if (ActiveAccount == null) return;

        var (startDate, endDate) = GetCurrentPayPeriodDates();
        FilteredTransactions.Clear();

        if (ActiveAccount.Type == AccountType.Current)
        {
            DisplayedBalance = FinanceForecaster.GetRollingStartingBalance(ActiveAccount, _state.FilterYear, _state.FilterMonth);

            var expanded = FinanceForecaster.ExpandTransactionsForPeriod(ActiveAccount.Transactions, startDate, endDate);
            foreach (var item in expanded)
            {
                FilteredTransactions.Add(item);
            }

            PeriodIncome = FilteredTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            PeriodExpenses = FilteredTransactions.Where(t => t.Type != TransactionType.Income).Sum(t => t.Amount);
            OneOffExpenses = FilteredTransactions.Where(t => t.Recurrence == RecurrenceUnit.OneTime && t.Type != TransactionType.Income).Sum(t => t.Amount);
            PayInXExpenses = FilteredTransactions.Where(t => t.Type == TransactionType.PayInX).Sum(t => t.Amount);

            decimal totalFlow = PeriodIncome + PeriodExpenses;
            if (totalFlow > 0)
            {
                IncomeBarWidth = (double)(PeriodIncome / totalFlow) * 300;
                ExpenseBarWidth = (double)(PeriodExpenses / totalFlow) * 300;
            }
            else
            {
                IncomeBarWidth = 150;
                ExpenseBarWidth = 150;
            }

            ProjectedEndBalance = FinanceForecaster.CalculateProjectedBalance(DisplayedBalance, FilteredTransactions, startDate, endDate);
        }
        else
        {
            var matching = ActiveAccount.Transactions.Where(t => t.EffectiveFromDate >= startDate && t.EffectiveFromDate <= endDate);
            foreach (var item in matching)
            {
                FilteredTransactions.Add(item);
            }

            PeriodIncome = matching.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            PeriodExpenses = matching.Where(t => t.Type != TransactionType.Income).Sum(t => t.Amount);
            OneOffExpenses = matching.Where(t => t.Recurrence == RecurrenceUnit.OneTime && t.Type != TransactionType.Income).Sum(t => t.Amount);
            PayInXExpenses = matching.Where(t => t.Type == TransactionType.PayInX).Sum(t => t.Amount);
            IncomeBarWidth = 150;
            ExpenseBarWidth = 150;

            DisplayedBalance = ActiveAccount.Transactions.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);
            ProjectedEndBalance = DisplayedBalance;
        }
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        var (startDate, _) = GetCurrentPayPeriodDates();
        var prevDate = startDate.AddMonths(-1);
        _state.FilterYear = prevDate.Year;
        _state.FilterMonth = prevDate.Month;
        UpdateMonthDisplay();
        RefreshActiveView();
    }

    [RelayCommand]
    private void NextMonth()
    {
        var (startDate, _) = GetCurrentPayPeriodDates();
        var nextDate = startDate.AddMonths(1);
        _state.FilterYear = nextDate.Year;
        _state.FilterMonth = nextDate.Month;
        UpdateMonthDisplay();
        RefreshActiveView();
    }

    [RelayCommand]
    private void ResetToCurrentMonth()
    {
        var today = DateTime.Today;
        int payDay = ActiveAccount?.PayCycleStartDay ?? 1;
        int targetYear = today.Year;
        int targetMonth = today.Month;

        if (payDay > 1 && today.Day < payDay)
        {
            var adjusted = today.AddMonths(-1);
            targetYear = adjusted.Year;
            targetMonth = adjusted.Month;
        }

        _state.FilterYear = targetYear;
        _state.FilterMonth = targetMonth;
        UpdateMonthDisplay();
        RefreshActiveView();
    }

    public void AddTransaction(TransactionItem item)
    {
        if (ActiveAccount == null) return;

        ActiveAccount.Transactions.Add(item);
        StorageService.SaveState(_state);
        RefreshActiveView();
    }

    public void UpdateTransaction(TransactionItem item)
    {
        StorageService.SaveState(_state);
        RefreshActiveView();
    }

    [RelayCommand]
    public void DeleteTransaction(TransactionItem? item)
    {
        if (ActiveAccount == null || item == null) return;
        var masterItem = ActiveAccount.Transactions.FirstOrDefault(t => t.Id == item.Id);
        if (masterItem != null)
        {
            ActiveAccount.Transactions.Remove(masterItem);
            StorageService.SaveState(_state);
            RefreshActiveView();
        }
    }

    private (DateTime start, DateTime end) GetCurrentPayPeriodDates()
    {
        int day = ActiveAccount != null ? Math.Clamp(ActiveAccount.PayCycleStartDay, 1, 28) : 1;
        
        try
        {
            var start = new DateTime(_state.FilterYear, _state.FilterMonth, day);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }
        catch
        {
            var start = new DateTime(_state.FilterYear, _state.FilterMonth, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }
    }

    public void UpdateMonthDisplay()
    {
        var (startDate, endDate) = GetCurrentPayPeriodDates();

        CurrentMonthDisplay = $"{startDate:dd MMM} - {endDate:dd MMM yyyy}";
        ProjectedEndDateDisplay = endDate.ToString("dd MMMM yyyy");
    }
}