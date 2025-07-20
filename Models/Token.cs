public class Token
{
    public int Id { get; set; }

    public string IssuerUsername { get; set; } = string.Empty;   // Who gave it
    public string RecipientUsername { get; set; } = string.Empty; // Who received it
    public string? RecipientName { get; set; } = null; // Optional, can be null if not provided

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending"; // pending | accepted | rejected

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; } = null; // Optional, can be null if not set
    public string? Remarks { get; set; }

    public int IssuerId { get; set; }
    public int RecipientId { get; set; }
}
