using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        // Dummy check (replace with real DB lookup)
        if (model.Username == "admin" && model.Password == "password")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, model.Username)
            };

            var keyString = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
                return StatusCode(500, "JWT key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        return Unauthorized();
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterModel model)
    {
        if (_db.Users.Any(u => u.Username == model.Username))
            return BadRequest("Username already taken");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

        var newUser = new User
        {
            Username = model.Username,
            PasswordHash = passwordHash,
            Role = "user"
        };

        _db.Users.Add(newUser);
        _db.SaveChanges();

        // Auto-login: generate token
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, newUser.Username),
            new Claim(ClaimTypes.Role, newUser.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = jwt });
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            // 1. Verify token using Google API
            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

            var email = payload.Email;
            var name = payload.Name;

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Invalid Google account.");

            // 2. Check if user exists, otherwise create
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = email.Split('@')[0], // generate username
                    Name = name,
                    Role = "User" // default
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            // 3. Generate JWT for your app
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new { token = jwt });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("Invalid Google token");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Server error: {ex.Message}");
        }
    }

    [HttpPost("apple-login")]
    public async Task<IActionResult> AppleLogin([FromBody] AppleLoginDto dto)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(dto.IdentityToken))
                return BadRequest("Invalid identity token");

            var jwt = handler.ReadJwtToken(dto.IdentityToken);
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
                return BadRequest("Missing email or subject in token");

            // Optional: verify the signature (see advanced section below)
            // For most cases, you can skip if identityToken came directly from Apple

            // Look up or create user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = email.Split('@')[0], // simple username
                    Name = "Apple User",
                    Role = "User"
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            // Generate your JWT
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            });

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Apple login error: {ex.Message}");
        }
    }
}
