using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class ProjectGroupHasReadOnlyFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "readonly",
                schema: "public",
                table: "groupsusers");

            migrationBuilder.AddColumn<bool>(
                name: "readonly",
                schema: "public",
                table: "projectsgroups",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "readonly",
                schema: "public",
                table: "projectsgroups");

            migrationBuilder.AddColumn<bool>(
                name: "readonly",
                schema: "public",
                table: "groupsusers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
