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

    // Helpers
    private string? GetCurrentUsername() =>
        User.FindFirstValue(ClaimTypes.Name); // you’re already using this for username elsewhere

    private int? GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : (int?)null;
    }

    private bool IsAdmin() =>
        User.IsInRole("Admin"); // requires Role claim in your JWT

    // ------------------------------------------------------------------------------------
    // Admin-only: view ALL tokens (any status, any user)
    // ------------------------------------------------------------------------------------
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTokens()
    {
        var tokens = await _db.Tokens
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return Ok(tokens);
    }

    // ------------------------------------------------------------------------------------
    // Admin-only: view incoming tokens for a specific user by id (kept your endpoint)
    // ------------------------------------------------------------------------------------
    [Authorize(Roles = "Admin")]
    [HttpGet("incoming/{recipientId:int}")]
    public async Task<IActionResult> GetIncomingTokens(int recipientId)
    {
        var tokens = await _db.Tokens
            .Where(t => t.RecipientId == recipientId && t.Status == "pending")
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return Ok(tokens);
    }

    // ------------------------------------------------------------------------------------
    // Current user: tokens I sent
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpGet("sent")]
    public async Task<IActionResult> GetSentTokens()
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Unauthorized();

        var sentTokens = await _db.Tokens
            .Where(t => t.IssuerUsername == currentUsername)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return Ok(sentTokens);
    }

    // ------------------------------------------------------------------------------------
    // Current user: tokens I received and accepted
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpGet("accepted")]
    public async Task<IActionResult> GetAcceptedTokensForCurrentUser()
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Unauthorized();

        var tokens = await _db.Tokens
            .Where(t => t.RecipientUsername == currentUsername && t.Status == "accepted")
            .Select(t => new
            {
                t.Id,
                t.RecipientUsername,
                t.RecipientName,
                t.Amount,
                t.IssuedAt
            })
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return Ok(tokens);
    }

    // ------------------------------------------------------------------------------------
    // Current user: unified “my history” (no need to pass ?username=)
    // Admins may optionally pass ?username= to view someone else’s history
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? username = null)
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrEmpty(currentUsername))
            return Unauthorized();

        var isAdmin = IsAdmin();

        // If a username is provided, only admins may query others.
        string targetUsername;
        if (!string.IsNullOrWhiteSpace(username))
        {
            if (!isAdmin && !string.Equals(username, currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                // Do NOT use Forbid("message") — that string is treated as a scheme.
                return StatusCode(403, "Only admins can query other users' history.");
            }

            targetUsername = username;
        }
        else
        {
            // Default to current user
            targetUsername = currentUsername;
        }

        var tokens = await _db.Tokens
            .Where(t => t.IssuerUsername == targetUsername || t.RecipientUsername == targetUsername)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        var history = tokens.Select(t => new TokenHistoryDto
        {
            Id = t.Id,
            Type = t.IssuerUsername == targetUsername ? "Sent" : "Received",
            PartnerUsername = t.IssuerUsername == targetUsername ? t.RecipientUsername : t.IssuerUsername,
            Amount = t.Amount,
            Status = t.Status,
            Remarks = t.Remarks,
            IssuedAt = t.IssuedAt
        });

        return Ok(history);
    }

    // ------------------------------------------------------------------------------------
    // Issue token (current user is the issuer)
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpPost("issue")]
    public async Task<IActionResult> IssueToken([FromBody] IssueTokenDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Invalid amount.");

        var issuerUsername = GetCurrentUsername();
        var issuerId = GetCurrentUserId();

        if (issuerId is null || string.IsNullOrWhiteSpace(issuerUsername))
            return Unauthorized("Invalid issuer.");

        if (string.IsNullOrWhiteSpace(dto.Recipient))
            return BadRequest("Recipient is required.");

        var recipientUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Recipient);
        if (recipientUser == null)
            return NotFound("Recipient user not found.");

        var token = new Token
        {
            IssuerUsername = issuerUsername,
            IssuerId = issuerId.Value,
            RecipientUsername = recipientUser.Username!,
            RecipientId = recipientUser.Id,
            RecipientName = recipientUser.Name,
            Amount = dto.Amount,
            Remarks = dto.Remarks,
            Status = "pending",
            IssuedAt = DateTime.UtcNow,
            ExpirationDate = dto.ExpirationDate
        };

        await _db.Tokens.AddAsync(token);
        await _db.SaveChangesAsync();

        return Ok(token);
    }

    // ------------------------------------------------------------------------------------
    // Recipient actions: accept / decline
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> AcceptToken(int id)
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Unauthorized();

        var token = await _db.Tokens.FindAsync(id);
        if (token == null) return NotFound();

        if (!string.Equals(token.RecipientUsername, currentUsername, StringComparison.OrdinalIgnoreCase))
            return Forbid("You are not the intended recipient.");

        if (token.Status != "pending")
            return BadRequest("Only pending tokens can be accepted.");

        token.Status = "accepted";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Token accepted." });
    }

    [Authorize]
    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> DeclineToken(int id)
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Unauthorized();

        var token = await _db.Tokens.FindAsync(id);
        if (token == null) return NotFound();

        if (!string.Equals(token.RecipientUsername, currentUsername, StringComparison.OrdinalIgnoreCase))
            return Forbid("You are not the intended recipient.");

        if (token.Status != "pending")
            return BadRequest("Only pending tokens can be declined.");

        token.Status = "declined";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Token declined." });
    }

    // ------------------------------------------------------------------------------------
    // Transfer an accepted token you own (you must be the current Recipient)
    // ------------------------------------------------------------------------------------
    [Authorize]
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferToken([FromBody] TransferTokenDto dto)
    {
        var currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Unauthorized();

        var token = await _db.Tokens.FindAsync(dto.TokenId);
        if (token == null) return NotFound("Token not found.");

        if (token.Status != "accepted")
            return BadRequest("Only accepted tokens can be transferred.");

        if (!string.Equals(token.RecipientUsername, currentUsername, StringComparison.OrdinalIgnoreCase))
            return Forbid("You are not the owner of this token.");

        var newRecipient = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.NewRecipientUsername);
        if (newRecipient == null)
            return NotFound("New recipient user not found.");

        // Option A: create a new pending token for the next recipient (leave original as historical)
        var newToken = new Token
        {
            IssuerUsername = currentUsername,
            IssuerId = token.RecipientId, // issuer is the current owner in this transfer model
            RecipientUsername = newRecipient.Username!,
            RecipientId = newRecipient.Id,
            RecipientName = newRecipient.Name,
            Amount = token.Amount,
            Remarks = dto.Remarks,
            Status = "pending",
            IssuedAt = DateTime.UtcNow
        };

        await _db.Tokens.AddAsync(newToken);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Token transferred." });
    }
}
