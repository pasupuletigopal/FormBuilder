// New file: Backend/FormBuilder.API/Controllers/AnalyticsController.cs

using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

// ── Dashboards ──────────────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboards")]
public class DashboardsController : ControllerBase
{
    private readonly IAnalyticsService _svc;
    public DashboardsController(IAnalyticsService svc) => _svc = svc;


    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DashboardDto>>>> GetAll()
    {
        var result = await _svc.GetDashboardsAsync();
        return Ok(new ApiResponse<List<DashboardDto>>(true, "OK", result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetById(int id)
    {
        var result = await _svc.GetDashboardByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<DashboardDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<DashboardDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Create(
        [FromBody] SaveDashboardDto dto)
    {
        var result = await _svc.SaveDashboardAsync(null, dto);
        return Ok(new ApiResponse<DashboardDto>(true, "Dashboard created", result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Update(
        int id, [FromBody] SaveDashboardDto dto)
    {
        try
        {
            var result = await _svc.SaveDashboardAsync(id, dto);
            return Ok(new ApiResponse<DashboardDto>(true, "Dashboard updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<DashboardDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteDashboardAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }
}

// ── Widgets ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboards/{dashboardId:int}/widgets")]
public class DashboardWidgetsController : ControllerBase
{
    private readonly IAnalyticsService _svc;
    public DashboardWidgetsController(IAnalyticsService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DashboardWidgetDto>>>> GetAll(int dashboardId)
    {
        var result = await _svc.GetWidgetsAsync(dashboardId);
        return Ok(new ApiResponse<List<DashboardWidgetDto>>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> Create(
        int dashboardId, [FromBody] SaveWidgetDto dto)
    {
        var result = await _svc.SaveWidgetAsync(dashboardId, null, dto);
        return Ok(new ApiResponse<DashboardWidgetDto>(true, "Widget created", result));
    }

    [HttpPut("{widgetId:int}")]
    public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> Update(
        int dashboardId, int widgetId, [FromBody] SaveWidgetDto dto)
    {
        try
        {
            var result = await _svc.SaveWidgetAsync(dashboardId, widgetId, dto);
            return Ok(new ApiResponse<DashboardWidgetDto>(true, "Widget updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<DashboardWidgetDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("{widgetId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int dashboardId, int widgetId)
    {
        var ok = await _svc.DeleteWidgetAsync(widgetId);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // PATCH /api/dashboards/{id}/widgets/positions
    [HttpPatch("positions")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePositions(
        int dashboardId, [FromBody] UpdateWidgetPositionsDto dto)
    {
        await _svc.UpdateWidgetPositionsAsync(dto);
        return Ok(new ApiResponse<bool>(true, "Positions updated", true));
    }
}

// ── Data ─────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboards/{dashboardId:int}/data")]
public class DashboardDataController : ControllerBase
{
    private readonly IAnalyticsService _svc;
    public DashboardDataController(IAnalyticsService svc) => _svc = svc;

    // POST api/dashboards/1/data/kpi
    [HttpPost("kpi")]
    public async Task<ActionResult<ApiResponse<KpiResult>>> GetKpi(
        int dashboardId, [FromBody] WidgetDataRequest req)
    {
        try
        {
            var result = await _svc.GetKpiDataAsync(req with { DashboardId = dashboardId });
            return Ok(new ApiResponse<KpiResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<KpiResult>(false, ex.Message, null, 400));
        }
    }

    // POST api/dashboards/1/data/chart
    [HttpPost("chart")]
    public async Task<ActionResult<ApiResponse<ChartResult>>> GetChart(
        int dashboardId, [FromBody] WidgetDataRequest req)
    {
        try
        {
            var result = await _svc.GetChartDataAsync(req with { DashboardId = dashboardId });
            return Ok(new ApiResponse<ChartResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ChartResult>(false, ex.Message, null, 400));
        }
    }

    // POST api/dashboards/1/data/table?page=1&pageSize=10
    [HttpPost("table")]
    public async Task<ActionResult<ApiResponse<TableResult>>> GetTable(
        int dashboardId, [FromBody] WidgetDataRequest req,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _svc.GetTableDataAsync(
                req with { DashboardId = dashboardId }, page, pageSize);
            return Ok(new ApiResponse<TableResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<TableResult>(false, ex.Message, null, 400));
        }
    }

    // POST api/dashboards/1/data/drilldown?label=Electronics
    [HttpPost("drilldown")]
    public async Task<ActionResult<ApiResponse<DrillDownResult>>> DrillDown(
        int dashboardId, [FromBody] WidgetDataRequest req,
        [FromQuery] string label = "")
    {
        try
        {
            var result = await _svc.GetDrillDownAsync(
                req with { DashboardId = dashboardId }, label);
            return Ok(new ApiResponse<DrillDownResult>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<DrillDownResult>(false, ex.Message, null, 400));
        }
    }
}

// ── Meta ─────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/analytics/meta")]
public class AnalyticsMetaController : ControllerBase
{
    private readonly IAnalyticsService _svc;
    public AnalyticsMetaController(IAnalyticsService svc) => _svc = svc;

    // GET api/analytics/meta/columns?connId=1&db=LumiDB&schema=dbo&table=Sales&type=all
    [HttpGet("columns")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetColumns(
        [FromQuery] int connId,
        [FromQuery] string db,
        [FromQuery] string schema = "dbo",
        [FromQuery] string table = "",
        [FromQuery] string type = "all")
    {
        try
        {
            var result = type switch
            {
                "numeric" => await _svc.GetNumericColumnsAsync(connId, db, schema, table),
                "date"    => await _svc.GetDateColumnsAsync(connId, db, schema, table),
                _         => await _svc.GetAllColumnsAsync(connId, db, schema, table)
            };
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }

    // GET api/analytics/meta/distinct?connId=1&db=LumiDB&schema=dbo&table=Sales&column=Category
    [HttpGet("distinct")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetDistinct(
        [FromQuery] int connId,
        [FromQuery] string db,
        [FromQuery] string schema = "dbo",
        [FromQuery] string table = "",
        [FromQuery] string column = "")
    {
        try
        {
            var result = await _svc.GetDistinctValuesAsync(connId, db, schema, table, column);
            return Ok(new ApiResponse<List<string>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<string>>(false, ex.Message, null, 400));
        }
    }
}
