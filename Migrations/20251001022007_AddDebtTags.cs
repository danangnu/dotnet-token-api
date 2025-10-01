using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dotnet_token_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtTags : Migration
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
                    Amount = table.Column<double>(type: "double", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Tag = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    { 1, 50m, new DateTime(2025, 10, 31, 2, 20, 6, 158, DateTimeKind.Utc).AddTicks(1750), new DateTime(2025, 9, 28, 2, 20, 6, 158, DateTimeKind.Utc).AddTicks(1734), 4, "user1", 5, "Bob Johnson", "user2", "Welcome credit", "accepted" },
                    { 2, 75m, new DateTime(2025, 11, 15, 2, 20, 6, 158, DateTimeKind.Utc).AddTicks(1768), new DateTime(2025, 9, 30, 2, 20, 6, 158, DateTimeKind.Utc).AddTicks(1767), 5, "user2", 6, "Charlie Brown", "user3", "Project advance", "pending" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "EmailTokenExpiry", "EmailVerificationToken", "IsEmailVerified", "Name", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "admin@example.com", null, null, false, "Administrator", "$2a$11$lzafyhbuWpstvCb3R94BSu8aQMBZlvE6.HmYAvFbljcfNDTPXcUKq", null, null, "Admin", "admin" },
                    { 2, "manager@example.com", null, null, false, "Manager One", "$2a$11$Ie3KNErrWHIa8hbeGurIUeVyorcowt9XF9VkgMJ18/Yud7sD6nj.G", null, null, "Manager", "manager" },
                    { 3, "auditor@example.com", null, null, false, "Auditor Jane", "$2a$11$Y2Owt8BD9VzkXBVKfuGk7.wVtVGVPp82OhhBBeZYuO/MG.xwG2mRi", null, null, "Auditor", "auditor" },
                    { 4, "alice@example.com", null, null, false, "Alice Smith", "$2a$11$1oS3BHCyTwjDEqc7GGn1VuKOIc.DTO/C4OLnpvmcXbCQBq2c4SqVS", null, null, "User", "user1" },
                    { 5, "bob@example.com", null, null, false, "Bob Johnson", "$2a$11$iVqwR0zTC63bQgAUCWVv5.zobRRRo.5ziHT7zoBcFt5XhecLKXL/S", null, null, "User", "user2" },
                    { 6, "charlie@example.com", null, null, false, "Charlie Brown", "$2a$11$0VRgfHkwdRlIOTtU2EPTTubDiaOdgB6rly/vHsiclv9Yu7vpfq9iS", null, null, "User", "user3" },
                    { 7, "diana@example.com", null, null, false, "Diana Prince", "$2a$11$BIPa1rFN5UtecqtYdAyTU.Ob/bc10q5LGeRhTrCuz6t1.hcDt0jyS", null, null, "User", "user4" },
                    { 8, "ethan@example.com", null, null, false, "Ethan Hunt", "$2a$11$yCubssNv6zjt3C0PaDZDEeOORdMbrie6X/s07rY0ryZo7XSD2Ujgu", null, null, "User", "user5" },
                    { 9, "fiona@example.com", null, null, false, "Fiona Apple", "$2a$11$SnxeWBThsVkFy6tf0mCbxe3aNT72053hm9cssNjedQdHkDNOIY2tm", null, null, "User", "user6" },
                    { 10, "george@example.com", null, null, false, "George Miller", "$2a$11$JVWmAmMLD5mVwUoXAh1YE.9tk/G8qz.3Oy7mkvzRaMOKP0s9x6d32", null, null, "User", "user7" }
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
