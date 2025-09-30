using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dotnet_token_api.Migrations
{
    /// <inheritdoc />
    public partial class DebtsActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Debts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromUserId = table.Column<int>(type: "int", nullable: false),
                    ToUserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsSettled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Debts", x => x.Id);
                })
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
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DebtActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DebtId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PerformedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtActivities_Debts_DebtId",
                        column: x => x.DebtId,
                        principalTable: "Debts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Tokens",
                columns: new[] { "Id", "Amount", "ExpirationDate", "IssuedAt", "IssuerId", "IssuerUsername", "RecipientId", "RecipientName", "RecipientUsername", "Remarks", "Status" },
                values: new object[,]
                {
                    { 1, 50m, new DateTime(2025, 10, 30, 11, 53, 54, 70, DateTimeKind.Utc).AddTicks(5068), new DateTime(2025, 9, 27, 11, 53, 54, 70, DateTimeKind.Utc).AddTicks(5059), 4, "user1", 5, "Bob Johnson", "user2", "Welcome credit", "accepted" },
                    { 2, 75m, new DateTime(2025, 11, 14, 11, 53, 54, 70, DateTimeKind.Utc).AddTicks(5077), new DateTime(2025, 9, 29, 11, 53, 54, 70, DateTimeKind.Utc).AddTicks(5076), 5, "user2", 6, "Charlie Brown", "user3", "Project advance", "pending" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailTokenExpiry", "EmailVerificationToken", "IsEmailVerified", "Name", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@example.com", null, null, false, "Administrator", "$2a$11$iHc/wjm1ky1sdneODv8NQeI6.HXjDU6oJONNMOHYPIxsZLIPcwkJm", null, null, "Admin", "admin" },
                    { 2, "manager@example.com", null, null, false, "Manager One", "$2a$11$RGGxh5hD7YYQ3hkG86mHPOee6uIyoKkdLuVh/SdHVIfw87vyLHsMe", null, null, "Manager", "manager" },
                    { 3, "auditor@example.com", null, null, false, "Auditor Jane", "$2a$11$f5oBFOdg4N8KizHVz1twh.LwDWKF2T4zR1bwcenbVm6jrQgAjd336", null, null, "Auditor", "auditor" },
                    { 4, "alice@example.com", null, null, false, "Alice Smith", "$2a$11$ELDnV1tFdB0xbo6T3bQJWOw8TjTNinbGIgfqKoz6S02p0hIdlW.eK", null, null, "User", "user1" },
                    { 5, "bob@example.com", null, null, false, "Bob Johnson", "$2a$11$77hdjyB5HOYKPCVt0XVLaeFglitCLdiTH63QtZBujQF9MaPQbeSo.", null, null, "User", "user2" },
                    { 6, "charlie@example.com", null, null, false, "Charlie Brown", "$2a$11$59oMcQzl4moHmcWS83yyBO7XV0f54qEYMXcXOQokTjz8mGDGzEnSO", null, null, "User", "user3" },
                    { 7, "diana@example.com", null, null, false, "Diana Prince", "$2a$11$5.skadJK/pzZf8VK25i79euL6D2mQ6iikrOn/MzmXLKcgRhNCHA.i", null, null, "User", "user4" },
                    { 8, "ethan@example.com", null, null, false, "Ethan Hunt", "$2a$11$yIzRVp3JeRFS1CyD.KQGauKcmI/5yUiynoCdJPfVCZn0F1W2q47Fy", null, null, "User", "user5" },
                    { 9, "fiona@example.com", null, null, false, "Fiona Apple", "$2a$11$JJFDfdWrUlPslkBVSsL2FuBaDPxz7Ommmg3JpjHEQGafY9H4MQjXu", null, null, "User", "user6" },
                    { 10, "george@example.com", null, null, false, "George Miller", "$2a$11$Hiy5DefyUX2./60aTojCLuBQF.2BZD/IAB/F2eO.a4QKN/TaQAs4W", null, null, "User", "user7" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DebtActivities_DebtId",
                table: "DebtActivities",
                column: "DebtId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebtActivities");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Debts");
        }
    }
}
