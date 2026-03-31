using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BgituGrades.Migrations
{
    /// <inheritdoc />
    public partial class AddSubGroupToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_GroupId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Presences_StudentId",
                table: "Presences");

            migrationBuilder.AddColumn<string>(
                name: "SubGroup",
                table: "Groups",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_GroupId_DisciplineId",
                table: "Transfers",
                columns: new[] { "GroupId", "DisciplineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Presences_StudentId_DisciplineId_Date",
                table: "Presences",
                columns: new[] { "StudentId", "DisciplineId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_GroupId_DisciplineId",
                table: "Classes",
                columns: new[] { "GroupId", "DisciplineId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_GroupId_DisciplineId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Presences_StudentId_DisciplineId_Date",
                table: "Presences");

            migrationBuilder.DropIndex(
                name: "IX_Classes_GroupId_DisciplineId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SubGroup",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_GroupId",
                table: "Transfers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Presences_StudentId",
                table: "Presences",
                column: "StudentId");
        }
    }
}
