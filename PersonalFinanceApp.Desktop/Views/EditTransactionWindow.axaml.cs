using Avalonia.Controls;
using PersonalFinanceApp.Core.Models;
using System;
using System.Linq;

namespace PersonalFinanceApp.Desktop.Views;

public partial class EditTransactionWindow : Window
{
    private readonly TransactionItem? _transaction;
    public bool IsDeleted { get; private set; }

    public EditTransactionWindow()
    {
        InitializeComponent();
    }

    public EditTransactionWindow(TransactionItem transaction, DateTime currentPeriodDate) : this()
    {
        _transaction = transaction;

        // Populate dropdowns using your actual control names (e.g., TypeCombo, RecurrenceCombo)
        if (this.FindControl<ComboBox>("TypeCombo") is { } typeCombo)
        {
            typeCombo.ItemsSource = Enum.GetNames(typeof(TransactionType));
            typeCombo.SelectedItem = _transaction.Type.ToString();
        }

        if (this.FindControl<ComboBox>("RecurrenceCombo") is { } recCombo)
        {
            recCombo.ItemsSource = Enum.GetNames(typeof(RecurrenceUnit));
            recCombo.SelectedItem = _transaction.Recurrence.ToString();
        }

        if (this.FindControl<TextBox>("TitleBox") is { } titleBox)
            titleBox.Text = _transaction.Title;

        string monthKey = currentPeriodDate.ToString("yyyy-MM");
        decimal effectiveAmount = _transaction.MonthlyAmountOverrides.TryGetValue(monthKey, out var overridden) 
            ? overridden 
            : _transaction.Amount;

        if (this.FindControl<TextBox>("AmountBox") is { } amountBox)
            amountBox.Text = effectiveAmount.ToString("0.00");

        if (this.FindControl<TextBox>("IntervalBox") is { } intervalBox)
            intervalBox.Text = _transaction.Interval.ToString();

        if (this.FindControl<DatePicker>("EffectiveFromDatePicker") is { } fromPicker)
            fromPicker.SelectedDate = new DateTimeOffset(_transaction.EffectiveFromDate);

        if (this.FindControl<DatePicker>("EffectiveToDatePicker") is { } toPicker)
            toPicker.SelectedDate = _transaction.EffectiveToDate.HasValue ? new DateTimeOffset(_transaction.EffectiveToDate.Value) : null;
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_transaction == null) return;

        var titleBox = this.FindControl<TextBox>("TitleBox");
        var amountBox = this.FindControl<TextBox>("AmountBox");
        var typeCombo = this.FindControl<ComboBox>("TypeCombo");
        var recCombo = this.FindControl<ComboBox>("RecurrenceCombo");
        var intervalBox = this.FindControl<TextBox>("IntervalBox");
        var fromPicker = this.FindControl<DatePicker>("EffectiveFromDatePicker");
        var toPicker = this.FindControl<DatePicker>("EffectiveToDatePicker");

        if (string.IsNullOrWhiteSpace(titleBox?.Text) || !decimal.TryParse(amountBox?.Text, out var amount))
            return;

        Enum.TryParse<TransactionType>(typeCombo?.SelectedItem?.ToString(), out var type);
        Enum.TryParse<RecurrenceUnit>(recCombo?.SelectedItem?.ToString(), out var recurrence);
        int.TryParse(intervalBox?.Text, out var interval);

        _transaction.Title = titleBox.Text.Trim();
        _transaction.Amount = amount;
        _transaction.Type = type;
        _transaction.Recurrence = recurrence;
        _transaction.Interval = Math.Max(1, interval);
        _transaction.EffectiveFromDate = fromPicker?.SelectedDate?.DateTime ?? _transaction.EffectiveFromDate;
        _transaction.EffectiveToDate = toPicker?.SelectedDate?.DateTime;

        Close();
    }

    private void OnDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsDeleted = true;
        Close();
    }

    private void OnCurrencyTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text ?? string.Empty;
            string filtered = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());

            int firstDecimal = filtered.IndexOf('.');
            if (firstDecimal != -1)
            {
                var pre = filtered.Substring(0, firstDecimal + 1);
                var post = filtered.Substring(firstDecimal + 1).Replace(".", "");
                filtered = pre + post;
            }

            if (filtered != text)
            {
                textBox.Text = filtered;
                textBox.CaretIndex = filtered.Length;
            }
        }
    }
}