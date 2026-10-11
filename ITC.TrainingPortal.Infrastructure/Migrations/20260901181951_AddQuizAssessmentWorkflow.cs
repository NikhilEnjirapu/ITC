using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizAssessmentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_Employees_EmployeeId",
                table: "QuizResults");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_Quizzes_QuizId",
                table: "QuizResults");

            migrationBuilder.DropIndex(
                name: "IX_QuizResults_EmployeeId",
                table: "QuizResults");

            migrationBuilder.DropIndex(
                name: "IX_QuizQuestions_QuizId",
                table: "QuizQuestions");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "QuizResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_EmployeeId_QuizId_IsCompleted",
                table: "QuizResults",
                columns: new[] { "EmployeeId", "QuizId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_SequenceOrder",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "SequenceOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_Employees_EmployeeId",
                table: "QuizResults",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_Quizzes_QuizId",
                table: "QuizResults",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_Employees_EmployeeId",
                table: "QuizResults");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizResults_Quizzes_QuizId",
                table: "QuizResults");

            migrationBuilder.DropIndex(
                name: "IX_QuizResults_EmployeeId_QuizId_IsCompleted",
                table: "QuizResults");

            migrationBuilder.DropIndex(
                name: "IX_QuizQuestions_QuizId_SequenceOrder",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "QuizResults");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_EmployeeId",
                table: "QuizResults",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId",
                table: "QuizQuestions",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_Employees_EmployeeId",
                table: "QuizResults",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizResults_Quizzes_QuizId",
                table: "QuizResults",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
