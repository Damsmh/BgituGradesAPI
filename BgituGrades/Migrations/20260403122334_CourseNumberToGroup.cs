using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BgituGrades.Migrations
{
    /// <inheritdoc />
    public partial class CourseNumberToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubGroup",
                table: "Groups");

            migrationBuilder.AddColumn<int>(
                name: "GroupCourseNumber",
                table: "ReportSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CourseNumber",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupCourseNumber",
                table: "ReportSnapshots");

            migrationBuilder.DropColumn(
                name: "CourseNumber",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "SubGroup",
                table: "Groups",
                type: "text",
                nullable: true);
        }
    }
}
