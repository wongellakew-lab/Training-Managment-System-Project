using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmissionFileName",
                table: "Assessments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubmissionFileSize",
                table: "Assessments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionFileType",
                table: "Assessments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionFileName",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "SubmissionFileSize",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "SubmissionFileType",
                table: "Assessments");
        }
    }
}
