using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPostRunParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PostTime",
                table: "ScheduledPostAutomationRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE ScheduledPostAutomationRuns
                SET PostTime = (
                    SELECT automation.PostTime
                    FROM AutomationRuns AS run
                    INNER JOIN ScheduledPostAutomations AS automation
                        ON automation.Id = run.AutomationId
                    WHERE run.Id = ScheduledPostAutomationRuns.Id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostTime",
                table: "ScheduledPostAutomationRuns");
        }
    }
}
