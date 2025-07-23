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


    public static void Seed(ModelBuilder modelBuilder)
    {
        var admin = new User
        {
            Id = 1,
            Username = "admin",
            Name = "Administrator",
            Email = "admin@example.com",
            Role = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
        };

        var user1 = new User
        {
            Id = 2,
            Username = "alice",
            Name = "Alice Smith",
            Email = "alice@example.com",
            Role = "user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
        };

        var user2 = new User
        {
            Id = 3,
            Username = "bob",
            Name = "Bob Johnson",
            Email = "bob@example.com",
            Role = "user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
        };

        modelBuilder.Entity<User>().HasData(admin, user1, user2);

        modelBuilder.Entity<Token>().HasData(
            new Token
            {
                Id = 1,
                IssuerId = 2,
                IssuerUsername = "alice",
                RecipientId = 3,
                RecipientUsername = "bob",
                RecipientName = "Bob Johnson",
                Amount = 10,
                Status = "pending",
                Remarks = "Test token",
                IssuedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(7)
            }
        );
    }

    public static void SeedDebts(AppDbContext context)
    {
        if (!context.Debts.Any())
        {
            var users = context.Users.OrderBy(u => u.Id).Take(4).ToList();
            if (users.Count < 4) return;

            var debts = new List<Debt>
            {
                new Debt
                {
                    FromUserId = users[0].Id, // User A
                    ToUserId = users[1].Id,   // → User B
                    Amount = 100
                },
                new Debt
                {
                    FromUserId = users[1].Id, // B
                    ToUserId = users[2].Id,   // → C
                    Amount = 110
                },
                new Debt
                {
                    FromUserId = users[2].Id, // C
                    ToUserId = users[3].Id,   // → D
                    Amount = 120
                },
                new Debt
                {
                    FromUserId = users[3].Id, // D
                    ToUserId = users[0].Id,   // → A (loop)
                    Amount = 130
                }
            };

            context.Debts.AddRange(debts);
            context.SaveChanges();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        Seed(modelBuilder);
    }
}
