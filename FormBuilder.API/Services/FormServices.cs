using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FormBuilder.API.Services;

// ============================================================
// Interface â€” add new methods here for any extension
// ============================================================
public interface IFormService
{
    Task<PagedResult<FormSummaryDto>> GetFormsAsync(int page, int pageSize, string? search);
    Task<FormDto?> GetFormByIdAsync(int id);
    Task<FormDto> CreateFormAsync(CreateFormDto dto);
    Task<FormDto?> UpdateFormAsync(int id, UpdateFormDto dto);
    Task<bool> DeleteFormAsync(int id);
    Task<FormDto?> UpdateFormControlsAsync(int formId, UpdateFormControlsDto dto);
    Task<int?> CloneFormAsync(int formId, string newName);
    Task<FormSubmission?> SubmitFormAsync(FormSubmissionDto dto, string? ip);
    Task<List<FormSubmission>> GetSubmissionsAsync(int formId);
}

public interface IMasterDataService
{
    Task<List<ControlTypeDto>> GetControlTypesAsync();
    Task<ControlTypeDto?> GetControlTypeByIdAsync(int id);
    Task<ControlTypeDto> CreateControlTypeAsync(CreateControlTypeDto dto);
    Task<ControlTypeDto?> UpdateControlTypeAsync(int id, UpdateControlTypeDto dto);
    Task<bool> DeleteControlTypeAsync(int id);

    Task<List<DataTypeDto>> GetDataTypesAsync();

    Task<List<DataSourceDto>> GetDataSourcesAsync();
    Task<DataSourceDto?> GetDataSourceByIdAsync(int id);
    Task<DataSourceDto> CreateDataSourceAsync(CreateDataSourceDto dto);
    Task<DataSourceDto?> UpdateDataSourceAsync(int id, UpdateDataSourceDto dto);
    Task<bool> DeleteDataSourceAsync(int id);
    Task<List<DataSourceItemDto>> GetDataSourceItemsAsync(int id);
    Task<List<DataSourceItemDto>> PreviewApiDataSourceAsync(PreviewApiDataSourceRequest req);

    Task<List<ValidationRuleDto>> GetValidationRulesAsync();
}

// ============================================================
// Master Data Service Implementation
// ============================================================
public class MasterDataService : IMasterDataService
{
    private readonly FormBuilderDbContext _db;
    private readonly IExternalConnectionService _connSvc;
    private readonly IHttpClientFactory _httpFactory;

    public MasterDataService(
        FormBuilderDbContext db,
        IExternalConnectionService connSvc,
        IHttpClientFactory httpFactory)
    {
        _db = db;
        _connSvc = connSvc;
        _httpFactory = httpFactory;
    }

    public async Task<List<ControlTypeDto>> GetControlTypesAsync() =>
        await _db.ControlTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new ControlTypeDto(x.Id, x.Name, x.DisplayName, x.Icon, x.Description, x.Category, x.IsActive, x.SortOrder))
            .ToListAsync();

    public async Task<ControlTypeDto?> GetControlTypeByIdAsync(int id) =>
        await _db.ControlTypes
            .Where(x => x.Id == id)
            .Select(x => new ControlTypeDto(x.Id, x.Name, x.DisplayName, x.Icon, x.Description, x.Category, x.IsActive, x.SortOrder))
            .FirstOrDefaultAsync();

    public async Task<ControlTypeDto> CreateControlTypeAsync(CreateControlTypeDto dto)
    {
        var entity = new ControlType {
            Name = dto.Name, DisplayName = dto.DisplayName, Icon = dto.Icon,
            Description = dto.Description, Category = dto.Category, SortOrder = dto.SortOrder
        };
        _db.ControlTypes.Add(entity);
        await _db.SaveChangesAsync();
        return new ControlTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.Icon,
            entity.Description, entity.Category, entity.IsActive, entity.SortOrder);
    }

    public async Task<ControlTypeDto?> UpdateControlTypeAsync(int id, UpdateControlTypeDto dto)
    {
        var entity = await _db.ControlTypes.FindAsync(id);
        if (entity == null) return null;
        entity.Name = dto.Name; entity.DisplayName = dto.DisplayName;
        entity.Icon = dto.Icon; entity.Description = dto.Description;
        entity.Category = dto.Category; entity.IsActive = dto.IsActive;
        entity.SortOrder = dto.SortOrder; entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new ControlTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.Icon,
            entity.Description, entity.Category, entity.IsActive, entity.SortOrder);
    }

    public async Task<bool> DeleteControlTypeAsync(int id)
    {
        var entity = await _db.ControlTypes.FindAsync(id);
        if (entity == null) return false;
        entity.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataTypeDto>> GetDataTypesAsync() =>
        await _db.DataTypes
            .Where(x => x.IsActive)
            .Select(x => new DataTypeDto(x.Id, x.Name, x.DisplayName, x.DotNetType, x.IsCollection, x.IsActive))
            .ToListAsync();

    public async Task<List<DataSourceDto>> GetDataSourcesAsync() =>
        await _db.DataSources
            .Where(x => x.IsActive)
            .Include(x => x.Items.Where(i => i.IsActive))
            .OrderBy(x => x.Name)
            .Select(x => new DataSourceDto(x.Id, x.Name, x.Description, x.SourceType,
                x.ApiUrl, x.ApiMethod, x.ApiHeaders, x.ValueField, x.LabelField, x.IsActive,
                x.Items.Where(i => i.IsActive).OrderBy(i => i.SortOrder)
                    .Select(i => new DataSourceItemDto(i.Id, i.Value, i.Label, i.SortOrder, i.IsDefault, i.IsActive))
                    .ToList(),
                x.ExternalConnectionId, x.DatabaseName, x.SchemaName,
                x.TableName, x.ValueColumn, x.LabelColumn,
                x.FilterExpression, x.OrderByExpression))
            .ToListAsync();

    public async Task<DataSourceDto?> GetDataSourceByIdAsync(int id)
    {
        var ds = await _db.DataSources
            .Include(x => x.Items.Where(i => i.IsActive))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (ds == null) return null;
        return MapDataSource(ds);
    }

    public async Task<DataSourceDto> CreateDataSourceAsync(CreateDataSourceDto dto)
    {
        var entity = new DataSource {
            Name = dto.Name, Description = dto.Description, SourceType = dto.SourceType,
            ApiUrl = dto.ApiUrl, ApiMethod = dto.ApiMethod, ApiHeaders = dto.ApiHeaders,
            ValueField = dto.ValueField, LabelField = dto.LabelField,
            ExternalConnectionId = dto.ExternalConnectionId,
            DatabaseName = dto.DatabaseName, SchemaName = dto.SchemaName,
            TableName = dto.TableName, ValueColumn = dto.ValueColumn,
            LabelColumn = dto.LabelColumn, FilterExpression = dto.FilterExpression,
            OrderByExpression = dto.OrderByExpression,
            Items = dto.Items.Select(i => new DataSourceItem {
                Value = i.Value, Label = i.Label, SortOrder = i.SortOrder, IsDefault = i.IsDefault
            }).ToList()
        };
        _db.DataSources.Add(entity);
        await _db.SaveChangesAsync();
        return MapDataSource(entity);
    }

    public async Task<DataSourceDto?> UpdateDataSourceAsync(int id, UpdateDataSourceDto dto)
    {
        var entity = await _db.DataSources.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return null;
        entity.Name = dto.Name; entity.Description = dto.Description;
        entity.SourceType = dto.SourceType; entity.ApiUrl = dto.ApiUrl;
        entity.ApiMethod = dto.ApiMethod; entity.ApiHeaders = dto.ApiHeaders;
        entity.ValueField = dto.ValueField;
        entity.LabelField = dto.LabelField; entity.IsActive = dto.IsActive;
        entity.ExternalConnectionId = dto.ExternalConnectionId;
        entity.DatabaseName = dto.DatabaseName; entity.SchemaName = dto.SchemaName;
        entity.TableName = dto.TableName; entity.ValueColumn = dto.ValueColumn;
        entity.LabelColumn = dto.LabelColumn; entity.FilterExpression = dto.FilterExpression;
        entity.OrderByExpression = dto.OrderByExpression;
        entity.UpdatedAt = DateTime.UtcNow;
        // Replace items
        entity.Items.Clear();
        foreach (var item in dto.Items)
            entity.Items.Add(new DataSourceItem { Value = item.Value, Label = item.Label, SortOrder = item.SortOrder, IsDefault = item.IsDefault });
        await _db.SaveChangesAsync();
        return MapDataSource(entity);
    }

    public async Task<bool> DeleteDataSourceAsync(int id)
    {
        var entity = await _db.DataSources.FindAsync(id);
        if (entity == null) return false;
        entity.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ValidationRuleDto>> GetValidationRulesAsync() =>
        await _db.ValidationRules
            .Where(x => x.IsActive)
            .Select(x => new ValidationRuleDto(x.Id, x.Name, x.DisplayName, x.Pattern, x.ErrorMessage, x.IsActive))
            .ToListAsync();

    public async Task<List<DataSourceItemDto>> GetDataSourceItemsAsync(int id)
    {
        var ds = await _db.DataSources
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ds == null) return [];

        // Static: return stored items directly
        if (string.Equals(ds.SourceType, "Static", StringComparison.OrdinalIgnoreCase))
        {
            return ds.Items.Where(i => i.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => new DataSourceItemDto(i.Id, i.Value, i.Label, i.SortOrder, i.IsDefault, i.IsActive))
                .ToList();
        }

        // Database: query the external table using stored config
        if (string.Equals(ds.SourceType, "Database", StringComparison.OrdinalIgnoreCase))
        {
            if (!ds.ExternalConnectionId.HasValue)
                throw new InvalidOperationException("No external connection configured for this data source.");

            if (string.IsNullOrWhiteSpace(ds.TableName) || string.IsNullOrWhiteSpace(ds.ValueColumn) || string.IsNullOrWhiteSpace(ds.LabelColumn))
                throw new InvalidOperationException("Database data source is missing TableName, ValueColumn, or LabelColumn configuration.");

            await using var conn = await _connSvc.OpenConnectionAsync(ds.ExternalConnectionId.Value, ds.DatabaseName);

            var schema = ds.SchemaName ?? "dbo";
            var sql = $"SELECT [{ds.ValueColumn}], [{ds.LabelColumn}] FROM [{schema}].[{ds.TableName}]";

            if (!string.IsNullOrWhiteSpace(ds.FilterExpression))
                sql += $" WHERE {ds.FilterExpression}";

            if (!string.IsNullOrWhiteSpace(ds.OrderByExpression))
                sql += $" ORDER BY {ds.OrderByExpression}";

            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<DataSourceItemDto>();
            var sort = 0;
            while (await reader.ReadAsync())
            {
                items.Add(new DataSourceItemDto(
                    0,
                    reader[ds.ValueColumn]?.ToString() ?? "",
                    reader[ds.LabelColumn]?.ToString() ?? "",
                    sort++,
                    false,
                    true));
            }
            return items;
        }

        // API: call external URL and parse response
        if (string.Equals(ds.SourceType, "API", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ds.ApiUrl))
                throw new InvalidOperationException("API data source is missing ApiUrl.");
            return await FetchApiItemsAsync(
                ds.ApiUrl, ds.ApiMethod ?? "GET", ds.ApiHeaders,
                ds.ValueField ?? "value", ds.LabelField ?? "label");
        }

        return [];
    }

    public async Task<List<DataSourceItemDto>> PreviewApiDataSourceAsync(PreviewApiDataSourceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ApiUrl))
            throw new InvalidOperationException("ApiUrl is required.");
        return await FetchApiItemsAsync(
            req.ApiUrl, req.ApiMethod ?? "GET", req.ApiHeaders,
            req.ValueField ?? "value", req.LabelField ?? "label");
    }

    private async Task<List<DataSourceItemDto>> FetchApiItemsAsync(
        string apiUrl, string method, string? headersJson, string valueField, string labelField)
    {
        var client = _httpFactory.CreateClient();

        // Apply custom headers (e.g. Authorization, API keys)
        if (!string.IsNullOrWhiteSpace(headersJson))
        {
            try
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                    headersJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
                    }
                }
            }
            catch { /* ignore malformed headers JSON */ }
        }

        HttpResponseMessage response;

        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            response = await client.PostAsync(apiUrl, null);
        else
            response = await client.GetAsync(apiUrl);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Handle: array at root, or { "data": [...] }, or { "items": [...] }, or { "results": [...] }
        System.Text.Json.JsonElement arrayElement;
        if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            arrayElement = root;
        }
        else
        {
            if (root.TryGetProperty("data", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Array)
                arrayElement = d;
            else if (root.TryGetProperty("items", out var it) && it.ValueKind == System.Text.Json.JsonValueKind.Array)
                arrayElement = it;
            else if (root.TryGetProperty("results", out var r) && r.ValueKind == System.Text.Json.JsonValueKind.Array)
                arrayElement = r;
            else
                throw new InvalidOperationException(
                    "API response must be a JSON array or an object with a 'data', 'items', or 'results' array property.");
        }

        var items = new List<DataSourceItemDto>();
        var sort = 0;
        foreach (var el in arrayElement.EnumerateArray())
        {
            var val = el.TryGetProperty(valueField, out var vProp)
                ? vProp.ToString() : "";
            var lbl = el.TryGetProperty(labelField, out var lProp)
                ? lProp.ToString() : val;

            items.Add(new DataSourceItemDto(0, val, lbl, sort++, false, true));
        }
        return items;
    }

    private static DataSourceDto MapDataSource(DataSource ds) =>
        new(ds.Id, ds.Name, ds.Description, ds.SourceType, ds.ApiUrl, ds.ApiMethod,
            ds.ApiHeaders, ds.ValueField, ds.LabelField, ds.IsActive,
            ds.Items.Where(i => i.IsActive).OrderBy(i => i.SortOrder)
                .Select(i => new DataSourceItemDto(i.Id, i.Value, i.Label, i.SortOrder, i.IsDefault, i.IsActive))
                .ToList(),
            ds.ExternalConnectionId, ds.DatabaseName, ds.SchemaName,
            ds.TableName, ds.ValueColumn, ds.LabelColumn,
            ds.FilterExpression, ds.OrderByExpression);
}

// ============================================================
// Form Service Implementation
// ============================================================
public class FormService : IFormService
{
    private readonly FormBuilderDbContext _db;
    public FormService(FormBuilderDbContext db) => _db = db;

    public async Task<PagedResult<FormSummaryDto>> GetFormsAsync(int page, int pageSize, string? search)
    {
        var query = _db.Forms.Where(f => f.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.Contains(search) || (f.Description != null && f.Description.Contains(search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new FormSummaryDto(
                f.Id, f.Name, f.Description, f.Title, f.IsActive, f.Version, f.CreatedAt, f.UpdatedAt,
                f.Controls.Count(c => c.IsActive)))
            .ToListAsync();

        return new PagedResult<FormSummaryDto>(items, total, page, pageSize);
    }

    public async Task<FormDto?> GetFormByIdAsync(int id)
    {
        var form = await _db.Forms
            .Include(f => f.Controls.Where(c => c.IsActive))
                .ThenInclude(c => c.ControlType)
            .Include(f => f.Controls.Where(c => c.IsActive))
                .ThenInclude(c => c.DataType)
            .Include(f => f.Controls.Where(c => c.IsActive))
                .ThenInclude(c => c.DataSource)
                    .ThenInclude(ds => ds!.Items.Where(i => i.IsActive))
            .Include(f => f.Controls.Where(c => c.IsActive))
                .ThenInclude(c => c.Validations)
                    .ThenInclude(v => v.ValidationRule)
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

        return form == null ? null : MapForm(form);
    }

    public async Task<FormDto> CreateFormAsync(CreateFormDto dto)
    {
        var form = new Form {
            Name = dto.Name, Description = dto.Description, Title = dto.Title,
            SubmitUrl = dto.SubmitUrl, SubmitMethod = dto.SubmitMethod,
            BackgroundColor = dto.BackgroundColor, PrimaryColor = dto.PrimaryColor,
            FontFamily = dto.FontFamily, FontSize = dto.FontSize,
            BorderRadius = dto.BorderRadius, Padding = dto.Padding, MaxWidth = dto.MaxWidth
        };
        _db.Forms.Add(form);
        await _db.SaveChangesAsync();
        return (await GetFormByIdAsync(form.Id))!;
    }

    public async Task<FormDto?> UpdateFormAsync(int id, UpdateFormDto dto)
    {
        var form = await _db.Forms.FindAsync(id);
        if (form == null) return null;
        form.Name = dto.Name; form.Description = dto.Description; form.Title = dto.Title;
        form.SubmitUrl = dto.SubmitUrl; form.SubmitMethod = dto.SubmitMethod;
        form.IsActive = dto.IsActive; form.BackgroundColor = dto.BackgroundColor;
        form.PrimaryColor = dto.PrimaryColor; form.FontFamily = dto.FontFamily;
        form.FontSize = dto.FontSize; form.BorderRadius = dto.BorderRadius;
        form.Padding = dto.Padding; form.MaxWidth = dto.MaxWidth;
        form.Version++; form.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetFormByIdAsync(id);
    }

    public async Task<bool> DeleteFormAsync(int id)
    {
        var form = await _db.Forms.FindAsync(id);
        if (form == null) return false;
        form.IsActive = false; form.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<FormDto?> UpdateFormControlsAsync(int formId, UpdateFormControlsDto dto)
    {
        var form = await _db.Forms.Include(f => f.Controls).FirstOrDefaultAsync(f => f.Id == formId);
        if (form == null) return null;

        // Hard-delete existing controls to avoid unique constraint violation on (FormId, FieldName)
        _db.FormControls.RemoveRange(form.Controls);
        await _db.SaveChangesAsync();

        // Add new controls
        foreach (var controlDto in dto.Controls)
        {
            form.Controls.Add(new FormControl {
                FormId = formId,
                ControlTypeId = controlDto.ControlTypeId,
                DataTypeId = controlDto.DataTypeId,
                DataSourceId = controlDto.DataSourceId,
                FieldName = controlDto.FieldName,
                Label = controlDto.Label,
                Placeholder = controlDto.Placeholder,
                HelperText = controlDto.HelperText,
                DefaultValue = controlDto.DefaultValue,
                Tooltip = controlDto.Tooltip,
                RowIndex = controlDto.RowIndex,
                ColIndex = controlDto.ColIndex,
                ColSpan = controlDto.ColSpan,
                SortOrder = controlDto.SortOrder,
                Width = controlDto.Width,
                Height = controlDto.Height,
                LabelColor = controlDto.LabelColor,
                ControlColor = controlDto.ControlColor,
                BackgroundColor = controlDto.BackgroundColor,
                BorderColor = controlDto.BorderColor,
                BorderWidth = controlDto.BorderWidth,
                BorderRadius = controlDto.BorderRadius,
                FontFamily = controlDto.FontFamily,
                FontSize = controlDto.FontSize,
                FontWeight = controlDto.FontWeight,
                FontStyle = controlDto.FontStyle,
                TextColor = controlDto.TextColor,
                Padding = controlDto.Padding,
                Margin = controlDto.Margin,
                CustomCssClass = controlDto.CustomCssClass,
                IsRequired = controlDto.IsRequired,
                IsReadOnly = controlDto.IsReadOnly,
                IsDisabled = controlDto.IsDisabled,
                IsHidden = controlDto.IsHidden,
                MinValue = controlDto.MinValue,
                MaxValue = controlDto.MaxValue,
                MinLength = controlDto.MinLength,
                MaxLength = controlDto.MaxLength,
                Pattern = controlDto.Pattern,
                ButtonType = controlDto.ButtonType,
                ButtonVariant = controlDto.ButtonVariant,
                ClickAction = controlDto.ClickAction,
                Rows = controlDto.Rows,
                AutoResize = controlDto.AutoResize,
                ExtendedOptions = controlDto.ExtendedOptions,
                IsActive = true
            });
        }

        form.Version++;
        form.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetFormByIdAsync(formId);
    }

    public async Task<int?> CloneFormAsync(int formId, string newName)
    {
        var form = await _db.Forms
            .Include(f => f.Controls.Where(c => c.IsActive))
            .FirstOrDefaultAsync(f => f.Id == formId);
        if (form == null) return null;

        var clone = new Form {
            Name = newName, Description = form.Description,
            Title = form.Title + " (Copy)", SubmitUrl = form.SubmitUrl,
            SubmitMethod = form.SubmitMethod, BackgroundColor = form.BackgroundColor,
            PrimaryColor = form.PrimaryColor, FontFamily = form.FontFamily,
            FontSize = form.FontSize, BorderRadius = form.BorderRadius,
            Padding = form.Padding, MaxWidth = form.MaxWidth
        };

        foreach (var c in form.Controls)
        {
            clone.Controls.Add(new FormControl {
                ControlTypeId = c.ControlTypeId, DataTypeId = c.DataTypeId,
                DataSourceId = c.DataSourceId, FieldName = c.FieldName,
                Label = c.Label, Placeholder = c.Placeholder, HelperText = c.HelperText,
                DefaultValue = c.DefaultValue, RowIndex = c.RowIndex, ColIndex = c.ColIndex,
                ColSpan = c.ColSpan, SortOrder = c.SortOrder, Width = c.Width, Height = c.Height,
                LabelColor = c.LabelColor, ControlColor = c.ControlColor,
                BackgroundColor = c.BackgroundColor, BorderColor = c.BorderColor,
                BorderWidth = c.BorderWidth, BorderRadius = c.BorderRadius,
                FontFamily = c.FontFamily, FontSize = c.FontSize, FontWeight = c.FontWeight,
                FontStyle = c.FontStyle, TextColor = c.TextColor, IsRequired = c.IsRequired,
                IsReadOnly = c.IsReadOnly, IsDisabled = c.IsDisabled,
                MinValue = c.MinValue, MaxValue = c.MaxValue,
                MinLength = c.MinLength, MaxLength = c.MaxLength, Pattern = c.Pattern,
                ButtonType = c.ButtonType, ButtonVariant = c.ButtonVariant,
                ClickAction = c.ClickAction, Rows = c.Rows, AutoResize = c.AutoResize,
                ExtendedOptions = c.ExtendedOptions
            });
        }

        _db.Forms.Add(clone);
        await _db.SaveChangesAsync();
        return clone.Id;
    }

    public async Task<FormSubmission?> SubmitFormAsync(FormSubmissionDto dto, string? ip)
    {
        var form = await _db.Forms.FindAsync(dto.FormId);
        if (form == null) return null;

        var submission = new FormSubmission {
            FormId = dto.FormId, FormVersion = form.Version,
            Data = dto.Data, SubmittedBy = dto.SubmittedBy, IPAddress = ip
        };
        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        return submission;
    }

    public async Task<List<FormSubmission>> GetSubmissionsAsync(int formId) =>
        await _db.FormSubmissions
            .Where(s => s.FormId == formId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    private static FormDto MapForm(Form form) => new() {
        Id = form.Id, Name = form.Name, Description = form.Description,
        Title = form.Title, SubmitUrl = form.SubmitUrl, SubmitMethod = form.SubmitMethod,
        IsActive = form.IsActive, Version = form.Version,
        BackgroundColor = form.BackgroundColor, PrimaryColor = form.PrimaryColor,
        FontFamily = form.FontFamily, FontSize = form.FontSize,
        BorderRadius = form.BorderRadius, Padding = form.Padding, MaxWidth = form.MaxWidth,
        CreatedAt = form.CreatedAt, UpdatedAt = form.UpdatedAt,
        Controls = form.Controls.OrderBy(c => c.SortOrder).Select(MapControl).ToList()
    };

    private static FormControlDto MapControl(FormControl c) => new() {
        Id = c.Id, FormId = c.FormId,
        ControlTypeId = c.ControlTypeId,
        ControlTypeName = c.ControlType?.Name ?? "",
        ControlTypeDisplayName = c.ControlType?.DisplayName ?? "",
        ControlTypeCategory = c.ControlType?.Category ?? "",
        DataTypeId = c.DataTypeId, DataTypeName = c.DataType?.Name,
        DataSourceId = c.DataSourceId, DataSourceName = c.DataSource?.Name,
        FieldName = c.FieldName, Label = c.Label, Placeholder = c.Placeholder,
        HelperText = c.HelperText, DefaultValue = c.DefaultValue, Tooltip = c.Tooltip,
        RowIndex = c.RowIndex, ColIndex = c.ColIndex, ColSpan = c.ColSpan,
        RowSpan = c.RowSpan, SortOrder = c.SortOrder,
        Width = c.Width, Height = c.Height, MinWidth = c.MinWidth, MaxWidth = c.MaxWidth,
        LabelColor = c.LabelColor, ControlColor = c.ControlColor,
        BackgroundColor = c.BackgroundColor, BorderColor = c.BorderColor,
        BorderWidth = c.BorderWidth, BorderRadius = c.BorderRadius,
        FontFamily = c.FontFamily, FontSize = c.FontSize, FontWeight = c.FontWeight,
        FontStyle = c.FontStyle, TextColor = c.TextColor,
        Padding = c.Padding, Margin = c.Margin, CustomCssClass = c.CustomCssClass,
        CustomCss = c.CustomCss, IsRequired = c.IsRequired, IsReadOnly = c.IsReadOnly,
        IsDisabled = c.IsDisabled, IsHidden = c.IsHidden,
        MinValue = c.MinValue, MaxValue = c.MaxValue,
        MinLength = c.MinLength, MaxLength = c.MaxLength, Pattern = c.Pattern,
        ButtonType = c.ButtonType, ButtonVariant = c.ButtonVariant, ClickAction = c.ClickAction,
        Rows = c.Rows, AutoResize = c.AutoResize, ExtendedOptions = c.ExtendedOptions,
        DataSourceItems = c.DataSource?.Items.OrderBy(i => i.SortOrder)
            .Select(i => new DataSourceItemDto(i.Id, i.Value, i.Label, i.SortOrder, i.IsDefault, i.IsActive))
            .ToList(),
        Validations = c.Validations.Select(v => new FormControlValidationDto(
            v.Id, v.ValidationRuleId, v.ValidationRule?.DisplayName ?? "", v.CustomMessage))
            .ToList()
    };
}
