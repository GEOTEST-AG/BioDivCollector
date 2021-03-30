using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class groupform_removed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "groupsforms",
                schema: "public");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "public",
                table: "formfields",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                schema: "public",
                table: "formfields");

            migrationBuilder.CreateTable(
                name: "groupsforms",
                schema: "public",
                columns: table => new
                {
                    groupid = table.Column<Guid>(type: "uuid", nullable: false),
                    formid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupsforms", x => new { x.groupid, x.formid });
                    table.ForeignKey(
                        name: "fk_groupsforms_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groupsforms_groups_groupid",
                        column: x => x.groupid,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "groupid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groupsforms_formid",
                schema: "public",
                table: "groupsforms",
                column: "formid");
        }
    }
}
