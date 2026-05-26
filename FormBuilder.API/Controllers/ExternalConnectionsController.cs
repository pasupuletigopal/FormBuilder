using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

/// <summary>
/// Saved connections library
/// GET    /api/connections                      → list all
/// GET    /api/connections/{id}                 → get one
/// POST   /api/connections                      → create
/// PUT    /api/connections/{id}                 → update
/// DELETE /api/connections/{id}                 → soft delete
/// POST   /api/connections/{id}/test            → test by id
/// POST   /api/connections/test                 → test without saving
/// GET    /api/connections/{id}/databases       → list databases on server
/// GET    /api/connections/{id}/databases/{db}/tables          → list tables
/// GET    /api/connections/{id}/databases/{db}/tables/{t}/columns → list columns
/// </summary>
[ApiController]
[Route("api/connections")]
public class ExternalConnectionsController : ControllerBase
{
    private readonly IExternalConnectionService _svc;
    public ExternalConnectionsController(IExternalConnectionService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExternalConnectionDto>>>> GetAll()
    {
        var result = await _svc.GetAllAsync();
        return Ok(new ApiResponse<List<ExternalConnectionDto>>(true, "OK", result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ExternalConnectionDto>>> GetById(int id)
    {
        var result = await _svc.GetByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<ExternalConnectionDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ExternalConnectionDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExternalConnectionDto>>> Create(
        [FromBody] SaveExternalConnectionDto dto)
    {
        var result = await _svc.SaveAsync(null, dto);
        return Ok(new ApiResponse<ExternalConnectionDto>(true, "Connection saved", result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ExternalConnectionDto>>> Update(
        int id, [FromBody] SaveExternalConnectionDto dto)
    {
        try
        {
            var result = await _svc.SaveAsync(id, dto);
            return Ok(new ApiResponse<ExternalConnectionDto>(true, "Connection updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ExternalConnectionDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // POST /api/connections/{id}/test
    [HttpPost("{id:int}/test")]
    public async Task<ActionResult<ApiResponse<TestConnectionResult>>> TestById(
        int id, [FromQuery] string? database = null)
    {
        var result = await _svc.TestByIdAsync(id, database);
        return Ok(new ApiResponse<TestConnectionResult>(result.Success, result.Message, result));
    }

    // POST /api/connections/test  (test without saving)
    [HttpPost("test")]
    public async Task<ActionResult<ApiResponse<TestConnectionResult>>> TestDirect(
        [FromBody] TestExternalConnectionDto dto)
    {
        var result = await _svc.TestAsync(dto);
        return Ok(new ApiResponse<TestConnectionResult>(result.Success, result.Message, result));
    }

    // GET /api/connections/{id}/databases
    [HttpGet("{id:int}/databases")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetDatabases(int id)
    {
        try
        {
            var result = await _svc.GetDatabasesAsync(id);
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET /api/connections/{id}/databases/{db}/tables?schema=dbo
    [HttpGet("{id:int}/databases/{db}/tables")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetTables(
        int id, string db, [FromQuery] string schema = "dbo")
    {
        try
        {
            var result = await _svc.GetTablesAsync(id, db, schema);
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET /api/connections/{id}/databases/{db}/tables/{table}/columns?schema=dbo
    [HttpGet("{id:int}/databases/{db}/tables/{table}/columns")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetColumns(
        int id, string db, string table, [FromQuery] string schema = "dbo")
    {
        try
        {
            var result = await _svc.GetColumnsAsync(id, db, schema, table);
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }
}
