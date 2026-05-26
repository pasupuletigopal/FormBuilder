// New file: Backend/FormBuilder.API/Controllers/FormRecordsController.cs

using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

/// <summary>
/// CRUD operations for data records stored through a Form.
/// GET    /api/forms/{formId}/records           → paged list
/// GET    /api/forms/{formId}/records/{id}      → single record
/// POST   /api/forms/{formId}/records           → create
/// PUT    /api/forms/{formId}/records/{id}      → update
/// DELETE /api/forms/{formId}/records/{id}      → soft-delete
/// DELETE /api/forms/{formId}/records/bulk      → bulk delete
/// GET    /api/forms/{formId}/records/export    → all records (CSV/JSON export)
/// </summary>
[ApiController]
[Route("api/forms/{formId:int}/records")]
public class FormRecordsController : ControllerBase
{
    private readonly IFormRecordService _svc;
    public FormRecordsController(IFormRecordService svc) => _svc = svc;

    // GET api/forms/1/records?page=1&pageSize=20&search=gopal
    [HttpGet]
    public async Task<ActionResult<ApiResponse<FormRecordPagedResult>>> GetAll(
        int formId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _svc.GetRecordsAsync(formId, page, pageSize, search);
        return Ok(new ApiResponse<FormRecordPagedResult>(true, "OK", result));
    }

    // GET api/forms/1/records/export
    [HttpGet("export")]
    public async Task<ActionResult<ApiResponse<List<FormRecordDto>>>> Export(int formId)
    {
        var result = await _svc.ExportRecordsAsync(formId);
        return Ok(new ApiResponse<List<FormRecordDto>>(true, "OK", result));
    }

    // GET api/forms/1/records/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FormRecordDto>>> GetById(int formId, int id)
    {
        var result = await _svc.GetRecordByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<FormRecordDto>(false, "Record not found", null, 404))
            : Ok(new ApiResponse<FormRecordDto>(true, "OK", result));
    }

    // POST api/forms/1/records
    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormRecordDto>>> Create(
        int formId, [FromBody] CreateFormRecordDto dto)
    {
        // Ensure formId from route matches body
        var correctedDto = dto with { FormId = formId };
        var result = await _svc.CreateRecordAsync(correctedDto);
        return CreatedAtAction(nameof(GetById), new { formId, id = result.Id },
            new ApiResponse<FormRecordDto>(true, "Record created", result, 201));
    }

    // PUT api/forms/1/records/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<FormRecordDto>>> Update(
        int formId, int id, [FromBody] UpdateFormRecordDto dto)
    {
        var result = await _svc.UpdateRecordAsync(id, dto);
        return result == null
            ? NotFound(new ApiResponse<FormRecordDto>(false, "Record not found", null, 404))
            : Ok(new ApiResponse<FormRecordDto>(true, "Record updated", result));
    }

    // DELETE api/forms/1/records/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int formId, int id)
    {
        var ok = await _svc.DeleteRecordAsync(id);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Record deleted", true))
            : NotFound(new ApiResponse<bool>(false, "Record not found", false, 404));
    }

    // DELETE api/forms/1/records/bulk  body: [1,2,3]
    [HttpDelete("bulk")]
    public async Task<ActionResult<ApiResponse<int>>> BulkDelete(
        int formId, [FromBody] List<int> ids)
    {
        var count = await _svc.BulkDeleteAsync(formId, ids);
        return Ok(new ApiResponse<int>(true, $"{count} records deleted", count));
    }
}
