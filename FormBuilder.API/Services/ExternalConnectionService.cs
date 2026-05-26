using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.API.Services;

public interface IExternalConnectionService
{
    // Library CRUD
    Task<List<ExternalConnectionDto>> GetAllAsync();
    Task<ExternalConnectionDto?> GetByIdAsync(int id);
    Task<ExternalConnectionDto> SaveAsync(int? id, SaveExternalConnectionDto dto);
    Task<bool> DeleteAsync(int id);

    // Testing
    Task<TestConnectionResult> TestAsync(TestExternalConnectionDto dto);
    Task<TestConnectionResult> TestByIdAsync(int id, string? databaseName = null);

    // Meta — browse server (by saved connection id)
    Task<List<string>> GetDatabasesAsync(int connectionId);
    Task<List<string>> GetTablesAsync(int connectionId, string databaseName, string schema = "dbo");
    Task<List<string>> GetColumnsAsync(int connectionId, string databaseName, string schema, string tableName);

    // Meta — browse server (by ad-hoc connection details)
    Task<List<string>> GetDatabasesAsync(TestExternalConnectionDto dto);
    Task<List<string>> GetTablesAsync(TestExternalConnectionDto dto, string databaseName, string schema = "dbo");
    Task<List<string>> GetColumnsAsync(TestExternalConnectionDto dto, string databaseName, string schema, string tableName);

    // Used by ExternalTableService
    Task<SqlConnection> OpenConnectionAsync(int connectionId, string? databaseName = null);
    Task<SqlConnection> OpenConnectionFromDtoAsync(TestExternalConnectionDto dto);
}

public class ExternalConnectionService : IExternalConnectionService
{
    private readonly FormBuilderDbContext _db;
    private readonly IDataProtector _protector;

    private const string Purpose = "FormBuilder.ExternalConnections.v1";

    public ExternalConnectionService(
        FormBuilderDbContext db,
        IDataProtectionProvider dpProvider)
    {
        _db       = db;
        _protector = dpProvider.CreateProtector(Purpose);
    }

    // ── Library CRUD ────────────────────────────────────────────────────

    public async Task<List<ExternalConnectionDto>> GetAllAsync()
    {
        var conns = await _db.ExternalConnections
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var formCounts = await _db.FormConnectors
            .Where(fc => fc.ExternalConnectionId != null)
            .GroupBy(fc => fc.ExternalConnectionId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return conns.Select(c => MapDto(c, formCounts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<ExternalConnectionDto?> GetByIdAsync(int id)
    {
        var c = await _db.ExternalConnections.FindAsync(id);
        return c == null ? null : MapDto(c, 0);
    }

    public async Task<ExternalConnectionDto> SaveAsync(int? id, SaveExternalConnectionDto dto)
    {
        ExternalConnection conn;

        if (id.HasValue)
        {
            conn = await _db.ExternalConnections.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Connection {id} not found.");
        }
        else
        {
            conn = new ExternalConnection();
            _db.ExternalConnections.Add(conn);
        }

        conn.Name        = dto.Name.Trim();
        conn.ServerName  = dto.ServerName.Trim();
        conn.AuthType    = dto.AuthType;
        conn.Description = dto.Description;
        conn.IsActive    = true;
        conn.UpdatedAt   = DateTime.UtcNow;

        // Only update credentials if provided
        if (string.Equals(dto.AuthType, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(dto.Username))
                conn.Username = _protector.Protect(dto.Username);

            if (!string.IsNullOrWhiteSpace(dto.Password))
                conn.Password = _protector.Protect(dto.Password);
        }
        else
        {
            // Windows auth — clear any stored credentials
            conn.Username = null;
            conn.Password = null;
        }

        await _db.SaveChangesAsync();
        return MapDto(conn, 0);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var conn = await _db.ExternalConnections.FindAsync(id);
        if (conn == null) return false;
        conn.IsActive  = false;
        conn.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Testing ──────────────────────────────────────────────────────────

    public async Task<TestConnectionResult> TestAsync(TestExternalConnectionDto dto)
    {
        try
        {
            await using var conn = await OpenConnectionFromDtoAsync(dto);
            var dbName = conn.Database;
            var sql    = "SELECT @@SERVERNAME, @@VERSION";
            await using var cmd = new SqlCommand(sql, conn);
            await using var rdr = await cmd.ExecuteReaderAsync();
            await rdr.ReadAsync();
            var serverName = rdr.GetString(0);
            rdr.Close();

            var message = $"✓ Connected to {serverName}" +
                (dto.DatabaseName != null ? $" → {dto.DatabaseName}" : "");

            return new TestConnectionResult(true, message, null, null);
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, ex.Message, null, null);
        }
    }

    public async Task<TestConnectionResult> TestByIdAsync(int id, string? databaseName = null)
    {
        var conn = await _db.ExternalConnections.FindAsync(id);
        if (conn == null)
            return new TestConnectionResult(false, "Connection not found.", null, null);

        var dto = new TestExternalConnectionDto(
            conn.ServerName,
            conn.AuthType,
            conn.Username != null ? _protector.Unprotect(conn.Username) : null,
            conn.Password != null ? _protector.Unprotect(conn.Password) : null,
            databaseName
        );

        var result = await TestAsync(dto);

        // Update last tested status
        conn.LastTestedAt = DateTime.UtcNow;
        conn.LastTestOk   = result.Success;
        await _db.SaveChangesAsync();

        return result;
    }

    // ── Meta ─────────────────────────────────────────────────────────────

    public async Task<List<string>> GetDatabasesAsync(int connectionId)
    {
        await using var conn = await OpenConnectionAsync(connectionId);
        var sql = @"SELECT name FROM sys.databases
                    WHERE name NOT IN ('master','tempdb','model','msdb')
                      AND state_desc = 'ONLINE'
                    ORDER BY name";
        return await ReadStringListAsync(conn, sql);
    }

    public async Task<List<string>> GetTablesAsync(
        int connectionId, string databaseName, string schema = "dbo")
    {
        await using var conn = await OpenConnectionAsync(connectionId, databaseName);
        var sql = @"SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        return await ReadStringListAsync(cmd);
    }

    public async Task<List<string>> GetColumnsAsync(
        int connectionId, string databaseName, string schema, string tableName)
    {
        await using var conn = await OpenConnectionAsync(connectionId, databaseName);
        var sql = @"SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                    ORDER BY ORDINAL_POSITION";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table",  tableName);
        return await ReadStringListAsync(cmd);
    }

    // ── Ad-hoc connection overloads (from header-supplied details) ─────────

    public async Task<List<string>> GetDatabasesAsync(TestExternalConnectionDto dto)
    {
        await using var conn = await OpenConnectionFromDtoAsync(dto);
        var sql = @"SELECT name FROM sys.databases
                    WHERE name NOT IN ('master','tempdb','model','msdb')
                      AND state_desc = 'ONLINE'
                    ORDER BY name";
        return await ReadStringListAsync(conn, sql);
    }

    public async Task<List<string>> GetTablesAsync(
        TestExternalConnectionDto dto, string databaseName, string schema = "dbo")
    {
        var withDb = dto with { DatabaseName = databaseName };
        await using var conn = await OpenConnectionFromDtoAsync(withDb);
        var sql = @"SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        return await ReadStringListAsync(cmd);
    }

    public async Task<List<string>> GetColumnsAsync(
        TestExternalConnectionDto dto, string databaseName, string schema, string tableName)
    {
        var withDb = dto with { DatabaseName = databaseName };
        await using var conn = await OpenConnectionFromDtoAsync(withDb);
        var sql = @"SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                    ORDER BY ORDINAL_POSITION";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table",  tableName);
        return await ReadStringListAsync(cmd);
    }

    // ── Connection factory ────────────────────────────────────────────────

    public async Task<SqlConnection> OpenConnectionAsync(
        int connectionId, string? databaseName = null)
    {
        var c = await _db.ExternalConnections.FindAsync(connectionId)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found.");

        var dto = new TestExternalConnectionDto(
            c.ServerName,
            c.AuthType,
            c.Username != null ? _protector.Unprotect(c.Username) : null,
            c.Password != null ? _protector.Unprotect(c.Password) : null,
            databaseName
        );

        return await OpenConnectionFromDtoAsync(dto);
    }

    public async Task<SqlConnection> OpenConnectionFromDtoAsync(TestExternalConnectionDto dto)
    {
        var connStr = BuildConnectionString(dto);
        var conn    = new SqlConnection(connStr);
        await conn.OpenAsync();
        return conn;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public static string BuildConnectionString(TestExternalConnectionDto dto)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource             = dto.ServerName,
            TrustServerCertificate = true,
            ConnectTimeout         = 10
        };

        if (!string.IsNullOrWhiteSpace(dto.DatabaseName))
            builder.InitialCatalog = dto.DatabaseName;

        if (string.Equals(dto.AuthType, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            builder.IntegratedSecurity = false;
            builder.UserID             = dto.Username ?? "";
            builder.Password           = dto.Password ?? "";
        }
        else // Windows
        {
            builder.IntegratedSecurity = true;
        }

        return builder.ConnectionString;
    }

    private static async Task<List<string>> ReadStringListAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        return await ReadStringListAsync(cmd);
    }

    private static async Task<List<string>> ReadStringListAsync(SqlCommand cmd)
    {
        var list = new List<string>();
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private static ExternalConnectionDto MapDto(ExternalConnection c, int formsCount) => new(
        c.Id, c.Name, c.ServerName, c.AuthType,
        c.Username != null ? "••••••" : null,  // never expose actual username
        c.Description, c.IsActive,
        c.LastTestedAt, c.LastTestOk, formsCount
    );
}
