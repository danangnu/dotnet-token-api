using dotnet_token_api.Models;

public class DebtActivity
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "Issued", "Settled", "Partial Payment"
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = string.Empty; // User name or ID

    public Debt? Debt { get; set; }
}
