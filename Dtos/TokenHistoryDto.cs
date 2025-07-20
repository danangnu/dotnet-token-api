public class TokenHistoryDto
{
    public string? Type { get; set; }              // "Sent" or "Received"
    public string? PartnerUsername { get; set; }   // The other person
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime IssuedAt { get; set; }
}
