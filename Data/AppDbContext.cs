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
    /// Static data seeded via EF migrations (HasData).
    /// Good for fixed users/roles and a couple tokens.
    /// </summary>
    public static void Seed(ModelBuilder modelBuilder)
    {
        // ---- Fixed users (stable IDs for HasData) ----
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

        // ---- A couple of baseline tokens for demo ----
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
    /// (Optional) Generate additional demo users at runtime (NOT HasData).
    /// Call once at startup before seeding debts/tokens if you need a large graph.
    /// </summary>
    public static void EnsureDemoUsers(AppDbContext context, int totalUsers = 40)
    {
        // We already have 10 from HasData. Create the rest up to totalUsers.
        var existing = context.Users.Count();
        if (existing >= totalUsers) return;

        var toCreate = totalUsers - existing;
        var startIndex = existing + 1;

        var list = new List<User>();
        for (int i = 0; i < toCreate; i++)
        {
            var idx = startIndex + i;
            list.Add(new User
            {
                Username = $"user{idx}",
                Name = $"Demo User {idx}",
                Email = $"user{idx}@example.com",
                Role = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!")
            });
        }

        context.Users.AddRange(list);
        context.SaveChanges();
    }

    /// <summary>
    /// Runtime seed for debts so you can build multiple loops and cross edges easily.
    /// Call this once on app startup AFTER users exist (and after migrations).
    /// </summary>
    public static void SeedDebts(AppDbContext context, int loopCount = 6, int usersPerLoop = 4)
    {
        if (context.Debts.Any()) return;

        var users = context.Users
            .OrderBy(u => u.Id)
            .ToList();

        // Need enough users to form the requested loops
        if (users.Count < loopCount * usersPerLoop) return;

        var now = DateTime.UtcNow;
        var debts = new List<Debt>();

        // Create N loops of K users each
        for (int l = 0; l < loopCount; l++)
        {
            var block = users.Skip(l * usersPerLoop).Take(usersPerLoop).ToList();
            if (block.Count < usersPerLoop) break;

            for (int i = 0; i < usersPerLoop; i++)
            {
                var from = block[i];
                var to = block[(i + 1) % usersPerLoop];
                debts.Add(new Debt
                {
                    FromUserId = from.Id,
                    ToUserId = to.Id,
                    Amount = 100 + (l * 15) + (i * 7), // staggered amounts
                    CreatedAt = now
                });
            }
        }

        // Cross-edges between loops to make the graph interesting
        for (int i = 0; i < loopCount; i++)
        {
            var fromIdx = (i * usersPerLoop) % users.Count;
            var toIdx = ((i + 1) * usersPerLoop + 1) % users.Count;
            debts.Add(new Debt
            {
                FromUserId = users[fromIdx].Id,
                ToUserId = users[toIdx].Id,
                Amount = 40 + i * 10,
                CreatedAt = now
            });
        }

        // A few random extra edges
        var rnd = new Random();
        for (int i = 0; i < loopCount * 2; i++)
        {
            var a = users[rnd.Next(users.Count)];
            var b = users[rnd.Next(users.Count)];
            if (a.Id == b.Id) continue;

            debts.Add(new Debt
            {
                FromUserId = a.Id,
                ToUserId = b.Id,
                Amount = rnd.Next(30, 250),
                CreatedAt = now.AddMinutes(-rnd.Next(0, 5000))
            });
        }

        context.Debts.AddRange(debts);
        context.SaveChanges();

        // Activities (optional)
        var activities = debts.Select(d => new DebtActivity
        {
            DebtId = d.Id,
            Action = "Issued",
            Timestamp = now,
            PerformedBy = "system-seed"
        }).ToList();

        context.DebtActivities.AddRange(activities);
        context.SaveChanges();
    }

    /// <summary>
    /// Runtime seed for tokens to simulate trading/transfer behavior.
    /// </summary>
    public static void SeedTokens(AppDbContext context, int tokenCount = 120)
    {
        // Avoid duplicating too much demo data if already seeded
        if (context.Tokens.Count() > 40) return;

        var users = context.Users.ToList();
        if (users.Count < 8) return;

        var rnd = new Random();
        var now = DateTime.UtcNow;

        var tokens = new List<Token>();
        for (int i = 0; i < tokenCount; i++)
        {
            var issuer = users[rnd.Next(users.Count)];
            var recipient = users[rnd.Next(users.Count)];
            if (issuer.Id == recipient.Id) continue;

            var status = (i % 5 == 0) ? "declined"
                       : (i % 3 == 0) ? "pending"
                       : "accepted";

            tokens.Add(new Token
            {
                IssuerId = issuer.Id,
                IssuerUsername = issuer.Username,
                RecipientId = recipient.Id,
                RecipientUsername = recipient.Username,
                RecipientName = recipient.Name,
                Amount = rnd.Next(10, 400),
                Status = status,
                Remarks = "Auto-seeded demo token",
                IssuedAt = now.AddMinutes(-rnd.Next(10, 20_000)),
                ExpirationDate = now.AddDays(rnd.Next(15, 120))
            });
        }

        context.Tokens.AddRange(tokens);
        context.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        Seed(modelBuilder);
    }
}
