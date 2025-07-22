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

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    [Authorize]
     [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptToken(int id)
    {
        var token = await _db.Tokens.FindAsync(id);
        if (token == null) return NotFound();

        token.Status = "accepted";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Token accepted." });
    }

    [HttpPost("{id}/decline")]
    public async Task<IActionResult> DeclineToken(int id)
    {
        var token = await _db.Tokens.FindAsync(id);
        if (token == null) return NotFound();

        token.Status = "declined";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Token declined." });
    }

    // Optional: View pending tokens for a user
    [HttpGet("incoming/{recipientId}")]
    public async Task<IActionResult> GetIncomingTokens(int recipientId)
    {
        var tokens = await _db.Tokens
            .Where(t => t.RecipientId == recipientId && t.Status == "pending")
            .ToListAsync();

        return Ok(tokens);
    }


    // POST: /api/token/issue
    [Authorize]
    [HttpPost("issue")]
    public IActionResult IssueToken([FromBody] IssueTokenDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Invalid amount.");

        var issuerUsername = User.FindFirstValue(ClaimTypes.Name);

        var issuerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"[DEBUG] Issuer ID: {issuerIdStr}");

        if (!int.TryParse(issuerIdStr, out var issuerId))
        {
            return Unauthorized("Invalid user ID.");
        }

        if (string.IsNullOrEmpty(issuerUsername) || string.IsNullOrEmpty(dto.Recipient))
            return BadRequest("Invalid issuer or recipient.");

        // Optional: fetch recipient details from DB if needed (e.g., full name, ID)
        var recipientUser = _db.Users.FirstOrDefault(u => u.Username == dto.Recipient);
        if (recipientUser == null)
            return NotFound("Recipient user not found.");

        var token = new Token
        {
            IssuerUsername = issuerUsername,
            IssuerId = issuerId,
            RecipientUsername = dto.Recipient,
            RecipientId = recipientUser.Id,
            RecipientName = recipientUser.Name,
            Amount = dto.Amount,
            Remarks = dto.Remarks,
            Status = "pending",
            IssuedAt = DateTime.UtcNow,
            ExpirationDate = dto.ExpirationDate
        };

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
    [HttpGet("sent")]
    public IActionResult GetSentTokens()
    {
        var userId = GetCurrentUserId(); // Implement based on your auth
        var sentTokens = _db.Tokens
            .Where(t => t.IssuerUsername == userId)
            .OrderByDescending(t => t.IssuedAt)
            .ToList();

        return Ok(sentTokens);
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
        var currentUser = User.FindFirstValue(ClaimTypes.Name);
        if (currentUser != username)
            return Forbid();

        var tokens = await _db.Tokens
            .Where(t => t.IssuerUsername == username || t.RecipientUsername == username)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        var history = tokens.Select(t => new TokenHistoryDto
        {
            Id = t.Id,
            Type = t.IssuerUsername == username ? "Sent" : "Received",
            PartnerUsername = t.IssuerUsername == username ? t.RecipientUsername : t.IssuerUsername,
            Amount = t.Amount,
            Status = t.Status,
            Remarks = t.Remarks,
            IssuedAt = t.IssuedAt
        });

        return Ok(history);
    }
}
