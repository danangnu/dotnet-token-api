using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/summary")]
    public async Task<IActionResult> GetAdminSummary()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalTokens = await _db.Tokens.CountAsync();
        var issued = await _db.Tokens.CountAsync(t => t.Status == "accepted");
        var pending = await _db.Tokens.CountAsync(t => t.Status == "pending");
        var rejected = await _db.Tokens.CountAsync(t => t.Status == "rejected");

        return Ok(new {
            totalUsers,
            totalTokens,
            issued,
            pending,
            rejected
        });
    }

}