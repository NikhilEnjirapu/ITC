using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistShuffledQuestionOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionOrderJson",
                table: "QuizResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionOrderJson",
                table: "QuizResults");
        }
    }
}
