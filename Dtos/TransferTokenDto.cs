public class TransferTokenDto
{
    public int TokenId { get; set; }
    public string NewRecipientUsername { get; set; } = string.Empty;
    public string? NewRecipientName { get; set; } = null;
    public string? Remarks { get; set; }
}

