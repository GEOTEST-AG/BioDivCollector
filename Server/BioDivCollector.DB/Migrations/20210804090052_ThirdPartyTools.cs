using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace BioDivCollector.DB.Migrations
{
    public partial class ThirdPartyTools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "thirdpartytools",
                schema: "public",
                columns: table => new
                {
                    thirdpartytoolid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thirdpartytools", x => x.thirdpartytoolid);
                });

            migrationBuilder.CreateTable(
                name: "projectthirdpartytool",
                schema: "public",
                columns: table => new
                {
                    projectthirdpartytoolid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    projectid = table.Column<Guid>(nullable: true),
                    thirdpartytoolid = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projectthirdpartytool", x => x.projectthirdpartytoolid);
                    table.ForeignKey(
                        name: "fk_projectthirdpartytool_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projectthirdpartytool_thirdpartytools_thirdpartytoolid",
                        column: x => x.thirdpartytoolid,
                        principalSchema: "public",
                        principalTable: "thirdpartytools",
                        principalColumn: "thirdpartytoolid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_projectthirdpartytool_projectid",
                schema: "public",
                table: "projectthirdpartytool",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_projectthirdpartytool_thirdpartytoolid",
                schema: "public",
                table: "projectthirdpartytool",
                column: "thirdpartytoolid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projectthirdpartytool",
                schema: "public");

            migrationBuilder.DropTable(
                name: "thirdpartytools",
                schema: "public");
        }
    }
}
