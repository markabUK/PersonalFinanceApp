namespace PersonalFinanceApp.Core.Models;

public class CategoryBreakdownItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
    public double RemainingPercentage => Math.Max(0, 100 - Percentage);
    public string ColorHex { get; set; } = "#8A2BE2";
}