using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITC.TrainingPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM TrainingPrograms WHERE LEN(Title) > 200 OR LEN(Description) > 2000)
                    THROW 51000, 'Migration blocked: a training program value exceeds its new maximum length.', 1;
                IF EXISTS (SELECT 1 FROM TrainingVideos WHERE LEN(Title) > 200 OR LEN(Description) > 2000 OR LEN(VideoUrl) > 2048)
                    THROW 51000, 'Migration blocked: a training video value exceeds its new maximum length.', 1;
                IF EXISTS (SELECT TrainingProgramId, SequenceOrder FROM TrainingVideos GROUP BY TrainingProgramId, SequenceOrder HAVING COUNT(*) > 1)
                    THROW 51000, 'Migration blocked: duplicate video sequence orders exist within a training program.', 1;
                IF EXISTS (SELECT EmployeeId, TrainingProgramId FROM TrainingAssignments GROUP BY EmployeeId, TrainingProgramId HAVING COUNT(*) > 1)
                    THROW 51000, 'Migration blocked: duplicate employee training assignments exist.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_TrainingPrograms_TrainingProgramId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAssignments_Employees_EmployeeId",
                table: "TrainingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAssignments_TrainingPrograms_TrainingProgramId",
                table: "TrainingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingVideos_TrainingPrograms_TrainingProgramId",
                table: "TrainingVideos");

            migrationBuilder.DropIndex(
                name: "IX_TrainingVideos_TrainingProgramId",
                table: "TrainingVideos");

            migrationBuilder.DropIndex(
                name: "IX_TrainingAssignments_EmployeeId",
                table: "TrainingAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "VideoUrl",
                table: "TrainingVideos",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainingVideos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TrainingVideos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TrainingVideos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainingPrograms",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TrainingPrograms",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TrainingPrograms",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TrainingAssignments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideos_TrainingProgramId_SequenceOrder",
                table: "TrainingVideos",
                columns: new[] { "TrainingProgramId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_EmployeeId_TrainingProgramId",
                table: "TrainingAssignments",
                columns: new[] { "EmployeeId", "TrainingProgramId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_TrainingPrograms_TrainingProgramId",
                table: "Quizzes",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAssignments_Employees_EmployeeId",
                table: "TrainingAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAssignments_TrainingPrograms_TrainingProgramId",
                table: "TrainingAssignments",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingVideos_TrainingPrograms_TrainingProgramId",
                table: "TrainingVideos",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_TrainingPrograms_TrainingProgramId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAssignments_Employees_EmployeeId",
                table: "TrainingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAssignments_TrainingPrograms_TrainingProgramId",
                table: "TrainingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingVideos_TrainingPrograms_TrainingProgramId",
                table: "TrainingVideos");

            migrationBuilder.DropIndex(
                name: "IX_TrainingVideos_TrainingProgramId_SequenceOrder",
                table: "TrainingVideos");

            migrationBuilder.DropIndex(
                name: "IX_TrainingAssignments_EmployeeId_TrainingProgramId",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TrainingVideos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TrainingPrograms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TrainingAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "VideoUrl",
                table: "TrainingVideos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainingVideos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TrainingVideos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainingPrograms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TrainingPrograms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideos_TrainingProgramId",
                table: "TrainingVideos",
                column: "TrainingProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_EmployeeId",
                table: "TrainingAssignments",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_TrainingPrograms_TrainingProgramId",
                table: "Quizzes",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAssignments_Employees_EmployeeId",
                table: "TrainingAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAssignments_TrainingPrograms_TrainingProgramId",
                table: "TrainingAssignments",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingVideos_TrainingPrograms_TrainingProgramId",
                table: "TrainingVideos",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
