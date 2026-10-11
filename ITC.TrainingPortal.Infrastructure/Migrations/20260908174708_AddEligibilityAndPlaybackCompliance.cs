using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEligibilityAndPlaybackCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PlaybackCreditSeconds",
                table: "VideoProgresses",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "PlaybackState",
                table: "VideoProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "paused");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VideoProgresses",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "WorkerType",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.Sql("UPDATE p SET IsCompleted = 0 FROM VideoProgresses p INNER JOIN TrainingVideos v ON v.Id = p.TrainingVideoId WHERE p.CurrentPositionSeconds < v.DurationSeconds;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaybackCreditSeconds",
                table: "VideoProgresses");

            migrationBuilder.DropColumn(
                name: "PlaybackState",
                table: "VideoProgresses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VideoProgresses");

            migrationBuilder.DropColumn(
                name: "WorkerType",
                table: "Employees");
        }
    }
}
