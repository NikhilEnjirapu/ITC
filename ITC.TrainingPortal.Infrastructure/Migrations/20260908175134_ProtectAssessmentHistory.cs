using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAssessmentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuizResults",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedAnswersJson",
                table: "QuizResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_EmployeeId",
                table: "QuizResults",
                column: "EmployeeId",
                unique: true,
                filter: "[IsCompleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizResults_EmployeeId",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "SubmittedAnswersJson",
                table: "QuizResults");
        }
    }
}
