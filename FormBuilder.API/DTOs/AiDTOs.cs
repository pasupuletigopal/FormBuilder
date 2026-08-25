namespace FormBuilder.API.DTOs;

// ============================================================
// AI: Form Generator
// ============================================================

public record AiGenerateFormRequest(string Prompt);

public record AiControlDraftDto(
    string ControlTypeName,
    string FieldName,
    string Label,
    string? Placeholder,
    string? HelperText,
    bool IsRequired,
    int ColSpan,
    int RowIndex,
    int SortOrder,
    List<AiDataSourceItemDraftDto>? DataSourceItems,
    List<string>? ValidationRuleNames
);

public record AiDataSourceItemDraftDto(string Value, string Label);

public record AiFormDraftDto(
    string Name,
    string? Title,
    string? Description,
    List<AiControlDraftDto> Controls,
    List<string>? Warnings
);

// ============================================================
// AI: Layout suggestions
// ============================================================

public record AiControlLayoutInputDto(
    string FieldName,
    string? Label,
    string ControlTypeName,
    string? ControlTypeCategory,
    int ColSpan,
    int RowIndex,
    int ColIndex,
    int SortOrder
);

public record AiSuggestLayoutRequest(List<AiControlLayoutInputDto> Controls);

public record AiLayoutSuggestionDto(
    string FieldName,
    int RowIndex,
    int ColIndex,
    int ColSpan,
    int SortOrder,
    string? Reason
);

public record AiLayoutSuggestionResultDto(List<AiLayoutSuggestionDto> Suggestions, string? Summary);

// ============================================================
// AI: Validation suggestions
// ============================================================

public record AiSuggestValidationRequest(
    string FieldName,
    string? Label,
    string ControlTypeName,
    string? DataTypeName,
    string? Placeholder,
    string? HelperText
);

public record AiValidationSuggestionDto(
    bool IsRequired,
    int? MinLength,
    int? MaxLength,
    string? MinValue,
    string? MaxValue,
    string? Pattern,
    List<string> ValidationRuleNames,
    string? Reason,
    List<string>? Warnings
);

// ============================================================
// AI: Report query suggestions
// ============================================================

public record AiReportJoinInputDto(string Table, string Alias, string JoinType, string LeftColumn, string RightColumn);

public record AiSuggestReportQueryRequest(
    string Prompt,
    string PrimaryTable,
    List<AiReportJoinInputDto> Joins,
    Dictionary<string, List<string>> AvailableColumns
);

public record AiReportColumnSuggestionDto(string Table, string Column, string Alias, string Aggregation);
public record AiReportFilterSuggestionDto(string Table, string Column, string Operator, string Value);
public record AiReportOrderBySuggestionDto(string Table, string Column, string Direction);

public record AiReportQuerySuggestionDto(
    List<AiReportColumnSuggestionDto> Columns,
    List<AiReportFilterSuggestionDto> Filters,
    List<string> GroupBy,
    List<AiReportOrderBySuggestionDto> OrderBy,
    int TopN,
    string? Reason,
    List<string>? Warnings
);

// ============================================================
// AI: Dashboard summary
// ============================================================

public record AiWidgetInputDto(string Title, string WidgetType, System.Text.Json.JsonElement Data);
public record AiSummarizeDashboardRequest(string DashboardName, List<AiWidgetInputDto> Widgets);

public record AiDashboardSummaryDto(string Summary, List<string> Insights, List<string>? Warnings);

// ============================================================
// AI: Cross-cutting assistant
// ============================================================

public record AiAssistantHistoryItemDto(string Role, string Content);
public record AiAssistantRequest(string Message, List<AiAssistantHistoryItemDto> History);

public record AiAssistantActionDto(string Type, Dictionary<string, object> Params);
public record AiAssistantResponseDto(string Reply, AiAssistantActionDto? Action);
