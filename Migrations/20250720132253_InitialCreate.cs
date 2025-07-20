using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dotnet_token_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IssuerUsername = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientUsername = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    IssuerId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tokens",
                columns: new[] { "Id", "Amount", "ExpirationDate", "IssuedAt", "IssuerId", "IssuerUsername", "RecipientId", "RecipientName", "RecipientUsername", "Remarks", "Status" },
                values: new object[] { 1, 10m, new DateTime(2025, 7, 27, 13, 22, 53, 635, DateTimeKind.Utc).AddTicks(8248), new DateTime(2025, 7, 20, 13, 22, 53, 635, DateTimeKind.Utc).AddTicks(8144), 2, "alice", 3, "Bob Johnson", "bob", "Test token", "pending" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@example.com", "Administrator", "$2a$11$hpBmz45nJ7LD14XfqzBQ2uIEIVEUYoHDwR3jTC4h9K9gCRhV7Wk9y", "admin", "admin" },
                    { 2, "alice@example.com", "Alice Smith", "$2a$11$4uveJRhnaCu3lqkIjT/uruH3lQWg4CNKb4cvjUEVEM1nVfduPf/JG", "user", "alice" },
                    { 3, "bob@example.com", "Bob Johnson", "$2a$11$q7ingtuqvm8DtQ/hHqbzPeXp66nDNx4V4AlNuVyDGJNAIwnkB.Nf.", "user", "bob" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
