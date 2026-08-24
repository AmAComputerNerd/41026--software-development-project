using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCanvasSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CanvasAssignmentId",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanvasIsActive",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasSubmissionState",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CanvasUpdatedAt",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasWorkflowState",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CanvasCourseId",
                table: "Courses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanvasIsActive",
                table: "Courses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanvasWorkflowState",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCanvasSyncAt",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CanvasAssignmentId",
                table: "Tasks",
                column: "CanvasAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CanvasCourseId",
                table: "Courses",
                column: "CanvasCourseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_CanvasAssignmentId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CanvasCourseId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CanvasAssignmentId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CanvasIsActive",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CanvasSubmissionState",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CanvasUpdatedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CanvasWorkflowState",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CanvasCourseId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CanvasIsActive",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CanvasWorkflowState",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LastCanvasSyncAt",
                table: "Courses");
        }
    }
}
