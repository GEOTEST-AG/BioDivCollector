using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class statusForCreated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Unverändert");

            migrationBuilder.InsertData(
                schema: "public",
                table: "statuses",
                columns: new[] { "id", "description" },
                values: new object[] { -1, "Neu" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: -1);

            migrationBuilder.UpdateData(
                schema: "public",
                table: "statuses",
                keyColumn: "id",
                keyValue: 1,
                column: "description",
                value: "Unverändert/Neu");
        }
    }
}
