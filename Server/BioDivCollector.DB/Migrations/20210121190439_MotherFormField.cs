using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class MotherFormField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_formfields_publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields",
                column: "publicmotherformfieldformfieldid");

            migrationBuilder.AddForeignKey(
                name: "fk_formfields_formfields_publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields",
                column: "publicmotherformfieldformfieldid",
                principalSchema: "public",
                principalTable: "formfields",
                principalColumn: "formfieldid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_formfields_formfields_publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields");

            migrationBuilder.DropIndex(
                name: "ix_formfields_publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields");

            migrationBuilder.DropColumn(
                name: "publicmotherformfieldformfieldid",
                schema: "public",
                table: "formfields");
        }
    }
}
