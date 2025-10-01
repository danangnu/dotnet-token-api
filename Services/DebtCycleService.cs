namespace dotnet_token_api.Services;
using Microsoft.EntityFrameworkCore;

public class DebtCycleService
{
    private readonly AppDbContext _db;

    public DebtCycleService(AppDbContext db)
    {
        _db = db;
    }

    // ----------------------------
    // Public DTOs used by the API
    // ----------------------------
    public class DebtEdge
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public decimal Amount { get; set; }
    }

    public class DebtCycleDto
    {
        public List<int> Nodes { get; set; } = new();  // user IDs in order, closed loop (first = last)
        public List<DebtEdge> Edges { get; set; } = new();
        public decimal TotalAmount { get; set; }       // sum of edge amounts in this cycle
    }

    // ---------------------------------------------------
    // 1) Legacy call: read from DB (unfiltered, unsettled)
    //    You can keep this around if other code uses it.
    // ---------------------------------------------------
    public async Task<List<DebtCycleDto>> DetectDebtCyclesAsync()
    {
        var edges = await _db.Debts
            .AsNoTracking()
            .Where(d => !d.IsSettled)
            .Select(d => new DebtEdge
            {
                FromUserId = d.FromUserId,
                ToUserId = d.ToUserId,
                Amount = (decimal)d.Amount
            })
            .ToListAsync();

        return DetectCyclesInMemory(edges);
    }

    // ----------------------------------------------------------------
    // 2) New overload: controller passes the already-filtered edge set
    //    (e.g., filtered by Tag, Scope, etc.)
    // ----------------------------------------------------------------
    public Task<List<DebtCycleDto>> DetectDebtCyclesAsync(IEnumerable<DebtEdge> edges)
    {
        var result = DetectCyclesInMemory(edges);
        return Task.FromResult(result);
    }

    // ----------------------------------------------------------------
    // Core cycle detection (simple directed graph cycle finder)
    // - Finds simple cycles using DFS
    // - De-duplicates cycles via a canonical signature
    // - Limits cycle length to protect against path explosion
    // ----------------------------------------------------------------
    private List<DebtCycleDto> DetectCyclesInMemory(IEnumerable<DebtEdge> rawEdges, int maxCycleLength = 8)
    {
        var edges = rawEdges
            .GroupBy(e => (e.FromUserId, e.ToUserId))
            // if multiple parallel edges exist, sum them for cycle purposes
            .Select(g => new DebtEdge { FromUserId = g.Key.FromUserId, ToUserId = g.Key.ToUserId, Amount = g.Sum(x => x.Amount) })
            .ToList();

        // Build adjacency list
        var adj = edges
            .GroupBy(e => e.FromUserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allNodes = edges.SelectMany(e => new[] { e.FromUserId, e.ToUserId }).Distinct().ToList();

        var seenCycles = new HashSet<string>();
        var found = new List<DebtCycleDto>();

        foreach (var start in allNodes)
        {
            var stack = new List<int>();
            var visitedOnPath = new HashSet<int>();

            void Dfs(int current, int depth)
            {
                if (depth > maxCycleLength) return;

                stack.Add(current);
                visitedOnPath.Add(current);

                if (!adj.TryGetValue(current, out var outs))
                {
                    stack.RemoveAt(stack.Count - 1);
                    visitedOnPath.Remove(current);
                    return;
                }

                foreach (var e in outs)
                {
                    var next = e.ToUserId;

                    // Cycle found: next goes back to start and cycle length >= 2
                    if (next == start && stack.Count >= 2)
                    {
                        var cycleNodes = new List<int>(stack) { start }; // close the loop
                        var cycleEdges = ToEdges(cycleNodes, edges);

                        var signature = CanonicalSignature(cycleNodes);
                        if (seenCycles.Add(signature))
                        {
                            found.Add(new DebtCycleDto
                            {
                                Nodes = cycleNodes,
                                Edges = cycleEdges,
                                TotalAmount = cycleEdges.Sum(x => x.Amount)
                            });
                        }
                        continue;
                    }

                    // Continue DFS if we haven't visited next on this path
                    if (!visitedOnPath.Contains(next))
                    {
                        Dfs(next, depth + 1);
                    }
                }

                // backtrack
                stack.RemoveAt(stack.Count - 1);
                visitedOnPath.Remove(current);
            }

            Dfs(start, 0);
        }

        // Optionally sort cycles by TotalAmount desc, length, etc.
        return found
            .OrderByDescending(c => c.TotalAmount)
            .ThenBy(c => c.Nodes.Count)
            .ToList();
    }

    // Build edge list along a path of nodes (closed: last == first)
    private static List<DebtEdge> ToEdges(List<int> nodes, List<DebtEdge> masterEdges)
    {
        var list = new List<DebtEdge>();
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var from = nodes[i];
            var to = nodes[i + 1];

            // Find the aggregated edge (we pre-summed duplicates earlier)
            var edge = masterEdges.FirstOrDefault(e => e.FromUserId == from && e.ToUserId == to);
            if (edge != null)
                list.Add(edge);
        }
        return list;
    }

    // Create a canonical signature for a cycle to avoid duplicates
    // - rotate to start at smallest node id
    // - keep direction as-is to distinguish reverse cycles if needed
    private static string CanonicalSignature(List<int> closedCycle)
    {
        // closedCycle: [a, b, c, a]
        // work on the open part [a, b, c]
        var path = closedCycle.Take(closedCycle.Count - 1).ToList();
        if (path.Count == 0) return string.Empty;

        // rotate so the minimal node id is first
        int minVal = path.Min();
        int idx = path.IndexOf(minVal);

        var rotated = path.Skip(idx).Concat(path.Take(idx)).ToList();

        // signature like "a->b->c->a"
        return string.Join("->", rotated) + "->" + rotated[0];
    }
    
    public async Task<List<OffsetCycleResult>> OffsetDebtCyclesAsync(string? tag = null, int maxCycleLength = 8)
    {
        // 1) Load unsettled debts (optionally by tag)
        var q = _db.Debts.AsQueryable().Where(d => !d.IsSettled);
        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(d => d.Tag == tag);

        // Pull the full entities; we’ll need IDs to update
        var debts = await q
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (debts.Count == 0)
            return new List<OffsetCycleResult>();

        // Build edges for cycle detection (aggregate parallel edges by sum)
        var edgesForDetection = debts
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new DebtEdge
            {
                FromUserId = g.Key.FromUserId,
                ToUserId   = g.Key.ToUserId,
                Amount     = g.Sum(x => (decimal)x.Amount - x.PaidAmount) // current outstanding
            })
            .Where(e => e.Amount > 0)
            .ToList();

        // 2) Find cycles in-memory
        var cycles = DetectCyclesInMemory(edgesForDetection, maxCycleLength);
        if (cycles.Count == 0)
            return new List<OffsetCycleResult>();

        var results = new List<OffsetCycleResult>();
        var now = DateTime.UtcNow;

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var cycle in cycles)
            {
                // Outstanding amount on each edge in this cycle (use current DB state)
                var amounts = new List<decimal>();
                foreach (var e in cycle.Edges)
                {
                    var outstanding = debts
                        .Where(d => d.FromUserId == e.FromUserId &&
                                    d.ToUserId   == e.ToUserId &&
                                    !d.IsSettled)
                        .Sum(d => (decimal)d.Amount - d.PaidAmount);

                    amounts.Add(outstanding);
                }

                var offset = amounts.Count > 0 ? amounts.Min() : 0m;
                if (offset <= 0) continue;

                var affectedIds = new List<int>();

                // 3) Apply the offset across each edge: subtract "offset" from the debt(s) along that edge
                foreach (var e in cycle.Edges)
                {
                    var remaining = offset;

                    // Get all open debts for this (from -> to), oldest first (or any order you prefer)
                    var chain = debts
                        .Where(d => d.FromUserId == e.FromUserId &&
                                    d.ToUserId   == e.ToUserId &&
                                    !d.IsSettled)
                        .OrderBy(d => d.Id)
                        .ToList();

                    foreach (var d in chain)
                    {
                        if (remaining <= 0) break;

                        var open = (decimal)d.Amount - d.PaidAmount;
                        if (open <= 0) continue;

                        var pay = Math.Min(open, remaining);
                        d.PaidAmount += pay;
                        affectedIds.Add(d.Id);

                        // mark settled if fully paid
                        if (d.PaidAmount >= (decimal)d.Amount)
                        {
                            d.IsSettled = true;
                        }

                        // Activity log
                        _db.DebtActivities.Add(new DebtActivity
                        {
                            DebtId      = d.Id,
                            Action      = "OffsetCycle",
                            Timestamp   = now,
                            PerformedBy = "system-offset"  // or current user if you run this from a user action
                        });

                        remaining -= pay;
                    }
                }

                results.Add(new OffsetCycleResult
                {
                    Nodes        = cycle.Nodes,
                    OffsetAmount = offset,
                    AffectedDebtIds = affectedIds.Distinct().ToList()
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return results;
    }
}