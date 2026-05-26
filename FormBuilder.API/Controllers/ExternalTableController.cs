using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FormBuilder.API.Controllers;

/// <summary>
/// Connector config endpoints
/// GET    /api/forms/{formId}/connector              → get config
/// POST   /api/forms/{formId}/connector              → save/update config
/// DELETE /api/forms/{formId}/connector              → remove connector
/// POST   /api/forms/{formId}/connector/test         → test connection
/// GET    /api/forms/{formId}/connector/columns      → list external table columns
///
/// External table CRUD endpoints (replaces FormRecords when connector is active)
/// GET    /api/forms/{formId}/ext-records            → paged list from external table
/// GET    /api/forms/{formId}/ext-records/{pk}       → single record
/// POST   /api/forms/{formId}/ext-records            → insert into external table
/// PUT    /api/forms/{formId}/ext-records/{pk}       → update in external table
/// DELETE /api/forms/{formId}/ext-records/{pk}       → delete from external table
/// DELETE /api/forms/{formId}/ext-records/bulk       → bulk delete
///
/// Meta endpoints
/// GET    /api/external/databases                    → list all databases on server
/// GET    /api/external/databases/{db}/tables        → list tables in a database
/// </summary>

// ── Connector config ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/forms/{formId:int}/connector")]
public class FormConnectorController : ControllerBase
{
    private readonly IExternalTableService _svc;
    public FormConnectorController(IExternalTableService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<FormConnectorDto>>> Get(int formId)
    {
        var result = await _svc.GetConnectorAsync(formId);
        return result == null
            ? Ok(new ApiResponse<FormConnectorDto>(true, "No connector configured", null))
            : Ok(new ApiResponse<FormConnectorDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormConnectorDto>>> Save(
        int formId, [FromBody] SaveConnectorDto dto)
    {
        // If body has no ExternalConnectionId, check the X-Connection-Id header
        if (!dto.ExternalConnectionId.HasValue)
        {
            var headerConnId = Request.Headers["X-Connection-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerConnId) && int.TryParse(headerConnId, out var connId))
            {
                dto = dto with { ExternalConnectionId = connId };
            }
        }

        var result = await _svc.SaveConnectorAsync(formId, dto);
        return Ok(new ApiResponse<FormConnectorDto>(true, "Connector saved", result));
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int formId)
    {
        var ok = await _svc.DeleteConnectorAsync(formId);
        return Ok(new ApiResponse<bool>(true, ok ? "Connector removed" : "Not found", ok));
    }

    [HttpPost("test")]
    public async Task<ActionResult<ApiResponse<TestConnectionResult>>> Test(int formId)
    {
        var result = await _svc.TestConnectionAsync(formId);
        return Ok(new ApiResponse<TestConnectionResult>(result.Success, result.Message, result));
    }

    [HttpGet("columns")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetColumns(int formId)
    {
        try
        {
            var cols = await _svc.GetTableColumnsAsync(formId);
            return Ok(new ApiResponse<List<string>>(true, "OK", cols));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }
}

// ── External table CRUD ────────────────────────────────────────────────────────

[ApiController]
[Route("api/forms/{formId:int}/ext-records")]
public class ExternalRecordsController : ControllerBase
{
    private readonly IExternalTableService _svc;
    public ExternalRecordsController(IExternalTableService svc) => _svc = svc;

    // GET api/forms/1/ext-records?page=1&pageSize=20&search=gopal&orderBy=CreatedAt
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ExternalRecordPagedResult>>> GetAll(
        int formId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? orderBy = null)
    {
        try
        {
            var result = await _svc.GetRecordsAsync(formId, page, pageSize, search, orderBy);
            return Ok(new ApiResponse<ExternalRecordPagedResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ExternalRecordPagedResult>(false, ex.Message, null, 400));
        }
    }

    // GET api/forms/1/ext-records/42
    [HttpGet("{pk}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> GetById(
        int formId, string pk)
    {
        try
        {
            var result = await _svc.GetRecordByIdAsync(formId, pk);
            return Ok(new ApiResponse<Dictionary<string, object?>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<Dictionary<string, object?>>(false, ex.Message, null, 400));
        }
    }

    // POST api/forms/1/ext-records   body: { "FirstName": "Gopal", "Email": "g@g.com" }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> Create(
        int formId, [FromBody] Dictionary<string, JsonElement> values)
    {
        try
        {
            var result = await _svc.CreateRecordAsync(formId, values);
            return Ok(new ApiResponse<Dictionary<string, object?>>(true, "Record created", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<Dictionary<string, object?>>(false, ex.Message, null, 400));
        }
    }

    // PUT api/forms/1/ext-records/42
    [HttpPut("{pk}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> Update(
        int formId, string pk, [FromBody] Dictionary<string, JsonElement> values)
    {
        try
        {
            var result = await _svc.UpdateRecordAsync(formId, pk, values);
            return Ok(new ApiResponse<Dictionary<string, object?>>(true, "Record updated", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<Dictionary<string, object?>>(false, ex.Message, null, 400));
        }
    }

    // DELETE api/forms/1/ext-records/42
    [HttpDelete("{pk}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int formId, string pk)
    {
        try
        {
            var ok = await _svc.DeleteRecordAsync(formId, pk);
            return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool>(false, ex.Message, false, 400));
        }
    }

    // DELETE api/forms/1/ext-records/bulk   body: ["1","2","3"]
    [HttpDelete("bulk")]
    public async Task<ActionResult<ApiResponse<int>>> BulkDelete(
        int formId, [FromBody] List<object> pkValues)
    {
        try
        {
            var count = await _svc.BulkDeleteAsync(formId, pkValues);
            return Ok(new ApiResponse<int>(true, $"{count} records deleted", count));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<int>(false, ex.Message, 0, 400));
        }
    }
}

// ── Meta endpoints ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/external")]
public class ExternalMetaController : ControllerBase
{
    private readonly IExternalTableService _svc;
    private readonly IExternalConnectionService _connSvc;

    public ExternalMetaController(IExternalTableService svc, IExternalConnectionService connSvc)
    {
        _svc = svc;
        _connSvc = connSvc;
    }

    // GET api/external/databases
    // Priority: X-Connection-Id header → X-Server-Name headers → default server
    [HttpGet("databases")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetDatabases()
    {
        try
        {
            List<string> result;

            if (TryGetConnectionId(out var connId))
                result = await _connSvc.GetDatabasesAsync(connId);
            else if (TryBuildConnectionDto(out var dto))
                result = await _connSvc.GetDatabasesAsync(dto);
            else
                result = await _svc.GetDatabasesAsync();

            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET api/external/databases/LumiereDB/tables?schema=dbo
    [HttpGet("databases/{dbName}/tables")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetTables(
        string dbName, [FromQuery] string schema = "dbo")
    {
        try
        {
            List<string> result;

            if (TryGetConnectionId(out var connId))
                result = await _connSvc.GetTablesAsync(connId, dbName, schema);
            else if (TryBuildConnectionDto(out var dto))
                result = await _connSvc.GetTablesAsync(dto, dbName, schema);
            else
                result = await _svc.GetTablesAsync(dbName, schema);

            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET api/external/databases/LumiereDB/tables/Users/columns?schema=dbo
    [HttpGet("databases/{dbName}/tables/{tableName}/columns")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetColumns(
        string dbName, string tableName, [FromQuery] string schema = "dbo")
    {
        try
        {
            List<string> result;

            if (TryGetConnectionId(out var connId))
                result = await _connSvc.GetColumnsAsync(connId, dbName, schema, tableName);
            else if (TryBuildConnectionDto(out var dto))
                result = await _connSvc.GetColumnsAsync(dto, dbName, schema, tableName);
            else
                return BadRequest(new ApiResponse<List<string>>(
                    false, "Provide X-Connection-Id or X-Server-Name header", null, 400));

            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    /// <summary>
    /// Reads a saved connection id from the X-Connection-Id header.
    /// </summary>
    private bool TryGetConnectionId(out int connectionId)
    {
        var raw = Request.Headers["X-Connection-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out connectionId))
            return true;

        connectionId = 0;
        return false;
    }

    /// <summary>
    /// Reads ad-hoc connection details from request headers:
    ///   X-Server-Name  (required)
    ///   X-Auth-Type    "Windows" | "SqlServer"  (default: Windows)
    ///   X-Username     (optional, for SqlServer auth)
    ///   X-Password     (optional, for SqlServer auth)
    ///   X-Database     (optional)
    /// </summary>
    private bool TryBuildConnectionDto(out TestExternalConnectionDto dto)
    {
        var serverName = Request.Headers["X-Server-Name"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(serverName))
        {
            dto = default!;
            return false;
        }

        var authType = Request.Headers["X-Auth-Type"].FirstOrDefault() ?? "Windows";
        var username = Request.Headers["X-Username"].FirstOrDefault();
        var password = Request.Headers["X-Password"].FirstOrDefault();
        var database = Request.Headers["X-Database"].FirstOrDefault();

        dto = new TestExternalConnectionDto(serverName, authType, username, password, database);
        return true;
    }
}
