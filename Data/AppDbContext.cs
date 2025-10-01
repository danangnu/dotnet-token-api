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
        if (context.Debts.Any(d => d.Tag == null)) return; // don't re-seed generic debts if they exist

        var users = context.Users
            .OrderBy(u => u.Id)
            .ToList();

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
                    Amount = 100 + (l * 15) + (i * 7),
                    CreatedAt = now,
                    Tag = null // generic / untagged dataset
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
                CreatedAt = DateTime.UtcNow,
                Tag = null
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
                CreatedAt = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 5000)),
                Tag = null
            });
        }

        context.Debts.AddRange(debts);
        context.SaveChanges();

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
                IssuerUsername = issuer.Username!,
                RecipientId = recipient.Id,
                RecipientUsername = recipient.Username!,
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

    /// <summary>
    /// Seed a labeled “BeforeOffset” dataset (circular/raw) and a labeled “AfterOffset” dataset (netted).
    /// Useful for visual comparisons in the UI and demos.
    /// </summary>
    public static void SeedBeforeAndAfterOffset(AppDbContext context)
    {
        // avoid reseeding the scenario if either tag already exists
        if (context.Debts.Any(d => d.Tag == "BeforeOffset" || d.Tag == "AfterOffset"))
            return;

        var users = context.Users.OrderBy(u => u.Id).Take(6).ToList(); // use the first 6 users
        if (users.Count < 3) return;

        var now = DateTime.UtcNow;

        // --- BEFORE OFFSET (contains a loop and a short chain) ---
        var before = new List<Debt>
        {
            // Loop: user1 → user2 → user3 → user1
            new Debt { FromUserId = users[0].Id, ToUserId = users[1].Id, Amount = 100, CreatedAt = now, Tag = "BeforeOffset" },
            new Debt { FromUserId = users[1].Id, ToUserId = users[2].Id, Amount = 120, CreatedAt = now, Tag = "BeforeOffset" },
            new Debt { FromUserId = users[2].Id, ToUserId = users[0].Id, Amount =  80, CreatedAt = now, Tag = "BeforeOffset" },

            // Chain: user4 → user5 → user6
            new Debt { FromUserId = users[3].Id, ToUserId = users[4].Id, Amount = 200, CreatedAt = now, Tag = "BeforeOffset" },
            new Debt { FromUserId = users[4].Id, ToUserId = users[5].Id, Amount = 150, CreatedAt = now, Tag = "BeforeOffset" }
        };
        context.Debts.AddRange(before);
        context.SaveChanges();

        // --- AFTER OFFSET (netted results) ---
        // Loop netting example yields user1 → user2 : 20
        // Chain netting example yields user4 → user6 : 50
        var after = new List<Debt>
        {
            new Debt { FromUserId = users[0].Id, ToUserId = users[1].Id, Amount = 20,  CreatedAt = now, Tag = "AfterOffset" },
            new Debt { FromUserId = users[3].Id, ToUserId = users[5].Id, Amount = 50,  CreatedAt = now, Tag = "AfterOffset" }
        };
        context.Debts.AddRange(after);
        context.SaveChanges();

        // Activities to label provenance in timeline
        var acts = before.Select(d => new DebtActivity
        {
            DebtId = d.Id,
            Action = "Issued",
            Timestamp = now,
            PerformedBy = "system-seed"
        }).ToList();

        acts.AddRange(after.Select(d => new DebtActivity
        {
            DebtId = d.Id,
            Action = "OffsetResult",
            Timestamp = now,
            PerformedBy = "system-seed"
        }));

        context.DebtActivities.AddRange(acts);
        context.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        Seed(modelBuilder);
    }
}