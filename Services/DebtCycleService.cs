using dotnet_token_api.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_token_api.Services;

public class DebtCycleService
{
    private readonly AppDbContext _context;

    public DebtCycleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<List<Debt>>> DetectDebtCyclesAsync()
    {
        var debts = await _context.Debts
            .Where(d => !d.IsSettled)
            .ToListAsync();

        // Build graph: FromUserId → List of debts
        var graph = new Dictionary<int, List<Debt>>();
        foreach (var debt in debts)
        {
            if (!graph.ContainsKey(debt.FromUserId))
                graph[debt.FromUserId] = new List<Debt>();

            graph[debt.FromUserId].Add(debt);
        }

        var visited = new HashSet<string>();
        var result = new List<List<Debt>>();

        foreach (var startUser in graph.Keys)
        {
            DFS(graph, startUser, startUser, new List<Debt>(), new HashSet<int>(), visited, result);
        }

        return result;
    }

    private void DFS(
        Dictionary<int, List<Debt>> graph,
        int currentUser,
        int targetUser,
        List<Debt> path,
        HashSet<int> visitedUsers,
        HashSet<string> cycleKeys,
        List<List<Debt>> result)
    {
        if (visitedUsers.Contains(currentUser))
            return;

        visitedUsers.Add(currentUser);

        if (!graph.ContainsKey(currentUser))
        {
            visitedUsers.Remove(currentUser);
            return;
        }

        foreach (var debt in graph[currentUser])
        {
            path.Add(debt);

            if (debt.ToUserId == targetUser && path.Count > 1)
            {
                // Found a cycle
                var key = GenerateCycleKey(path);
                if (!cycleKeys.Contains(key))
                {
                    cycleKeys.Add(key);
                    result.Add(new List<Debt>(path));
                }
            }
            else if (!visitedUsers.Contains(debt.ToUserId))
            {
                DFS(graph, debt.ToUserId, targetUser, path, visitedUsers, cycleKeys, result);
            }

            path.RemoveAt(path.Count - 1);
        }

        visitedUsers.Remove(currentUser);
    }

    private string GenerateCycleKey(List<Debt> cycle)
    {
        // Normalize to avoid duplicate cycle detection
        var ids = cycle.Select(d => d.FromUserId).OrderBy(id => id);
        return string.Join("-", ids);
    }

    public async Task<List<List<Debt>>> OffsetDebtCyclesAsync()
    {
        var cycles = await DetectDebtCyclesAsync();
        var updatedCycles = new List<List<Debt>>();

        foreach (var cycle in cycles)
        {
            // Find the minimum amount in the cycle
            decimal minAmount = (decimal)cycle.Min(d => d.Amount);

            foreach (var debt in cycle)
            {
                debt.Amount -= (double)minAmount;

                if (debt.Amount <= 0)
                {
                    debt.Amount = 0;
                    debt.IsSettled = true;
                }

                _context.Debts.Update(debt);
            }

            updatedCycles.Add(cycle);
        }

        await _context.SaveChangesAsync();
        return updatedCycles;
    }
}
