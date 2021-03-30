using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace BioDivCollector.DB.Migrations
{
    public partial class pointSridType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "point",
                schema: "public",
                table: "geometries",
                type: "geometry(POINT, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "point",
                schema: "public",
                table: "geometries",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry(POINT, 4326)",
                oldNullable: true);
        }
    }
}
