using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.StudyGroups
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "study_groups");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.Id, x.HandlerName });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    IsDead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyGroups",
                schema: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryTeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChatChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupEnrollments",
                schema: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LeaveReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TariffId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupEnrollments_StudyGroups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalSchema: "study_groups",
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupTeachers",
                schema: "study_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupTeachers_StudyGroups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalSchema: "study_groups",
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollments_Status",
                schema: "study_groups",
                table: "GroupEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollments_StudentId",
                schema: "study_groups",
                table: "GroupEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollments_StudyGroupId_StudentId",
                schema: "study_groups",
                table: "GroupEnrollments",
                columns: new[] { "StudyGroupId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupTeachers_StudyGroupId_TeacherId",
                schema: "study_groups",
                table: "GroupTeachers",
                columns: new[] { "StudyGroupId", "TeacherId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupTeachers_TeacherId",
                schema: "study_groups",
                table: "GroupTeachers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_Code",
                schema: "study_groups",
                table: "StudyGroups",
                columns: new[] { "Code", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_CourseId",
                schema: "study_groups",
                table: "StudyGroups",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_IsDeleted",
                schema: "study_groups",
                table: "StudyGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_PrimaryTeacherId",
                schema: "study_groups",
                table: "StudyGroups",
                column: "PrimaryTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_Status",
                schema: "study_groups",
                table: "StudyGroups",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupEnrollments",
                schema: "study_groups");

            migrationBuilder.DropTable(
                name: "GroupTeachers",
                schema: "study_groups");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "study_groups");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "study_groups");

            migrationBuilder.DropTable(
                name: "StudyGroups",
                schema: "study_groups");
        }
    }
}
