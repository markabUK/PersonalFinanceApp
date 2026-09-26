using ClosedXML.Excel;
using PersonalFinanceApp.Core.Models;
using PersonalFinanceApp.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalFinanceApp.Desktop.Services;

public static class ExcelExportService
{
    public static void ExportForecastToExcel(Account account, int forecastMonths, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Budget Forecast");

        // Ensure gridlines are visible
        ws.ShowGridLines = true;

        // Styling Palette (Professional Navy Theme)
        var navyHeaderFill = XLColor.FromHtml("#1F4E78");
        var headerFontColor = XLColor.White;
        var subtotalFill = XLColor.FromHtml("#D9E1F2");
        var summaryFill = XLColor.FromHtml("#E2EFDA");

        // Title Block
        ws.Cell("B2").Value = $"Budget Forecast: {account.Name} ({account.Type})";
        ws.Cell("B2").Style.Font.Bold = true;
        ws.Cell("B2").Style.Font.FontSize = 14;
        ws.Cell("B2").Style.Font.FontColor = XLColor.FromHtml("#1F4E78");

        int headerRow = 4;
        ws.Cell(headerRow, 2).Value = "Category / Line Item";
        ws.Cell(headerRow, 3).Value = "Type";
        
        // Month headers showing exact pay period date ranges (e.g. 26 Sept - 25 Oct 2026)
        DateTime startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, Math.Clamp(account.PayCycleStartDay, 1, 28));
        
        // If today is past the pay cycle day, adjust start to current active cycle
        if (DateTime.Today.Day < account.PayCycleStartDay)
        {
            startDate = startDate.AddMonths(-1);
        }

        for (int m = 0; m < forecastMonths; m++)
        {
            var periodStart = startDate.AddMonths(m);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            ws.Cell(headerRow, 4 + m).Value = $"{periodStart:dd MMM} - {periodEnd:dd MMM yyyy}";
        }
        ws.Cell(headerRow, 4 + forecastMonths).Value = "Total";

        // Style Header Row
        var headerRange = ws.Range(headerRow, 2, headerRow, 4 + forecastMonths);
        headerRange.Style.Fill.BackgroundColor = navyHeaderFill;
        headerRange.Style.Font.FontColor = headerFontColor;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int currentRow = headerRow + 1;

        // Separate Income and Expenses
        var incomeTransactions = account.Transactions.Where(t => t.Type == TransactionType.Income).ToList();
        var expenseTransactions = account.Transactions.Where(t => t.Type != TransactionType.Income).ToList();

        // --- 1. INCOME SECTION ---
        int incomeStartRow = currentRow;
        ws.Cell(currentRow, 2).Value = "Income";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        currentRow++;

        foreach (var tx in incomeTransactions)
        {
            WriteTransactionRow(ws, currentRow, tx, forecastMonths, startDate, isExpense: false);
            currentRow++;
        }

        // Income Subtotal Row
        int incomeSubtotalRow = currentRow;
        WriteSubtotalRow(ws, incomeSubtotalRow, "Total Income", incomeStartRow + 1, currentRow - 1, forecastMonths, subtotalFill);
        currentRow += 2;

        // --- 2. EXPENDITURE SECTION ---
        int expenseStartRow = currentRow;
        ws.Cell(currentRow, 2).Value = "Expenditure";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        currentRow++;

        foreach (var tx in expenseTransactions)
        {
            WriteTransactionRow(ws, currentRow, tx, forecastMonths, startDate, isExpense: true);
            currentRow++;
        }

        // Expenditure Subtotal Row
        int expenseSubtotalRow = currentRow;
        WriteSubtotalRow(ws, expenseSubtotalRow, "Total Expenditure", expenseStartRow + 1, currentRow - 1, forecastMonths, subtotalFill);
        currentRow += 2;

        // --- 3. NET MONTHLY CASH FLOW ---
        int netCashFlowRow = currentRow;
        ws.Cell(netCashFlowRow, 2).Value = "Net Monthly Cash Flow";
        ws.Cell(netCashFlowRow, 2).Style.Font.Bold = true;

        for (int m = 0; m < forecastMonths; m++)
        {
            string colLetter = XLSheetExtensions.ColumnLetter(4 + m);
            string incCell = $"{colLetter}{incomeSubtotalRow}";
            string expCell = $"{colLetter}{expenseSubtotalRow}";
            
            ws.Cell(netCashFlowRow, 4 + m).FormulaA1 = $"={incCell}+{expCell}"; // Expenses are stored as negative values
            ws.Cell(netCashFlowRow, 4 + m).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
            ws.Cell(netCashFlowRow, 4 + m).Style.Font.Bold = true;
        }
        string netFirst = XLSheetExtensions.ColumnLetter(4);
        string netLast = XLSheetExtensions.ColumnLetter(3 + forecastMonths);
        ws.Cell(netCashFlowRow, 4 + forecastMonths).FormulaA1 = $"=SUM({netFirst}{netCashFlowRow}:{netLast}{netCashFlowRow})";
        ws.Cell(netCashFlowRow, 4 + forecastMonths).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
        ws.Cell(netCashFlowRow, 4 + forecastMonths).Style.Font.Bold = true;
        
        var netRange = ws.Range(netCashFlowRow, 2, netCashFlowRow, 4 + forecastMonths);
        netRange.Style.Fill.BackgroundColor = subtotalFill;
        netRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        netRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        currentRow += 2;

        // --- 4. CUMULATIVE ENDING BALANCE ---
        int balanceRow = currentRow;
        ws.Cell(balanceRow, 2).Value = "Projected Ending Balance";
        ws.Cell(balanceRow, 2).Style.Font.Bold = true;

        // Calculate exact Month 1 ending balance using the app's forecaster engine to match current state
        var (pStart, pEnd) = GetPayPeriodDates(account, startDate.Year, startDate.Month);
        var expandedFirstMonth = FinanceForecaster.ExpandTransactionsForPeriod(account.Transactions, pStart, pEnd);
        decimal rollingStartFirstMonth = FinanceForecaster.GetRollingStartingBalance(account, pStart.Year, pStart.Month);
        decimal exactMonth1EndingBalance = FinanceForecaster.CalculateProjectedBalance(rollingStartFirstMonth, expandedFirstMonth, pStart, pEnd);

        for (int m = 0; m < forecastMonths; m++)
        {
            string colLetter = XLSheetExtensions.ColumnLetter(4 + m);
            string? prevColLetter = m > 0 ? XLSheetExtensions.ColumnLetter(3 + m) : null;
            
            if (m == 0)
            {
                // Month 1 uses the app's exact calculated ending balance for the active period
                ws.Cell(balanceRow, 4 + m).Value = exactMonth1EndingBalance;
            }
            else
            {
                // Subsequent months correctly roll forward: Previous Month Ending Balance + Current Month Net Cash Flow
                ws.Cell(balanceRow, 4 + m).FormulaA1 = $"={prevColLetter}{balanceRow}+{colLetter}{netCashFlowRow}";
            }

            ws.Cell(balanceRow, 4 + m).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
            ws.Cell(balanceRow, 4 + m).Style.Font.Bold = true;
        }

        ws.Cell(balanceRow, 4 + forecastMonths).Value = "-";
        ws.Cell(balanceRow, 4 + forecastMonths).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var balanceRange = ws.Range(balanceRow, 2, balanceRow, 4 + forecastMonths);
        balanceRange.Style.Fill.BackgroundColor = summaryFill;
        balanceRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        balanceRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        // Auto-fit columns
        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private static (DateTime start, DateTime end) GetPayPeriodDates(Account account, int year, int month)
    {
        int day = Math.Clamp(account.PayCycleStartDay, 1, 28);
        try
        {
            var start = new DateTime(year, month, day);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }
        catch
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }
    }

    private static void WriteTransactionRow(IXLWorksheet ws, int row, TransactionItem tx, int forecastMonths, DateTime startDate, bool isExpense)
    {
        ws.Cell(row, 2).Value = tx.Title;
        ws.Cell(row, 3).Value = tx.Type.ToString();

        for (int m = 0; m < forecastMonths; m++)
        {
            var targetMonth = startDate.AddMonths(m);
            string monthKey = targetMonth.ToString("yyyy-MM");

            decimal amount = tx.MonthlyAmountOverrides.TryGetValue(monthKey, out var overridden)
                ? overridden
                : tx.Amount;

            if (tx.Recurrence == RecurrenceUnit.OneTime)
            {
                if (tx.EffectiveFromDate.Year != targetMonth.Year || tx.EffectiveFromDate.Month != targetMonth.Month)
                {
                    amount = 0;
                }
            }

            // Make expenditures negative values
            if (isExpense && amount > 0)
            {
                amount = -amount;
            }

            var cell = ws.Cell(row, 4 + m);
            cell.Value = amount;
            cell.Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
        }

        string firstCol = XLSheetExtensions.ColumnLetter(4);
        string lastCol = XLSheetExtensions.ColumnLetter(3 + forecastMonths);
        ws.Cell(row, 4 + forecastMonths).FormulaA1 = $"=SUM({firstCol}{row}:{lastCol}{row})";
        ws.Cell(row, 4 + forecastMonths).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
    }

    private static void WriteSubtotalRow(IXLWorksheet ws, int row, string label, int startRow, int endRow, int forecastMonths, XLColor fill)
    {
        ws.Cell(row, 2).Value = label;
        ws.Cell(row, 2).Style.Font.Bold = true;

        for (int m = 0; m < forecastMonths; m++)
        {
            string colLetter = XLSheetExtensions.ColumnLetter(4 + m);
            ws.Cell(row, 4 + m).FormulaA1 = $"=SUM({colLetter}{startRow}:{colLetter}{endRow})";
            ws.Cell(row, 4 + m).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
            ws.Cell(row, 4 + m).Style.Font.Bold = true;
        }

        string subFirst = XLSheetExtensions.ColumnLetter(4);
        string subLast = XLSheetExtensions.ColumnLetter(3 + forecastMonths);
        ws.Cell(row, 4 + forecastMonths).FormulaA1 = $"=SUM({subFirst}{row}:{subLast}{row})";
        ws.Cell(row, 4 + forecastMonths).Style.NumberFormat.Format = "£#,##0.00;[Red](£#,##0.00);£0.00";
        ws.Cell(row, 4 + forecastMonths).Style.Font.Bold = true;

        var subtotalRange = ws.Range(row, 2, row, 4 + forecastMonths);
        subtotalRange.Style.Fill.BackgroundColor = fill;
        subtotalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        subtotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
    }
}

internal static class XLSheetExtensions
{
    public static string ColumnLetter(int colIndex)
    {
        int div = colIndex;
        string colLetter = string.Empty;
        while (div > 0)
        {
            int modulo = (div - 1) % 26;
            colLetter = (char)(65 + modulo) + colLetter;
            div = (div - modulo) / 26;
        }
        return colLetter;
    }
}