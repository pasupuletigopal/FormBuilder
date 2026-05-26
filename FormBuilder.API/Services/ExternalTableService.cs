using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FormBuilder.API.Services;

public interface IExternalTableService
{
    // Connector config
    Task<FormConnectorDto?> GetConnectorAsync(int formId);
    Task<FormConnectorDto>  SaveConnectorAsync(int formId, SaveConnectorDto dto);
    Task<bool>              DeleteConnectorAsync(int formId);
    Task<TestConnectionResult> TestConnectionAsync(int formId);

    // External table CRUD
    Task<ExternalRecordPagedResult> GetRecordsAsync(
        int formId, int page, int pageSize, string? search, string? orderBy);

    Task<Dictionary<string, object?>> GetRecordByIdAsync(int formId, object pkValue);

    Task<Dictionary<string, object?>> CreateRecordAsync(
        int formId, Dictionary<string, JsonElement> values);

    Task<Dictionary<string, object?>> UpdateRecordAsync(
        int formId, object pkValue, Dictionary<string, JsonElement> values);

    Task<bool>  DeleteRecordAsync(int formId, object pkValue);
    Task<int>   BulkDeleteAsync(int formId, List<object> pkValues);

    // Meta
    Task<List<string>> GetTableColumnsAsync(int formId);
    Task<List<string>> GetDatabasesAsync();
    Task<List<string>> GetTablesAsync(string dbName, string schema = "dbo");
}

public class ExternalTableService : IExternalTableService
{
    private readonly FormBuilderDbContext _db;
    private readonly IConfiguration _config;
    private readonly IExternalConnectionService _connSvc;
    private readonly IHttpContextAccessor _httpCtx;

    public ExternalTableService(
        FormBuilderDbContext db,
        IConfiguration config,
        IExternalConnectionService connSvc,
        IHttpContextAccessor httpCtx)
    {
        _db      = db;
        _config  = config;
        _connSvc = connSvc;
        _httpCtx = httpCtx;
    }

    // ── Connector management ────────────────────────────────────────────

    public async Task<FormConnectorDto?> GetConnectorAsync(int formId)
    {
        var c = await _db.FormConnectors.FirstOrDefaultAsync(x => x.FormId == formId);
        return c == null ? null : Map(c);
    }

    public async Task<FormConnectorDto> SaveConnectorAsync(int formId, SaveConnectorDto dto)
    {
        var existing = await _db.FormConnectors.FirstOrDefaultAsync(x => x.FormId == formId);

        if (existing == null)
        {
            existing = new FormConnector { FormId = formId };
            _db.FormConnectors.Add(existing);
        }

        existing.DatabaseName     = dto.DatabaseName.Trim();
        existing.SchemaName       = (dto.SchemaName?.Trim() is { Length: > 0 } s) ? s : "dbo";
        existing.TableName        = dto.TableName.Trim();
        existing.PrimaryKeyColumn = (dto.PrimaryKeyColumn?.Trim() is { Length: > 0 } pk) ? pk : "Id";
        existing.ExcludeColumns   = dto.ExcludeColumns;
        existing.DefaultFilter    = dto.DefaultFilter;
        existing.DefaultOrderBy   = dto.DefaultOrderBy;
        existing.ExternalConnectionId = dto.ExternalConnectionId;
        existing.IsActive         = true;
        existing.UpdatedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(existing);
    }

    public async Task<bool> DeleteConnectorAsync(int formId)
    {
        var c = await _db.FormConnectors.FirstOrDefaultAsync(x => x.FormId == formId);
        if (c == null) return false;
        _db.FormConnectors.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TestConnectionResult> TestConnectionAsync(int formId)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        try
        {
            await using var conn = OpenExternalConnection(connector);
            var cols = await GetColumnsInternalAsync(conn, connector);
            var count = await GetRowCountAsync(conn, connector);
            return new TestConnectionResult(
                true,
                $"Connected to [{connector.DatabaseName}].[{connector.SchemaName}].[{connector.TableName}] — {cols.Count} columns, {count} rows",
                cols, count);
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, ex.Message, null, null);
        }
    }

    // ── External CRUD ───────────────────────────────────────────────────

    public async Task<ExternalRecordPagedResult> GetRecordsAsync(
        int formId, int page, int pageSize, string? search, string? orderBy)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        var cols       = await GetColumnsInternalAsync(conn, connector);
        var excluded   = ParseExcluded(connector.ExcludeColumns);
        var selectCols = cols.Where(c => !excluded.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

        // Build WHERE
        var where = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(connector.DefaultFilter))
            where.Add(connector.DefaultFilter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Search across all varchar/nvarchar columns
            var searchCols = await GetSearchableColumnsAsync(conn, connector);
            if (searchCols.Count > 0)
            {
                var searchParts = searchCols
                    .Select((c, i) => $"CAST([{c}] AS NVARCHAR(MAX)) LIKE @search{i}");
                where.Add("(" + string.Join(" OR ", searchParts) + ")");
                for (int i = 0; i < searchCols.Count; i++)
                    parameters.Add(new SqlParameter($"@search{i}", $"%{search}%"));
            }
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        var orderClause = !string.IsNullOrWhiteSpace(orderBy)
            ? $"ORDER BY [{SanitizeIdentifier(orderBy)}]"
            : !string.IsNullOrWhiteSpace(connector.DefaultOrderBy)
                ? $"ORDER BY {connector.DefaultOrderBy}"
                : $"ORDER BY [{connector.PrimaryKeyColumn}] DESC";

        var fullTable = QualifiedTable(connector);
        var colList   = string.Join(", ", selectCols.Select(c => $"[{c}]"));

        // Total count
        var countSql = $"SELECT COUNT(*) FROM {fullTable} {whereClause}";
        await using var countCmd = new SqlCommand(countSql, conn);
        countCmd.Parameters.AddRange(parameters.Select(Clone).ToArray());
        var total = (int)await countCmd.ExecuteScalarAsync();

        // Paged data
        var offset  = (page - 1) * pageSize;
        var dataSql = $@"
            SELECT {colList}
            FROM {fullTable}
            {whereClause}
            {orderClause}
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        await using var dataCmd = new SqlCommand(dataSql, conn);
        dataCmd.Parameters.AddRange(parameters.Select(Clone).ToArray());

        var items = await ReadRowsAsync(dataCmd);
        return new ExternalRecordPagedResult(items, total, page, pageSize,
            connector.PrimaryKeyColumn, selectCols);
    }

    public async Task<Dictionary<string, object?>> GetRecordByIdAsync(int formId, object pkValue)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        var sql = $"SELECT * FROM {QualifiedTable(connector)} WHERE [{connector.PrimaryKeyColumn}] = @pk";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pk", pkValue);

        var rows = await ReadRowsAsync(cmd);
        return rows.FirstOrDefault() ?? new Dictionary<string, object?>();
    }

    public async Task<Dictionary<string, object?>> CreateRecordAsync(
        int formId, Dictionary<string, JsonElement> values)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        // Remove PK from insert if it's identity
        var insertValues = values
            .Where(kv => !kv.Key.Equals(connector.PrimaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cols   = string.Join(", ", insertValues.Select(kv => $"[{kv.Key}]"));
        var parms  = string.Join(", ", insertValues.Select((kv, i) => $"@p{i}"));
        var sql    = $"INSERT INTO {QualifiedTable(connector)} ({cols}) OUTPUT INSERTED.* VALUES ({parms})";

        await using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < insertValues.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", ConvertJsonValue(insertValues[i].Value));

        var rows = await ReadRowsAsync(cmd);
        return rows.FirstOrDefault() ?? new Dictionary<string, object?>();
    }

    public async Task<Dictionary<string, object?>> UpdateRecordAsync(
        int formId, object pkValue, Dictionary<string, JsonElement> values)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        var updateValues = values
            .Where(kv => !kv.Key.Equals(connector.PrimaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var setClauses = updateValues.Select((kv, i) => $"[{kv.Key}] = @p{i}");
        var sql = $@"
            UPDATE {QualifiedTable(connector)}
            SET {string.Join(", ", setClauses)}
            OUTPUT INSERTED.*
            WHERE [{connector.PrimaryKeyColumn}] = @pk";

        await using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < updateValues.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", ConvertJsonValue(updateValues[i].Value));
        cmd.Parameters.AddWithValue("@pk", pkValue);

        var rows = await ReadRowsAsync(cmd);
        return rows.FirstOrDefault() ?? new Dictionary<string, object?>();
    }

    public async Task<bool> DeleteRecordAsync(int formId, object pkValue)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        var sql = $"DELETE FROM {QualifiedTable(connector)} WHERE [{connector.PrimaryKeyColumn}] = @pk";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pk", pkValue);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> BulkDeleteAsync(int formId, List<object> pkValues)
    {
        if (!pkValues.Any()) return 0;
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);

        var parms = pkValues.Select((_, i) => $"@pk{i}").ToList();
        var sql   = $"DELETE FROM {QualifiedTable(connector)} WHERE [{connector.PrimaryKeyColumn}] IN ({string.Join(",", parms)})";

        await using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < pkValues.Count; i++)
            cmd.Parameters.AddWithValue($"@pk{i}", pkValues[i]);
        return await cmd.ExecuteNonQueryAsync();
    }

    // ── Meta ─────────────────────────────────────────────────────────────

    public async Task<List<string>> GetTableColumnsAsync(int formId)
    {
        var connector = await GetRequiredConnectorAsync(formId);
        await using var conn = OpenExternalConnection(connector);
        return await GetColumnsInternalAsync(conn, connector);
    }

    public async Task<List<string>> GetDatabasesAsync()
    {
        await using var conn = OpenDefaultConnection();
        var sql = "SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb') ORDER BY name";
        await using var cmd = new SqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    public async Task<List<string>> GetTablesAsync(string dbName, string schema = "dbo")
    {
        await using var conn = OpenDefaultConnection();
        conn.ChangeDatabase(dbName);
        var sql = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        await using var rdr = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    // ── Internals ─────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a connection for ext-records CRUD. Never falls back to DefaultConnection.
    /// Priority: X-Connection-Id header → connector's linked ExternalConnectionId.
    /// Throws if neither is available.
    /// </summary>
    private SqlConnection OpenExternalConnection(FormConnector connector)
    {
        // Priority 1: X-Connection-Id header overrides everything
        var headerConnId = _httpCtx.HttpContext?.Request.Headers["X-Connection-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerConnId) && int.TryParse(headerConnId, out var connId))
        {
            return _connSvc.OpenConnectionAsync(connId, connector.DatabaseName)
                .GetAwaiter().GetResult();
        }

        // Priority 2: connector's linked ExternalConnection
        if (connector.ExternalConnectionId.HasValue)
        {
            return _connSvc.OpenConnectionAsync(connector.ExternalConnectionId.Value, connector.DatabaseName)
                .GetAwaiter().GetResult();
        }

        throw new InvalidOperationException(
            $"No external connection configured for form connector {connector.FormId}. " +
            "Provide an X-Connection-Id header or link an ExternalConnection to the connector.");
    }

    /// <summary>
    /// Opens a connection for meta/browse endpoints (databases, tables).
    /// Falls back to DefaultConnection when no external connection is specified.
    /// </summary>
    private SqlConnection OpenDefaultConnection()
    {
        // Check if request carries an X-Connection-Id header
        var headerConnId = _httpCtx.HttpContext?.Request.Headers["X-Connection-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerConnId) && int.TryParse(headerConnId, out var connId))
        {
            return _connSvc.OpenConnectionAsync(connId, null)
                .GetAwaiter().GetResult();
        }

        // Fall back to DefaultConnection
        var baseConnStr = _config.GetConnectionString("DefaultConnection")!;
        var conn = new SqlConnection(baseConnStr);
        conn.Open();
        return conn;
    }

    private async Task<List<string>> GetColumnsInternalAsync(SqlConnection conn, FormConnector connector)
    {
        var sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_CATALOG = @db AND TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                    ORDER BY ORDINAL_POSITION";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db",     connector.DatabaseName);
        cmd.Parameters.AddWithValue("@schema", connector.SchemaName);
        cmd.Parameters.AddWithValue("@table",  connector.TableName);
        await using var rdr = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private async Task<List<string>> GetSearchableColumnsAsync(SqlConnection conn, FormConnector connector)
    {
        var sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_CATALOG = @db AND TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                      AND DATA_TYPE IN ('varchar','nvarchar','char','nchar','text','ntext')
                    ORDER BY ORDINAL_POSITION";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@db",     connector.DatabaseName);
        cmd.Parameters.AddWithValue("@schema", connector.SchemaName);
        cmd.Parameters.AddWithValue("@table",  connector.TableName);
        await using var rdr = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private static async Task<int> GetRowCountAsync(SqlConnection conn, FormConnector connector)
    {
        var sql = $"SELECT COUNT(*) FROM {QualifiedTable(connector)}";
        await using var cmd = new SqlCommand(sql, conn);
        return (int)await cmd.ExecuteScalarAsync();
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(SqlCommand cmd)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < rdr.FieldCount; i++)
                row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private static object ConvertJsonValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String  => el.GetString() ?? (object)DBNull.Value,
        JsonValueKind.Number  => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => DBNull.Value,
        _                     => el.ToString()
    };

    private static string QualifiedTable(FormConnector c)
        => $"[{c.DatabaseName}].[{c.SchemaName}].[{c.TableName}]";

    private static string SanitizeIdentifier(string input)
        => input.Replace("]", "").Replace("[", "").Trim();

    private static HashSet<string> ParseExcluded(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? new HashSet<string>()
            : csv.Split(',').Select(s => s.Trim()).ToHashSet();

    private static SqlParameter Clone(SqlParameter p)
        => new(p.ParameterName, p.Value);

    private async Task<FormConnector> GetRequiredConnectorAsync(int formId)
    {
        var c = await _db.FormConnectors
            .Include(x => x.ExternalConnection)
            .FirstOrDefaultAsync(x => x.FormId == formId && x.IsActive);
        if (c == null) throw new InvalidOperationException(
            $"No active connector configured for form {formId}.");
        return c;
    }

    private static FormConnectorDto Map(FormConnector c) => new(
        c.Id, c.FormId, c.ExternalConnectionId, c.DatabaseName, c.SchemaName, c.TableName,
        c.PrimaryKeyColumn, c.ExcludeColumns, c.DefaultFilter, c.DefaultOrderBy, c.IsActive);
}
