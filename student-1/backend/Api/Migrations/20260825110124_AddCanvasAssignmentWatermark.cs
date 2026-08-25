using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCanvasAssignmentWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanvasAssignmentWatermarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CanvasAssignmentId = table.Column<long>(type: "INTEGER", nullable: false),
                    LastDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastWorkflowState = table.Column<string>(type: "TEXT", nullable: true),
                    LastSubmissionState = table.Column<string>(type: "TEXT", nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanvasAssignmentWatermarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanvasAssignmentWatermarks_CanvasAssignmentId",
                table: "CanvasAssignmentWatermarks",
                column: "CanvasAssignmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanvasAssignmentWatermarks");
        }
    }
}
