using Avalonia.Controls;
using PersonalFinanceApp.Core.Models;
using System;
using System.Linq;

namespace PersonalFinanceApp.Desktop.Views;

public partial class EditTransactionWindow : Window
{
    private readonly TransactionItem? _item;
    private readonly DateTime? _instanceDate;
    public bool IsDeleted { get; private set; } = false;

    public EditTransactionWindow()
    {
        InitializeComponent();
    }

    public EditTransactionWindow(TransactionItem item, DateTime? instanceDate = null) : this()
    {
        _item = item;
        _instanceDate = instanceDate;

        TitleBox.Text = _item.Title;
        EffectiveFromDatePicker.SelectedDate = new DateTimeOffset(_item.EffectiveFromDate);
        if (_item.EffectiveToDate.HasValue)
        {
            EffectiveToDatePicker.SelectedDate = new DateTimeOffset(_item.EffectiveToDate.Value);
        }

        if (_instanceDate.HasValue && _item.Recurrence != RecurrenceUnit.OneTime)
        {
            string monthKey = _instanceDate.Value.ToString("yyyy-MM");
            OverrideTargetText.Text = $"Target Month: {_instanceDate.Value:MMMM yyyy}";

            if (_item.MonthlyAmountOverrides.TryGetValue(monthKey, out var overriddenVal))
            {
                AmountBox.Text = overriddenVal.ToString();
                OverrideCheckBox.IsChecked = true;
            }
            else
            {
                AmountBox.Text = _item.Amount.ToString();
            }
        }
        else
        {
            AmountBox.Text = _item.Amount.ToString();
            OverrideCheckBox.IsVisible = false;
            OverrideTargetText.IsVisible = false;
        }
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
        if (_item != null && decimal.TryParse(AmountBox.Text, out var amount))
        {
            _item.Title = TitleBox.Text ?? string.Empty;
            _item.EffectiveFromDate = EffectiveFromDatePicker.SelectedDate?.DateTime ?? _item.EffectiveFromDate;
            _item.EffectiveToDate = EffectiveToDatePicker.SelectedDate?.DateTime;

            if (_instanceDate.HasValue && _item.Recurrence != RecurrenceUnit.OneTime && (OverrideCheckBox.IsChecked == true))
            {
                string monthKey = _instanceDate.Value.ToString("yyyy-MM");
                _item.MonthlyAmountOverrides[monthKey] = amount;
            }
            else
            {
                _item.Amount = amount;
                if (_instanceDate.HasValue)
                {
                    string monthKey = _instanceDate.Value.ToString("yyyy-MM");
                    _item.MonthlyAmountOverrides.Remove(monthKey);
                }
            }
        }
        Close();
    }

    private void OnDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsDeleted = true;
        Close();
    }
}