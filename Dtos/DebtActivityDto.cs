public class DebtActivityDto
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public int From { get; set; }
    public int To { get; set; }
    public decimal Amount { get; set; }
}
