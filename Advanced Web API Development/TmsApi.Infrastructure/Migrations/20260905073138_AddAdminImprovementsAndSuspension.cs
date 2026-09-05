using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TmsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminImprovementsAndSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Students",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetAudience",
                table: "Schedules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "All");

            migrationBuilder.AddColumn<int>(
                name: "TargetCourseId",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetDepartment",
                table: "Schedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Instructors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InstructorSuspensionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalInstructorId = table.Column<int>(type: "integer", nullable: false),
                    ReplacementInstructorId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsRestored = table.Column<bool>(type: "boolean", nullable: false),
                    RestoredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorSuspensionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorSuspensionHistories_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstructorSuspensionHistories_Instructors_OriginalInstructo~",
                        column: x => x.OriginalInstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorSuspensionHistories_Instructors_ReplacementInstru~",
                        column: x => x.ReplacementInstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TargetCourseId",
                table: "Schedules",
                column: "TargetCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorSuspensionHistories_CourseId",
                table: "InstructorSuspensionHistories",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorSuspensionHistories_OriginalInstructorId",
                table: "InstructorSuspensionHistories",
                column: "OriginalInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorSuspensionHistories_ReplacementInstructorId",
                table: "InstructorSuspensionHistories",
                column: "ReplacementInstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Courses_TargetCourseId",
                table: "Schedules",
                column: "TargetCourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Courses_TargetCourseId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "InstructorSuspensionHistories");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TargetCourseId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TargetCourseId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TargetDepartment",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Instructors");
        }
    }
}
