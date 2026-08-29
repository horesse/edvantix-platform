using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Tickets
{
    /// <inheritdoc />
    public partial class TicketClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill existing rows with the enum's default name (the string converter round-trips
            // by name, so "" would fail to read back). New rows always carry an explicit value.
            migrationBuilder.AddColumn<string>(
                name: "Audience",
                schema: "tickets",
                table: "Tickets",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "School");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "tickets",
                table: "Tickets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Audience",
                schema: "tickets",
                table: "Tickets",
                column: "Audience");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Category",
                schema: "tickets",
                table: "Tickets",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Audience",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Category",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Audience",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "tickets",
                table: "Tickets");
        }
    }
}
