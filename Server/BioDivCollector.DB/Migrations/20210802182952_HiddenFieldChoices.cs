using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace BioDivCollector.DB.Migrations
{
    public partial class HiddenFieldChoices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hiddenfieldchoice",
                schema: "public",
                columns: table => new
                {
                    hiddenfieldchoiceid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    formfieldid = table.Column<int>(nullable: true),
                    fieldchoiceid = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hiddenfieldchoice", x => x.hiddenfieldchoiceid);
                    table.ForeignKey(
                        name: "fk_hiddenfieldchoice_fieldchoices_fieldchoiceid",
                        column: x => x.fieldchoiceid,
                        principalSchema: "public",
                        principalTable: "fieldchoices",
                        principalColumn: "fieldchoiceid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hiddenfieldchoice_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "fieldtypes",
                columns: new[] { "id", "description" },
                values: new object[] { 71, "Header" });

            migrationBuilder.CreateIndex(
                name: "ix_hiddenfieldchoice_fieldchoiceid",
                schema: "public",
                table: "hiddenfieldchoice",
                column: "fieldchoiceid");

            migrationBuilder.CreateIndex(
                name: "ix_hiddenfieldchoice_formfieldid",
                schema: "public",
                table: "hiddenfieldchoice",
                column: "formfieldid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hiddenfieldchoice",
                schema: "public");

            migrationBuilder.DeleteData(
                schema: "public",
                table: "fieldtypes",
                keyColumn: "id",
                keyValue: 71);
        }
    }
}
