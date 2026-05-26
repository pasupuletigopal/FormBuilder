using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

[ApiController]
[Route("api/sp-reports")]
public class SpReportsController : ControllerBase
{
    private readonly ISpReportService     _svc;
    private readonly IReportExportService _exportSvc;

    public SpReportsController(ISpReportService svc, IReportExportService exportSvc)
    {
        _svc       = svc;
        _exportSvc = exportSvc;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SpReportDto>>>> GetAll()
    {
        var result = await _svc.GetAllAsync();
        return Ok(new ApiResponse<List<SpReportDto>>(true, "OK", result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SpReportDto>>> GetById(int id)
    {
        var result = await _svc.GetByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<SpReportDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<SpReportDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SpReportDto>>> Create(
        [FromBody] SaveSpReportDto dto)
    {
        var result = await _svc.SaveAsync(null, dto);
        return Ok(new ApiResponse<SpReportDto>(true, "Report saved", result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SpReportDto>>> Update(
        int id, [FromBody] SaveSpReportDto dto)
    {
        try
        {
            var result = await _svc.SaveAsync(id, dto);
            return Ok(new ApiResponse<SpReportDto>(true, "Report updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<SpReportDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // POST /api/sp-reports/{id}/run
    [HttpPost("{id:int}/run")]
    public async Task<ActionResult<ApiResponse<RunReportResult>>> Run(int id)
    {
        try
        {
            var result = await _svc.RunAsync(id);
            return Ok(new ApiResponse<RunReportResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<RunReportResult>(false, ex.Message, null, 400));
        }
    }

    // GET /api/sp-reports/{id}/export/excel
    [HttpGet("{id:int}/export/excel")]
    public async Task<IActionResult> ExportExcel(int id)
    {
        try
        {
            var report = await _svc.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SpReport {id} not found.");
            var result = await _svc.RunAsync(id);
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

    // GET /api/sp-reports/{id}/export/csv
    [HttpGet("{id:int}/export/csv")]
    public async Task<IActionResult> ExportCsv(int id)
    {
        try
        {
            var report = await _svc.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SpReport {id} not found.");
            var result = await _svc.RunAsync(id);
            var bytes  = _exportSvc.ExportToCsv(result);
            var name   = $"{report.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", name);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
