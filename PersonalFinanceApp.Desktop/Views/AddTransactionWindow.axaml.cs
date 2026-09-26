using System;
using System.Linq;
using Avalonia.Controls;
using PersonalFinanceApp.Core.Models;

namespace PersonalFinanceApp.Desktop.Views;

public partial class AddTransactionWindow : Window
{
    public TransactionItem? CreatedTransaction { get; private set; }

    public AddTransactionWindow()
    {
        InitializeComponent();
        TypeBox.ItemsSource = Enum.GetNames(typeof(TransactionType));
        TypeBox.SelectedIndex = 1;
        RecurrenceBox.ItemsSource = Enum.GetNames(typeof(RecurrenceUnit));
        RecurrenceBox.SelectedIndex = 0;
        EffectiveFromDatePicker.SelectedDate = DateTimeOffset.Now;
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

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) || !decimal.TryParse(AmountBox.Text, out var amount))
            return;

        Enum.TryParse<TransactionType>(TypeBox.SelectedItem?.ToString(), out var type);
        Enum.TryParse<RecurrenceUnit>(RecurrenceBox.SelectedItem?.ToString(), out var recurrence);
        int.TryParse(IntervalBox.Text, out var interval);

        DateTime fromDate = EffectiveFromDatePicker.SelectedDate?.DateTime ?? DateTime.Today;
        DateTime? toDate = EffectiveToDatePicker.SelectedDate?.DateTime;

        CreatedTransaction = new TransactionItem
        {
            Title = TitleBox.Text,
            Amount = amount,
            Type = type,
            Recurrence = recurrence,
            Interval = Math.Max(1, interval),
            EffectiveFromDate = fromDate,
            EffectiveToDate = toDate
        };

        Close();
    }
}