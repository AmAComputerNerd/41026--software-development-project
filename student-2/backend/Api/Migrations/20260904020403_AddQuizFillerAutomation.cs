using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizFillerAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizFillerAutomationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuizId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuizTitle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    QuestionCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizFillerAutomationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizFillerAutomationRuns_AutomationRuns_Id",
                        column: x => x.Id,
                        principalTable: "AutomationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizFillerAutomations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<long>(type: "INTEGER", nullable: true),
                    MultipleChoice = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShortAnswer = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumberOfAttemptsRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowForNoTimeLimit = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizFillerAutomations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizFillerAutomations_Automations_Id",
                        column: x => x.Id,
                        principalTable: "Automations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizFillerAutomations_SubjectId",
                table: "QuizFillerAutomations",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizFillerAutomationRuns");

            migrationBuilder.DropTable(
                name: "QuizFillerAutomations");
        }
    }
}
