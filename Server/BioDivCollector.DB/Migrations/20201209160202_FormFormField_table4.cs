using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class FormFormField_table4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_formfields_forms_formid",
                schema: "public",
                table: "formfields");

            //migrationBuilder.DropIndex(
            //    name: "ix_formfields_formid",
            //    schema: "public",
            //    table: "formfields");

            migrationBuilder.DropColumn(
                name: "formid",
                schema: "public",
                table: "formfields");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "formid",
                schema: "public",
                table: "formfields",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_formfields_formid",
                schema: "public",
                table: "formfields",
                column: "formid");

            migrationBuilder.AddForeignKey(
                name: "fk_formfields_forms_formid",
                schema: "public",
                table: "formfields",
                column: "formid",
                principalSchema: "public",
                principalTable: "forms",
                principalColumn: "formid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
