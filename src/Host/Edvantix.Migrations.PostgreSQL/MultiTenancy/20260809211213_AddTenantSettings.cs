using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantSettings",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                schema: "tenant",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            // Backfill: every tenant that existed before this migration gets a settings row with
            // the defaults (UTC / USD) — see "Задачи · Доработки каркаса" > Multitenancy. Without
            // this, GetOrCreateAsync's DB-miss fallback masks the gap at read time, but writes
            // (UpdateTenantSettingsCommand) would silently start a fresh row instead of the one the
            // tenant thinks it's editing.
            migrationBuilder.Sql(
                """
                INSERT INTO tenant."TenantSettings" ("Id", "TenantId", "TimeZoneId", "Currency", "CreatedOnUtc", "CreatedBy")
                SELECT gen_random_uuid(), t."Id", 'UTC', 'USD', now(), 'migration:AddTenantSettings'
                FROM tenant."Tenants" t
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant."TenantSettings" s WHERE s."TenantId" = t."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSettings",
                schema: "tenant");
        }
    }
}
