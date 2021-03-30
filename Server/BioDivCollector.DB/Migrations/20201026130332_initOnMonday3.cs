using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace BioDivCollector.DB.Migrations
{
    public partial class initOnMonday3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "fieldtypes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fieldtypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forms",
                schema: "public",
                columns: table => new
                {
                    formid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forms", x => x.formid);
                });

            migrationBuilder.CreateTable(
                name: "layers",
                schema: "public",
                columns: table => new
                {
                    layerid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    @public = table.Column<bool>(name: "public", nullable: false),
                    title = table.Column<string>(nullable: true),
                    url = table.Column<string>(nullable: true),
                    wmslayer = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_layers", x => x.layerid);
                });

            migrationBuilder.CreateTable(
                name: "projectstatuses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projectstatuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "statuses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "formfields",
                schema: "public",
                columns: table => new
                {
                    formfieldid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fieldtypeid = table.Column<int>(nullable: false),
                    formid = table.Column<int>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    source = table.Column<string>(nullable: true),
                    order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formfields", x => x.formfieldid);
                    table.ForeignKey(
                        name: "fk_formfields_fieldtypes_fieldtypeid",
                        column: x => x.fieldtypeid,
                        principalSchema: "public",
                        principalTable: "fieldtypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_formfields_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                schema: "public",
                columns: table => new
                {
                    groupid = table.Column<Guid>(nullable: false),
                    groupname = table.Column<string>(nullable: true),
                    statusid = table.Column<int>(nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.groupid);
                    table.ForeignKey(
                        name: "fk_groups_statuses_statusid",
                        column: x => x.statusid,
                        principalSchema: "public",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "public",
                columns: table => new
                {
                    projectid = table.Column<Guid>(nullable: false),
                    projectname = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    projectnumber = table.Column<int>(nullable: true),
                    id_extern = table.Column<string>(nullable: true),
                    ogd = table.Column<bool>(nullable: false),
                    projectstatusid = table.Column<int>(nullable: false, defaultValue: 1),
                    statusid = table.Column<int>(nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.projectid);
                    table.ForeignKey(
                        name: "fk_projects_projectstatuses_projectstatusid",
                        column: x => x.projectstatusid,
                        principalSchema: "public",
                        principalTable: "projectstatuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_projects_statuses_statusid",
                        column: x => x.statusid,
                        principalSchema: "public",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    userid = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    firstname = table.Column<string>(nullable: true),
                    email = table.Column<string>(nullable: true),
                    statusid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.userid);
                    table.ForeignKey(
                        name: "fk_users_statuses_statusid",
                        column: x => x.statusid,
                        principalSchema: "public",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fieldchoices",
                schema: "public",
                columns: table => new
                {
                    fieldchoiceid = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    formfieldid = table.Column<int>(nullable: true),
                    text = table.Column<string>(nullable: true),
                    order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fieldchoices", x => x.fieldchoiceid);
                    table.ForeignKey(
                        name: "fk_fieldchoices_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "groupsforms",
                schema: "public",
                columns: table => new
                {
                    groupid = table.Column<Guid>(nullable: false),
                    formid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupsforms", x => new { x.groupid, x.formid });
                    table.ForeignKey(
                        name: "fk_groupsforms_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groupsforms_groups_groupid",
                        column: x => x.groupid,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "groupid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "geometries",
                schema: "public",
                columns: table => new
                {
                    geometryid = table.Column<Guid>(nullable: false),
                    geometryname = table.Column<string>(nullable: true),
                    projectid = table.Column<Guid>(nullable: true),
                    point = table.Column<Point>(nullable: true),
                    line = table.Column<LineString>(nullable: true),
                    polygon = table.Column<Polygon>(nullable: true),
                    statusid = table.Column<int>(nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_geometries", x => x.geometryid);
                    table.ForeignKey(
                        name: "fk_geometries_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_geometries_statuses_statusid",
                        column: x => x.statusid,
                        principalSchema: "public",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projectsgroups",
                schema: "public",
                columns: table => new
                {
                    projectid = table.Column<Guid>(nullable: false),
                    groupid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projectsgroups", x => new { x.projectid, x.groupid });
                    table.ForeignKey(
                        name: "fk_projectsgroups_groups_groupid",
                        column: x => x.groupid,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "groupid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_projectsgroups_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projectslayers",
                schema: "public",
                columns: table => new
                {
                    layerid = table.Column<int>(nullable: false),
                    projectid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projectslayers", x => new { x.projectid, x.layerid });
                    table.ForeignKey(
                        name: "fk_projectslayers_layers_layerid",
                        column: x => x.layerid,
                        principalSchema: "public",
                        principalTable: "layers",
                        principalColumn: "layerid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_projectslayers_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogs",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    changedate = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "now()"),
                    userid = table.Column<string>(nullable: false),
                    log = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogs", x => x.changelogid);
                    table.ForeignKey(
                        name: "fk_changelogs_users_userid",
                        column: x => x.userid,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "groupsusers",
                schema: "public",
                columns: table => new
                {
                    userid = table.Column<string>(nullable: false),
                    groupid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groupsusers", x => new { x.userid, x.groupid });
                    table.ForeignKey(
                        name: "fk_groupsusers_groups_groupid",
                        column: x => x.groupid,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "groupid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groupsusers_users_userid",
                        column: x => x.userid,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usershaveprojectlayers",
                schema: "public",
                columns: table => new
                {
                    userid = table.Column<string>(nullable: false),
                    projectid = table.Column<Guid>(nullable: false),
                    layerid = table.Column<int>(nullable: false),
                    visible = table.Column<bool>(nullable: false),
                    order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usershaveprojectlayers", x => new { x.userid, x.projectid, x.layerid });
                    table.ForeignKey(
                        name: "fk_usershaveprojectlayers_layers_layerid",
                        column: x => x.layerid,
                        principalSchema: "public",
                        principalTable: "layers",
                        principalColumn: "layerid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usershaveprojectlayers_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_usershaveprojectlayers_users_userid",
                        column: x => x.userid,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userslayers",
                schema: "public",
                columns: table => new
                {
                    userid = table.Column<string>(nullable: false),
                    layerid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_userslayers", x => new { x.userid, x.layerid });
                    table.ForeignKey(
                        name: "fk_userslayers_layers_layerid",
                        column: x => x.layerid,
                        principalSchema: "public",
                        principalTable: "layers",
                        principalColumn: "layerid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_userslayers_users_userid",
                        column: x => x.userid,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "records",
                schema: "public",
                columns: table => new
                {
                    recordid = table.Column<Guid>(nullable: false),
                    geometryid = table.Column<Guid>(nullable: true),
                    projectid = table.Column<Guid>(nullable: true),
                    formid = table.Column<int>(nullable: true),
                    statusid = table.Column<int>(nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_records", x => x.recordid);
                    table.ForeignKey(
                        name: "fk_records_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_records_geometries_geometryid",
                        column: x => x.geometryid,
                        principalSchema: "public",
                        principalTable: "geometries",
                        principalColumn: "geometryid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_records_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_records_statuses_statusid",
                        column: x => x.statusid,
                        principalSchema: "public",
                        principalTable: "statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogsforms",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    formid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogsforms", x => new { x.formid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogsforms_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogsforms_forms_formid",
                        column: x => x.formid,
                        principalSchema: "public",
                        principalTable: "forms",
                        principalColumn: "formid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogsgeometries",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    geometryid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogsgeometries", x => new { x.geometryid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogsgeometries_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogsgeometries_geometries_geometryid",
                        column: x => x.geometryid,
                        principalSchema: "public",
                        principalTable: "geometries",
                        principalColumn: "geometryid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogsgroups",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    groupid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogsgroups", x => new { x.groupid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogsgroups_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogsgroups_groups_groupid",
                        column: x => x.groupid,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "groupid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogslayers",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    layerid = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogslayers", x => new { x.layerid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogslayers_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogslayers_layers_layerid",
                        column: x => x.layerid,
                        principalSchema: "public",
                        principalTable: "layers",
                        principalColumn: "layerid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "changelogsprojects",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    projectid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogsprojects", x => new { x.projectid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogsprojects_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogsprojects_projects_projectid",
                        column: x => x.projectid,
                        principalSchema: "public",
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booleandata",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    recordid = table.Column<Guid>(nullable: true),
                    title = table.Column<string>(nullable: true),
                    value = table.Column<bool>(nullable: true),
                    formfieldid = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booleandata", x => x.id);
                    table.ForeignKey(
                        name: "fk_booleandata_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_booleandata_records_recordid",
                        column: x => x.recordid,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "recordid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "changelogsrecords",
                schema: "public",
                columns: table => new
                {
                    changelogid = table.Column<long>(nullable: false),
                    recordid = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changelogsrecords", x => new { x.recordid, x.changelogid });
                    table.ForeignKey(
                        name: "fk_changelogsrecords_changelogs_changelogid",
                        column: x => x.changelogid,
                        principalSchema: "public",
                        principalTable: "changelogs",
                        principalColumn: "changelogid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_changelogsrecords_records_recordid",
                        column: x => x.recordid,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "recordid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "numericdata",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    recordid = table.Column<Guid>(nullable: true),
                    title = table.Column<string>(nullable: true),
                    value = table.Column<double>(nullable: true),
                    formfieldid = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_numericdata", x => x.id);
                    table.ForeignKey(
                        name: "fk_numericdata_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_numericdata_records_recordid",
                        column: x => x.recordid,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "recordid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "textdata",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    recordid = table.Column<Guid>(nullable: true),
                    title = table.Column<string>(nullable: true),
                    value = table.Column<string>(nullable: true),
                    formfieldid = table.Column<int>(nullable: true),
                    fieldchoiceid = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_textdata", x => x.id);
                    table.ForeignKey(
                        name: "fk_textdata_fieldchoices_fieldchoiceid",
                        column: x => x.fieldchoiceid,
                        principalSchema: "public",
                        principalTable: "fieldchoices",
                        principalColumn: "fieldchoiceid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_textdata_formfields_formfieldid",
                        column: x => x.formfieldid,
                        principalSchema: "public",
                        principalTable: "formfields",
                        principalColumn: "formfieldid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_textdata_records_recordid",
                        column: x => x.recordid,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "recordid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "fieldtypes",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 11, "Text" },
                    { 21, "Number" },
                    { 31, "Boolean" },
                    { 41, "DateTime" },
                    { 51, "Choice" },
                    { 61, "Guid" }
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "projectstatuses",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 1, "Projekt_bereit" },
                    { 2, "Gruppe_bereit" },
                    { 3, "Gruppendaten_erfasst" },
                    { 4, "Gruppendaten_gueltig" },
                    { 5, "Projekt_gueltig" },
                    { 11, "Gruppendaten_fehlerhaft" }
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "statuses",
                columns: new[] { "id", "description" },
                values: new object[,]
                {
                    { 1, "unchanged" },
                    { 2, "changed" },
                    { 3, "deleted" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_booleandata_formfieldid",
                schema: "public",
                table: "booleandata",
                column: "formfieldid");

            migrationBuilder.CreateIndex(
                name: "ix_booleandata_recordid",
                schema: "public",
                table: "booleandata",
                column: "recordid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogs_userid",
                schema: "public",
                table: "changelogs",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogsforms_changelogid",
                schema: "public",
                table: "changelogsforms",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogsgeometries_changelogid",
                schema: "public",
                table: "changelogsgeometries",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogsgroups_changelogid",
                schema: "public",
                table: "changelogsgroups",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogslayers_changelogid",
                schema: "public",
                table: "changelogslayers",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogsprojects_changelogid",
                schema: "public",
                table: "changelogsprojects",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_changelogsrecords_changelogid",
                schema: "public",
                table: "changelogsrecords",
                column: "changelogid");

            migrationBuilder.CreateIndex(
                name: "ix_fieldchoices_formfieldid",
                schema: "public",
                table: "fieldchoices",
                column: "formfieldid");

            migrationBuilder.CreateIndex(
                name: "ix_formfields_fieldtypeid",
                schema: "public",
                table: "formfields",
                column: "fieldtypeid");

            migrationBuilder.CreateIndex(
                name: "ix_formfields_formid",
                schema: "public",
                table: "formfields",
                column: "formid");

            migrationBuilder.CreateIndex(
                name: "ix_geometries_projectid",
                schema: "public",
                table: "geometries",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_geometries_statusid",
                schema: "public",
                table: "geometries",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_groups_statusid",
                schema: "public",
                table: "groups",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_groupsforms_formid",
                schema: "public",
                table: "groupsforms",
                column: "formid");

            migrationBuilder.CreateIndex(
                name: "ix_groupsusers_groupid",
                schema: "public",
                table: "groupsusers",
                column: "groupid");

            migrationBuilder.CreateIndex(
                name: "ix_numericdata_formfieldid",
                schema: "public",
                table: "numericdata",
                column: "formfieldid");

            migrationBuilder.CreateIndex(
                name: "ix_numericdata_recordid",
                schema: "public",
                table: "numericdata",
                column: "recordid");

            migrationBuilder.CreateIndex(
                name: "ix_projects_projectstatusid",
                schema: "public",
                table: "projects",
                column: "projectstatusid");

            migrationBuilder.CreateIndex(
                name: "ix_projects_statusid",
                schema: "public",
                table: "projects",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_projectsgroups_groupid",
                schema: "public",
                table: "projectsgroups",
                column: "groupid");

            migrationBuilder.CreateIndex(
                name: "ix_projectslayers_layerid",
                schema: "public",
                table: "projectslayers",
                column: "layerid");

            migrationBuilder.CreateIndex(
                name: "ix_records_formid",
                schema: "public",
                table: "records",
                column: "formid");

            migrationBuilder.CreateIndex(
                name: "ix_records_geometryid",
                schema: "public",
                table: "records",
                column: "geometryid");

            migrationBuilder.CreateIndex(
                name: "ix_records_projectid",
                schema: "public",
                table: "records",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_records_statusid",
                schema: "public",
                table: "records",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_textdata_fieldchoiceid",
                schema: "public",
                table: "textdata",
                column: "fieldchoiceid");

            migrationBuilder.CreateIndex(
                name: "ix_textdata_formfieldid",
                schema: "public",
                table: "textdata",
                column: "formfieldid");

            migrationBuilder.CreateIndex(
                name: "ix_textdata_recordid",
                schema: "public",
                table: "textdata",
                column: "recordid");

            migrationBuilder.CreateIndex(
                name: "ix_users_statusid",
                schema: "public",
                table: "users",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_usershaveprojectlayers_layerid",
                schema: "public",
                table: "usershaveprojectlayers",
                column: "layerid");

            migrationBuilder.CreateIndex(
                name: "ix_usershaveprojectlayers_projectid",
                schema: "public",
                table: "usershaveprojectlayers",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_userslayers_layerid",
                schema: "public",
                table: "userslayers",
                column: "layerid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booleandata",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogsforms",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogsgeometries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogsgroups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogslayers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogsprojects",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogsrecords",
                schema: "public");

            migrationBuilder.DropTable(
                name: "groupsforms",
                schema: "public");

            migrationBuilder.DropTable(
                name: "groupsusers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "numericdata",
                schema: "public");

            migrationBuilder.DropTable(
                name: "projectsgroups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "projectslayers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "textdata",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usershaveprojectlayers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "userslayers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "changelogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "groups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "fieldchoices",
                schema: "public");

            migrationBuilder.DropTable(
                name: "records",
                schema: "public");

            migrationBuilder.DropTable(
                name: "layers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formfields",
                schema: "public");

            migrationBuilder.DropTable(
                name: "geometries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "fieldtypes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "forms",
                schema: "public");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "public");

            migrationBuilder.DropTable(
                name: "projectstatuses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "statuses",
                schema: "public");
        }
    }
}
