using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class group_project_statuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.AddColumn<int>(
                name: "groupstatusid",
                schema: "public",
                table: "groups",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "id_extern",
                schema: "public",
                table: "groups",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "groupstatuses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupstatuses", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "groupstatuses",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 1, "Gruppe_neu" },
                    { 2, "Gruppe_bereit" },
                    { 3, "Gruppendaten_erfasst" },
                    { 4, "Gruppendaten_gueltig" },
                    { 9, "Gruppendaten_fehlerhaft" }
                });

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Projekt_neu");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Projekt_bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Projekt_gueltig");

            migrationBuilder.CreateIndex(
                name: "ix_groups_groupstatusid",
                schema: "public",
                table: "groups",
                column: "groupstatusid");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_groupstatuses_groupstatusid",
                schema: "public",
                table: "groups",
                column: "groupstatusid",
                principalSchema: "public",
                principalTable: "groupstatuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_groupstatuses_groupstatusid",
                schema: "public",
                table: "groups");

            migrationBuilder.DropTable(
                name: "groupstatuses",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_groups_groupstatusid",
                schema: "public",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "groupstatusid",
                schema: "public",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "id_extern",
                schema: "public",
                table: "groups");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Projekt_bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Gruppe_bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Gruppendaten_erfasst");

            migrationBuilder.InsertData(
                schema: "public",
                table: "projectstatuses",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 4, "Gruppendaten_gueltig" },
                    { 5, "Projekt_gueltig" },
                    { 11, "Gruppendaten_fehlerhaft" }
                });
        }
    }
}
