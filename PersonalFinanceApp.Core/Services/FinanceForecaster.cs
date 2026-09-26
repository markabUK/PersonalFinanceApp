using PersonalFinanceApp.Core.Models;

namespace PersonalFinanceApp.Core.Services;

public class FinanceForecaster
{
    public static List<TransactionItem> ExpandTransactionsForPeriod(IEnumerable<TransactionItem> masterList, DateTime startDate, DateTime endDate)
    {
        var expandedList = new List<TransactionItem>();

        foreach (var item in masterList)
        {
            if (item.Recurrence == RecurrenceUnit.OneTime)
            {
                if (item.EffectiveFromDate >= startDate && item.EffectiveFromDate <= endDate)
                {
                    expandedList.Add(item);
                }
            }
            else
            {
                var currentDate = item.EffectiveFromDate;
                int safeInterval = Math.Max(1, item.Interval);

                if (item.EffectiveFromDate > endDate) continue;

                while (currentDate < startDate)
                {
                    if (item.EffectiveToDate.HasValue && currentDate > item.EffectiveToDate.Value)
                        break;

                    currentDate = AddInterval(currentDate, item.Recurrence, safeInterval);
                }

                while (currentDate <= endDate)
                {
                    if (item.EffectiveToDate.HasValue && currentDate > item.EffectiveToDate.Value)
                        break;

                    if (currentDate >= item.EffectiveFromDate)
                    {
                        string monthKey = currentDate.ToString("yyyy-MM");
                        decimal finalAmount = item.MonthlyAmountOverrides.TryGetValue(monthKey, out var overriddenAmount) 
                            ? overriddenAmount 
                            : item.Amount;

                        bool isOverridden = item.MonthlyAmountOverrides.ContainsKey(monthKey);

                        expandedList.Add(new TransactionItem
                        {
                            Id = item.Id,
                            Title = isOverridden ? $"{item.Title} (Overridden)" : (item.EffectiveToDate.HasValue ? $"{item.Title} (Scheduled End)" : item.Title),
                            Amount = finalAmount,
                            EffectiveFromDate = currentDate,
                            Type = item.Type,
                            Recurrence = item.Recurrence,
                            Interval = safeInterval,
                            EffectiveToDate = item.EffectiveToDate,
                            MonthlyAmountOverrides = item.MonthlyAmountOverrides
                        });
                    }

                    currentDate = AddInterval(currentDate, item.Recurrence, safeInterval);
                }
            }
        }

        return expandedList.OrderBy(t => t.EffectiveFromDate).ToList();
    }

    public static DateTime GetCycleStartForDate(DateTime date, int payCycleStartDay)
    {
        int day = Math.Clamp(payCycleStartDay, 1, 28);
        if (date.Day >= day)
        {
            return new DateTime(date.Year, date.Month, day);
        }
        else
        {
            return new DateTime(date.Year, date.Month, day).AddMonths(-1);
        }
    }

    public static decimal GetRollingStartingBalance(Account account, int targetYear, int targetMonth)
    {
        int payDay = Math.Clamp(account.PayCycleStartDay, 1, 28);
        var targetStart = new DateTime(targetYear, targetMonth, payDay);
        
        var baseCycleStart = GetCycleStartForDate(account.StartingBalanceDate, payDay);

        if (targetStart <= baseCycleStart)
        {
            return account.StartingBalance;
        }

        decimal rollingBalance = account.StartingBalance;
        var iterationStart = baseCycleStart;

        while (iterationStart < targetStart)
        {
            var iterationEnd = iterationStart.AddMonths(1).AddDays(-1);
            var periodTransactions = ExpandTransactionsForPeriod(account.Transactions, iterationStart, iterationEnd);

            // On the starting balance cycle, only count transactions occurring on or after the StartingBalanceDate
            IEnumerable<TransactionItem> activeTrans = periodTransactions;
            if (iterationStart == baseCycleStart)
            {
                activeTrans = periodTransactions.Where(t => t.EffectiveFromDate.Date >= account.StartingBalanceDate.Date);
            }

            decimal income = activeTrans.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal outflow = activeTrans.Where(t => t.Type != TransactionType.Income).Sum(t => t.Amount);

            rollingBalance += (income - outflow);
            iterationStart = iterationStart.AddMonths(1);
        }

        return rollingBalance;
    }

    public static decimal CalculateProjectedBalance(decimal startingBalance, IEnumerable<TransactionItem> periodTransactions, DateTime periodStartDate, DateTime periodEndDate)
    {
        var today = DateTime.Today;
        IEnumerable<TransactionItem> relevantTransactions;

        if (periodEndDate < today)
        {
            relevantTransactions = periodTransactions;
        }
        else if (periodStartDate <= today && periodEndDate >= today)
        {
            relevantTransactions = periodTransactions.Where(t => t.EffectiveFromDate.Date >= today.Date);
        }
        else
        {
            relevantTransactions = periodTransactions;
        }

        decimal totalIncome = relevantTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        decimal totalOutflow = relevantTransactions.Where(t => t.Type != TransactionType.Income).Sum(t => t.Amount);

        return startingBalance + totalIncome - totalOutflow;
    }

    private static DateTime AddInterval(DateTime date, RecurrenceUnit unit, int interval)
    {
        return unit switch
        {
            RecurrenceUnit.Days => date.AddDays(interval),
            RecurrenceUnit.Weeks => date.AddDays(7 * interval),
            RecurrenceUnit.Months => date.AddMonths(interval),
            RecurrenceUnit.Years => date.AddYears(interval),
            _ => date.AddYears(100)
        };
    }
}