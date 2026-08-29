using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Tickets
{
    /// <inheritdoc />
    public partial class TicketRelatedContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedInvoiceId",
                schema: "tickets",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedStudentId",
                schema: "tickets",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedStudyGroupId",
                schema: "tickets",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RelatedInvoiceId",
                schema: "tickets",
                table: "Tickets",
                column: "RelatedInvoiceId",
                filter: "\"RelatedInvoiceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RelatedStudentId",
                schema: "tickets",
                table: "Tickets",
                column: "RelatedStudentId",
                filter: "\"RelatedStudentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RelatedStudyGroupId",
                schema: "tickets",
                table: "Tickets",
                column: "RelatedStudyGroupId",
                filter: "\"RelatedStudyGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_RelatedInvoiceId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_RelatedStudentId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_RelatedStudyGroupId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RelatedInvoiceId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RelatedStudentId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RelatedStudyGroupId",
                schema: "tickets",
                table: "Tickets");
        }
    }
}
