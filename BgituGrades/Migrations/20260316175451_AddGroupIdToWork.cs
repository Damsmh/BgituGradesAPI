using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BgituGrades.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupIdToWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Works_GroupId",
                table: "Works",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Works_Groups_GroupId",
                table: "Works",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Works_Groups_GroupId",
                table: "Works");

            migrationBuilder.DropIndex(
                name: "IX_Works_GroupId",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Works");
        }
    }
}
