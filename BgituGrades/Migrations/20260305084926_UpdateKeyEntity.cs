using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BgutuGrades.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKeyEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ApiKeys",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "LookupHash",
                table: "ApiKeys",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredHash",
                table: "ApiKeys",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LookupHash",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "StoredHash",
                table: "ApiKeys");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ApiKeys",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
