using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizFillerHumanReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireHumanReview",
                table: "QuizFillerAutomations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProposalJson",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewDecision",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerName",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionAttempt",
                table: "QuizFillerAutomationRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubmissionId",
                table: "QuizFillerAutomationRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationToken",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "QuizFillerAutomationRuns",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireHumanReview",
                table: "QuizFillerAutomations");

            migrationBuilder.DropColumn(
                name: "ProposalJson",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "ReviewDecision",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "ReviewerName",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "SubmissionAttempt",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "ValidationToken",
                table: "QuizFillerAutomationRuns");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "QuizFillerAutomationRuns");
        }
    }
}
