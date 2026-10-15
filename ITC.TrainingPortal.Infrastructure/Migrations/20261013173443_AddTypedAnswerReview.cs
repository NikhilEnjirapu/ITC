using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedAnswerReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManualMarksJson",
                table: "QuizResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReview",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "QuizResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "QuizResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTypedAnswer",
                table: "QuizQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManualMarksJson",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "RequiresReview",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "IsTypedAnswer",
                table: "QuizQuestions");
        }
    }
}
