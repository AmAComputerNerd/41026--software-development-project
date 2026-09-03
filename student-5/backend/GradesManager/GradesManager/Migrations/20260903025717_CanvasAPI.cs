using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradesManager.Migrations
{
    /// <inheritdoc />
    public partial class CanvasAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "Completed",
                table: "Assignments",
                newName: "CanvasIsActive");

            migrationBuilder.AddColumn<long>(
                name: "CanvasUserId",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "TempMark",
                table: "StudentAssignments",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "FinalMark",
                table: "StudentAssignments",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasWorkflowState",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CanvasAssignmentId",
                table: "Assignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasSubmissionState",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasWorkflowState",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentGroups",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<double>(type: "REAL", nullable: true),
                    CanvasAssignmentGroupId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentGroups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_AssignmentGroups_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_GroupId",
                table: "Assignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentGroups_CourseId",
                table: "AssignmentGroups",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_AssignmentGroups_GroupId",
                table: "Assignments",
                column: "GroupId",
                principalTable: "AssignmentGroups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_AssignmentGroups_GroupId",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "AssignmentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_GroupId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CanvasUserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CanvasWorkflowState",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CanvasAssignmentId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CanvasSubmissionState",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CanvasWorkflowState",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "CanvasIsActive",
                table: "Assignments",
                newName: "Completed");

            migrationBuilder.AlterColumn<int>(
                name: "TempMark",
                table: "StudentAssignments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FinalMark",
                table: "StudentAssignments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Assignments",
                type: "REAL",
                nullable: true);
        }
    }
}
