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
}
