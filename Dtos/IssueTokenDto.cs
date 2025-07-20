public class IssueTokenDto
{
    public string Recipient { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string? Remarks { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
