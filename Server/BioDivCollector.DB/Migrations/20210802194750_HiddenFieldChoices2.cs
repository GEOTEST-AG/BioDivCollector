using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class HiddenFieldChoices2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hiddenfieldchoice_fieldchoices_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoice");

            migrationBuilder.DropForeignKey(
                name: "fk_hiddenfieldchoice_formfields_formfieldid",
                schema: "public",
                table: "hiddenfieldchoice");

            migrationBuilder.DropPrimaryKey(
                name: "pk_hiddenfieldchoice",
                schema: "public",
                table: "hiddenfieldchoice");

            migrationBuilder.RenameTable(
                name: "hiddenfieldchoice",
                schema: "public",
                newName: "hiddenfieldchoices",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_hiddenfieldchoice_formfieldid",
                schema: "public",
                table: "hiddenfieldchoices",
                newName: "ix_hiddenfieldchoices_formfieldid");

            migrationBuilder.RenameIndex(
                name: "ix_hiddenfieldchoice_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoices",
                newName: "ix_hiddenfieldchoices_fieldchoiceid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_hiddenfieldchoices",
                schema: "public",
                table: "hiddenfieldchoices",
                column: "hiddenfieldchoiceid");

            migrationBuilder.AddForeignKey(
                name: "fk_hiddenfieldchoices_fieldchoices_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoices",
                column: "fieldchoiceid",
                principalSchema: "public",
                principalTable: "fieldchoices",
                principalColumn: "fieldchoiceid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hiddenfieldchoices_formfields_formfieldid",
                schema: "public",
                table: "hiddenfieldchoices",
                column: "formfieldid",
                principalSchema: "public",
                principalTable: "formfields",
                principalColumn: "formfieldid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hiddenfieldchoices_fieldchoices_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoices");

            migrationBuilder.DropForeignKey(
                name: "fk_hiddenfieldchoices_formfields_formfieldid",
                schema: "public",
                table: "hiddenfieldchoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_hiddenfieldchoices",
                schema: "public",
                table: "hiddenfieldchoices");

            migrationBuilder.RenameTable(
                name: "hiddenfieldchoices",
                schema: "public",
                newName: "hiddenfieldchoice",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "ix_hiddenfieldchoices_formfieldid",
                schema: "public",
                table: "hiddenfieldchoice",
                newName: "ix_hiddenfieldchoice_formfieldid");

            migrationBuilder.RenameIndex(
                name: "ix_hiddenfieldchoices_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoice",
                newName: "ix_hiddenfieldchoice_fieldchoiceid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_hiddenfieldchoice",
                schema: "public",
                table: "hiddenfieldchoice",
                column: "hiddenfieldchoiceid");

            migrationBuilder.AddForeignKey(
                name: "fk_hiddenfieldchoice_fieldchoices_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoice",
                column: "fieldchoiceid",
                principalSchema: "public",
                principalTable: "fieldchoices",
                principalColumn: "fieldchoiceid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hiddenfieldchoice_formfields_formfieldid",
                schema: "public",
                table: "hiddenfieldchoice",
                column: "formfieldid",
                principalSchema: "public",
                principalTable: "formfields",
                principalColumn: "formfieldid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
