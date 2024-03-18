using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class BinaryID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "fieldtypes",
                columns: new[] { "id", "description" },
                values: new object[] { 81, "Binary" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "fieldtypes",
                keyColumn: "id",
                keyValue: 81);
        }
    }
}
