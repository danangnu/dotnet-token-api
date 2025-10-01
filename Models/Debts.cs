namespace dotnet_token_api.Models;

public class Debt
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public double Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    // NEW: lets us label rows for demo scenarios
    public string? Tag { get; set; } // "BeforeOffset" | "AfterOffset" | null

    public bool IsSettled { get; set; }
}

