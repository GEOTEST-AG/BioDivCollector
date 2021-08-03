using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class StandardValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "standardvalue",
                schema: "public",
                table: "formfields",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "standardvalue",
                schema: "public",
                table: "formfields");
        }
    }
}
