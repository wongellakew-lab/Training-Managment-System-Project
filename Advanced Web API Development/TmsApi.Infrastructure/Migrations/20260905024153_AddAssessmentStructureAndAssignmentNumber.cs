using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentStructureAndAssignmentNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_CourseId",
                table: "Assignments");

            migrationBuilder.AddColumn<int>(
                name: "ExpectedAssignmentCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedFinalCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedMidtermCount",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentNumber",
                table: "Assignments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
                WITH RankedAssignments AS (
                    SELECT ""Id"", ROW_NUMBER() OVER(PARTITION BY ""CourseId"" ORDER BY ""CreatedAt"", ""Id"") as rn
                    FROM ""Assignments""
                )
                UPDATE ""Assignments"" a
                SET ""AssignmentNumber"" = ra.rn
                FROM RankedAssignments ra
                WHERE a.""Id"" = ra.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseId_AssignmentNumber",
                table: "Assignments",
                columns: new[] { "CourseId", "AssignmentNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_CourseId_AssignmentNumber",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ExpectedAssignmentCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ExpectedFinalCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ExpectedMidtermCount",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AssignmentNumber",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseId",
                table: "Assignments",
                column: "CourseId");
        }
    }
}
