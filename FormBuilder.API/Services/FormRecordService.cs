// New file: Backend/FormBuilder.API/Services/FormRecordService.cs

using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.API.Services;

public interface IFormRecordService
{
    Task<FormRecordPagedResult> GetRecordsAsync(int formId, int page, int pageSize, string? search);
    Task<FormRecordDto?> GetRecordByIdAsync(int id);
    Task<FormRecordDto> CreateRecordAsync(CreateFormRecordDto dto);
    Task<FormRecordDto?> UpdateRecordAsync(int id, UpdateFormRecordDto dto);
    Task<bool> DeleteRecordAsync(int id);
    Task<int> BulkDeleteAsync(int formId, List<int> ids);
    Task<List<FormRecordDto>> ExportRecordsAsync(int formId);
}

public class FormRecordService : IFormRecordService
{
    private readonly FormBuilderDbContext _db;

    public FormRecordService(FormBuilderDbContext db) => _db = db;

    public async Task<FormRecordPagedResult> GetRecordsAsync(
        int formId, int page, int pageSize, string? search)
    {
        var query = _db.FormRecords
            .Where(r => r.FormId == formId && r.Status == "Active");

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.RecordData.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new FormRecordDto(
                r.Id, r.FormId, r.RecordData, r.Status,
                r.CreatedBy, r.CreatedAt, r.UpdatedAt))
            .ToListAsync();

        return new FormRecordPagedResult(items, total, page, pageSize);
    }

    public async Task<FormRecordDto?> GetRecordByIdAsync(int id)
    {
        var r = await _db.FormRecords.FindAsync(id);
        if (r == null || r.Status == "Deleted") return null;
        return new FormRecordDto(r.Id, r.FormId, r.RecordData,
            r.Status, r.CreatedBy, r.CreatedAt, r.UpdatedAt);
    }

    public async Task<FormRecordDto> CreateRecordAsync(CreateFormRecordDto dto)
    {
        var record = new FormRecord
        {
            FormId     = dto.FormId,
            RecordData = dto.RecordData,
            CreatedBy  = dto.CreatedBy,
            Status     = "Active"
        };
        _db.FormRecords.Add(record);
        await _db.SaveChangesAsync();
        return new FormRecordDto(record.Id, record.FormId, record.RecordData,
            record.Status, record.CreatedBy, record.CreatedAt, record.UpdatedAt);
    }

    public async Task<FormRecordDto?> UpdateRecordAsync(int id, UpdateFormRecordDto dto)
    {
        var record = await _db.FormRecords.FindAsync(id);
        if (record == null || record.Status == "Deleted") return null;

        record.RecordData = dto.RecordData;
        record.CreatedBy  = dto.CreatedBy;
        record.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new FormRecordDto(record.Id, record.FormId, record.RecordData,
            record.Status, record.CreatedBy, record.CreatedAt, record.UpdatedAt);
    }

    public async Task<bool> DeleteRecordAsync(int id)
    {
        var record = await _db.FormRecords.FindAsync(id);
        if (record == null) return false;
        record.Status    = "Deleted";
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkDeleteAsync(int formId, List<int> ids)
    {
        var records = await _db.FormRecords
            .Where(r => r.FormId == formId && ids.Contains(r.Id))
            .ToListAsync();

        foreach (var r in records)
        {
            r.Status    = "Deleted";
            r.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return records.Count;
    }

    public async Task<List<FormRecordDto>> ExportRecordsAsync(int formId)
    {
        return await _db.FormRecords
            .Where(r => r.FormId == formId && r.Status == "Active")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new FormRecordDto(r.Id, r.FormId, r.RecordData,
                r.Status, r.CreatedBy, r.CreatedAt, r.UpdatedAt))
            .ToListAsync();
    }
}
