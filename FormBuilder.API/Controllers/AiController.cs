using FormBuilder.API.DTOs;
using FormBuilder.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiService _svc;

    public AiController(IAiService svc)
    {
        _svc = svc;
    }

    [HttpPost("generate-form")]
    public async Task<ActionResult<ApiResponse<AiFormDraftDto>>> GenerateForm([FromBody] AiGenerateFormRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new ApiResponse<AiFormDraftDto>(false, "Prompt is required", null, 400));

        try
        {
            var result = await _svc.GenerateFormAsync(req);
            return Ok(new ApiResponse<AiFormDraftDto>(true, "Form draft generated", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AiFormDraftDto>(false, ex.Message, null, 502));
        }
    }

    [HttpPost("suggest-layout")]
    public async Task<ActionResult<ApiResponse<AiLayoutSuggestionResultDto>>> SuggestLayout([FromBody] AiSuggestLayoutRequest req)
    {
        try
        {
            var result = await _svc.SuggestLayoutAsync(req);
            return Ok(new ApiResponse<AiLayoutSuggestionResultDto>(true, "Layout suggestions generated", result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<AiLayoutSuggestionResultDto>(false, ex.Message, null, 400));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AiLayoutSuggestionResultDto>(false, ex.Message, null, 502));
        }
    }

    [HttpPost("suggest-validations")]
    public async Task<ActionResult<ApiResponse<AiValidationSuggestionDto>>> SuggestValidations([FromBody] AiSuggestValidationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FieldName))
            return BadRequest(new ApiResponse<AiValidationSuggestionDto>(false, "fieldName is required", null, 400));

        try
        {
            var result = await _svc.SuggestValidationsAsync(req);
            return Ok(new ApiResponse<AiValidationSuggestionDto>(true, "Validation suggestion generated", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AiValidationSuggestionDto>(false, ex.Message, null, 502));
        }
    }

    [HttpPost("suggest-report-query")]
    public async Task<ActionResult<ApiResponse<AiReportQuerySuggestionDto>>> SuggestReportQuery([FromBody] AiSuggestReportQueryRequest req)
    {
        try
        {
            var result = await _svc.SuggestReportQueryAsync(req);
            return Ok(new ApiResponse<AiReportQuerySuggestionDto>(true, "Report query suggestion generated", result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<AiReportQuerySuggestionDto>(false, ex.Message, null, 400));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AiReportQuerySuggestionDto>(false, ex.Message, null, 502));
        }
    }

    [HttpPost("summarize-dashboard")]
    public async Task<ActionResult<ApiResponse<AiDashboardSummaryDto>>> SummarizeDashboard([FromBody] AiSummarizeDashboardRequest req)
    {
        try
        {
            var result = await _svc.SummarizeDashboardAsync(req);
            return Ok(new ApiResponse<AiDashboardSummaryDto>(true, "Dashboard summary generated", result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<AiDashboardSummaryDto>(false, ex.Message, null, 400));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AiDashboardSummaryDto>(false, ex.Message, null, 502));
        }
    }

    [HttpPost("assistant")]
    public async Task<ActionResult<ApiResponse<AiAssistantResponseDto>>> Assistant([FromBody] AiAssistantRequest req)
    {
        try
        {
            var result = await _svc.AssistantAsync(req);
            return Ok(new ApiResponse<AiAssistantResponseDto>(true, "Assistant response", result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<AiAssistantResponseDto>(false, ex.Message, null, 400));
        }
        catch (Exception)
        {
            return BadRequest(new ApiResponse<AiAssistantResponseDto>(false, "Assistant is unavailable", null, 502));
        }
    }
}
