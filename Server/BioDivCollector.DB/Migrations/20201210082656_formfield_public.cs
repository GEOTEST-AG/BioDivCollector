using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class formfield_public : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "public",
                schema: "public",
                table: "formsformfields");

            migrationBuilder.AddColumn<bool>(
                name: "public",
                schema: "public",
                table: "formfields",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "public",
                schema: "public",
                table: "formfields");

            migrationBuilder.AddColumn<bool>(
                name: "public",
                schema: "public",
                table: "formsformfields",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
