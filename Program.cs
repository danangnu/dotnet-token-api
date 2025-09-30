using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using dotnet_token_api.Services;
using System.Security.Claims; // ✅ Required for UseSqlite

var basePath = System.IO.Directory.GetCurrentDirectory();
var configuration = new ConfigurationBuilder()
	.SetBasePath(basePath) 
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
var key = configuration["Jwt:Key"];
var issuer = configuration["Jwt:Issuer"];
Console.WriteLine($"[DEBUG] JWT Key: '{key}' (Length: {key.Length} chars)");
Console.WriteLine($"[DEBUG] Byte Length: {Encoding.UTF8.GetBytes(key).Length} bytes");

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(key))
    throw new Exception("JWT Key is not configured. Please set Jwt:Key in appsettings.json.");

// CORS policy name
var corsPolicy = "_allowReactApp";

// Register CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy =>
        {
            policy.WithOrigins(
                        "http://localhost:3000",
                        "https://nattesc.vercel.app",
                        "https://nattesc.onrender.com"
                    )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// ✅ Add EF Core DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            // 👇 make sure ASP.NET uses the same role claim type you put in the token
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// Optional but nice: policies
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("ManagerOrAdmin", p => p.RequireRole("Manager", "Admin"));
});


builder.Services.AddScoped<DebtCycleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Origin: {context.Request.Headers["Origin"]}");
    await next();
});

// Auto-migrate & seed (optional)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // 1) Ensure we have plenty of demo users (optional, bump 40 → 80 if you want bigger)
    AppDbContext.EnsureDemoUsers(db, totalUsers: 40);

    // 2) Seed runtime demo debts/tokens (only if not present)
    AppDbContext.SeedDebts(db, loopCount: 8, usersPerLoop: 4);   // tweak to get bigger loops
    AppDbContext.SeedTokens(db, tokenCount: 200);                // more tokens = richer tables
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(corsPolicy);
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run("http://0.0.0.0:10000");