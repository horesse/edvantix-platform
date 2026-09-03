using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class StudyGroupChannelBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only backfill (EDX-010). The SourceStudyGroupId column shipped together with the
            // study-group channel feature, so on a clean deployment there is nothing to fill. This is
            // a safety net for any channel that was created / linked before the marker existed: match
            // it back to its group via the id StudyGroups already stores on StudyGroup.ChatChannelId.
            // Guarded so it is a no-op when the StudyGroups module is not deployed in this database.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('study_groups."StudyGroups"') IS NOT NULL THEN
                        UPDATE chat."Channels" AS c
                        SET "SourceStudyGroupId" = g."Id"
                        FROM study_groups."StudyGroups" AS g
                        WHERE g."ChatChannelId" = c."Id"
                          AND c."SourceStudyGroupId" IS NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data backfill — nothing to undo.
        }
    }
}
