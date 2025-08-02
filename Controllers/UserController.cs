using Microsoft.EntityFrameworkCore;
using dotnet_token_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace dotnet_token_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("public")]
        public IActionResult Public() =>
            Ok("This is a public endpoint. No login required.");

        [Authorize]
        [HttpGet("secure")]
        public IActionResult Secure() =>
            Ok($"Hello {User.Identity?.Name}, this is a protected endpoint.");

        [Authorize]
        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required.");

            var users = await _context.Users
                .Where(u => u.Email.Contains(query) || u.Username.Contains(query) || u.Name.Contains(query))
                .Select(u => new
                {
                    u.Id,
                    Display = $"{u.Name} ({u.Email})"
                })
                .Take(10)
                .ToListAsync();

            return Ok(users);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? username = null,
            [FromQuery] string? email = null,
            [FromQuery] string? role = null,
            [FromQuery] string sortBy = "username",
            [FromQuery] string sortOrder = "asc"
        )
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than zero.");

            var query = _context.Users.AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(u => u.Username != null && u.Username.Contains(username));
            
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email != null && u.Email.Contains(email));
            
            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            // Sorting
            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("email", "desc") => query.OrderByDescending(u => u.Email),
                ("email", _) => query.OrderBy(u => u.Email),

                ("name", "desc") => query.OrderByDescending(u => u.Name),
                ("name", _) => query.OrderBy(u => u.Name),

                ("username", "desc") => query.OrderByDescending(u => u.Username),
                (_, _) => query.OrderBy(u => u.Username) // default
            };

            // Total count
            var total = await query.CountAsync();

            // Paging
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Name,
                    u.Role
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                users
            });
        }
    }
}
