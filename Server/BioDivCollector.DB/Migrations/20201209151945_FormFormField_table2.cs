using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class FormFormField_table2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "formsformfields",
                schema: "public",
                columns: table => new
                {
                    formfieldid = table.Column<int>(nullable: false),
                    formid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formsformfields", x => new { x.formid, x.formfieldid });
                    table.ForeignKey(
                        name: "fk_formsformfields_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_formsformfields_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_formsformfields_formfieldid",
                schema: "public",
                table: "formsformfields",
                column: "formfieldid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "formsformfields",
                schema: "public");
        }
    }
}
