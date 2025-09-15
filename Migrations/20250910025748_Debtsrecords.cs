using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dotnet_token_api.Migrations
{
    /// <inheritdoc />
    public partial class Debtsrecords : Migration
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
                    { 1, 50m, new DateTime(2025, 10, 10, 2, 57, 48, 336, DateTimeKind.Utc).AddTicks(8685), new DateTime(2025, 9, 7, 2, 57, 48, 336, DateTimeKind.Utc).AddTicks(8667), 4, "user1", 5, "Bob Johnson", "user2", "Welcome credit", "accepted" },
                    { 2, 75m, new DateTime(2025, 10, 25, 2, 57, 48, 336, DateTimeKind.Utc).AddTicks(8700), new DateTime(2025, 9, 9, 2, 57, 48, 336, DateTimeKind.Utc).AddTicks(8700), 5, "user2", 6, "Charlie Brown", "user3", "Project advance", "pending" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailTokenExpiry", "EmailVerificationToken", "IsEmailVerified", "Name", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@example.com", null, null, false, "Administrator", "$2a$11$DFqAGsSE3NGgjlAI7P8nI.d3maEW/hTvV2RpfP/baMRFc1avWQQie", null, null, "Admin", "admin" },
                    { 2, "manager@example.com", null, null, false, "Manager One", "$2a$11$DLvBftNyCYa3ITdbCwLf8uvPqRfEl7P31sGpmu9oqvm3gcEoenVRW", null, null, "Manager", "manager" },
                    { 3, "auditor@example.com", null, null, false, "Auditor Jane", "$2a$11$AbeQHLAtE50AHy/sNJ4g3em.SOvYJibdwzONtQF2HKuyX6p4ht.mS", null, null, "Auditor", "auditor" },
                    { 4, "alice@example.com", null, null, false, "Alice Smith", "$2a$11$yi.hFsOiDHD9nSerarnCIelTUtra839GqlQwF2wL8U8/In3THwGji", null, null, "User", "user1" },
                    { 5, "bob@example.com", null, null, false, "Bob Johnson", "$2a$11$4KHc1LH/nB8PUa9iJ4NovOAXW3QNWG74yk5Ft52uX7NbkVRqFK216", null, null, "User", "user2" },
                    { 6, "charlie@example.com", null, null, false, "Charlie Brown", "$2a$11$ijUAJd2bvT.QAr5k6PatZ.MannLhfRnvnFu2BZnk1p.4Bg69MO4W.", null, null, "User", "user3" },
                    { 7, "diana@example.com", null, null, false, "Diana Prince", "$2a$11$tdW8yUEDwE1PXHR8SzmsmO5MW6RWgn8ADxgxKZMQ1iopStybua0fK", null, null, "User", "user4" },
                    { 8, "ethan@example.com", null, null, false, "Ethan Hunt", "$2a$11$Lmrlq4IK3FS6CFqlwSQpbeWYn2oldPcpPdJJ7BY0jWbHtURxqkm8S", null, null, "User", "user5" },
                    { 9, "fiona@example.com", null, null, false, "Fiona Apple", "$2a$11$dD9akh5lCSIDt/sMUQ2pfO4vTLxtquPxuLhq.cJm2HBEbMAPfeevq", null, null, "User", "user6" },
                    { 10, "george@example.com", null, null, false, "George Miller", "$2a$11$iZzc9OfSuZfSCkfn5CJZCOO9gizJSuOgvce0KvS20.PaTNCo14z2C", null, null, "User", "user7" }
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
