using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class StatusDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Gruppe(n) neu");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Gruppe(n) bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Gruppendaten erfasst");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 4,
                column: "description",
                value: "Gruppendaten gültig");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 9,
                column: "description",
                value: "Gruppendaten fehlerhaft");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Neues Projekt");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Projekt bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "projectstatuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Projekt gültig");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Unverändert/Neu");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Bearbeitet");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Gelöscht");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Gruppe_neu");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Gruppe_bereit");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Gruppendaten_erfasst");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 4,
                column: "description",
                value: "Gruppendaten_gueltig");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "groupstatuses",
                keyColumn: "id",
                keyValue: 9,
                column: "description",
                value: "Gruppendaten_fehlerhaft");

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

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "unchanged");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "changed");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "deleted");
        }
    }
}
