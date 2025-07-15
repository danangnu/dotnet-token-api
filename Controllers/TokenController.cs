using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly AppDbContext _db;

    public TokenController(AppDbContext db)
    {
        _db = db;
    }

    // POST: /api/token/issue
    [HttpPost("issue")]
    public IActionResult IssueToken([FromBody] Token token)
    {
        if (token.Amount <= 0)
            return BadRequest("Invalid amount.");

        token.Status = "pending";
        token.IssuedAt = DateTime.UtcNow;

        _db.Tokens.Add(token);
        _db.SaveChanges();

        return Ok(token);
    }

    // GET: /api/token/mine
    [HttpGet("mine")]
    public IActionResult GetMyTokens([FromQuery] string username)
    {
        var tokens = _db.Tokens
            .Where(t => t.RecipientUsername == username || t.IssuerUsername == username)
            .OrderByDescending(t => t.IssuedAt)
            .ToList();

        return Ok(tokens);
    }

    // POST: /api/token/respond
    [HttpPost("respond")]
    public IActionResult Respond([FromBody] Token response)
    {
        var token = _db.Tokens.Find(response.Id);
        if (token == null) return NotFound();

        if (token.RecipientUsername != response.RecipientUsername)
            return Forbid();

        token.Status = response.Status;
        _db.SaveChanges();

        return Ok(token);
    }

    [Authorize]
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferToken([FromBody] TransferTokenDto dto)
    {
        var token = await _db.Tokens.FindAsync(dto.TokenId);
        if (token == null) return NotFound("Token not found.");

        if (token.Status != "accepted")
            return BadRequest("Only accepted tokens can be transferred.");

        // Optional: validate that current user owns the token
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (token.RecipientUsername != username)
            return Forbid("You are not the owner of this token.");

        // Create a new token for the new recipient
        var newToken = new Token
        {
            IssuerUsername = username!,
            RecipientUsername = dto.NewRecipientUsername,
            Amount = token.Amount,
            Status = "pending",
            IssuedAt = DateTime.UtcNow,
            Remarks = dto.Remarks
        };

        await _db.Tokens.AddAsync(newToken);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Token transferred." });
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string username)
    {
        // Ensure user is requesting their own history
        var user = User.FindFirstValue(ClaimTypes.Name);
        if (user != username)
            return Forbid("Unauthorized access to history");

        var tokens = await _db.Tokens
            .Where(t => t.IssuerUsername == username || t.RecipientUsername == username)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return Ok(tokens);
    }
}
