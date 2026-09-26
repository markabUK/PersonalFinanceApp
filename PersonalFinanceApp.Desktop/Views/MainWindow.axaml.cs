using Avalonia.Controls;
using PersonalFinanceApp.Core.Models;
using PersonalFinanceApp.Desktop.ViewModels;

namespace PersonalFinanceApp.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenAccountWizard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var wizard = new AccountWizardWindow();
            await wizard.ShowDialog(this);

            if (wizard.CreatedAccount != null)
            {
                vm.AddAccount(wizard.CreatedAccount);
            }
        }
    }

    private async void OnOpenEditAccount(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.ActiveAccount != null)
        {
            var wizard = new AccountWizardWindow(vm.ActiveAccount);
            await wizard.ShowDialog(this);
            vm.UpdateActiveAccountSettings();
        }
    }

    private async void OnOpenAddTransaction(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var dialog = new AddTransactionWindow();
            await dialog.ShowDialog(this);

            if (dialog.CreatedTransaction != null)
            {
                vm.AddTransaction(dialog.CreatedTransaction);
            }
        }
    }

    private async void OnOpenEditTransaction(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dataGrid = this.FindControl<DataGrid>("TransactionsGrid");
        if (dataGrid?.SelectedItem is TransactionItem selectedItem && DataContext is MainViewModel vm)
        {
            var dialog = new EditTransactionWindow(selectedItem, selectedItem.EffectiveFromDate);
            await dialog.ShowDialog(this);

            if (dialog.IsDeleted)
            {
                vm.DeleteTransaction(selectedItem);
            }
            else
            {
                vm.UpdateTransaction(selectedItem);
            }
        }
    }
}