using Avalonia.Controls;
using System.Linq;

namespace PersonalFinanceApp.Desktop.Views;

public partial class ExportDialog : Window
{
    public int ForecastMonths { get; private set; } = 12;

    public ExportDialog()
    {
        InitializeComponent();

        // Bind text changed event to strictly filter out non-digit characters
        if (this.FindControl<TextBox>("MonthsBox") is { } box)
        {
            box.TextChanged += OnMonthsTextChanged;
        }
    }

    private void OnMonthsTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text ?? string.Empty;
            // Keep only numeric characters
            string filtered = new string(text.Where(char.IsDigit).ToArray());

            if (filtered != text)
            {
                textBox.Text = filtered;
                textBox.CaretIndex = filtered.Length;
            }
        }
    }

    private void OnExportClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("MonthsBox") is { } box && int.TryParse(box.Text, out var months) && months > 0)
        {
            ForecastMonths = System.Math.Clamp(months, 1, 120); // Max 10 years forward
        }
        else
        {
            ForecastMonths = 12; // Safe fallback if empty
        }
        Close(true);
    }

    private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}