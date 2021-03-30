using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BioDivCollector.DB.Migrations
{
    public partial class projectGroupsHaveGeometriesAndRecords9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_geometries_projects_projectid",
                schema: "public",
                table: "geometries");

            migrationBuilder.DropForeignKey(
                name: "fk_records_projects_projectid",
                schema: "public",
                table: "records");

            migrationBuilder.DropIndex(
                name: "ix_records_projectid",
                schema: "public",
                table: "records");

            migrationBuilder.DropIndex(
                name: "ix_geometries_projectid",
                schema: "public",
                table: "geometries");

            migrationBuilder.AddColumn<Guid>(
                name: "groupid",
                schema: "public",
                table: "records",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "readonly",
                schema: "public",
                table: "groupsusers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "projectid",
                schema: "public",
                table: "geometries",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "groupid",
                schema: "public",
                table: "geometries",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_records_projectid_groupid",
                schema: "public",
                table: "records",
                columns: new[] { "projectid", "groupid" });

            migrationBuilder.CreateIndex(
                name: "ix_geometries_projectid_groupid",
                schema: "public",
                table: "geometries",
                columns: new[] { "projectid", "groupid" });

            migrationBuilder.AddForeignKey(
                name: "fk_geometries_projectsgroups_projectid_groupid",
                schema: "public",
                table: "geometries",
                columns: new[] { "projectid", "groupid" },
                principalSchema: "public",
                principalTable: "projectsgroups",
                principalColumns: new[] { "projectid", "groupid" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_records_projectsgroups_projectid_groupid",
                schema: "public",
                table: "records",
                columns: new[] { "projectid", "groupid" },
                principalSchema: "public",
                principalTable: "projectsgroups",
                principalColumns: new[] { "projectid", "groupid" },
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_geometries_projectsgroups_projectid_groupid",
                schema: "public",
                table: "geometries");

            migrationBuilder.DropForeignKey(
                name: "fk_records_projectsgroups_projectid_groupid",
                schema: "public",
                table: "records");

            migrationBuilder.DropIndex(
                name: "ix_records_projectid_groupid",
                schema: "public",
                table: "records");

            migrationBuilder.DropIndex(
                name: "ix_geometries_projectid_groupid",
                schema: "public",
                table: "geometries");

            migrationBuilder.DropColumn(
                name: "groupid",
                schema: "public",
                table: "records");

            migrationBuilder.DropColumn(
                name: "readonly",
                schema: "public",
                table: "groupsusers");

            migrationBuilder.DropColumn(
                name: "groupid",
                schema: "public",
                table: "geometries");

            migrationBuilder.AlterColumn<Guid>(
                name: "projectid",
                schema: "public",
                table: "geometries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.CreateIndex(
                name: "ix_records_projectid",
                schema: "public",
                table: "records",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_geometries_projectid",
                schema: "public",
                table: "geometries",
                column: "projectid");

            migrationBuilder.AddForeignKey(
                name: "fk_geometries_projects_projectid",
                schema: "public",
                table: "geometries",
                column: "projectid",
                principalSchema: "public",
                principalTable: "projects",
                principalColumn: "projectid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_records_projects_projectid",
                schema: "public",
                table: "records",
                column: "projectid",
                principalSchema: "public",
                principalTable: "projects",
                principalColumn: "projectid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
