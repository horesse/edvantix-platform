using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddDebtAccessRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DebtGraceDays",
                schema: "tenant",
                table: "TenantSettings",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictMaterialsOnDebt",
                schema: "tenant",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebtGraceDays",
                schema: "tenant",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "RestrictMaterialsOnDebt",
                schema: "tenant",
                table: "TenantSettings");
        }
    }
}
