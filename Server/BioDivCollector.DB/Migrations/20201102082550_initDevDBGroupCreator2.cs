using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class initDevDBGroupCreator2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "creatorid",
                schema: "public",
                table: "groups",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_creatorid",
                schema: "public",
                table: "groups",
                column: "creatorid");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_users_creatorid",
                schema: "public",
                table: "groups",
                column: "creatorid",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_users_creatorid",
                schema: "public",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_creatorid",
                schema: "public",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "creatorid",
                schema: "public",
                table: "groups");
        }
    }
}
