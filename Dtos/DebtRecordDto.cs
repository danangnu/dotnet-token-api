public class DebtRecordDto
{
    public int Id { get; set; }
    public string Debtor { get; set; } = string.Empty;
    public string Creditor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
    public bool IsSettled { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal PaidAmount { get; set; }
}
