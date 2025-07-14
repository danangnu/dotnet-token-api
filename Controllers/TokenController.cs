using Microsoft.AspNetCore.Mvc;

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
}
