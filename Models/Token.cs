public class Token
{
    public int Id { get; set; }

    public string IssuerUsername { get; set; } = string.Empty;   // Who gave it
    public string RecipientUsername { get; set; } = string.Empty; // Who received it

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending"; // pending | accepted | rejected

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
