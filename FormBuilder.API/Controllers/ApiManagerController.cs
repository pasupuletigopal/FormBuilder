using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

[ApiController]
[Route("api/api-manager")]
public class ApiManagerController : ControllerBase
{
    private readonly IApiManagerService _svc;
    public ApiManagerController(IApiManagerService svc) => _svc = svc;

    // ?? Environments ?????????????????????????????????????????????????

    [HttpGet("environments")]
    public async Task<ActionResult<ApiResponse<List<ApiEnvironmentDto>>>> GetEnvironments()
    {
        var result = await _svc.GetEnvironmentsAsync();
        return Ok(new ApiResponse<List<ApiEnvironmentDto>>(true, "OK", result));
    }

    [HttpPost("environments")]
    public async Task<ActionResult<ApiResponse<ApiEnvironmentDto>>> CreateEnvironment(
        [FromBody] SaveApiEnvironmentDto dto)
    {
        var result = await _svc.SaveEnvironmentAsync(null, dto);
        return Ok(new ApiResponse<ApiEnvironmentDto>(true, "Saved", result));
    }

    [HttpPut("environments/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiEnvironmentDto>>> UpdateEnvironment(
        int id, [FromBody] SaveApiEnvironmentDto dto)
    {
        try
        {
            var result = await _svc.SaveEnvironmentAsync(id, dto);
            return Ok(new ApiResponse<ApiEnvironmentDto>(true, "Updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ApiEnvironmentDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("environments/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEnvironment(int id)
    {
        var ok = await _svc.DeleteEnvironmentAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // ?? Collections ??????????????????????????????????????????????????

    [HttpGet("collections")]
    public async Task<ActionResult<ApiResponse<List<ApiCollectionDto>>>> GetCollections()
    {
        var result = await _svc.GetCollectionsAsync();
        return Ok(new ApiResponse<List<ApiCollectionDto>>(true, "OK", result));
    }

    [HttpGet("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiCollectionDto>>> GetCollection(int id)
    {
        var result = await _svc.GetCollectionByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<ApiCollectionDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ApiCollectionDto>(true, "OK", result));
    }

    [HttpPost("collections")]
    public async Task<ActionResult<ApiResponse<ApiCollectionDto>>> CreateCollection(
        [FromBody] SaveApiCollectionDto dto)
    {
        var result = await _svc.SaveCollectionAsync(null, dto);
        return Ok(new ApiResponse<ApiCollectionDto>(true, "Saved", result));
    }

    [HttpPut("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiCollectionDto>>> UpdateCollection(
        int id, [FromBody] SaveApiCollectionDto dto)
    {
        try
        {
            var result = await _svc.SaveCollectionAsync(id, dto);
            return Ok(new ApiResponse<ApiCollectionDto>(true, "Updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ApiCollectionDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCollection(int id)
    {
        var ok = await _svc.DeleteCollectionAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // ?? Folders ??????????????????????????????????????????????????????

    [HttpPost("folders")]
    public async Task<ActionResult<ApiResponse<ApiFolderDto>>> CreateFolder(
        [FromBody] SaveApiFolderDto dto)
    {
        var result = await _svc.SaveFolderAsync(null, dto);
        return Ok(new ApiResponse<ApiFolderDto>(true, "Saved", result));
    }

    [HttpPut("folders/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiFolderDto>>> UpdateFolder(
        int id, [FromBody] SaveApiFolderDto dto)
    {
        try
        {
            var result = await _svc.SaveFolderAsync(id, dto);
            return Ok(new ApiResponse<ApiFolderDto>(true, "Updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ApiFolderDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("folders/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFolder(int id)
    {
        var ok = await _svc.DeleteFolderAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // ?? Requests ?????????????????????????????????????????????????????

    [HttpGet("requests/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiRequestDto>>> GetRequest(int id)
    {
        var result = await _svc.GetRequestByIdAsync(id);
        return result == null
            ? NotFound(new ApiResponse<ApiRequestDto>(false, "Not found", null, 404))
            : Ok(new ApiResponse<ApiRequestDto>(true, "OK", result));
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApiResponse<ApiRequestDto>>> CreateRequest(
        [FromBody] SaveApiRequestDto dto)
    {
        var result = await _svc.SaveRequestAsync(null, dto);
        return Ok(new ApiResponse<ApiRequestDto>(true, "Saved", result));
    }

    [HttpPut("requests/{id:int}")]
    public async Task<ActionResult<ApiResponse<ApiRequestDto>>> UpdateRequest(
        int id, [FromBody] SaveApiRequestDto dto)
    {
        try
        {
            var result = await _svc.SaveRequestAsync(id, dto);
            return Ok(new ApiResponse<ApiRequestDto>(true, "Updated", result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ApiRequestDto>(false, ex.Message, null, 404));
        }
    }

    [HttpDelete("requests/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRequest(int id)
    {
        var ok = await _svc.DeleteRequestAsync(id);
        return Ok(new ApiResponse<bool>(true, ok ? "Deleted" : "Not found", ok));
    }

    // ?? Send ?????????????????????????????????????????????????????????

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<ApiResponseDto>>> Send(
        [FromBody] SendApiRequestDto dto)
    {
        try
        {
            var result = await _svc.SendRequestAsync(dto);
            return Ok(new ApiResponse<ApiResponseDto>(true, "OK", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ApiResponseDto>(false, ex.Message, null, 400));
        }
    }

    // ?? History ??????????????????????????????????????????????????????

    [HttpGet("requests/{id:int}/history")]
    public async Task<ActionResult<ApiResponse<List<ApiRequestHistoryDto>>>> GetHistory(int id)
    {
        var result = await _svc.GetHistoryAsync(id);
        return Ok(new ApiResponse<List<ApiRequestHistoryDto>>(true, "OK", result));
    }

    [HttpGet("history/{historyId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetHistoryDetail(int historyId)
    {
        var h = await _svc.GetHistoryDetailAsync(historyId);
        if (h == null)
            return NotFound(new ApiResponse<object>(false, "Not found", null, 404));

        return Ok(new ApiResponse<object>(true, "OK", new
        {
            h.Id, h.RequestId, h.StatusCode,
            h.ResponseHeaders, h.ResponseBody,
            h.ResponseSizeBytes, h.DurationMs, h.ExecutedAt
        }));
    }
}
