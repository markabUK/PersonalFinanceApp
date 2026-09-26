using Avalonia.Controls;
using PersonalFinanceApp.Core.Models;
using System;
using System.Linq;

namespace PersonalFinanceApp.Desktop.Views;

public partial class AccountWizardWindow : Window
{
    public Account? CreatedAccount { get; private set; }
    private readonly Account? _editingAccount;

    public AccountWizardWindow()
    {
        InitializeComponent();
        TypeBox.ItemsSource = Enum.GetNames(typeof(AccountType));
        TypeBox.SelectedIndex = 0;
        BalanceDatePicker.SelectedDate = DateTimeOffset.Now;
    }

    public AccountWizardWindow(Account accountToEdit) : this()
    {
        _editingAccount = accountToEdit;
        NameBox.Text = _editingAccount.Name;
        TypeBox.SelectedItem = _editingAccount.Type.ToString();
        BalanceBox.Text = _editingAccount.StartingBalance.ToString();
        PayDayBox.Text = _editingAccount.PayCycleStartDay.ToString();
        BalanceDatePicker.SelectedDate = new DateTimeOffset(_editingAccount.StartingBalanceDate);
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || !decimal.TryParse(BalanceBox.Text, out var balance))
            return;

        Enum.TryParse<AccountType>(TypeBox.SelectedItem?.ToString(), out var type);
        int.TryParse(PayDayBox.Text, out var payDay);
        DateTime balanceDate = BalanceDatePicker.SelectedDate?.DateTime ?? DateTime.Today;

        if (_editingAccount != null)
        {
            _editingAccount.Name = NameBox.Text;
            _editingAccount.Type = type;
            _editingAccount.StartingBalance = balance;
            _editingAccount.StartingBalanceDate = balanceDate;
            _editingAccount.PayCycleStartDay = Math.Clamp(payDay, 1, 28);
            CreatedAccount = _editingAccount;
        }
        else
        {
            CreatedAccount = new Account
            {
                Name = NameBox.Text,
                Type = type,
                StartingBalance = type == AccountType.Current ? balance : 0,
                StartingBalanceDate = balanceDate,
                PayCycleStartDay = Math.Clamp(payDay, 1, 28)
            };

            if (type == AccountType.Savings)
            {
                CreatedAccount.Transactions.Add(new TransactionItem
                {
                    Title = "Opening Balance",
                    Amount = balance,
                    EffectiveFromDate = balanceDate,
                    Type = TransactionType.Income
                });
            }
        }

        Close();
    }
}