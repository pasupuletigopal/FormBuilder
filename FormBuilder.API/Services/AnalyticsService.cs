// New file: Backend/FormBuilder.API/Services/AnalyticsService.cs

using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace FormBuilder.API.Services;

public interface IAnalyticsService
{
    // Dashboard CRUD
    Task<List<DashboardDto>>  GetDashboardsAsync();
    Task<DashboardDto?>       GetDashboardByIdAsync(int id);
    Task<DashboardDto>        SaveDashboardAsync(int? id, SaveDashboardDto dto);
    Task<bool>                DeleteDashboardAsync(int id);

    // Widget CRUD
    Task<List<DashboardWidgetDto>> GetWidgetsAsync(int dashboardId);
    Task<DashboardWidgetDto>       SaveWidgetAsync(int dashboardId, int? widgetId, SaveWidgetDto dto);
    Task<bool>                     DeleteWidgetAsync(int widgetId);
    Task                           UpdateWidgetPositionsAsync(UpdateWidgetPositionsDto dto);

    // Data queries
    Task<KpiResult>       GetKpiDataAsync(WidgetDataRequest req);
    Task<ChartResult>     GetChartDataAsync(WidgetDataRequest req);
    Task<TableResult>     GetTableDataAsync(WidgetDataRequest req, int page, int pageSize);
    Task<DrillDownResult> GetDrillDownAsync(WidgetDataRequest req, string labelValue);

    // Meta
    Task<List<string>> GetNumericColumnsAsync(int connectionId, string db, string schema, string table);
    Task<List<string>> GetDateColumnsAsync(int connectionId, string db, string schema, string table);
    Task<List<string>> GetAllColumnsAsync(int connectionId, string db, string schema, string table);
    Task<List<string>> GetDistinctValuesAsync(int connectionId, string db, string schema, string table, string column);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly FormBuilderDbContext _db;
    private readonly IExternalConnectionService _connSvc;

    public AnalyticsService(FormBuilderDbContext db, IExternalConnectionService connSvc)
    {
        _db      = db;
        _connSvc = connSvc;
    }

    // ── Dashboard CRUD ──────────────────────────────────────────────────

    public async Task<List<DashboardDto>> GetDashboardsAsync()
    {
        return await _db.Dashboards
            .Where(d => d.IsActive)
            .Include(d => d.ExternalConnection)
            .Include(d => d.Widgets)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => MapDashboard(d))
            .ToListAsync();
    }

    public async Task<DashboardDto?> GetDashboardByIdAsync(int id)
    {
        var d = await _db.Dashboards
            .Include(d => d.ExternalConnection)
            .Include(d => d.Widgets.Where(w => w.IsActive))
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
        return d == null ? null : MapDashboard(d);
    }

    public async Task<DashboardDto> SaveDashboardAsync(int? id, SaveDashboardDto dto)
    {
        Dashboard dash;
        if (id.HasValue)
        {
            dash = await _db.Dashboards.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Dashboard {id} not found.");
        }
        else
        {
            dash = new Dashboard();
            _db.Dashboards.Add(dash);
        }

        dash.Name                 = dto.Name;
        dash.Description          = dto.Description;
        dash.ExternalConnectionId = dto.ExternalConnectionId;
        dash.DatabaseName         = dto.DatabaseName;
        dash.SchemaName           = dto.SchemaName ?? "dbo";
        dash.TableName            = dto.TableName;
        dash.DateColumn           = dto.DateColumn;
        dash.IsActive             = true;
        dash.UpdatedAt            = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetDashboardByIdAsync(dash.Id))!;
    }

    public async Task<bool> DeleteDashboardAsync(int id)
    {
        var d = await _db.Dashboards.FindAsync(id);
        if (d == null) return false;
        d.IsActive  = false;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Widget CRUD ─────────────────────────────────────────────────────

    public async Task<List<DashboardWidgetDto>> GetWidgetsAsync(int dashboardId)
    {
        return await _db.DashboardWidgets
            .Where(w => w.DashboardId == dashboardId && w.IsActive)
            .OrderBy(w => w.SortOrder)
            .Select(w => MapWidget(w))
            .ToListAsync();
    }

    public async Task<DashboardWidgetDto> SaveWidgetAsync(
        int dashboardId, int? widgetId, SaveWidgetDto dto)
    {
        DashboardWidget widget;
        if (widgetId.HasValue)
        {
            widget = await _db.DashboardWidgets.FindAsync(widgetId.Value)
                ?? throw new KeyNotFoundException($"Widget {widgetId} not found.");
        }
        else
        {
            widget = new DashboardWidget { DashboardId = dashboardId };
            _db.DashboardWidgets.Add(widget);
        }

        widget.Title      = dto.Title;
        widget.WidgetType = dto.WidgetType;
        widget.PositionX  = dto.PositionX;
        widget.PositionY  = dto.PositionY;
        widget.Width      = dto.Width;
        widget.Height     = dto.Height;
        widget.Config     = dto.Config;
        widget.SortOrder  = dto.SortOrder;
        widget.IsActive   = true;
        widget.UpdatedAt  = DateTime.UtcNow;

        // Update dashboard updated time
        var dash = await _db.Dashboards.FindAsync(dashboardId);
        if (dash != null) dash.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapWidget(widget);
    }

    public async Task<bool> DeleteWidgetAsync(int widgetId)
    {
        var w = await _db.DashboardWidgets.FindAsync(widgetId);
        if (w == null) return false;
        w.IsActive  = false;
        w.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateWidgetPositionsAsync(UpdateWidgetPositionsDto dto)
    {
        foreach (var pos in dto.Positions)
        {
            var w = await _db.DashboardWidgets.FindAsync(pos.Id);
            if (w == null) continue;
            w.PositionX = pos.PositionX;
            w.PositionY = pos.PositionY;
            w.Width     = pos.Width;
            w.Height    = pos.Height;
            w.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    // ── Data Queries ────────────────────────────────────────────────────

    public async Task<KpiResult> GetKpiDataAsync(WidgetDataRequest req)
    {
        var (dash, widget) = await GetDashboardAndWidgetAsync(req);
        var config = ParseConfig(widget.Config);

        var agg    = GetString(config, "aggregation", "COUNT");
        var col    = GetString(config, "valueColumn", "");
        var label  = GetString(config, "label", widget.Title);
        var prefix = GetString(config, "prefix", "");
        var suffix = GetString(config, "suffix", "");
        var color  = GetString(config, "colorScheme", "blue");

        var aggExpr = agg == "COUNT"
            ? "COUNT(*)"
            : string.IsNullOrWhiteSpace(col)
                ? throw new InvalidOperationException(
                    $"Widget config is missing 'valueColumn' required for {agg} aggregation.")
                : $"{agg}([{col}])";

        var (where, parms) = BuildWhere(dash, req);
        var sql = $"SELECT {aggExpr} FROM {QTable(dash)} {where}";

        await using var conn = await _connSvc.OpenConnectionAsync(
            dash.ExternalConnectionId, dash.DatabaseName);
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parms) cmd.Parameters.Add(p);

        var raw   = await cmd.ExecuteScalarAsync();
        var value = Convert.ToDouble(raw ?? 0);

        var formatted = agg == "COUNT"
            ? ((long)value).ToString("N0")
            : value.ToString("N2");

        return new KpiResult(label, formatted, prefix, suffix, color, value);
    }

    public async Task<ChartResult> GetChartDataAsync(WidgetDataRequest req)
    {
        var (dash, widget) = await GetDashboardAndWidgetAsync(req);
        var config = ParseConfig(widget.Config);

        // Bar/Line use xColumn/yColumn; Pie/Donut use labelColumn/valueColumn
        var xCol  = GetString(config, "xColumn", "");
        if (string.IsNullOrWhiteSpace(xCol))
            xCol = GetString(config, "labelColumn", "");

        var yCol  = GetString(config, "yColumn", "");
        if (string.IsNullOrWhiteSpace(yCol))
            yCol = GetString(config, "valueColumn", "");

        var agg   = GetString(config, "aggregation", "COUNT");
        var order = GetString(config, "orderBy", "value_desc");
        var limit = GetInt(config, "limit", 10);

        if (string.IsNullOrWhiteSpace(xCol))
            throw new InvalidOperationException(
                "Widget config is missing grouping column ('xColumn' or 'labelColumn').");

        // Lookup config
        var useLookup        = GetBool(config, "useLookup", false);
        var lookupTable      = GetString(config, "lookupTable", "");
        var lookupSchema     = GetString(config, "lookupSchema", "");
        var lookupValueCol   = GetString(config, "lookupValueColumn", "");
        var lookupLabelCol   = GetString(config, "lookupLabelColumn", "");

        var hasLookup = useLookup
            && !string.IsNullOrWhiteSpace(lookupTable)
            && !string.IsNullOrWhiteSpace(lookupValueCol)
            && !string.IsNullOrWhiteSpace(lookupLabelCol);

        string sql;

        if (hasLookup)
        {
            // JOIN lookup table: labels come from l.[LabelCol], grouping on l.[LabelCol]
            var aggExpr = agg == "COUNT"
                ? "COUNT(*)"
                : string.IsNullOrWhiteSpace(yCol)
                    ? throw new InvalidOperationException(
                        $"Widget config is missing value column ('yColumn' or 'valueColumn') required for {agg} aggregation.")
                    : $"{agg}(o.[{yCol}])";

            var orderExpr = order == "label_asc"
                ? $"l.[{SanitizeId(lookupLabelCol)}] ASC"
                : "AggValue DESC";

            var lSchema = SanitizeId(string.IsNullOrWhiteSpace(lookupSchema) ? dash.SchemaName ?? "dbo" : lookupSchema);
            var qualifiedLookup = $"[{dash.DatabaseName}].[{lSchema}].[{SanitizeId(lookupTable)}]";

            var (where, parms) = BuildWhere(dash, req, "o");

            sql = $@"
                SELECT TOP {limit}
                    l.[{SanitizeId(lookupLabelCol)}] AS Label,
                    {aggExpr} AS AggValue
                FROM {QTable(dash)} o
                LEFT JOIN {qualifiedLookup} l
                    ON o.[{SanitizeId(xCol)}] = l.[{SanitizeId(lookupValueCol)}]
                {where}
                GROUP BY l.[{SanitizeId(lookupLabelCol)}]
                ORDER BY {orderExpr}";

            await using var conn = await _connSvc.OpenConnectionAsync(
                dash.ExternalConnectionId, dash.DatabaseName);
            await using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parms) cmd.Parameters.Add(p);

            var labels = new List<string>();
            var values = new List<double>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                labels.Add(rdr.IsDBNull(0) ? "Unknown" : rdr.GetValue(0).ToString()!);
                values.Add(rdr.IsDBNull(1) ? 0 : Convert.ToDouble(rdr.GetValue(1)));
            }
            return new ChartResult(labels, values, xCol, yCol);
        }
        else
        {
            // No lookup — simple GROUP BY on the main table
            var aggExpr = agg == "COUNT"
                ? "COUNT(*)"
                : string.IsNullOrWhiteSpace(yCol)
                    ? throw new InvalidOperationException(
                        $"Widget config is missing value column ('yColumn' or 'valueColumn') required for {agg} aggregation.")
                    : $"{agg}([{yCol}])";

            var orderExpr = order == "label_asc"
                ? $"[{xCol}] ASC"
                : "AggValue DESC";

            var (where, parms) = BuildWhere(dash, req);

            sql = $@"
                SELECT TOP {limit}
                    [{xCol}] AS Label,
                    {aggExpr} AS AggValue
                FROM {QTable(dash)}
                {where}
                GROUP BY [{xCol}]
                ORDER BY {orderExpr}";

            await using var conn = await _connSvc.OpenConnectionAsync(
                dash.ExternalConnectionId, dash.DatabaseName);
            await using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parms) cmd.Parameters.Add(p);

            var labels = new List<string>();
            var values = new List<double>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                labels.Add(rdr.IsDBNull(0) ? "Unknown" : rdr.GetValue(0).ToString()!);
                values.Add(rdr.IsDBNull(1) ? 0 : Convert.ToDouble(rdr.GetValue(1)));
            }
            return new ChartResult(labels, values, xCol, yCol);
        }
    }

    public async Task<TableResult> GetTableDataAsync(
        WidgetDataRequest req, int page, int pageSize)
    {
        var (dash, widget) = await GetDashboardAndWidgetAsync(req);
        var config  = ParseConfig(widget.Config);
        var cols    = GetStringList(config, "columns");
        var orderBy = GetString(config, "orderByColumn", "");
        var orderDir = GetString(config, "orderByDir", "DESC");

        var colList  = cols.Count > 0
            ? string.Join(", ", cols.Select(c => $"[{c}]"))
            : "*";
        var orderExpr = !string.IsNullOrWhiteSpace(orderBy)
            ? $"ORDER BY [{orderBy}] {orderDir}"
            : "ORDER BY (SELECT NULL)";

        var (where, parms) = BuildWhere(dash, req);
        var offset = (page - 1) * pageSize;

        await using var conn = await _connSvc.OpenConnectionAsync(
            dash.ExternalConnectionId, dash.DatabaseName);

        // Count
        var countSql = $"SELECT COUNT(*) FROM {QTable(dash)} {where}";
        await using var countCmd = new SqlCommand(countSql, conn);
        foreach (var p in parms) countCmd.Parameters.Add(CloneParam(p));
        var total = (int)await countCmd.ExecuteScalarAsync();

        // Data
        var dataSql = $@"
            SELECT {colList}
            FROM {QTable(dash)}
            {where}
            {orderExpr}
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        await using var dataCmd = new SqlCommand(dataSql, conn);
        foreach (var p in parms) dataCmd.Parameters.Add(CloneParam(p));

        var rows    = new List<Dictionary<string, object?>>();
        var columns = new List<string>();

        await using var rdr = await dataCmd.ExecuteReaderAsync();
        for (int i = 0; i < rdr.FieldCount; i++)
            columns.Add(rdr.GetName(i));

        while (await rdr.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < rdr.FieldCount; i++)
                row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
            rows.Add(row);
        }

        return new TableResult(rows, total, page, pageSize, columns);
    }

    public async Task<DrillDownResult> GetDrillDownAsync(
        WidgetDataRequest req, string labelValue)
    {
        var (dash, widget) = await GetDashboardAndWidgetAsync(req);
        var config = ParseConfig(widget.Config);

        var xCol = widget.WidgetType is "Bar" or "Line"
            ? GetString(config, "xColumn", "")
            : GetString(config, "labelColumn", "");

        if (string.IsNullOrWhiteSpace(xCol))
            throw new InvalidOperationException(
                "Widget config is missing the grouping column ('xColumn' or 'labelColumn') needed for drill-down.");

        var (where, parms) = BuildWhere(dash, req);
        var drillWhere = string.IsNullOrWhiteSpace(where)
            ? $"WHERE [{xCol}] = @drillVal"
            : $"{where} AND [{xCol}] = @drillVal";

        var sql = $@"
            SELECT TOP 100 *
            FROM {QTable(dash)}
            {drillWhere}
            ORDER BY (SELECT NULL)";

        await using var conn = await _connSvc.OpenConnectionAsync(
            dash.ExternalConnectionId, dash.DatabaseName);
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parms) cmd.Parameters.Add(CloneParam(p));
        cmd.Parameters.AddWithValue("@drillVal", labelValue);

        var rows    = new List<Dictionary<string, object?>>();
        var columns = new List<string>();

        await using var rdr = await cmd.ExecuteReaderAsync();
        for (int i = 0; i < rdr.FieldCount; i++)
            columns.Add(rdr.GetName(i));
        while (await rdr.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < rdr.FieldCount; i++)
                row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
            rows.Add(row);
        }

        return new DrillDownResult(rows, rows.Count, columns,
            $"{xCol} = '{labelValue}'");
    }

    // ── Meta ─────────────────────────────────────────────────────────────

    public async Task<List<string>> GetNumericColumnsAsync(
        int connId, string db, string schema, string table)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connId, db);
        var sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_CATALOG=@db AND TABLE_SCHEMA=@schema AND TABLE_NAME=@table
                      AND DATA_TYPE IN ('int','bigint','smallint','decimal','numeric','float','real','money','smallmoney')
                    ORDER BY ORDINAL_POSITION";
        return await ReadListAsync(conn, sql, db, schema, table);
    }

    public async Task<List<string>> GetDateColumnsAsync(
        int connId, string db, string schema, string table)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connId, db);
        var sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_CATALOG=@db AND TABLE_SCHEMA=@schema AND TABLE_NAME=@table
                      AND DATA_TYPE IN ('date','datetime','datetime2','smalldatetime','datetimeoffset')
                    ORDER BY ORDINAL_POSITION";
        return await ReadListAsync(conn, sql, db, schema, table);
    }

    public async Task<List<string>> GetAllColumnsAsync(
        int connId, string db, string schema, string table)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connId, db);
        var sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_CATALOG=@db AND TABLE_SCHEMA=@schema AND TABLE_NAME=@table
                    ORDER BY ORDINAL_POSITION";
        return await ReadListAsync(conn, sql, db, schema, table);
    }

    public async Task<List<string>> GetDistinctValuesAsync(
        int connId, string db, string schema, string table, string column)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connId, db);
        var sql = $"SELECT DISTINCT TOP 50 [{SanitizeId(column)}] FROM [{db}].[{schema}].[{SanitizeId(table)}] WHERE [{SanitizeId(column)}] IS NOT NULL ORDER BY 1";
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetValue(0).ToString()!);
        return list;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(Dashboard dash, DashboardWidget widget)>
        GetDashboardAndWidgetAsync(WidgetDataRequest req)
    {
        var dash = await _db.Dashboards
            .Include(d => d.ExternalConnection)
            .FirstOrDefaultAsync(d => d.Id == req.DashboardId)
            ?? throw new KeyNotFoundException($"Dashboard {req.DashboardId} not found.");

        var widget = await _db.DashboardWidgets.FindAsync(req.WidgetId)
            ?? throw new KeyNotFoundException($"Widget {req.WidgetId} not found.");

        return (dash, widget);
    }

    private (string where, List<SqlParameter> parms) BuildWhere(
        Dashboard dash, WidgetDataRequest req, string tableAlias = "")
    {
        var clauses = new List<string>();
        var parms   = new List<SqlParameter>();
        int pi      = 0;

        // When a table alias is provided, prefix columns as "o.[Col]" instead of "[Col]"
        var prefix = string.IsNullOrWhiteSpace(tableAlias) ? "" : $"{tableAlias}.";

        // Date range
        if (!string.IsNullOrWhiteSpace(dash.DateColumn))
        {
            if (!string.IsNullOrWhiteSpace(req.DateFrom))
            {
                clauses.Add($"{prefix}[{dash.DateColumn}] >= @p{pi}");
                parms.Add(new SqlParameter($"@p{pi++}", DateTime.Parse(req.DateFrom)));
            }
            if (!string.IsNullOrWhiteSpace(req.DateTo))
            {
                clauses.Add($"{prefix}[{dash.DateColumn}] <= @p{pi}");
                parms.Add(new SqlParameter($"@p{pi++}",
                    DateTime.Parse(req.DateTo).AddDays(1).AddSeconds(-1)));
            }
        }

        // Column filters
        foreach (var f in req.Filters ?? [])
        {
            var col = SanitizeId(f.Column);
            switch (f.Operator.ToLower())
            {
                case "eq":
                    clauses.Add($"{prefix}[{col}] = @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
                case "neq":
                    clauses.Add($"{prefix}[{col}] <> @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
                case "contains":
                    clauses.Add($"CAST({prefix}[{col}] AS NVARCHAR(MAX)) LIKE @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", $"%{f.Value}%"));
                    break;
                case "gt":
                    clauses.Add($"{prefix}[{col}] > @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
                case "lt":
                    clauses.Add($"{prefix}[{col}] < @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
                case "gte":
                    clauses.Add($"{prefix}[{col}] >= @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
                case "lte":
                    clauses.Add($"{prefix}[{col}] <= @p{pi}");
                    parms.Add(new SqlParameter($"@p{pi++}", f.Value));
                    break;
            }
        }

        var where = clauses.Count > 0 ? "WHERE " + string.Join(" AND ", clauses) : "";
        return (where, parms);
    }

    private static string QTable(Dashboard d)
        => $"[{d.DatabaseName}].[{d.SchemaName}].[{d.TableName}]";

    private static string SanitizeId(string s)
        => s.Replace("]", "").Replace("[", "").Trim();

    private static JsonDocument ParseConfig(string json)
    {
        try { return JsonDocument.Parse(json); }
        catch { return JsonDocument.Parse("{}"); }
    }

    private static string GetString(JsonDocument doc, string key, string def)
    {
        if (doc.RootElement.TryGetProperty(key, out var v))
            return v.GetString() ?? def;
        return def;
    }

    private static int GetInt(JsonDocument doc, string key, int def)
    {
        if (doc.RootElement.TryGetProperty(key, out var v))
            return v.TryGetInt32(out var i) ? i : def;
        return def;
    }

    private static bool GetBool(JsonDocument doc, string key, bool def)
    {
        if (doc.RootElement.TryGetProperty(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
        }
        return def;
    }

    private static List<string> GetStringList(JsonDocument doc, string key)
    {
        if (!doc.RootElement.TryGetProperty(key, out var v)) return [];
        return v.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
    }

    private static async Task<List<string>> ReadListAsync(
        SqlConnection conn, string sql, string db, string schema, string table)
    {
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db",     db);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table",  table);
        var list = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private static SqlParameter CloneParam(SqlParameter p)
        => new(p.ParameterName, p.Value);

    private static DashboardDto MapDashboard(Dashboard d) => new(
        d.Id, d.Name, d.Description,
        d.ExternalConnectionId,
        d.ExternalConnection?.Name ?? "",
        d.DatabaseName, d.SchemaName, d.TableName,
        d.DateColumn, d.IsActive, d.CreatedAt,
        d.Widgets.Count(w => w.IsActive));

    private static DashboardWidgetDto MapWidget(DashboardWidget w) => new(
        w.Id, w.DashboardId, w.Title, w.WidgetType,
        w.PositionX, w.PositionY, w.Width, w.Height,
        w.Config, w.SortOrder);
}
