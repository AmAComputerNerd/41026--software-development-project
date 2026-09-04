using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationExecutionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionKey",
                table: "AutomationRuns",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE AutomationRuns
                SET ExecutionKey = Id;

                UPDATE AutomationRuns
                SET ExecutionKey = 'once'
                WHERE Id IN (
                    SELECT MIN(r.Id)
                    FROM AutomationRuns AS r
                    INNER JOIN ScheduledPostAutomationRuns AS p ON p.Id = r.Id
                    GROUP BY r.AutomationId
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_AutomationId_ExecutionKey",
                table: "AutomationRuns",
                columns: ["AutomationId", "ExecutionKey"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutomationRuns_AutomationId_ExecutionKey",
                table: "AutomationRuns");

            migrationBuilder.DropColumn(
                name: "ExecutionKey",
                table: "AutomationRuns");
        }
    }
}
