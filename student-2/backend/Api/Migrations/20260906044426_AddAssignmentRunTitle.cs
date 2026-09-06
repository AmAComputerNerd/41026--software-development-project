using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentRunTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentTitle",
                table: "AssignmentExtensionAutomationRuns",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE AssignmentExtensionAutomationRuns
                SET AssignmentTitle = 'Assessment 1'
                WHERE AssignmentId = 'assignment-1';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentTitle",
                table: "AssignmentExtensionAutomationRuns");
        }
    }
}
