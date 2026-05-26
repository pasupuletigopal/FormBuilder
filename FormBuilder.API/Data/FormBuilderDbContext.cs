using FormBuilder.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.API.Data;

public class FormBuilderDbContext : DbContext
{
    public FormBuilderDbContext(DbContextOptions<FormBuilderDbContext> options) : base(options) { }

    public DbSet<ControlType> ControlTypes => Set<ControlType>();
    public DbSet<Models.DataType> DataTypes => Set<Models.DataType>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<DataSourceItem> DataSourceItems => Set<DataSourceItem>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<FormControl> FormControls => Set<FormControl>();
    public DbSet<FormControlValidation> FormControlValidations => Set<FormControlValidation>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormRecord> FormRecords => Set<FormRecord>();
    public DbSet<FormConnector> FormConnectors => Set<FormConnector>();
    public DbSet<ExternalConnection> ExternalConnections => Set<ExternalConnection>();
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();
    public DbSet<ReportRunHistory> ReportRunHistory => Set<ReportRunHistory>();
     public DbSet<SpReport> SpReports => Set<SpReport>();
    public DbSet<ApiEnvironment> ApiEnvironments => Set<ApiEnvironment>();
    public DbSet<ApiCollection> ApiCollections => Set<ApiCollection>();
    public DbSet<ApiFolder> ApiFolders => Set<ApiFolder>();
    public DbSet<ApiRequest> ApiRequests => Set<ApiRequest>();
    public DbSet<ApiRequestHistory> ApiRequestHistory => Set<ApiRequestHistory>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<ControlType>(e => {
            e.ToTable("ControlTypes");
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<Models.DataType>(e => {
            e.ToTable("DataTypes");
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<DataSource>(e => {
            e.ToTable("DataSources");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Items)
             .WithOne(x => x.DataSource)
             .HasForeignKey(x => x.DataSourceId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExternalConnection)
             .WithMany()
             .HasForeignKey(x => x.ExternalConnectionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<DataSourceItem>(e => {
            e.ToTable("DataSourceItems");
        });

        mb.Entity<ValidationRule>(e => {
            e.ToTable("ValidationRules");
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<Form>(e => {
            e.ToTable("Forms");
            e.HasMany(x => x.Controls)
             .WithOne(x => x.Form)
             .HasForeignKey(x => x.FormId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FormControl>(e => {
            e.ToTable("FormControls");
            e.HasIndex(x => new { x.FormId, x.FieldName }).IsUnique();
            e.HasOne(x => x.ControlType)
             .WithMany()
             .HasForeignKey(x => x.ControlTypeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DataType)
             .WithMany()
             .HasForeignKey(x => x.DataTypeId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.DataSource)
             .WithMany()
             .HasForeignKey(x => x.DataSourceId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Validations)
             .WithOne(x => x.FormControl)
             .HasForeignKey(x => x.FormControlId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FormControlValidation>(e => {
            e.ToTable("FormControlValidations");
            e.HasOne(x => x.ValidationRule)
             .WithMany()
             .HasForeignKey(x => x.ValidationRuleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<FormSubmission>(e => {
            e.ToTable("FormSubmissions");
            e.HasOne(x => x.Form)
             .WithMany()
             .HasForeignKey(x => x.FormId)
             .OnDelete(DeleteBehavior.Restrict);
        });

         mb.Entity<FormRecord>(e =>
         {
             e.ToTable("FormRecords");
             e.HasOne(x => x.Form)
              .WithMany()
              .HasForeignKey(x => x.FormId)
              .OnDelete(DeleteBehavior.Restrict);
         });

        mb.Entity<FormConnector>(e => {
            e.ToTable("FormConnectors");
            e.HasOne(x => x.Form)
             .WithOne()
             .HasForeignKey<FormConnector>(x => x.FormId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExternalConnection)
             .WithMany(x => x.FormConnectors)
             .HasForeignKey(x => x.ExternalConnectionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<ExternalConnection>(e => {
            e.ToTable("ExternalConnections");
        });

        mb.Entity<Dashboard>(e => {
            e.ToTable("Dashboards");
            e.HasOne(x => x.ExternalConnection)
             .WithMany()
             .HasForeignKey(x => x.ExternalConnectionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Widgets)
             .WithOne(x => x.Dashboard)
             .HasForeignKey(x => x.DashboardId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<DashboardWidget>(e => {
            e.ToTable("DashboardWidgets");
        });
        mb.Entity<Report>(e => {
            e.ToTable("Reports");
            e.HasOne(x => x.ExternalConnection)
             .WithMany()
             .HasForeignKey(x => x.ExternalConnectionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Schedule)
             .WithOne(x => x.Report)
             .HasForeignKey<ReportSchedule>(x => x.ReportId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.RunHistory)
             .WithOne(x => x.Report)
             .HasForeignKey(x => x.ReportId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ReportSchedule>(e => e.ToTable("ReportSchedules"));
        mb.Entity<ReportRunHistory>(e => e.ToTable("ReportRunHistory"));

        mb.Entity<SpReport>(e => {
            e.ToTable("SpReports");
            e.HasOne(x => x.Connection)
             .WithMany()
             .HasForeignKey(x => x.ConnectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ?? API Manager ??
        mb.Entity<ApiEnvironment>(e => e.ToTable("ApiEnvironments"));

        mb.Entity<ApiCollection>(e => {
            e.ToTable("ApiCollections");
            e.HasMany(x => x.Folders)
             .WithOne(x => x.Collection)
             .HasForeignKey(x => x.CollectionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Requests)
             .WithOne(x => x.Collection)
             .HasForeignKey(x => x.CollectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ApiFolder>(e => {
            e.ToTable("ApiFolders");
            e.HasMany(x => x.SubFolders)
             .WithOne(x => x.ParentFolder)
             .HasForeignKey(x => x.ParentFolderId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Requests)
             .WithOne(x => x.Folder)
             .HasForeignKey(x => x.FolderId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<ApiRequest>(e => {
            e.ToTable("ApiRequests");
            e.HasMany(x => x.History)
             .WithOne(x => x.Request)
             .HasForeignKey(x => x.RequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ApiRequestHistory>(e => e.ToTable("ApiRequestHistory"));
    }
}
