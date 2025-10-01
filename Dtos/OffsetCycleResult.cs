public class OffsetCycleResult
{
    public List<int> Nodes { get; set; } = new();      // closed loop (first == last)
    public decimal OffsetAmount { get; set; }          // min edge amount used to offset
    public List<int> AffectedDebtIds { get; set; } = new();
}
