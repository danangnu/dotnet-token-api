using dotnet_token_api.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!;
    public DbSet<Debt> Debts { get; set; } = null!;
    public DbSet<DebtActivity> DebtActivities { get; set; } = null!;

    /// <summary>
    /// Static data seeded via EF migrations (HasData). Good for fixed users/roles and a couple tokens.
    /// </summary>
    public static void Seed(ModelBuilder modelBuilder)
    {
        // --- Users (role-diverse) ---
        // NOTE: HasData requires explicit IDs & deterministic values
        var users = new[]
        {
            new User { Id = 1, Username = "admin",   Name = "Administrator", Email = "admin@example.com",   Role = "Admin",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!") },
            new User { Id = 2, Username = "manager", Name = "Manager One",   Email = "manager@example.com", Role = "Manager", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!") },
            new User { Id = 3, Username = "auditor", Name = "Auditor Jane",  Email = "auditor@example.com", Role = "Auditor", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Auditor123!") },

            new User { Id = 4, Username = "user1", Name = "Alice Smith",    Email = "alice@example.com",   Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 5, Username = "user2", Name = "Bob Johnson",    Email = "bob@example.com",     Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 6, Username = "user3", Name = "Charlie Brown",  Email = "charlie@example.com", Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 7, Username = "user4", Name = "Diana Prince",   Email = "diana@example.com",   Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 8, Username = "user5", Name = "Ethan Hunt",     Email = "ethan@example.com",   Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 9, Username = "user6", Name = "Fiona Apple",    Email = "fiona@example.com",   Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") },
            new User { Id = 10, Username = "user7", Name = "George Miller", Email = "george@example.com",  Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!") }
        };
        modelBuilder.Entity<User>().HasData(users);

        // --- A couple of starting tokens (purely illustrative) ---
        // CAUTION: DateTime.UtcNow is evaluated at migration generation time.
        // That’s OK for demo—but if you regenerate migrations frequently, consider fixed dates.
        modelBuilder.Entity<Token>().HasData(
            new Token
            {
                Id = 1,
                IssuerId = 4, IssuerUsername = "user1",
                RecipientId = 5, RecipientUsername = "user2", RecipientName = "Bob Johnson",
                Amount = 50, Status = "accepted",
                Remarks = "Welcome credit",
                IssuedAt = DateTime.UtcNow.AddDays(-3),
                ExpirationDate = DateTime.UtcNow.AddDays(30)
            },
            new Token
            {
                Id = 2,
                IssuerId = 5, IssuerUsername = "user2",
                RecipientId = 6, RecipientUsername = "user3", RecipientName = "Charlie Brown",
                Amount = 75, Status = "pending",
                Remarks = "Project advance",
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                ExpirationDate = DateTime.UtcNow.AddDays(45)
            }
        );
    }

    /// <summary>
    /// Runtime seed for debts (and optionally activities) so you can build loops and cross edges easily.
    /// Call this once on app startup after migrations.
    /// </summary>
    public static void SeedDebts(AppDbContext context)
    {
        if (context.Debts.Any()) return;

        // Make sure our users exist
        var u = context.Users.ToDictionary(x => x.Username, x => x);
        if (!u.ContainsKey("user1") || !u.ContainsKey("user2") || !u.ContainsKey("user3")
            || !u.ContainsKey("user4") || !u.ContainsKey("user5") || !u.ContainsKey("user6") || !u.ContainsKey("user7"))
        {
            // Not enough users yet (likely first migration not applied). Just bail safely.
            return;
        }

        var now = DateTime.UtcNow;

        var debts = new List<Debt>
        {
            // --- Loop A: user1 → user2 → user3 → user1 ---
            new Debt { FromUserId = u["user1"].Id, ToUserId = u["user2"].Id, Amount = 100, CreatedAt = now },
            new Debt { FromUserId = u["user2"].Id, ToUserId = u["user3"].Id, Amount = 110, CreatedAt = now },
            new Debt { FromUserId = u["user3"].Id, ToUserId = u["user1"].Id, Amount = 120, CreatedAt = now },

            // --- Loop B: user4 → user5 → user6 → user7 → user4 ---
            new Debt { FromUserId = u["user4"].Id, ToUserId = u["user5"].Id, Amount = 200, CreatedAt = now },
            new Debt { FromUserId = u["user5"].Id, ToUserId = u["user6"].Id, Amount = 210, CreatedAt = now },
            new Debt { FromUserId = u["user6"].Id, ToUserId = u["user7"].Id, Amount = 220, CreatedAt = now },
            new Debt { FromUserId = u["user7"].Id, ToUserId = u["user4"].Id, Amount = 230, CreatedAt = now },

            // --- Cross edges (for extra realism) ---
            new Debt { FromUserId = u["user2"].Id, ToUserId = u["user5"].Id, Amount = 90, CreatedAt = now },
            new Debt { FromUserId = u["user6"].Id, ToUserId = u["user1"].Id, Amount = 60, CreatedAt = now }
        };

        context.Debts.AddRange(debts);
        context.SaveChanges();

        // Optional: seed a few activities
        var activities = debts.Select(d => new DebtActivity
        {
            DebtId = d.Id,
            Action = "Issued",
            Timestamp = now,
            PerformedBy = "system-seed"
        });
        context.DebtActivities.AddRange(activities);
        context.SaveChanges();
    }

    /// <summary>
    /// Optional: runtime token seeding for larger demos.
    /// </summary>
    public static void SeedTokens(AppDbContext context)
    {
        if (context.Tokens.Count() > 5) return; // avoid duplicating a lot

        var u = context.Users.ToDictionary(x => x.Username, x => x);
        if (!u.ContainsKey("user1") || !u.ContainsKey("user2") || !u.ContainsKey("user3") || !u.ContainsKey("user4")) return;

        var issue = DateTime.UtcNow.AddHours(-6);

        var extras = new[]
        {
            new Token {
                IssuerId = u["user3"].Id, IssuerUsername = "user3",
                RecipientId = u["user4"].Id, RecipientUsername = "user4", RecipientName = u["user4"].Name,
                Amount = 35, Status = "accepted", Remarks = "Referral bonus",
                IssuedAt = issue, ExpirationDate = issue.AddDays(60)
            },
            new Token {
                IssuerId = u["user4"].Id, IssuerUsername = "user4",
                RecipientId = u["user1"].Id, RecipientUsername = "user1", RecipientName = u["user1"].Name,
                Amount = 15, Status = "declined", Remarks = "Promo",
                IssuedAt = issue.AddHours(1), ExpirationDate = issue.AddDays(90)
            }
        };

        context.Tokens.AddRange(extras);
        context.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        Seed(modelBuilder);
    }
}
