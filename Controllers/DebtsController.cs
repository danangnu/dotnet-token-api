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
    public IActionResult CreateDebt([FromBody] Debt debt)
    {
        _context.Debts.Add(debt);
        _context.SaveChanges();
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
    public ActionResult<DebtSummaryDto> GetSummary()
    {
        var totalDebt = _context.Debts.Sum(d => d.Amount);
        var totalSettled = _context.Debts.Where(d => d.IsSettled).Sum(d => d.Amount);
        var totalUnsettled = totalDebt - totalSettled;

        // ✅ Replace problematic SelectMany
        var fromUsers = _context.Debts
            .Where(d => !d.IsSettled)
            .Select(d => d.FromUserId);

        var toUsers = _context.Debts
            .Where(d => !d.IsSettled)
            .Select(d => d.ToUserId);

        var usersInDebt = fromUsers
            .Union(toUsers)
            .Distinct()
            .Count();

        var topDebtor = _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.FromUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefault();

        var topCreditor = _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.ToUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefault();

        var topDebtorName = topDebtor != null
            ? _context.Users.FirstOrDefault(u => u.Id == topDebtor.UserId)?.Name ?? "N/A"
            : "N/A";

        var topCreditorName = topCreditor != null
            ? _context.Users.FirstOrDefault(u => u.Id == topCreditor.UserId)?.Name ?? "N/A"
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
    public ActionResult<IEnumerable<DebtRecordDto>> GetAllDebts2()
    {
        var debts = (from d in _context.Debts
                    join fromUser in _context.Users on d.FromUserId equals fromUser.Id
                    join toUser in _context.Users on d.ToUserId equals toUser.Id
                    select new DebtRecordDto
                    {
                        Id = d.Id,
                        Debtor = fromUser.Name,
                        Creditor = toUser.Name,
                        Amount = d.Amount,
                        Remarks = "", // Adjust if you add remarks column later
                        IsSettled = d.IsSettled,
                        CreatedAt = d.CreatedAt
                    }).ToList();

        return Ok(debts);
    }

}
