using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

// ============================================================
// Master Data Controllers
// ============================================================

[ApiController]
[Route("api/[controller]")]
public class ControlTypesController : ControllerBase
{
    private readonly IMasterDataService _svc;
    public ControlTypesController(IMasterDataService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ControlTypeDto>>>> GetAll() =>
        Ok(new ApiResponse<List<ControlTypeDto>>(true, "OK", await _svc.GetControlTypesAsync()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ControlTypeDto>>> GetById(int id)
    {
        var result = await _svc.GetControlTypeByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<ControlTypeDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ControlTypeDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ControlTypeDto>>> Create([FromBody] CreateControlTypeDto dto)
    {
        var result = await _svc.CreateControlTypeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            new ApiResponse<ControlTypeDto>(true, "Created", result, 201));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ControlTypeDto>>> Update(int id, [FromBody] UpdateControlTypeDto dto)
    {
        var result = await _svc.UpdateControlTypeAsync(id, dto);
        return result == null
            ? NotFound(new ApiResponse<ControlTypeDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ControlTypeDto>(true, "Updated", result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteControlTypeAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Deleted", true))
            : NotFound(new ApiResponse<bool>(false, "Not found", false, 404));
    }
}

[ApiController]
[Route("api/[controller]")]
public class DataTypesController : ControllerBase
{
    private readonly IMasterDataService _svc;
    public DataTypesController(IMasterDataService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DataTypeDto>>>> GetAll() =>
        Ok(new ApiResponse<List<DataTypeDto>>(true, "OK", await _svc.GetDataTypesAsync()));
}

[ApiController]
[Route("api/[controller]")]
public class DataSourcesController : ControllerBase
{
    private readonly IMasterDataService _svc;
    public DataSourcesController(IMasterDataService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DataSourceDto>>>> GetAll() =>
        Ok(new ApiResponse<List<DataSourceDto>>(true, "OK", await _svc.GetDataSourcesAsync()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> GetById(int id)
    {
        var result = await _svc.GetDataSourceByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<DataSourceDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<DataSourceDto>(true, "OK", result));
    }

    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<ApiResponse<List<DataSourceItemDto>>>> GetItems(int id)
    {
        try
        {
            var result = await _svc.GetDataSourceItemsAsync(id);
            return Ok(new ApiResponse<List<DataSourceItemDto>>(true, "OK", result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<List<DataSourceItemDto>>(false, ex.Message, null, 400));
        }
    }

    [HttpPost("preview-api")]
    public async Task<ActionResult<ApiResponse<List<DataSourceItemDto>>>> PreviewApi(
        [FromBody] PreviewApiDataSourceRequest req)
    {
        try
        {
            var result = await _svc.PreviewApiDataSourceAsync(req);
            return Ok(new ApiResponse<List<DataSourceItemDto>>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<List<DataSourceItemDto>>(false, ex.Message, null, 400));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> Create([FromBody] CreateDataSourceDto dto)
    {
        var result = await _svc.CreateDataSourceAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            new ApiResponse<DataSourceDto>(true, "Created", result, 201));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<DataSourceDto>>> Update(int id, [FromBody] UpdateDataSourceDto dto)
    {
        var result = await _svc.UpdateDataSourceAsync(id, dto);
        return result == null
            ? NotFound(new ApiResponse<DataSourceDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<DataSourceDto>(true, "Updated", result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteDataSourceAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Deleted", true))
            : NotFound(new ApiResponse<bool>(false, "Not found", false, 404));
    }
}

[ApiController]
[Route("api/[controller]")]
public class ValidationRulesController : ControllerBase
{
    private readonly IMasterDataService _svc;
    public ValidationRulesController(IMasterDataService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ValidationRuleDto>>>> GetAll() =>
        Ok(new ApiResponse<List<ValidationRuleDto>>(true, "OK", await _svc.GetValidationRulesAsync()));
}

// ============================================================
// Forms Controller
// ============================================================
[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly IFormService _svc;
    public FormsController(IFormService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FormSummaryDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _svc.GetFormsAsync(page, pageSize, search);
        return Ok(new ApiResponse<PagedResult<FormSummaryDto>>(true, "OK", result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FormDto>>> GetById(int id)
    {
        var result = await _svc.GetFormByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<FormDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<FormDto>(true, "OK", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormDto>>> Create([FromBody] CreateFormDto dto)
    {
        var result = await _svc.CreateFormAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            new ApiResponse<FormDto>(true, "Created", result, 201));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<FormDto>>> Update(int id, [FromBody] UpdateFormDto dto)
    {
        var result = await _svc.UpdateFormAsync(id, dto);
        return result == null
            ? NotFound(new ApiResponse<FormDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<FormDto>(true, "Updated", result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var ok = await _svc.DeleteFormAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Deleted", true))
            : NotFound(new ApiResponse<bool>(false, "Not found", false, 404));
    }

    [HttpPut("{id:int}/controls")]
    public async Task<ActionResult<ApiResponse<FormDto>>> UpdateControls(int id, [FromBody] UpdateFormControlsDto dto)
    {
        var result = await _svc.UpdateFormControlsAsync(id, dto);
        return result == null
            ? NotFound(new ApiResponse<FormDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<FormDto>(true, "Controls updated", result));
    }

    [HttpPost("{id:int}/clone")]
    public async Task<ActionResult<ApiResponse<int>>> Clone(int id, [FromBody] string newName)
    {
        var newId = await _svc.CloneFormAsync(id, newName);
        return newId == null
            ? NotFound(new ApiResponse<int>(false, "Not found", 0, 404))
            : Ok(new ApiResponse<int>(true, "Cloned", newId.Value));
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ApiResponse<string>>> Submit(int id, [FromBody] object data)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var dto = new FormSubmissionDto(id, data.ToString() ?? "{}", null);
        var result = await _svc.SubmitFormAsync(dto, ip);
        return result == null
            ? NotFound(new ApiResponse<string>(false, "Form not found", null, 404))
            : Ok(new ApiResponse<string>(true, "Submitted", result.Id.ToString()));
    }

    [HttpGet("{id:int}/submissions")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetSubmissions(int id)
    {
        var submissions = await _svc.GetSubmissionsAsync(id);
        var result = submissions.Select(s => new {
            s.Id, s.FormId, s.FormVersion, s.Data, s.SubmittedBy, s.CreatedAt
        }).Cast<object>().ToList();
        return Ok(new ApiResponse<List<object>>(true, "OK", result));
    }
}
