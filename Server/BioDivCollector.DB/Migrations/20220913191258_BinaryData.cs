using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class BinaryData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "objectstorage",
                schema: "public",
                columns: table => new
                {
                    objectstorageid = table.Column<Guid>(nullable: false),
                    originalfilename = table.Column<string>(nullable: true),
                    savedfilename = table.Column<string>(nullable: true),
                    savedfilepath = table.Column<string>(nullable: true),
                    metadata = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_objectstorage", x => x.objectstorageid);
                });

            migrationBuilder.CreateTable(
                name: "binarydata",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    recordid = table.Column<Guid>(nullable: true),
                    title = table.Column<string>(nullable: true),
                    formfieldid = table.Column<int>(nullable: true),
                    objectstorageid = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_binarydata", x => x.id);
                    table.ForeignKey(
                        name: "fk_binarydata_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_binarydata_records_recordid",
                        column: x => x.recordid,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "recordid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_binarydata_objectstorage_objectstorageid",
                        column: x => x.objectstorageid,
                        principalSchema: "public",
                        principalTable: "objectstorage",
                        principalColumn: "objectstorageid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_binarydata_formfieldid",
                schema: "public",
                table: "binarydata",
                column: "formfieldid");

            migrationBuilder.CreateIndex(
                name: "ix_binarydata_recordid",
                schema: "public",
                table: "binarydata",
                column: "recordid");

            migrationBuilder.CreateIndex(
                name: "ix_binarydata_objectstorageid",
                schema: "public",
                table: "binarydata",
                column: "objectstorageid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "binarydata",
                schema: "public");

            migrationBuilder.DropTable(
                name: "objectstorage",
                schema: "public");
        }
    }
}
