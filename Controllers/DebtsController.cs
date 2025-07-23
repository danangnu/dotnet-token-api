using dotnet_token_api.Models;
using dotnet_token_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DebtsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DebtCycleService _debtCycleService;

    public DebtsController(AppDbContext context, DebtCycleService debtCycleService)
    {
        _context = context;
        _debtCycleService = debtCycleService;
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetAllDebts()
    {
        var debts = _context.Debts.ToList();
        return Ok(debts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDebt([FromBody] Debt debt)
    {
        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAllDebts), new { id = debt.Id }, debt);
    }

    [HttpGet("cycles")]
    public async Task<IActionResult> GetDebtCycles()
    {
        var cycles = await _debtCycleService.DetectDebtCyclesAsync();
        return Ok(cycles);
    }

    [HttpPost("offset-cycles")]
    public async Task<IActionResult> OffsetCycles()
    {
        var updatedCycles = await _debtCycleService.OffsetDebtCyclesAsync();
        return Ok(updatedCycles);
    }

    [Authorize]
    [HttpGet("overview")]
    public async Task<IActionResult> GetDebtOverview()
    {
        var grouped = await _context.Debts
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new
            {
                FromUserId = g.Key.FromUserId,
                ToUserId = g.Key.ToUserId,
                TotalAmount = g.Sum(d => d.Amount),
                IsFullySettled = g.All(d => d.IsSettled),
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(grouped);
    }

    [Authorize]
    [HttpGet("summary")]
    public async Task<ActionResult<DebtSummaryDto>> GetSummary()
    {
        var totalDebt = await _context.Debts.SumAsync(d => d.Amount);
        var totalSettled = await _context.Debts.Where(d => d.IsSettled).SumAsync(d => d.Amount);
        var totalUnsettled = totalDebt - totalSettled;

        var fromUsers = await _context.Debts
            .Where(d => !d.IsSettled)
            .Select(d => d.FromUserId)
            .ToListAsync();

        var toUsers = await _context.Debts
            .Where(d => !d.IsSettled)
            .Select(d => d.ToUserId)
            .ToListAsync();

        var usersInDebt = fromUsers
            .Union(toUsers)
            .Distinct()
            .Count();

        var topDebtor = await _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.FromUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefaultAsync();

        var topCreditor = await _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.ToUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefaultAsync();

        var topDebtorName = topDebtor != null
            ? (await _context.Users.FirstOrDefaultAsync(u => u.Id == topDebtor.UserId))?.Name ?? "N/A"
            : "N/A";

        var topCreditorName = topCreditor != null
            ? (await _context.Users.FirstOrDefaultAsync(u => u.Id == topCreditor.UserId))?.Name ?? "N/A"
            : "N/A";

        return Ok(new DebtSummaryDto
        {
            TotalDebt = totalDebt,
            TotalSettled = totalSettled,
            TotalUnsettled = totalUnsettled,
            ActiveUsersInDebt = usersInDebt,
            TopDebtorName = topDebtorName,
            TopCreditorName = topCreditorName
        });
    }

    [Authorize]
    [HttpGet("activity")]
    public async Task<ActionResult<IEnumerable<DebtActivityDto>>> GetRecentActivities()
    {
        var activities = await _context.DebtActivities
            .Include(a => a.Debt)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new DebtActivityDto
            {
                Action = a.Action,
                Timestamp = a.Timestamp,
                PerformedBy = a.PerformedBy,
                From = a.Debt != null ? a.Debt.FromUserId : default,
                To = a.Debt != null ? a.Debt.ToUserId : default,
                Amount = a.Debt != null ? a.Debt.Amount : default
            })
            .ToListAsync();

        return Ok(activities);
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<DebtRecordDto>>> GetAllDebts2()
    {
        var debts = await (from d in _context.Debts
                           join fromUser in _context.Users on d.FromUserId equals fromUser.Id into fromGroup
                           from fromUser in fromGroup.DefaultIfEmpty()

                           join toUser in _context.Users on d.ToUserId equals toUser.Id into toGroup
                           from toUser in toGroup.DefaultIfEmpty()

                           select new DebtRecordDto
                           {
                               Id = d.Id,
                               Debtor = fromUser.Name,
                               Creditor = toUser.Name,
                               Amount = d.Amount,
                               Remarks = "", // Adjust if you add remarks column later
                               IsSettled = d.IsSettled,
                               CreatedAt = d.CreatedAt
                           }).ToListAsync();

        return Ok(debts);
    }

    [Authorize]
    [HttpPost("{id}/repay")]
    public async Task<IActionResult> RepayDebt(int id, [FromBody] RepaymentDto repayment)
    {
        var debt = await _context.Debts.FirstOrDefaultAsync(d => d.Id == id);
        if (debt == null) return NotFound("Debt not found.");

        if (debt.IsSettled) return BadRequest("Debt is already settled.");

        if (repayment.Amount <= 0 || repayment.Amount > debt.Amount)
            return BadRequest("Invalid repayment amount.");

        debt.Amount -= repayment.Amount;

        if (debt.Amount == 0)
            debt.IsSettled = true;

        await _context.SaveChangesAsync();

        return Ok(new { debt.Id, debt.Amount, debt.IsSettled });
    }
    
    [Authorize]
    [HttpGet("graph")]
    public async Task<IActionResult> GetDebtGraph()
    {
        var unsettledDebts = await _context.Debts
            .Where(d => !d.IsSettled)
            .ToListAsync();

        var userIds = unsettledDebts
            .SelectMany(d => new[] { d.FromUserId, d.ToUserId })
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var graphData = unsettledDebts
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new
            {
                FromUser = users.ContainsKey(g.Key.FromUserId) ? users[g.Key.FromUserId] : "Unknown",
                ToUser = users.ContainsKey(g.Key.ToUserId) ? users[g.Key.ToUserId] : "Unknown",
                Amount = g.Sum(d => d.Amount)
            })
            .ToList();

        return Ok(graphData);
    }
}
