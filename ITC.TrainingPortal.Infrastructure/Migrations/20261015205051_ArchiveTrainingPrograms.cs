using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveTrainingPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "TrainingPrograms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "TrainingPrograms");
        }
    }
}
