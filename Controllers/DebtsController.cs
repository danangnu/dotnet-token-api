using System.Security.Claims;
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

    // --------------------------
    // helpers
    // --------------------------
    private int? TryGetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(id, out var uid)) return uid;
        return null;
    }
    private int? GetCurrentUserIdInt()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : (int?)null;
    }

    private bool IsAdmin() =>
        string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

    private int GetUserIdOrThrow()
    {
        var uid = TryGetUserId();
        if (uid is null) throw new UnauthorizedAccessException("Invalid token user id.");
        return uid.Value;
    }

    // =========================================================
    // ADMIN ENDPOINTS (global data)
    // =========================================================

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    public async Task<ActionResult<IEnumerable<Debt>>> GetAllDebts_Admin()
    {
        var debts = await _context.Debts.AsNoTracking().ToListAsync();
        return Ok(debts);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/overview")]
    public async Task<IActionResult> GetDebtOverview_Admin()
    {
        var grouped = await _context.Debts
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new
            {
                g.Key.FromUserId,
                g.Key.ToUserId,
                TotalAmount = g.Sum(d => d.Amount),
                TotalPaid = g.Sum(d => d.PaidAmount),
                Remaining = g.Sum(d => (decimal)d.Amount - d.PaidAmount),
                IsFullySettled = g.All(d => d.IsSettled),
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(grouped);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/summary")]
    public async Task<ActionResult<DebtSummaryDto>> GetSummary_Admin()
    {
        var totalDebt = await _context.Debts.SumAsync(d => d.Amount);
        var totalPaid = await _context.Debts.SumAsync(d => d.PaidAmount);
        var totalUnsettled = (decimal)totalDebt - totalPaid;

        var fromUsers = await _context.Debts.Where(d => !d.IsSettled).Select(d => d.FromUserId).ToListAsync();
        var toUsers = await _context.Debts.Where(d => !d.IsSettled).Select(d => d.ToUserId).ToListAsync();
        var usersInDebt = fromUsers.Union(toUsers).Distinct().Count();

        var topDebtor = await _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.FromUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => (decimal)x.Amount - x.PaidAmount) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefaultAsync();

        var topCreditor = await _context.Debts
            .Where(d => !d.IsSettled)
            .GroupBy(d => d.ToUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => (decimal)x.Amount - x.PaidAmount) })
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
            TotalDebt = (decimal)totalDebt,
            TotalSettled = totalPaid,
            TotalUnsettled = totalUnsettled,
            ActiveUsersInDebt = usersInDebt,
            TopDebtorName = topDebtorName,
            TopCreditorName = topCreditorName
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/graph")]
    public async Task<IActionResult> GetDebtGraph_Admin()
    {
        var unsettled = await _context.Debts.Where(d => !d.IsSettled).ToListAsync();
        var userIds = unsettled.SelectMany(d => new[] { d.FromUserId, d.ToUserId }).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name ?? u.Username ?? $"User {u.Id}");

        var graphData = unsettled
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new
            {
                FromUser = users.GetValueOrDefault(g.Key.FromUserId, $"User {g.Key.FromUserId}"),
                ToUser = users.GetValueOrDefault(g.Key.ToUserId, $"User {g.Key.ToUserId}"),
                Amount = g.Sum(d => (decimal)d.Amount - d.PaidAmount)
            })
            .ToList();

        return Ok(graphData);
    }

    // Cycle detection / offset is potentially destructive => admin
    [Authorize]
    [HttpGet("cycles")]
    public async Task<IActionResult> GetDebtCycles(
        [FromQuery] string? tag = null,         // "BeforeOffset" | "AfterOffset" | null
        [FromQuery] string scope = "all"        // "all" (default) | "my"
    )
    {
        var q = _context.Debts
            .AsNoTracking()
            .Where(d => !d.IsSettled);

        // Optional tag filter
        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(d => d.Tag == tag);

        // Optional scope filter (non-admins default to "my" if requested "all")
        if (string.Equals(scope, "my", StringComparison.OrdinalIgnoreCase) || !IsAdmin())
        {
            var me = GetCurrentUserIdInt();
            if (me == null) return Unauthorized();
            q = q.Where(d => d.FromUserId == me || d.ToUserId == me);
        }

        var edges = await q
            .Select(d => new { d.FromUserId, d.ToUserId, d.Amount })
            .ToListAsync();

        // If your DebtCycleService has a method that accepts the debt list, use it.
        // Otherwise, adapt it to accept a list; passing the filtered edges is key.
        var cycles = await _debtCycleService.DetectDebtCyclesAsync();

        return Ok(cycles);
    }


    [Authorize]
    [HttpPost("offset-cycles")]
    public async Task<IActionResult> OffsetCycles_Admin([FromQuery] string? tag = "BeforeOffset")
    {
        var updated = await _debtCycleService.OffsetDebtCyclesAsync(tag);
        return Ok(updated);
    }

    // =========================================================
    // USER-SCOPED ENDPOINTS (only data involving the caller)
    // =========================================================

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<DebtRecordDto>>> GetMyDebts()
    {
        var userId = GetUserIdOrThrow();
        var query = _context.Debts
            .Where(d => d.FromUserId == userId || d.ToUserId == userId);

        var debts = await (
            from d in query
            join fu in _context.Users on d.FromUserId equals fu.Id into fgrp
            from fu in fgrp.DefaultIfEmpty()
            join tu in _context.Users on d.ToUserId equals tu.Id into tgrp
            from tu in tgrp.DefaultIfEmpty()
            select new DebtRecordDto
            {
                Id = d.Id,
                Debtor = fu.Name ?? fu.Username ?? $"User {d.FromUserId}",
                Creditor = tu.Name ?? tu.Username ?? $"User {d.ToUserId}",
                Amount = (decimal)d.Amount,
                Remarks = "", // future-proof
                IsSettled = d.IsSettled,
                CreatedAt = d.CreatedAt,
                PaidAmount = d.PaidAmount
            }
        ).ToListAsync();

        return Ok(debts);
    }

    [Authorize]
    [HttpGet("my/summary")]
    public async Task<ActionResult<object>> GetMySummary()
    {
        var userId = GetUserIdOrThrow();

        var myDebts = _context.Debts.Where(d => d.FromUserId == userId || d.ToUserId == userId);

        var total = await myDebts.SumAsync(d => d.Amount);
        var paid = await myDebts.SumAsync(d => d.PaidAmount);
        var remaining = (decimal)total - paid;

        return Ok(new
        {
            Total = total,
            Paid = paid,
            Remaining = remaining,
            MyIssuedCount = await myDebts.CountAsync(d => d.FromUserId == userId),
            MyReceivedCount = await myDebts.CountAsync(d => d.ToUserId == userId)
        });
    }

    [Authorize]
    [HttpGet("my/graph")]
    public async Task<IActionResult> GetMyGraph()
    {
        var userId = GetUserIdOrThrow();

        var unsettled = await _context.Debts
            .Where(d => !d.IsSettled && (d.FromUserId == userId || d.ToUserId == userId))
            .ToListAsync();

        var userIds = unsettled.SelectMany(d => new[] { d.FromUserId, d.ToUserId }).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name ?? u.Username ?? $"User {u.Id}");

        var graphData = unsettled
            .GroupBy(d => new { d.FromUserId, d.ToUserId })
            .Select(g => new
            {
                FromUser = users.GetValueOrDefault(g.Key.FromUserId, $"User {g.Key.FromUserId}"),
                ToUser = users.GetValueOrDefault(g.Key.ToUserId, $"User {g.Key.ToUserId}"),
                Amount = g.Sum(d => (decimal)d.Amount - d.PaidAmount)
            })
            .ToList();

        return Ok(graphData);
    }

    [Authorize]
    [HttpGet("activity")]
    public async Task<ActionResult<IEnumerable<DebtActivityDto>>> GetMyRecentActivities()
    {
        var userId = GetUserIdOrThrow();

        var activities = await _context.DebtActivities
            .Include(a => a.Debt)
            .Where(a => a.Debt != null && (a.Debt.FromUserId == userId || a.Debt.ToUserId == userId))
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new DebtActivityDto
            {
                Action = a.Action,
                Timestamp = a.Timestamp,
                PerformedBy = a.PerformedBy,
                From = a.Debt!.FromUserId,
                To = a.Debt!.ToUserId,
                Amount = (decimal)a.Debt!.Amount
            })
            .ToListAsync();

        return Ok(activities);
    }

    // =========================================================
    // CREATE / MUTATIONS (ownership checks)
    // =========================================================

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateDebt([FromBody] Debt debt)
    {
        var userId = GetUserIdOrThrow();

        // Non-admins can only create debts they issue (from themselves)
        if (!IsAdmin() && debt.FromUserId != userId)
            return Forbid("You can only issue debts from your own account.");

        // sanitize
        debt.Id = 0;
        debt.PaidAmount = 0; // assume your model has PaidAmount now
        debt.IsSettled = false;
        debt.CreatedAt = DateTime.UtcNow;

        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMyDebts), new { id = debt.Id }, debt);
    }

    // Creditor (or Admin) can mark partial/full settlement
    [Authorize]
    [HttpPost("{id}/settle")]
    public async Task<IActionResult> SettleDebt(int id, [FromBody] DebtSettlementRequest request)
    {
        var userId = GetUserIdOrThrow();
        var actor = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Name ?? u.Username ?? u.Email ?? u.Id.ToString())
            .FirstOrDefaultAsync() ?? userId.ToString();
        var debt = await _context.Debts.FindAsync(id);
        if (debt == null) return NotFound(new { message = "Debt not found" });

        // Only creditor or admin can settle (mark payments received)
        if (!IsAdmin() && debt.ToUserId != userId)
            return Forbid("Only the creditor can settle this debt.");

        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero" });

        if (debt.IsSettled)
            return BadRequest(new { message = "Debt is already settled" });

        var remaining = (decimal)debt.Amount - debt.PaidAmount;
        var settleAmount = Math.Min(request.Amount, remaining);
        debt.PaidAmount += settleAmount;

        if (debt.PaidAmount >= (decimal)debt.Amount)
            debt.IsSettled = true;

        // Optionally log activity:
        _context.DebtActivities.Add(new DebtActivity
        {
            DebtId = debt.Id,
            Action = debt.IsSettled ? "Settled" : "PartialSettlement",
            Timestamp = DateTime.UtcNow,
            PerformedBy = actor
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = debt.IsSettled ? "Debt fully settled" : "Partial settlement successful",
            debt.Id,
            debt.FromUserId,
            debt.ToUserId,
            debt.Amount,
            debt.PaidAmount,
            debt.IsSettled
        });
    }

    // Debtor (or Admin) can repay
    [Authorize]
    [HttpPost("{id}/repay")]
    public async Task<IActionResult> RepayDebt(int id, [FromBody] RepaymentDto repayment)
    {
        var userId = GetUserIdOrThrow();
        var actor = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Name ?? u.Username ?? u.Email ?? u.Id.ToString())
            .FirstOrDefaultAsync() ?? userId.ToString();
        var debt = await _context.Debts.FirstOrDefaultAsync(d => d.Id == id);
        if (debt == null) return NotFound("Debt not found.");

        // Only debtor or admin can repay
        if (!IsAdmin() && debt.FromUserId != userId)
            return Forbid("Only the debtor can repay this debt.");

        if (debt.IsSettled) return BadRequest("Debt is already settled.");

        if (repayment.Amount <= 0)
            return BadRequest("Invalid repayment amount.");

        var remaining = (decimal)debt.Amount - debt.PaidAmount;
        if (repayment.Amount > remaining)
            return BadRequest($"Repayment exceeds remaining balance ({remaining}).");

        debt.PaidAmount += repayment.Amount;
        if (debt.PaidAmount >= (decimal)debt.Amount)
            debt.IsSettled = true;

        // Optionally log activity:
        _context.DebtActivities.Add(new DebtActivity
        {
            DebtId = debt.Id,
            Action = "Repayment",
            Timestamp = DateTime.UtcNow,
            PerformedBy = actor
        });

        await _context.SaveChangesAsync();

        return Ok(new { debt.Id, debt.Amount, debt.PaidAmount, debt.IsSettled });
    }
}
