using FormBuilder.API.Data;
using FormBuilder.API.Middleware;
using FormBuilder.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
builder.Services.AddDbContext<FormBuilderDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOpt =>
        {
            sqlOpt.EnableRetryOnFailure(3);
            sqlOpt.CommandTimeout(30);
        }
    )
);

// ---- Services ----
builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<IFormRecordService, FormRecordService>();
builder.Services.AddScoped<IExternalConnectionService, ExternalConnectionService>();
builder.Services.AddScoped<IExternalTableService, ExternalTableService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<ISpReportService, SpReportService>();
builder.Services.AddScoped<IApiManagerService, ApiManagerService>();

builder.Services.AddHttpClient();

// ---- Controllers ----
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---- CORS â€” allow Angular dev server ----
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(opt =>
    opt.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    })
);

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FormBuilder API",
        Version = "v1",
        Description = "Dynamic Form Builder â€” CRUD API for forms, controls, data sources and master data"
    });
});

// ---- Health checks ----
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FormBuilderDbContext>();

var app = builder.Build();

// ---- Middleware ----
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormBuilder API v1");
    c.RoutePrefix = "swagger";
});
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// ---- Auto-migrate on startup (dev only) ----
//if (app.Environment.IsDevelopment())
//{
//    using var scope = app.Services.CreateScope();
//    var db = scope.ServiceProvider.GetRequiredService<FormBuilderDbContext>();
//    await db.Database.MigrateAsync();
//}

app.Run();
