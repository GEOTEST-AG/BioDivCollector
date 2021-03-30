using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class GroupStatusPerProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "groupstatusid",
                schema: "public",
                table: "projectsgroups",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AlterColumn<int>(
                name: "groupstatusid",
                schema: "public",
                table: "groups",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_projectsgroups_groupstatusid",
                schema: "public",
                table: "projectsgroups",
                column: "groupstatusid");

            migrationBuilder.AddForeignKey(
                name: "fk_projectsgroups_groupstatuses_groupstatusid",
                schema: "public",
                table: "projectsgroups",
                column: "groupstatusid",
                principalSchema: "public",
                principalTable: "groupstatuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projectsgroups_groupstatuses_groupstatusid",
                schema: "public",
                table: "projectsgroups");

            migrationBuilder.DropIndex(
                name: "ix_projectsgroups_groupstatusid",
                schema: "public",
                table: "projectsgroups");

            migrationBuilder.DropColumn(
                name: "groupstatusid",
                schema: "public",
                table: "projectsgroups");

            migrationBuilder.AlterColumn<int>(
                name: "groupstatusid",
                schema: "public",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldDefaultValue: 2);
        }
    }
}
