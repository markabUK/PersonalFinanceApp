namespace PersonalFinanceApp.Core.Models;

public class SavingsAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal TargetGoal { get; set; }
}