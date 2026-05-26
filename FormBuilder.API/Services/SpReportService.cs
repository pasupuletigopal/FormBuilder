using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FormBuilder.API.Services;

public interface ISpReportService
{
    Task<List<SpReportDto>> GetAllAsync();
    Task<SpReportDto?> GetByIdAsync(int id);
    Task<SpReportDto> SaveAsync(int? id, SaveSpReportDto dto);
    Task<bool> DeleteAsync(int id);
    Task<RunReportResult> RunAsync(int id);
}

public class SpReportService : ISpReportService
{
    private readonly FormBuilderDbContext _db;
    private readonly IReportService _reportSvc;

    public SpReportService(FormBuilderDbContext db, IReportService reportSvc)
    {
        _db = db;
        _reportSvc = reportSvc;
    }

    public async Task<List<SpReportDto>> GetAllAsync()
    {
        return await _db.SpReports
            .Include(r => r.Connection)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => Map(r))
            .ToListAsync();
    }

    public async Task<SpReportDto?> GetByIdAsync(int id)
    {
        var r = await _db.SpReports
            .Include(r => r.Connection)
            .FirstOrDefaultAsync(r => r.Id == id);
        return r == null ? null : Map(r);
    }

    public async Task<SpReportDto> SaveAsync(int? id, SaveSpReportDto dto)
    {
        SpReport entity;
        if (id.HasValue)
        {
            entity = await _db.SpReports.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"SpReport {id} not found.");
        }
        else
        {
            entity = new SpReport();
            _db.SpReports.Add(entity);
        }

        entity.Name            = dto.Name;
        entity.Description     = dto.Description;
        entity.ConnectionId    = dto.ConnectionId;
        entity.DatabaseName    = dto.DatabaseName;
        entity.SchemaName      = dto.SchemaName ?? "dbo";
        entity.ObjectName      = dto.ObjectName;
        entity.ObjectType      = dto.ObjectType;
        entity.ParameterConfig = dto.ParameterConfig;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.SpReports.FindAsync(id);
        if (entity == null) return false;
        _db.SpReports.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<RunReportResult> RunAsync(int id)
    {
        var entity = await _db.SpReports.FindAsync(id)
            ?? throw new KeyNotFoundException($"SpReport {id} not found.");

        // Parse stored parameter config into parameter values
        List<SpParameterValue>? parameters = null;
        if (!string.IsNullOrWhiteSpace(entity.ParameterConfig))
        {
            parameters = JsonSerializer.Deserialize<List<SpParameterValue>>(
                entity.ParameterConfig,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        var req = new ExecuteSpRequest(
            entity.ConnectionId,
            entity.DatabaseName,
            entity.SchemaName,
            entity.ObjectName,
            entity.ObjectType == "StoredProcedure" ? "SP" : "VIEW",
            parameters);

        var result = await _reportSvc.ExecuteSpOrViewAsync(req);

        // Update last run stats
        entity.LastRunAt    = DateTime.UtcNow;
        entity.LastRowCount = result.TotalCount;
        await _db.SaveChangesAsync();

        return result;
    }

    private static SpReportDto Map(SpReport r) => new(
        r.Id, r.Name, r.Description,
        r.ConnectionId, r.Connection?.Name,
        r.DatabaseName, r.SchemaName,
        r.ObjectName, r.ObjectType,
        r.ParameterConfig,
        r.CreatedAt, r.LastRunAt, r.LastRowCount);
}
