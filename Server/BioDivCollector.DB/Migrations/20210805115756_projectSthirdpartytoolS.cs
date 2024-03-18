using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class projectSthirdpartytoolS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projectthirdpartytool_projects_projectid",
                schema: "public",
                table: "projectthirdpartytool");

            migrationBuilder.DropForeignKey(
                name: "fk_projectthirdpartytool_thirdpartytools_thirdpartytoolid",
                schema: "public",
                table: "projectthirdpartytool");

            migrationBuilder.DropPrimaryKey(
                name: "pk_projectthirdpartytool",
                schema: "public",
                table: "projectthirdpartytool");

            migrationBuilder.RenameTable(
                name: "projectthirdpartytool",
                schema: "public",
                newName: "projectsthirdpartytools",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_projectthirdpartytool_thirdpartytoolid",
                schema: "public",
                table: "projectsthirdpartytools",
                newName: "ix_projectsthirdpartytools_thirdpartytoolid");

            migrationBuilder.RenameIndex(
                name: "ix_projectthirdpartytool_projectid",
                schema: "public",
                table: "projectsthirdpartytools",
                newName: "ix_projectsthirdpartytools_projectid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_projectsthirdpartytools",
                schema: "public",
                table: "projectsthirdpartytools",
                column: "projectthirdpartytoolid");

            migrationBuilder.AddForeignKey(
                name: "fk_projectsthirdpartytools_projects_projectid",
                schema: "public",
                table: "projectsthirdpartytools",
                column: "projectid",
                principalSchema: "public",
                principalTable: "projects",
                principalColumn: "projectid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_projectsthirdpartytools_thirdpartytools_thirdpartytoolid",
                schema: "public",
                table: "projectsthirdpartytools",
                column: "thirdpartytoolid",
                principalSchema: "public",
                principalTable: "thirdpartytools",
                principalColumn: "thirdpartytoolid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projectsthirdpartytools_projects_projectid",
                schema: "public",
                table: "projectsthirdpartytools");

            migrationBuilder.DropForeignKey(
                name: "fk_projectsthirdpartytools_thirdpartytools_thirdpartytoolid",
                schema: "public",
                table: "projectsthirdpartytools");

            migrationBuilder.DropPrimaryKey(
                name: "pk_projectsthirdpartytools",
                schema: "public",
                table: "projectsthirdpartytools");

            migrationBuilder.RenameTable(
                name: "projectsthirdpartytools",
                schema: "public",
                newName: "projectthirdpartytool",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_projectsthirdpartytools_thirdpartytoolid",
                schema: "public",
                table: "projectthirdpartytool",
                newName: "ix_projectthirdpartytool_thirdpartytoolid");

            migrationBuilder.RenameIndex(
                name: "ix_projectsthirdpartytools_projectid",
                schema: "public",
                table: "projectthirdpartytool",
                newName: "ix_projectthirdpartytool_projectid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_projectthirdpartytool",
                schema: "public",
                table: "projectthirdpartytool",
                column: "projectthirdpartytoolid");

            migrationBuilder.AddForeignKey(
                name: "fk_projectthirdpartytool_projects_projectid",
                schema: "public",
                table: "projectthirdpartytool",
                column: "projectid",
                principalSchema: "public",
                principalTable: "projects",
                principalColumn: "projectid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_projectthirdpartytool_thirdpartytools_thirdpartytoolid",
                schema: "public",
                table: "projectthirdpartytool",
                column: "thirdpartytoolid",
                principalSchema: "public",
                principalTable: "thirdpartytools",
                principalColumn: "thirdpartytoolid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
