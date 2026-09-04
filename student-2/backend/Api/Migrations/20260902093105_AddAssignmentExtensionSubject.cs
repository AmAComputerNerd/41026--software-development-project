using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentExtensionSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubjectId",
                table: "AssignmentExtensionAutomations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentExtensionAutomations_SubjectId",
                table: "AssignmentExtensionAutomations",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignmentExtensionAutomations_SubjectId",
                table: "AssignmentExtensionAutomations");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "AssignmentExtensionAutomations");
        }
    }
}
