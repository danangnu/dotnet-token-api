using System;
using Microsoft.EntityFrameworkCore.Metadata;
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
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IssuerUsername = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientUsername = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuerId = table.Column<int>(type: "int", nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Tokens",
                columns: new[] { "Id", "Amount", "ExpirationDate", "IssuedAt", "IssuerId", "IssuerUsername", "RecipientId", "RecipientName", "RecipientUsername", "Remarks", "Status" },
                values: new object[] { 1, 10m, new DateTime(2025, 7, 29, 5, 19, 14, 144, DateTimeKind.Utc).AddTicks(9910), new DateTime(2025, 7, 22, 5, 19, 14, 144, DateTimeKind.Utc).AddTicks(9909), 2, "alice", 3, "Bob Johnson", "bob", "Test token", "pending" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@example.com", "Administrator", "$2a$11$kz5HrCyCm5074gPvm4RLF.zXIQbgRmS08SIwp1ydQbJraKttRJeg2", "admin", "admin" },
                    { 2, "alice@example.com", "Alice Smith", "$2a$11$MNz8xemb7cLH14PowZgz2eURx30NkhXiV.ixANQC21s3qC8NVphAu", "user", "alice" },
                    { 3, "bob@example.com", "Bob Johnson", "$2a$11$zReEwOoU8Wd5H3cxsbavm.RL8C7mlzdhns/NBvjmFTm43xwUhnj7i", "user", "bob" }
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
