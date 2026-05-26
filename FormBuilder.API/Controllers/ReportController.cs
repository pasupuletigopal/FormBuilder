using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService       _svc;
    private readonly IReportExportService _exportSvc;

    public ReportsController(IReportService svc, IReportExportService exportSvc)
    {
        _svc       = svc;
        _exportSvc = exportSvc;
    }

    // GET /api/reports
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReportDto>>>> GetAll()
    {
        var result = await _svc.GetReportsAsync();
        return Ok(new ApiResponse<List<ReportDto>>(true, "OK", result));
    }

    // GET /api/reports/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(int id)
    {
        var result = await _svc.GetReportByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<ReportDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ReportDto>(true, "OK", result));
    }

    // POST /api/reports
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create(
        [FromBody] SaveReportDto dto)
    {
        var result = await _svc.SaveReportAsync(null, dto);
        return Ok(new ApiResponse<ReportDto>(true, "Report saved", result));
    }

    // PUT /api/reports/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Update(
        int id, [FromBody] SaveReportDto dto)
    {
        try
        {
            var result = await _svc.SaveReportAsync(id, dto);
            return Ok(new ApiResponse<ReportDto>(true, "Report updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ReportDto>(false, ex.Message, null, 404));
        }
    }

    // DELETE /api/reports/{id}
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteReportAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // POST /api/reports/{id}/run
    [HttpPost("{id:int}/run")]
    public async Task<ActionResult<ApiResponse<RunReportResult>>> Run(int id)
    {
        try
        {
            var result = await _svc.RunReportAsync(id);
            return Ok(new ApiResponse<RunReportResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RunReportResult>(false, ex.Message, null, 400));
        }
    }

    // POST /api/reports/preview
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<RunReportResult>>> Preview(
        [FromBody] SaveReportDto dto)
    {
        try
        {
            var result = await _svc.PreviewReportAsync(dto);
            return Ok(new ApiResponse<RunReportResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RunReportResult>(false, ex.Message, null, 400));
        }
    }

    // POST /api/reports/generate-sql
    [HttpPost("generate-sql")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateSql(
        [FromBody] SaveReportDto dto)
    {
        try
        {
            var sql = await _svc.GenerateSqlAsync(dto);
            return Ok(new ApiResponse<string>(true, "OK", sql));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(false, ex.Message, null, 400));
        }
    }

    // GET /api/reports/{id}/history
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<List<ReportRunHistoryDto>>>> GetHistory(int id)
    {
        var result = await _svc.GetRunHistoryAsync(id);
        return Ok(new ApiResponse<List<ReportRunHistoryDto>>(true, "OK", result));
    }

    // ---- Export ----

    // GET /api/reports/{id}/export/excel
    [HttpGet("{id:int}/export/excel")]
    public async Task<IActionResult> ExportExcel(int id)
    {
        try
        {
            var report = await _svc.GetReportByIdAsync(id)
                ?? throw new KeyNotFoundException($"Report {id} not found.");
            var result = await _svc.RunReportAsync(id);
            var bytes  = _exportSvc.ExportToExcel(result, report.Name);
            var name   = $"{report.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/reports/{id}/export/csv
    [HttpGet("{id:int}/export/csv")]
    public async Task<IActionResult> ExportCsv(int id)
    {
        try
        {
            var report = await _svc.GetReportByIdAsync(id)
                ?? throw new KeyNotFoundException($"Report {id} not found.");
            var result = await _svc.RunReportAsync(id);
            var bytes  = _exportSvc.ExportToCsv(result);
            var name   = $"{report.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", name);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/reports/{id}/export/excel (from preview data, no re-run)
    [HttpPost("{id:int}/export/excel")]
    public async Task<IActionResult> ExportExcelFromData(
        int id, [FromBody] RunReportResult data)
    {
        var report = await _svc.GetReportByIdAsync(id);
        var bytes  = _exportSvc.ExportToExcel(data, report?.Name ?? "Report");
        var name   = $"Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    // ---- Schedule ----

    // PUT /api/reports/{id}/schedule
    [HttpPut("{id:int}/schedule")]
    public async Task<ActionResult<ApiResponse<ReportScheduleDto>>> SaveSchedule(
        int id, [FromBody] SaveScheduleDto dto)
    {
        var result = await _svc.SaveScheduleAsync(id, dto);
        return Ok(new ApiResponse<ReportScheduleDto>(true, "Schedule saved", result));
    }

    // DELETE /api/reports/{id}/schedule
    [HttpDelete("{id:int}/schedule")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSchedule(int id)
    {
        var ok = await _svc.DeleteScheduleAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // ---- Meta: SPs and Views ----

    // GET /api/reports/meta/sp-and-views?connId=1&db=MyDB&schema=dbo
    [HttpGet("meta/sp-and-views")]
    public async Task<ActionResult<ApiResponse<List<SpOrViewInfo>>>> GetSpAndViews(
        [FromQuery] int connId, [FromQuery] string db, [FromQuery] string schema = "dbo")
    {
        try
        {
            var result = await _svc.GetStoredProceduresAndViewsAsync(connId, db, schema);
            return Ok(new ApiResponse<List<SpOrViewInfo>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<SpOrViewInfo>>(false, ex.Message, null, 400));
        }
    }

    // GET /api/reports/meta/sp-parameters?connId=1&db=MyDB&schema=dbo&spName=GetOrders
    [HttpGet("meta/sp-parameters")]
    public async Task<ActionResult<ApiResponse<List<SpParameterInfo>>>> GetSpParameters(
        [FromQuery] int connId, [FromQuery] string db,
        [FromQuery] string schema, [FromQuery] string spName)
    {
        try
        {
            var result = await _svc.GetSpParametersAsync(connId, db, schema, spName);
            return Ok(new ApiResponse<List<SpParameterInfo>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<SpParameterInfo>>(false, ex.Message, null, 400));
        }
    }

    // POST /api/reports/execute-sp
    [HttpPost("execute-sp")]
    public async Task<ActionResult<ApiResponse<RunReportResult>>> ExecuteSp(
        [FromBody] ExecuteSpRequest req)
    {
        try
        {
            var result = await _svc.ExecuteSpOrViewAsync(req);
            return Ok(new ApiResponse<RunReportResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RunReportResult>(false, ex.Message, null, 400));
        }
    }
}

// ---- Meta endpoints ----

[ApiController]
[Route("api/reports/meta")]
public class ReportMetaController : ControllerBase
{
    private readonly IReportService _svc;
    public ReportMetaController(IReportService svc) => _svc = svc;

    // GET /api/reports/meta/tables?connId=1&db=LumiDB&schema=dbo
    [HttpGet("tables")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetTables(
        [FromQuery] int connId,
        [FromQuery] string db,
        [FromQuery] string schema = "dbo")
    {
        try
        {
            var result = await _svc.GetTablesAsync(connId, db, schema);
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET /api/reports/meta/columns?connId=1&db=LumiDB&schema=dbo&table=Orders
    [HttpGet("columns")]
    public async Task<ActionResult<ApiResponse<List<TableColumnInfo>>>> GetColumns(
        [FromQuery] int connId,
        [FromQuery] string db,
        [FromQuery] string schema = "dbo",
        [FromQuery] string table  = "")
    {
        try
        {
            var result = await _svc.GetTableColumnsAsync(connId, db, schema, table);
            return Ok(new ApiResponse<List<TableColumnInfo>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<TableColumnInfo>>(false, ex.Message, null, 400));
        }
    }
}
