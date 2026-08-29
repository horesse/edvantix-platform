using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class StudyGroupChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                schema: "chat",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceStudyGroupId",
                schema: "chat",
                table: "Channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_SourceStudyGroupId",
                schema: "chat",
                table: "Channels",
                column: "SourceStudyGroupId",
                filter: "\"SourceStudyGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Channels_SourceStudyGroupId",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "SourceStudyGroupId",
                schema: "chat",
                table: "Channels");
        }
    }
}
