using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerKeywords",
                table: "QuizQuestions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerKeywords",
                table: "QuizQuestions");
        }
    }
}
