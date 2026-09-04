using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignScheduledPostsWithCanvasConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextCode",
                table: "ScheduledPostAutomations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GroupConversation",
                table: "ScheduledPostAutomations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContextCode",
                table: "ScheduledPostAutomationRuns",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GroupConversation",
                table: "ScheduledPostAutomationRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE ScheduledPostAutomations
                SET ContextCode = CASE Subject
                        WHEN 'Scheduled post 1' THEN 'course_1001'
                        WHEN 'Scheduled post 2' THEN 'course_1002'
                        WHEN 'Scheduled post 3' THEN 'course_1003'
                        WHEN 'Scheduled post 4' THEN 'course_1004'
                        WHEN 'Scheduled post 5' THEN 'course_1005'
                        WHEN 'Scheduled post 6' THEN 'course_1006'
                        WHEN 'Scheduled post 7' THEN 'course_1007'
                        WHEN 'Scheduled post 8' THEN 'course_1008'
                        WHEN 'Scheduled post 9' THEN 'course_1009'
                        WHEN 'Scheduled post 10' THEN 'course_1010'
                        ELSE ContextCode
                    END,
                    Recipients = CASE Subject
                        WHEN 'Scheduled post 1' THEN '["100001"]'
                        WHEN 'Scheduled post 2' THEN '["100002"]'
                        WHEN 'Scheduled post 3' THEN '["100003"]'
                        WHEN 'Scheduled post 4' THEN '["100004"]'
                        WHEN 'Scheduled post 5' THEN '["100005"]'
                        WHEN 'Scheduled post 6' THEN '["100006"]'
                        WHEN 'Scheduled post 7' THEN '["100007"]'
                        WHEN 'Scheduled post 8' THEN '["100008"]'
                        WHEN 'Scheduled post 9' THEN '["100009"]'
                        WHEN 'Scheduled post 10' THEN '["100010"]'
                        ELSE '[]'
                    END,
                    GroupConversation = CASE
                        WHEN Subject LIKE 'Scheduled post %' THEN 1
                        ELSE GroupConversation
                    END
                WHERE Recipients LIKE '%@%';

                UPDATE ScheduledPostAutomationRuns
                SET ContextCode = CASE Subject
                        WHEN 'Scheduled post 1' THEN 'course_1001'
                        WHEN 'Scheduled post 2' THEN 'course_1002'
                        WHEN 'Scheduled post 3' THEN 'course_1003'
                        WHEN 'Scheduled post 4' THEN 'course_1004'
                        WHEN 'Scheduled post 5' THEN 'course_1005'
                        WHEN 'Scheduled post 6' THEN 'course_1006'
                        WHEN 'Scheduled post 7' THEN 'course_1007'
                        WHEN 'Scheduled post 8' THEN 'course_1008'
                        WHEN 'Scheduled post 9' THEN 'course_1009'
                        WHEN 'Scheduled post 10' THEN 'course_1010'
                        ELSE ContextCode
                    END,
                    Recipients = CASE Subject
                        WHEN 'Scheduled post 1' THEN '["100001"]'
                        WHEN 'Scheduled post 2' THEN '["100002"]'
                        WHEN 'Scheduled post 3' THEN '["100003"]'
                        WHEN 'Scheduled post 4' THEN '["100004"]'
                        WHEN 'Scheduled post 5' THEN '["100005"]'
                        WHEN 'Scheduled post 6' THEN '["100006"]'
                        WHEN 'Scheduled post 7' THEN '["100007"]'
                        WHEN 'Scheduled post 8' THEN '["100008"]'
                        WHEN 'Scheduled post 9' THEN '["100009"]'
                        WHEN 'Scheduled post 10' THEN '["100010"]'
                        ELSE '[]'
                    END,
                    GroupConversation = CASE
                        WHEN Subject LIKE 'Scheduled post %' THEN 1
                        ELSE GroupConversation
                    END
                WHERE Recipients LIKE '%@%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextCode",
                table: "ScheduledPostAutomations");

            migrationBuilder.DropColumn(
                name: "GroupConversation",
                table: "ScheduledPostAutomations");

            migrationBuilder.DropColumn(
                name: "ContextCode",
                table: "ScheduledPostAutomationRuns");

            migrationBuilder.DropColumn(
                name: "GroupConversation",
                table: "ScheduledPostAutomationRuns");
        }
    }
}
