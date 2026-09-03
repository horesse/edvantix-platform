using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddInvoiceNumberTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumberTemplate",
                schema: "tenant",
                table: "TenantSettings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "{YYYY}-{NNNN}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceNumberTemplate",
                schema: "tenant",
                table: "TenantSettings");
        }
    }
}
