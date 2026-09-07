using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Scheduling
{
    /// <inheritdoc />
    public partial class AddIcalSubscriptionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IcalSubscriptionTokens",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcalSubscriptionTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IcalSubscriptionTokens_Token",
                schema: "scheduling",
                table: "IcalSubscriptionTokens",
                columns: new[] { "Token", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IcalSubscriptionTokens_UserId",
                schema: "scheduling",
                table: "IcalSubscriptionTokens",
                columns: new[] { "UserId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IcalSubscriptionTokens",
                schema: "scheduling");
        }
    }
}
