using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentRunCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CourseId",
                table: "AssignmentExtensionAutomationRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE AssignmentExtensionAutomationRuns
                SET CourseId = 39716
                WHERE AssignmentId = 'assignment-1';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "AssignmentExtensionAutomationRuns");
        }
    }
}
