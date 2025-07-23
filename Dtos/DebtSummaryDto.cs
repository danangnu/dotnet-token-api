public class DebtSummaryDto
{
    public decimal TotalDebt { get; set; }
    public decimal TotalSettled { get; set; }
    public decimal TotalUnsettled { get; set; }
    public int ActiveUsersInDebt { get; set; }
    public string TopDebtorName { get; set; } = string.Empty;
    public string TopCreditorName { get; set; } = string.Empty;
}
