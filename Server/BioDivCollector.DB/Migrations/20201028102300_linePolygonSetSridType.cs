using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace BioDivCollector.DB.Migrations
{
    public partial class linePolygonSetSridType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Polygon>(
                name: "polygon",
                schema: "public",
                table: "geometries",
                type: "geometry(POLYGON, 4326)",
                nullable: true,
                oldClrType: typeof(Polygon),
                oldType: "geometry",
                oldNullable: true);

            migrationBuilder.AlterColumn<LineString>(
                name: "line",
                schema: "public",
                table: "geometries",
                type: "geometry(LINESTRING, 4326)",
                nullable: true,
                oldClrType: typeof(LineString),
                oldType: "geometry",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Polygon>(
                name: "polygon",
                schema: "public",
                table: "geometries",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Polygon),
                oldType: "geometry(POLYGON, 4326)",
                oldNullable: true);

            migrationBuilder.AlterColumn<LineString>(
                name: "line",
                schema: "public",
                table: "geometries",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(LineString),
                oldType: "geometry(LINESTRING, 4326)",
                oldNullable: true);
        }
    }
}
