using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class projectForms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "mandatory",
                schema: "public",
                table: "formfields",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "useinrecordtitle",
                schema: "public",
                table: "formfields",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "projectsforms",
                schema: "public",
                columns: table => new
                {
                    projectid = table.Column<Guid>(nullable: false),
                    formid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projectsforms", x => new { x.projectid, x.formid });
                    table.ForeignKey(
                        name: "fk_projectsforms_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_projectsforms_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_projectsforms_formid",
                schema: "public",
                table: "projectsforms",
                column: "formid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projectsforms",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "mandatory",
                schema: "public",
                table: "formfields");

            migrationBuilder.DropColumn(
                name: "useinrecordtitle",
                schema: "public",
                table: "formfields");
        }
    }
}
