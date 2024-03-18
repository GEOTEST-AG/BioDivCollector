using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class SecuredWMS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password",
                schema: "public",
                table: "layers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                schema: "public",
                table: "layers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password",
                schema: "public",
                table: "layers");

            migrationBuilder.DropColumn(
                name: "username",
                schema: "public",
                table: "layers");
        }
    }
}
