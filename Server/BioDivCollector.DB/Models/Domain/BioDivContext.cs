using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace BioDivCollector.DB.Models.Domain
{
    //https://docs.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext
    public class BioDivContext : DbContext
    {
        //public DbSet<Content> Contents { get; set; }
        //public DbSet<ContentDataType> ContentDataTypes { get; set; }

        public DbSet<TextData> TextData { get; set; }
        public DbSet<BooleanData> BooleanData { get; set; }
        public DbSet<NumericData> NumericData { get; set; }

        public DbSet<Record> Records { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<ReferenceGeometry> Geometries { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectStatus> ProjectStatuses { get; set; }
        //public DbSet<ProjectGeometry> ProjectsGeometries { get; set; } //not in use
        public DbSet<ProjectLayer> ProjectsLayers { get; set; }
        public DbSet<Layer> Layers { get; set; }
        public DbSet<ChangeLog> ChangeLogs { get; set; }
        public DbSet<ChangeLogProject> ChangeLogsProjects { get; set; }
        public DbSet<ChangeLogGeometry> ChangeLogsGeometries { get; set; }
        public DbSet<ChangeLogRecord> ChangeLogsRecords { get; set; }
        public DbSet<ChangeLogGroup> ChangeLogsGroups { get; set; }
        public DbSet<ChangeLogLayer> ChangeLogsLayers { get; set; }
        public DbSet<ChangeLogForm> ChangeLogsForms { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupStatus> GroupStatuses { get; set; }
        public DbSet<ProjectGroup> ProjectsGroups { get; set; }
        public DbSet<GroupUser> GroupsUsers { get; set; }
        //public DbSet<UserRole> UsersRoles { get; set; }
        public DbSet<UserLayer> UsersLayers { get; set; }
        public DbSet<UserHasProjectLayer> UsersHaveProjectLayers { get; set; }
        //public DbSet<Role> Roles { get; set; }

        public DbSet<Form> Forms { get; set; }
        public DbSet<FormFormField> FormsFormFields { get; set; }
        public DbSet<FormField> FormFields { get; set; }
        public DbSet<FieldType> FieldTypes { get; set; }
        public DbSet<FieldChoice> FieldChoices { get; set; }
        public DbSet<ProjectForm> ProjectsForms { get; set; }

        //public BioDivContext(DbContextOptions<BioDivContext> options)
        //    : base(options)
        //{ }

        public static readonly ILoggerFactory DebugLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddDebug(); });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<BioDivContext>()
                .AddEnvironmentVariables()
                .Build();

            string hmm = configuration.GetSection("Environment").GetSection("DBHost").Value;

            optionsBuilder
                .UseNpgsql(
                    "Host="+ configuration.GetSection("Environment").GetSection("DBHost").Value + "; Database=" + configuration.GetSection("Environment").GetSection("DB").Value + ";Username=" + configuration.GetSection("Environment").GetSection("DBUser").Value + ";Password=" + configuration.GetSection("Environment").GetSection("DBPassword").Value + "",   
                    options =>
                    {
                        options.UseNetTopologySuite();
                        options.EnableRetryOnFailure();     //TODO: needed? https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
                    }
                 )
                .UseLowerCaseNamingConvention()
              ;
            //optionsBuilder.UseLazyLoadingProxies();
            optionsBuilder.UseLoggerFactory(DebugLoggerFactory);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<ReferenceGeometry>().Property(g => g.Point).HasColumnType("geometry(POINT, 4326)");
            modelBuilder.Entity<ReferenceGeometry>().Property(g => g.Line).HasColumnType("geometry(LINESTRING, 4326)");
            modelBuilder.Entity<ReferenceGeometry>().Property(g => g.Polygon).HasColumnType("geometry(POLYGON, 4326)");

            //Project n:m Geometry. not implemented so far...
            //modelBuilder.Entity<ProjectGeometry>()
            //    .HasKey(pg => new { pg.ProjectId, pg.GeometryId });
            //modelBuilder.Entity<ProjectGeometry>()
            //    .HasOne(pg => pg.Project)
            //    .WithMany(p => p.ProjectGeometries)
            //    .HasForeignKey(pg => pg.ProjectId);
            //modelBuilder.Entity<ProjectGeometry>()
            //    .HasOne(pg => pg.Geometry)
            //    .WithMany(g => g.GeometryProjects)
            //    .HasForeignKey(pg => pg.GeometryId);

            modelBuilder.Entity<ProjectGroup>()
               .HasKey(pg => new { pg.ProjectId, pg.GroupId });
            modelBuilder.Entity<ProjectGroup>()
                .HasOne(pg => pg.Project)
                .WithMany(p => p.ProjectGroups)
                .HasForeignKey(pg => pg.ProjectId);
            modelBuilder.Entity<ProjectGroup>()
                .HasOne(pg => pg.Group)
                .WithMany(g => g.GroupProjects)
                .HasForeignKey(pg => pg.GroupId);

            modelBuilder.Entity<ProjectLayer>()
                .HasKey(pl => new { pl.ProjectId, pl.LayerId });
            modelBuilder.Entity<ProjectLayer>()
                .HasOne(pl => pl.Project)
                .WithMany(p => p.ProjectLayers)
                .HasForeignKey(pg => pg.ProjectId);
            modelBuilder.Entity<ProjectLayer>()
                .HasOne(pl => pl.Layer)
                .WithMany(g => g.LayerProjects)
                .HasForeignKey(pl => pl.LayerId);

            modelBuilder.Entity<GroupUser>()
                .HasKey(gu => new { gu.UserId, gu.GroupId });
            modelBuilder.Entity<GroupUser>()
                .HasOne(gu => gu.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(gu => gu.UserId);
            modelBuilder.Entity<GroupUser>()
                .HasOne(gu => gu.Group)
                .WithMany(g => g.GroupUsers)
                .HasForeignKey(gu => gu.GroupId);

            modelBuilder.Entity<FormFormField>()
                .HasKey(fff => new { fff.FormId, fff.FormFieldId });
            modelBuilder.Entity<FormFormField>()
                .HasOne(fff => fff.Form)
                .WithMany(f => f.FormFormFields)
                .HasForeignKey(fff => fff.FormId);
            modelBuilder.Entity<FormFormField>()
                .HasOne(fff => fff.FormField)
                .WithMany(ff => ff.FormFieldForms)
                .HasForeignKey(fff => fff.FormFieldId);
           
            modelBuilder.Entity<UserLayer>()
                .HasKey(ul => new { ul.UserId, ul.LayerId });

            modelBuilder.Entity<UserHasProjectLayer>()
                .HasKey(uhpl => new { uhpl.UserId, uhpl.ProjectId, uhpl.LayerId });
            
            modelBuilder.Entity<ProjectForm>()
                .HasKey(ur => new { ur.ProjectId, ur.FormId });
            modelBuilder.Entity<ProjectForm>()
                .HasOne(gf => gf.Project)
                .WithMany(g => g.ProjectForms)
                .HasForeignKey(gf => gf.ProjectId);
            modelBuilder.Entity<ProjectForm>()
                .HasOne(gf => gf.Form)
                .WithMany(f => f.FormProjects)
                .HasForeignKey(gf => gf.FormId);

            modelBuilder.Entity<ChangeLog>()
                .Property(c => c.ChangeDate)
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<ChangeLogProject>()
                .HasKey(cl => new { cl.ProjectId, cl.ChangeLogId });
            modelBuilder.Entity<ChangeLogGeometry>()
                .HasKey(cl => new { cl.GeometryId, cl.ChangeLogId });
            modelBuilder.Entity<ChangeLogRecord>()
                .HasKey(cl => new { cl.RecordId, cl.ChangeLogId });
            modelBuilder.Entity<ChangeLogGroup>()
                .HasKey(cl => new { cl.GroupId, cl.ChangeLogId });
            modelBuilder.Entity<ChangeLogLayer>()
                .HasKey(cl => new { cl.LayerId, cl.ChangeLogId });
            modelBuilder.Entity<ChangeLogForm>()
                .HasKey(cl => new { cl.FormId, cl.ChangeLogId });

            modelBuilder.Entity<Project>().Property(p => p.StatusId).HasDefaultValue(StatusEnum.unchanged);
            modelBuilder.Entity<ReferenceGeometry>().Property(p => p.StatusId).HasDefaultValue(StatusEnum.unchanged);
            modelBuilder.Entity<Record>().Property(p => p.StatusId).HasDefaultValue(StatusEnum.unchanged);
            modelBuilder.Entity<Group>().Property(p => p.StatusId).HasDefaultValue(StatusEnum.unchanged);
            foreach (StatusEnum type in (StatusEnum[])Enum.GetValues(typeof(StatusEnum)))
            {
                modelBuilder.Entity<Status>()
                    .HasData(new Status(type, type.ToString()));
            }

            modelBuilder.Entity<Project>().Property(p => p.ProjectStatusId).HasDefaultValue(ProjectStatusEnum.Projekt_neu);
            foreach (ProjectStatusEnum type in (ProjectStatusEnum[])Enum.GetValues(typeof(ProjectStatusEnum)))
            {
                modelBuilder.Entity<ProjectStatus>()
                    .HasData(new ProjectStatus(type, type.ToString()));
            }
            // 20210109 chs: Standard is Gruppe_Bereit
            modelBuilder.Entity<Group>().Property(p => p.GroupStatusId).HasDefaultValue(GroupStatusEnum.Gruppe_bereit);
            foreach (GroupStatusEnum type in (GroupStatusEnum[])Enum.GetValues(typeof(GroupStatusEnum)))
            {
                modelBuilder.Entity<GroupStatus>()
                    .HasData(new GroupStatus(type, type.ToString()));
            }
            // 20210109 chs: Groupstate per Project
            modelBuilder.Entity<ProjectGroup>().Property(p => p.GroupStatusId).HasDefaultValue(GroupStatusEnum.Gruppe_bereit);

            //modelBuilder.Entity<UserRole>().Property(p => p.Role)
            //foreach (RoleEnum type in (RoleEnum[])Enum.GetValues(typeof(RoleEnum)))
            //{
            //    modelBuilder.Entity<Role>()
            //        .HasData(new Role(type, type.ToString()));
            //}

            foreach (FieldTypeEnum type in (FieldTypeEnum[])Enum.GetValues(typeof(FieldTypeEnum)))
            {
                modelBuilder.Entity<FieldType>()
                    .HasData(new FieldType(type, type.ToString()));
            }
        }
    }
}
