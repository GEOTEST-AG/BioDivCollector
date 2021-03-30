using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class projectLeadConfigAdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "projectconfiguratoruserid",
                schema: "public",
                table: "projects",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "projectmanageruserid",
                schema: "public",
                table: "projects",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_projectconfiguratoruserid",
                schema: "public",
                table: "projects",
                column: "projectconfiguratoruserid");

            migrationBuilder.CreateIndex(
                name: "ix_projects_projectmanageruserid",
                schema: "public",
                table: "projects",
                column: "projectmanageruserid");

            migrationBuilder.AddForeignKey(
                name: "fk_projects_users_projectconfiguratoruserid",
                schema: "public",
                table: "projects",
                column: "projectconfiguratoruserid",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_projects_users_projectmanageruserid",
                schema: "public",
                table: "projects",
                column: "projectmanageruserid",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projects_users_projectconfiguratoruserid",
                schema: "public",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "fk_projects_users_projectmanageruserid",
                schema: "public",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_projects_projectconfiguratoruserid",
                schema: "public",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_projects_projectmanageruserid",
                schema: "public",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "projectconfiguratoruserid",
                schema: "public",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "projectmanageruserid",
                schema: "public",
                table: "projects");
        }
    }
}
