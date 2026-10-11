using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramExamUnlockCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExamUnlockCondition",
                table: "TrainingPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExamUnlockVideoId",
                table: "TrainingPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_ExamUnlockVideoId",
                table: "TrainingPrograms",
                column: "ExamUnlockVideoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingPrograms_ExamUnlock",
                table: "TrainingPrograms",
                sql: "([ExamUnlockCondition] = 0 AND [ExamUnlockVideoId] IS NULL) OR ([ExamUnlockCondition] = 1 AND [ExamUnlockVideoId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingPrograms_TrainingVideos_ExamUnlockVideoId",
                table: "TrainingPrograms",
                column: "ExamUnlockVideoId",
                principalTable: "TrainingVideos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingPrograms_TrainingVideos_ExamUnlockVideoId",
                table: "TrainingPrograms");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPrograms_ExamUnlockVideoId",
                table: "TrainingPrograms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingPrograms_ExamUnlock",
                table: "TrainingPrograms");

            migrationBuilder.DropColumn(
                name: "ExamUnlockCondition",
                table: "TrainingPrograms");

            migrationBuilder.DropColumn(
                name: "ExamUnlockVideoId",
                table: "TrainingPrograms");
        }
    }
}
