// New file: Backend/FormBuilder.API/Services/ReportService.cs

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.API.Services;

public interface IReportService
{
    Task<List<ReportDto>>      GetReportsAsync();
    Task<ReportDto?>           GetReportByIdAsync(int id);
    Task<ReportDto>            SaveReportAsync(int? id, SaveReportDto dto);
    Task<bool>                 DeleteReportAsync(int id);
    Task<RunReportResult>      RunReportAsync(int id);
    Task<RunReportResult>      PreviewReportAsync(SaveReportDto dto);
    Task<string>               GenerateSqlAsync(SaveReportDto dto);
    Task<List<ReportRunHistoryDto>> GetRunHistoryAsync(int reportId);

    // Schedule
    Task<ReportScheduleDto>    SaveScheduleAsync(int reportId, SaveScheduleDto dto);
    Task<bool>                 DeleteScheduleAsync(int reportId);
    Task                       RunScheduledReportsAsync();

    // Meta
    Task<List<string>>         GetTablesAsync(int connectionId, string db, string schema = "dbo");
    Task<List<TableColumnInfo>> GetTableColumnsAsync(int connectionId, string db, string schema, string table);
    Task<List<SpOrViewInfo>>   GetStoredProceduresAndViewsAsync(int connectionId, string db, string schema = "dbo");
    Task<List<SpParameterInfo>> GetSpParametersAsync(int connectionId, string db, string schema, string spName);
    Task<RunReportResult>      ExecuteSpOrViewAsync(ExecuteSpRequest req);
}

public record TableColumnInfo(string ColumnName, string DataType, bool IsNullable, bool IsPrimaryKey);

public class ReportService : IReportService
{
    private readonly FormBuilderDbContext       _db;
    private readonly IExternalConnectionService _connSvc;
    private readonly IReportExportService       _exportSvc;

    public ReportService(
        FormBuilderDbContext db,
        IExternalConnectionService connSvc,
        IReportExportService exportSvc)
    {
        _db        = db;
        _connSvc   = connSvc;
        _exportSvc = exportSvc;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────

    public async Task<List<ReportDto>> GetReportsAsync()
    {
        return await _db.Reports
            .Where(r => r.IsActive)
            .Include(r => r.ExternalConnection)
            .Include(r => r.Schedule)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => MapReport(r))
            .ToListAsync();
    }

    public async Task<ReportDto?> GetReportByIdAsync(int id)
    {
        var r = await _db.Reports
            .Include(r => r.ExternalConnection)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        return r == null ? null : MapReport(r);
    }

    public async Task<ReportDto> SaveReportAsync(int? id, SaveReportDto dto)
    {
        Report report;
        if (id.HasValue)
        {
            report = await _db.Reports.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Report {id} not found.");
        }
        else
        {
            report = new Report();
            _db.Reports.Add(report);
        }

        report.Name                 = dto.Name;
        report.Description          = dto.Description;
        report.ExternalConnectionId = dto.ExternalConnectionId;
        report.DatabaseName         = dto.DatabaseName;
        report.SchemaName           = dto.SchemaName ?? "dbo";
        report.QueryConfig          = dto.QueryConfig;
        report.IsActive             = true;
        report.UpdatedAt            = DateTime.UtcNow;

        // Pre-generate and cache the SQL
        try { report.LastGeneratedSql = await BuildSqlFromDto(dto); }
        catch { /* ignore — sql preview optional */ }

        await _db.SaveChangesAsync();
        return (await GetReportByIdAsync(report.Id))!;
    }

    public async Task<bool> DeleteReportAsync(int id)
    {
        var r = await _db.Reports.FindAsync(id);
        if (r == null) return false;
        r.IsActive  = false;
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Run ───────────────────────────────────────────────────────────────

    public async Task<RunReportResult> RunReportAsync(int id)
    {
        var report = await _db.Reports
            .Include(r => r.ExternalConnection)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive)
            ?? throw new KeyNotFoundException($"Report {id} not found.");

        var dto = new SaveReportDto(
            report.Name, report.Description,
            report.ExternalConnectionId,
            report.DatabaseName, report.SchemaName,
            report.QueryConfig);

        var sw     = Stopwatch.StartNew();
        var result = await ExecuteQueryAsync(dto);
        sw.Stop();

        // Log run history
        _db.ReportRunHistory.Add(new ReportRunHistory
        {
            ReportId    = id,
            RowCount    = result.TotalCount,
            DurationMs  = (int)sw.ElapsedMilliseconds,
            Status      = "Success",
            TriggeredBy = "Manual"
        });
        report.LastRunAt    = DateTime.UtcNow;
        report.LastRowCount = result.TotalCount;
        report.LastGeneratedSql = result.GeneratedSql;
        await _db.SaveChangesAsync();

        return result;
    }

    public async Task<RunReportResult> PreviewReportAsync(SaveReportDto dto)
        => await ExecuteQueryAsync(dto, previewLimit: 100);

    public async Task<string> GenerateSqlAsync(SaveReportDto dto)
        => await BuildSqlFromDto(dto);

    public async Task<List<ReportRunHistoryDto>> GetRunHistoryAsync(int reportId)
    {
        return await _db.ReportRunHistory
            .Where(h => h.ReportId == reportId)
            .OrderByDescending(h => h.RunAt)
            .Take(20)
            .Select(h => new ReportRunHistoryDto(
                h.Id, h.RunAt, h.RowCount, h.DurationMs,
                h.Status, h.ErrorMsg, h.TriggeredBy))
            .ToListAsync();
    }

    // ── Schedule ──────────────────────────────────────────────────────────

    public async Task<ReportScheduleDto> SaveScheduleAsync(int reportId, SaveScheduleDto dto)
    {
        var existing = await _db.ReportSchedules
            .FirstOrDefaultAsync(s => s.ReportId == reportId);

        if (existing == null)
        {
            existing = new ReportSchedule { ReportId = reportId };
            _db.ReportSchedules.Add(existing);
        }

        existing.Frequency    = dto.Frequency;
        existing.DayOfWeek    = dto.DayOfWeek;
        existing.DayOfMonth   = dto.DayOfMonth;
        existing.RunAtHour    = dto.RunAtHour;
        existing.EmailTo      = dto.EmailTo;
        existing.EmailSubject = dto.EmailSubject;
        existing.IsActive     = true;
        existing.NextRunAt    = CalculateNextRun(dto);

        await _db.SaveChangesAsync();
        return MapSchedule(existing);
    }

    public async Task<bool> DeleteScheduleAsync(int reportId)
    {
        var s = await _db.ReportSchedules.FirstOrDefaultAsync(x => x.ReportId == reportId);
        if (s == null) return false;
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task RunScheduledReportsAsync()
    {
        var due = await _db.ReportSchedules
            .Include(s => s.Report)
            .Where(s => s.IsActive && s.NextRunAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var schedule in due)
        {
            try
            {
                var result = await RunReportAsync(schedule.ReportId);
                // TODO: email result via IEmailService
                schedule.LastRunAt = DateTime.UtcNow;
                schedule.NextRunAt = CalculateNextRun(new SaveScheduleDto(
                    schedule.Frequency, schedule.DayOfWeek, schedule.DayOfMonth,
                    schedule.RunAtHour, schedule.EmailTo, schedule.EmailSubject));
            }
            catch (Exception ex)
            {
                _db.ReportRunHistory.Add(new ReportRunHistory
                {
                    ReportId    = schedule.ReportId,
                    Status      = "Failed",
                    ErrorMsg    = ex.Message,
                    TriggeredBy = "Schedule"
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    // ── Meta ──────────────────────────────────────────────────────────────

    public async Task<List<string>> GetTablesAsync(
        int connectionId, string db, string schema = "dbo")
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connectionId, db);
        var sql = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        var list = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    public async Task<List<TableColumnInfo>> GetTableColumnsAsync(
        int connectionId, string db, string schema, string table)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connectionId, db);
        var sql = @"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PK
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                  AND ku.TABLE_NAME = @table
                  AND ku.TABLE_SCHEMA = @schema
            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @table AND c.TABLE_SCHEMA = @schema
            ORDER BY c.ORDINAL_POSITION";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@table",  table);
        cmd.Parameters.AddWithValue("@schema", schema);

        var list = new List<TableColumnInfo>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new TableColumnInfo(
                rdr.GetString(0),
                rdr.GetString(1),
                rdr.GetString(2) == "YES",
                rdr.GetInt32(3) == 1));
        }
        return list;
    }

    public async Task<List<SpOrViewInfo>> GetStoredProceduresAndViewsAsync(
        int connectionId, string db, string schema = "dbo")
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connectionId, db);

        // Stored procedures from sys.procedures + views from INFORMATION_SCHEMA.VIEWS
        var sql = @"
            SELECT p.name AS Name, 'SP' AS Type, s.name AS SchemaName
            FROM sys.procedures p
            INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
            WHERE s.name = @schema
            UNION ALL
            SELECT TABLE_NAME AS Name, 'VIEW' AS Type, TABLE_SCHEMA AS SchemaName
            FROM INFORMATION_SCHEMA.VIEWS
            WHERE TABLE_SCHEMA = @schema
            ORDER BY Type, Name";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);

        var list = new List<SpOrViewInfo>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new SpOrViewInfo(
                rdr.GetString(0),
                rdr.GetString(1),
                rdr.GetString(2)));
        }
        return list;
    }

    public async Task<List<SpParameterInfo>> GetSpParametersAsync(
        int connectionId, string db, string schema, string spName)
    {
        await using var conn = await _connSvc.OpenConnectionAsync(connectionId, db);
        var sql = @"
            SELECT
                p.name                          AS Name,
                TYPE_NAME(p.user_type_id)       AS DataType,
                p.max_length                    AS MaxLength,
                p.has_default_value             AS HasDefault,
                CAST(p.default_value AS NVARCHAR(256)) AS DefaultValue,
                p.is_output                     AS IsOutput
            FROM sys.parameters p
            INNER JOIN sys.procedures sp ON p.object_id = sp.object_id
            INNER JOIN sys.schemas s ON sp.schema_id = s.schema_id
            WHERE s.name = @schema AND sp.name = @spName
            ORDER BY p.parameter_id";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@spName", spName);

        var list = new List<SpParameterInfo>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new SpParameterInfo(
                rdr.GetString(0),
                rdr.GetString(1),
                rdr.IsDBNull(2) ? null : rdr.GetInt16(2),
                rdr.GetBoolean(3),
                rdr.IsDBNull(4) ? null : rdr.GetString(4),
                rdr.GetBoolean(5)));
        }
        return list;
    }

    // ── Execution ────────────────────────────────────────────────────────

    public async Task<RunReportResult> ExecuteSpOrViewAsync(ExecuteSpRequest req)
    {
        var sw = Stopwatch.StartNew();
        var schema = Sanitize(req.SchemaName ?? "dbo");

        await using var conn = await _connSvc.OpenConnectionAsync(req.ConnectionId, req.DatabaseName);

        string sql;
        SqlCommand cmd;

        if (string.Equals(req.Type, "VIEW", StringComparison.OrdinalIgnoreCase))
        {
            // Query the view with TOP N + optional filters
            var sb = new StringBuilder($"SELECT TOP {req.TopN} * FROM [{schema}].[{Sanitize(req.Name)}]");

            var parms = new List<SqlParameter>();
            if (req.Filters is { Count: > 0 })
            {
                var clauses = new List<string>();
                for (int i = 0; i < req.Filters.Count; i++)
                {
                    var f = req.Filters[i];
                    var col = Sanitize(f.Column);
                    var pName = $"@f{i}";
                    var resolved = ResolveParamValue(f.Value);

                    var clause = f.Operator.ToLower() switch
                    {
                        "eq"       => $"[{col}] = {pName}",
                        "neq"      => $"[{col}] <> {pName}",
                        "contains" => $"CAST([{col}] AS NVARCHAR(MAX)) LIKE {pName}",
                        "starts"   => $"CAST([{col}] AS NVARCHAR(MAX)) LIKE {pName}",
                        "gt"       => $"[{col}] > {pName}",
                        "lt"       => $"[{col}] < {pName}",
                        "gte"      => $"[{col}] >= {pName}",
                        "lte"      => $"[{col}] <= {pName}",
                        "isnull"   => $"[{col}] IS NULL",
                        "notnull"  => $"[{col}] IS NOT NULL",
                        _          => $"[{col}] = {pName}"
                    };
                    clauses.Add(clause);

                    // isnull/notnull don't need a parameter
                    if (f.Operator.ToLower() is not "isnull" and not "notnull")
                    {
                        var paramValue = f.Operator.ToLower() switch
                        {
                            "contains" => $"%{resolved}%",
                            "starts"   => $"{resolved}%",
                            _          => resolved
                        };
                        parms.Add(new SqlParameter(pName, paramValue));
                    }
                }
                sb.Append(" WHERE " + string.Join(" AND ", clauses));
            }

            sql = sb.ToString();
            cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            foreach (var p in parms) cmd.Parameters.Add(p);
        }
        else
        {
            // Execute stored procedure
            sql = $"[{schema}].[{Sanitize(req.Name)}]";
            cmd = new SqlCommand(sql, conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            foreach (var p in req.Parameters ?? [])
            {
                var paramName = p.Name.StartsWith("@") ? p.Name : $"@{p.Name}";
                var resolved = ResolveParamValue(p.Value);

                if (!string.IsNullOrWhiteSpace(p.DataType))
                {
                    var sqlParam = new SqlParameter(paramName, MapSqlDbType(p.DataType))
                    {
                        Value = resolved
                    };
                    cmd.Parameters.Add(sqlParam);
                }
                else
                {
                    cmd.Parameters.AddWithValue(paramName, resolved);
                }
            }
        }

        await using (cmd)
        {
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
            sw.Stop();

            var displaySql = string.Equals(req.Type, "VIEW", StringComparison.OrdinalIgnoreCase)
                ? sql
                : $"EXEC [{schema}].[{Sanitize(req.Name)}] " +
                  string.Join(", ", (req.Parameters ?? []).Select(p =>
                      $"{(p.Name.StartsWith("@") ? p.Name : $"@{p.Name}")} = '{p.Value}'"));

            return new RunReportResult(rows, rows.Count, (int)sw.ElapsedMilliseconds, columns, displaySql);
        }
    }

    // ── SQL Builder ────────────────────────────────────────────────────────

    private async Task<RunReportResult> ExecuteQueryAsync(
        SaveReportDto dto, int? previewLimit = null)
    {
        var sql = await BuildSqlFromDto(dto, previewLimit);
        var sw  = Stopwatch.StartNew();

        await using var conn = await _connSvc.OpenConnectionAsync(
            dto.ExternalConnectionId, dto.DatabaseName);
        await using var cmd  = new SqlCommand(sql, conn) { CommandTimeout = 60 };

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
        sw.Stop();

        return new RunReportResult(rows, rows.Count, (int)sw.ElapsedMilliseconds, columns, sql);
    }

    private Task<string> BuildSqlFromDto(SaveReportDto dto, int? previewLimit = null)
    {
        var config = JsonSerializer.Deserialize<QueryConfig>(
            dto.QueryConfig,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid query config.");

        return Task.FromResult(BuildSql(config, dto.SchemaName, previewLimit));
    }

    private string BuildSql(QueryConfig cfg, string schema, int? previewLimit = null)
    {
        var sb   = new StringBuilder();
        var topN = previewLimit ?? cfg.TopN;

        // ---- SELECT ----
        sb.Append($"SELECT TOP {topN} ");

        if (cfg.Columns.Count == 0)
        {
            sb.Append("*");
        }
        else
        {
            var cols = cfg.Columns.Select(c =>
            {
                var tableAlias = GetTableAlias(cfg, c.Table);
                var colRef     = $"[{tableAlias}].[{Sanitize(c.Column)}]";
                var withAgg    = string.IsNullOrWhiteSpace(c.Aggregation)
                    ? colRef
                    : $"{c.Aggregation}({colRef})";
                var alias      = !string.IsNullOrWhiteSpace(c.Alias)
                    ? $" AS [{Sanitize(c.Alias)}]"
                    : "";
                return withAgg + alias;
            });
            sb.Append(string.Join(",\n       ", cols));
        }

        // ---- FROM ----
        sb.Append($"\nFROM [{Sanitize(schema)}].[{Sanitize(cfg.PrimaryTable)}] AS [{GetTableAlias(cfg, cfg.PrimaryTable)}]");

        // ---- JOINs ----
        foreach (var join in cfg.Joins)
        {
            var alias    = !string.IsNullOrWhiteSpace(join.Alias)
                ? join.Alias : join.Table;
            var leftParts  = join.LeftColumn.Split('.');
            var rightParts = join.RightColumn.Split('.');
            var leftRef    = leftParts.Length == 2
                ? $"[{GetTableAlias(cfg, leftParts[0])}].[{Sanitize(leftParts[1])}]"
                : $"[{Sanitize(join.LeftColumn)}]";
            var rightRef   = rightParts.Length == 2
                ? $"[{Sanitize(alias)}].[{Sanitize(rightParts[1])}]"
                : $"[{Sanitize(join.RightColumn)}]";

            sb.Append($"\n{join.JoinType} JOIN [{Sanitize(schema)}].[{Sanitize(join.Table)}] AS [{Sanitize(alias)}]");
            sb.Append($"\n    ON {leftRef} = {rightRef}");
        }

        // ---- WHERE ----
        if (cfg.Filters.Count > 0)
        {
            var clauses = cfg.Filters.Select((f, i) =>
            {
                var tableAlias = GetTableAlias(cfg, f.Table);
                var colRef = $"[{tableAlias}].[{Sanitize(f.Column)}]";
                return f.Operator.ToLower() switch
                {
                    "eq"       => $"{colRef} = '{EscapeSql(f.Value)}'",
                    "neq"      => $"{colRef} <> '{EscapeSql(f.Value)}'",
                    "contains" => $"{colRef} LIKE '%{EscapeSql(f.Value)}%'",
                    "starts"   => $"{colRef} LIKE '{EscapeSql(f.Value)}%'",
                    "gt"       => $"{colRef} > '{EscapeSql(f.Value)}'",
                    "lt"       => $"{colRef} < '{EscapeSql(f.Value)}'",
                    "gte"      => $"{colRef} >= '{EscapeSql(f.Value)}'",
                    "lte"      => $"{colRef} <= '{EscapeSql(f.Value)}'",
                    "isnull"   => $"{colRef} IS NULL",
                    "notnull"  => $"{colRef} IS NOT NULL",
                    _          => $"{colRef} = '{EscapeSql(f.Value)}'"
                };
            });
            sb.Append("\nWHERE " + string.Join("\n  AND ", clauses));
        }

        // ---- GROUP BY ----
        var hasAgg = cfg.Columns.Any(c => !string.IsNullOrWhiteSpace(c.Aggregation));
        if (hasAgg)
        {
            var nonAggCols = cfg.Columns
                .Where(c => string.IsNullOrWhiteSpace(c.Aggregation))
                .Select(c => $"[{GetTableAlias(cfg, c.Table)}].[{Sanitize(c.Column)}]");

            var groupCols = cfg.GroupBy.Count > 0
                ? cfg.GroupBy.Select(g => {
                    var parts = g.Split('.');
                    return parts.Length == 2
                        ? $"[{GetTableAlias(cfg, parts[0])}].[{Sanitize(parts[1])}]"
                        : $"[{Sanitize(g)}]";
                  })
                : nonAggCols;

            var groupList = groupCols.ToList();
            if (groupList.Count > 0)
                sb.Append("\nGROUP BY " + string.Join(", ", groupList));
        }

        // ---- ORDER BY ----
        if (cfg.OrderBy.Count > 0)
        {
            var orderCols = cfg.OrderBy.Select(o =>
            {
                var tableAlias = GetTableAlias(cfg, o.Table);
                return $"[{tableAlias}].[{Sanitize(o.Column)}] {o.Direction}";
            });
            sb.Append("\nORDER BY " + string.Join(", ", orderCols));
        }

        return sb.ToString();
    }

    private static string GetTableAlias(QueryConfig cfg, string tableName)
    {
        if (tableName == cfg.PrimaryTable) return tableName;
        var join = cfg.Joins.FirstOrDefault(j =>
            j.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        return join != null && !string.IsNullOrWhiteSpace(join.Alias)
            ? join.Alias : tableName;
    }

    private static string Sanitize(string s)
        => s.Replace("]", "").Replace("[", "").Replace("'", "").Trim();

    private static string EscapeSql(string s) => s.Replace("'", "''");

    private static System.Data.SqlDbType MapSqlDbType(string dataType) =>
        dataType.ToLower() switch
        {
            "int"              => System.Data.SqlDbType.Int,
            "bigint"           => System.Data.SqlDbType.BigInt,
            "smallint"         => System.Data.SqlDbType.SmallInt,
            "tinyint"          => System.Data.SqlDbType.TinyInt,
            "bit"              => System.Data.SqlDbType.Bit,
            "decimal" or "numeric" => System.Data.SqlDbType.Decimal,
            "float"            => System.Data.SqlDbType.Float,
            "real"             => System.Data.SqlDbType.Real,
            "money"            => System.Data.SqlDbType.Money,
            "smallmoney"       => System.Data.SqlDbType.SmallMoney,
            "date"             => System.Data.SqlDbType.Date,
            "datetime"         => System.Data.SqlDbType.DateTime,
            "datetime2"        => System.Data.SqlDbType.DateTime2,
            "smalldatetime"    => System.Data.SqlDbType.SmallDateTime,
            "datetimeoffset"   => System.Data.SqlDbType.DateTimeOffset,
            "time"             => System.Data.SqlDbType.Time,
            "char"             => System.Data.SqlDbType.Char,
            "nchar"            => System.Data.SqlDbType.NChar,
            "varchar"          => System.Data.SqlDbType.VarChar,
            "nvarchar"         => System.Data.SqlDbType.NVarChar,
            "text"             => System.Data.SqlDbType.Text,
            "ntext"            => System.Data.SqlDbType.NText,
            "uniqueidentifier" => System.Data.SqlDbType.UniqueIdentifier,
            "xml"              => System.Data.SqlDbType.Xml,
            "varbinary"        => System.Data.SqlDbType.VarBinary,
            "image"            => System.Data.SqlDbType.Image,
            _                  => System.Data.SqlDbType.NVarChar
        };

    private static object ResolveParamValue(object? value)
    {
        if (value == null) return DBNull.Value;
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String  => je.GetString() ?? (object)DBNull.Value,
                JsonValueKind.Number  => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                JsonValueKind.True    => true,
                JsonValueKind.False   => false,
                JsonValueKind.Null    => DBNull.Value,
                _                     => je.ToString()
            };
        }
        return value;
    }

    private static DateTime CalculateNextRun(SaveScheduleDto dto)
    {
        var now  = DateTime.UtcNow;
        var base_ = new DateTime(now.Year, now.Month, now.Day, dto.RunAtHour, 0, 0, DateTimeKind.Utc);

        return dto.Frequency switch
        {
            "Weekly"  => base_.AddDays(((dto.DayOfWeek ?? 1) - (int)now.DayOfWeek + 7) % 7),
            "Monthly" => new DateTime(now.Year, now.Month, dto.DayOfMonth ?? 1, dto.RunAtHour, 0, 0, DateTimeKind.Utc),
            _         => base_ <= now ? base_.AddDays(1) : base_
        };
    }

    private static ReportDto MapReport(Report r) => new(
        r.Id, r.Name, r.Description,
        r.ExternalConnectionId,
        r.ExternalConnection?.Name ?? "",
        r.DatabaseName, r.SchemaName,
        r.QueryConfig, r.LastGeneratedSql,
        r.IsActive, r.CreatedAt,
        r.LastRunAt, r.LastRowCount,
        r.Schedule != null ? MapSchedule(r.Schedule) : null);

    private static ReportScheduleDto MapSchedule(ReportSchedule s) => new(
        s.Id, s.ReportId, s.Frequency,
        s.DayOfWeek, s.DayOfMonth, s.RunAtHour,
        s.EmailTo, s.EmailSubject, s.IsActive,
        s.LastRunAt, s.NextRunAt);
}
