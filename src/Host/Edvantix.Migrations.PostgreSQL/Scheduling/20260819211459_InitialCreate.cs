using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Scheduling
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    MarkedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MarkedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "scheduling",
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
                name: "NonWorkingDays",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonWorkingDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "scheduling",
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
                name: "Rooms",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Topic = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MeetingUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RescheduledFromId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeacherComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SessionId_StudentId",
                schema: "scheduling",
                table: "Attendances",
                columns: new[] { "SessionId", "StudentId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Status",
                schema: "scheduling",
                table: "Attendances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId",
                schema: "scheduling",
                table: "Attendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_NonWorkingDays_Date",
                schema: "scheduling",
                table: "NonWorkingDays",
                columns: new[] { "Date", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_IsVirtual",
                schema: "scheduling",
                table: "Rooms",
                column: "IsVirtual");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                schema: "scheduling",
                table: "Rooms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_IsActive",
                schema: "scheduling",
                table: "ScheduleTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_RoomId",
                schema: "scheduling",
                table: "ScheduleTemplates",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_StudyGroupId",
                schema: "scheduling",
                table: "ScheduleTemplates",
                column: "StudyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_TeacherId",
                schema: "scheduling",
                table: "ScheduleTemplates",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LessonId",
                schema: "scheduling",
                table: "Sessions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RescheduledFromId",
                schema: "scheduling",
                table: "Sessions",
                column: "RescheduledFromId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RoomId",
                schema: "scheduling",
                table: "Sessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ScheduleTemplateId_StartUtc",
                schema: "scheduling",
                table: "Sessions",
                columns: new[] { "ScheduleTemplateId", "StartUtc", "TenantId" },
                unique: true,
                filter: "\"ScheduleTemplateId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartUtc",
                schema: "scheduling",
                table: "Sessions",
                column: "StartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                schema: "scheduling",
                table: "Sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StudyGroupId",
                schema: "scheduling",
                table: "Sessions",
                column: "StudyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TeacherId",
                schema: "scheduling",
                table: "Sessions",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "NonWorkingDays",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ScheduleTemplates",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "scheduling");
        }
    }
}
