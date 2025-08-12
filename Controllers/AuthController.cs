using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
    private readonly IEmailService _emailService;

    public AuthController(AppDbContext db, IConfiguration config, IEmailService emailService)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            email = user.Email,
            name = user.Name,
            username = user.Username,
            role = user.Role
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Name = dto.Name,
            Role = "User",
            EmailVerificationToken = Guid.NewGuid().ToString(),
            EmailTokenExpiry = DateTime.UtcNow.AddHours(24),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Send email
        var verificationLink = $"{_config["AppSettings:FrontendUrl"]}/verify-email?token={user.EmailVerificationToken}";
        await _emailService.SendVerificationEmailAsync(user.Email, $"Click here to verify: {verificationLink}");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null || user.EmailTokenExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or expired token");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailTokenExpiry = null;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Email successfully verified." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return Ok(); // Don't reveal existence

        var token = Guid.NewGuid(   ).ToString();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        var resetLink = $"{_config["FrontendUrl"]}/reset-password?token={token}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest("Passwords do not match");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
            return BadRequest("Invalid or expired token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _db.SaveChangesAsync();

        return Ok("Password has been reset");
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var httpClient = new HttpClient();

            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            var tokenRequestData = new Dictionary<string, string>
            {
                { "code", dto.Code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequestData)
            );

            if (!tokenResponse.IsSuccessStatusCode)
            {
                return Unauthorized("Token exchange failed.");
            }

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var tokenResult = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenContent, options);

            var idToken = tokenResult?.IdToken;

            if (string.IsNullOrWhiteSpace(idToken))
            {
                return StatusCode(500, "Missing id_token from Google.");
            }

            // Step 2: Validate the id_token
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

            // Step 3: Check or create user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = payload.Email,
                    Username = payload.Email.Split('@')[0],
                    Name = payload.Name,
                    Role = "user"
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            // Step 4: Generate your own JWT
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                email = user.Email,
                name = user.Name,
                username = user.Username,
                role = user.Role
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("Invalid ID token");
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
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sub))
                return BadRequest("Missing required claims");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = email.Split('@')[0],
                    Name = "Apple User",
                    Role = "user"
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Apple login error: {ex.Message}");
        }
    }

    // 🔐 Helper method
    private string GenerateJwtToken(User user)
    {
        // Force reload from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var keyString = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"]; // optional

        if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
            throw new Exception("JWT key in appsettings.json is invalid or too short");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role ?? "user")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
