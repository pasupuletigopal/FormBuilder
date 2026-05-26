// ============================================================
// DTOs (Data Transfer Objects)
// ============================================================
namespace FormBuilder.API.DTOs;

// -------- Control Types --------
public record ControlTypeDto(int Id, string Name, string DisplayName, string? Icon,
    string? Description, string Category, bool IsActive, int SortOrder);

public record CreateControlTypeDto(string Name, string DisplayName, string? Icon,
    string? Description, string Category, int SortOrder);

public record UpdateControlTypeDto(string Name, string DisplayName, string? Icon,
    string? Description, string Category, bool IsActive, int SortOrder);

// -------- Data Types --------
public record DataTypeDto(int Id, string Name, string DisplayName, string DotNetType,
    bool IsCollection, bool IsActive);

// -------- Data Sources --------
public record DataSourceItemDto(int Id, string Value, string Label, int SortOrder,
    bool IsDefault, bool IsActive);

public record DataSourceDto(int Id, string Name, string? Description, string SourceType,
    string? ApiUrl, string? ApiMethod, string? ApiHeaders, string ValueField, string LabelField, bool IsActive,
    List<DataSourceItemDto> Items,
    int? ExternalConnectionId = null, string? DatabaseName = null, string? SchemaName = null,
    string? TableName = null, string? ValueColumn = null, string? LabelColumn = null,
    string? FilterExpression = null, string? OrderByExpression = null);

public record CreateDataSourceDto(string Name, string? Description, string SourceType,
    string? ApiUrl, string? ApiMethod, string? ApiHeaders, string ValueField, string LabelField,
    List<CreateDataSourceItemDto> Items,
    int? ExternalConnectionId = null, string? DatabaseName = null, string? SchemaName = null,
    string? TableName = null, string? ValueColumn = null, string? LabelColumn = null,
    string? FilterExpression = null, string? OrderByExpression = null);

public record CreateDataSourceItemDto(string Value, string Label, int SortOrder, bool IsDefault);

public record UpdateDataSourceDto(string Name, string? Description, string SourceType,
    string? ApiUrl, string? ApiMethod, string? ApiHeaders, string ValueField, string LabelField, bool IsActive,
    List<CreateDataSourceItemDto> Items,
    int? ExternalConnectionId = null, string? DatabaseName = null, string? SchemaName = null,
    string? TableName = null, string? ValueColumn = null, string? LabelColumn = null,
    string? FilterExpression = null, string? OrderByExpression = null);

public record PreviewApiDataSourceRequest(
    string ApiUrl,
    string? ApiMethod,
    string? ApiHeaders,
    string? ValueField,
    string? LabelField);

// -------- Validation Rules --------
public record ValidationRuleDto(int Id, string Name, string DisplayName, string? Pattern,
    string? ErrorMessage, bool IsActive);

// -------- Forms --------
public record FormSummaryDto(int Id, string Name, string? Description, string? Title,
    bool IsActive, int Version, DateTime CreatedAt, DateTime UpdatedAt, int ControlCount);

public record FormControlValidationDto(int Id, int ValidationRuleId, string ValidationRuleName,
    string? CustomMessage);

public record FormControlDto
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public int ControlTypeId { get; set; }
    public string ControlTypeName { get; set; } = "";
    public string ControlTypeDisplayName { get; set; } = "";
    public string ControlTypeCategory { get; set; } = "";
    public int? DataTypeId { get; set; }
    public string? DataTypeName { get; set; }
    public int? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }

    // Identity
    public string FieldName { get; set; } = "";
    public string? Label { get; set; }
    public string? Placeholder { get; set; }
    public string? HelperText { get; set; }
    public string? DefaultValue { get; set; }
    public string? Tooltip { get; set; }

    // Layout
    public int RowIndex { get; set; }
    public int ColIndex { get; set; }
    public int ColSpan { get; set; } = 12;
    public int RowSpan { get; set; } = 1;
    public int SortOrder { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? MinWidth { get; set; }
    public string? MaxWidth { get; set; }

    // Styling
    public string? LabelColor { get; set; }
    public string? ControlColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public int? BorderWidth { get; set; }
    public int? BorderRadius { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public string? FontWeight { get; set; }
    public string? FontStyle { get; set; }
    public string? TextColor { get; set; }
    public string? Padding { get; set; }
    public string? Margin { get; set; }
    public string? CustomCssClass { get; set; }
    public string? CustomCss { get; set; }

    // Validation
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsHidden { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }

    // Button
    public string? ButtonType { get; set; }
    public string? ButtonVariant { get; set; }
    public string? ClickAction { get; set; }

    // TextArea
    public int? Rows { get; set; }
    public bool? AutoResize { get; set; }

    // Extended
    public string? ExtendedOptions { get; set; }

    // Nested data
    public List<DataSourceItemDto>? DataSourceItems { get; set; }
    public List<FormControlValidationDto> Validations { get; set; } = new();
}

public record FormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Title { get; set; }
    public string? SubmitUrl { get; set; }
    public string SubmitMethod { get; set; } = "POST";
    public bool IsActive { get; set; }
    public int Version { get; set; }
    // Styling
    public string? BackgroundColor { get; set; }
    public string? PrimaryColor { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public int? BorderRadius { get; set; }
    public int? Padding { get; set; }
    public int? MaxWidth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FormControlDto> Controls { get; set; } = new();
}

public record CreateFormDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Title { get; set; }
    public string? SubmitUrl { get; set; }
    public string SubmitMethod { get; set; } = "POST";
    public string? BackgroundColor { get; set; } = "#FFFFFF";
    public string? PrimaryColor { get; set; } = "#1976D2";
    public string? FontFamily { get; set; } = "Segoe UI";
    public int? FontSize { get; set; } = 14;
    public int? BorderRadius { get; set; } = 4;
    public int? Padding { get; set; } = 24;
    public int? MaxWidth { get; set; } = 800;
}

public record UpdateFormDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Title { get; set; }
    public string? SubmitUrl { get; set; }
    public string SubmitMethod { get; set; } = "POST";
    public bool IsActive { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? PrimaryColor { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public int? BorderRadius { get; set; }
    public int? Padding { get; set; }
    public int? MaxWidth { get; set; }
}

public record CreateFormControlDto
{
    public int ControlTypeId { get; set; }
    public int? DataTypeId { get; set; }
    public int? DataSourceId { get; set; }
    public string FieldName { get; set; } = "";
    public string? Label { get; set; }
    public string? Placeholder { get; set; }
    public string? HelperText { get; set; }
    public string? DefaultValue { get; set; }
    public string? Tooltip { get; set; }
    public int RowIndex { get; set; }
    public int ColIndex { get; set; }
    public int ColSpan { get; set; } = 12;
    public int SortOrder { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? LabelColor { get; set; }
    public string? ControlColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public int? BorderWidth { get; set; }
    public int? BorderRadius { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public string? FontWeight { get; set; }
    public string? FontStyle { get; set; }
    public string? TextColor { get; set; }
    public string? Padding { get; set; }
    public string? Margin { get; set; }
    public string? CustomCssClass { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsHidden { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? ButtonType { get; set; }
    public string? ButtonVariant { get; set; }
    public string? ClickAction { get; set; }
    public int? Rows { get; set; }
    public bool? AutoResize { get; set; }
    public string? ExtendedOptions { get; set; }
}

public record UpdateFormControlsDto(List<CreateFormControlDto> Controls);

public record FormSubmissionDto(int FormId, string Data, string? SubmittedBy);

public record ApiResponse<T>(bool Success, string Message, T? Data, int StatusCode = 200);
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

// -------- External Connections --------
public record ExternalConnectionDto(
    int Id,
    string Name,
    string ServerName,
    string AuthType,
    string? Username,       // never returns password
    string? Description,
    bool IsActive,
    DateTime? LastTestedAt,
    bool? LastTestOk,
    int FormsCount
);

public record SaveExternalConnectionDto(
    string Name,
    string ServerName,
    string AuthType,        // "Windows" | "SqlServer"
    string? Username,
    string? Password,       // null = keep existing password
    string? Description
);

public record TestExternalConnectionDto(
    string ServerName,
    string AuthType,
    string? Username,
    string? Password,
    string? DatabaseName    // optional — if null just tests server connectivity
);

// -------- Form Connectors --------
public record FormConnectorDto(
    int Id,
    int FormId,
    int? ExternalConnectionId,
    string DatabaseName,
    string SchemaName,
    string TableName,
    string PrimaryKeyColumn,
    string? ExcludeColumns,
    string? DefaultFilter,
    string? DefaultOrderBy,
    bool IsActive
);

public record SaveConnectorDto(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string PrimaryKeyColumn,
    string? ExcludeColumns,
    string? DefaultFilter,
    string? DefaultOrderBy,
    int? ExternalConnectionId
);

public record ExternalRecordPagedResult(
    List<Dictionary<string, object?>> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string PrimaryKeyColumn,
    List<string> Columns
);

public record TestConnectionResult(
    bool Success,
    string Message,
    List<string>? Columns,
    int? RowCount
);

public record DashboardDto(
    int Id, string Name, string? Description,
    int ExternalConnectionId, string ConnectionName,
    string DatabaseName, string SchemaName, string TableName,
    string? DateColumn, bool IsActive,
    DateTime CreatedAt, int WidgetCount
);

public record SaveDashboardDto(
    string Name, string? Description,
    int ExternalConnectionId,
    string DatabaseName, string SchemaName, string TableName,
    string? DateColumn
);

public record DashboardWidgetDto(
    int Id, int DashboardId, string Title,
    string WidgetType, int PositionX, int PositionY,
    int Width, int Height, string Config, int SortOrder
);

public record SaveWidgetDto(
    string Title, string WidgetType,
    int PositionX, int PositionY,
    int Width, int Height,
    string Config, int SortOrder
);

public record UpdateWidgetPositionsDto(
    List<WidgetPositionDto> Positions
);

public record WidgetPositionDto(
    int Id, int PositionX, int PositionY,
    int Width, int Height
);

public record WidgetDataRequest(
    int DashboardId,
    int WidgetId,
    string? DateFrom,
    string? DateTo,
    List<ColumnFilterDto>? Filters
);

public record ColumnFilterDto(
    string Column,
    string Operator,   // eq|neq|contains|gt|lt|gte|lte
    string Value
);

public record KpiResult(
    string Label, string Value,
    string? Prefix, string? Suffix,
    string ColorScheme, double RawValue
);

public record ChartResult(
    List<string> Labels,
    List<double> Values,
    string XColumn,
    string YColumn
);

public record TableResult(
    List<Dictionary<string, object?>> Rows,
    int TotalCount,
    int Page,
    int PageSize,
    List<string> Columns
);

public record DrillDownResult(
    List<Dictionary<string, object?>> Rows,
    int TotalCount,
    List<string> Columns,
    string FilterDescription
);

public record ReportDto(
    int Id, string Name, string? Description,
    int ExternalConnectionId, string ConnectionName,
    string DatabaseName, string SchemaName,
    string QueryConfig, string? LastGeneratedSql,
    bool IsActive, DateTime CreatedAt,
    DateTime? LastRunAt, int? LastRowCount,
    ReportScheduleDto? Schedule
);

public record SaveReportDto(
    string Name, string? Description,
    int ExternalConnectionId,
    string DatabaseName, string SchemaName,
    string QueryConfig
);

public record ReportScheduleDto(
    int Id, int ReportId,
    string Frequency, int? DayOfWeek, int? DayOfMonth,
    int RunAtHour, string EmailTo, string? EmailSubject,
    bool IsActive, DateTime? LastRunAt, DateTime? NextRunAt
);

public record SaveScheduleDto(
    string Frequency, int? DayOfWeek, int? DayOfMonth,
    int RunAtHour, string EmailTo, string? EmailSubject
);

public record RunReportResult(
    List<Dictionary<string, object?>> Rows,
    int TotalCount, int DurationMs,
    List<string> Columns, string GeneratedSql
);

public record ReportRunHistoryDto(
    int Id, DateTime RunAt, int? RowCount,
    int? DurationMs, string Status,
    string? ErrorMsg, string? TriggeredBy
);

// Query config models (deserialized from QueryConfig JSON)
public class QueryConfig
{
    public string PrimaryTable { get; set; } = "";
    public List<JoinConfig> Joins { get; set; } = new();
    public List<ColumnConfig> Columns { get; set; } = new();
    public List<FilterConfig> Filters { get; set; } = new();
    public List<string> GroupBy { get; set; } = new();
    public List<OrderByConfig> OrderBy { get; set; } = new();
    public int TopN { get; set; } = 1000;
}

public class JoinConfig
{
    public string Table { get; set; } = "";
    public string Alias { get; set; } = "";
    public string JoinType { get; set; } = "INNER"; // INNER|LEFT|RIGHT
    public string LeftColumn { get; set; } = "";    // e.g. "Orders.CustomerId"
    public string RightColumn { get; set; } = "";   // e.g. "Customers.Id"
}

public class ColumnConfig
{
    public string Table { get; set; } = "";
    public string Column { get; set; } = "";
    public string? Alias { get; set; }
    public string? Aggregation { get; set; } // null|SUM|COUNT|AVG|MIN|MAX
}

public class FilterConfig
{
    public string Table { get; set; } = "";
    public string Column { get; set; } = "";
    public string Operator { get; set; } = "eq";
    public string Value { get; set; } = "";
}

public class OrderByConfig
{
    public string Table { get; set; } = "";
    public string Column { get; set; } = "";
    public string Direction { get; set; } = "ASC";
}

// -------- Report Meta: SPs and Views --------
public record SpOrViewInfo(string Name, string Type, string? Schema);

public record SpParameterInfo(
    string Name, string DataType, int? MaxLength,
    bool HasDefault, string? DefaultValue, bool IsOutput);

public record ExecuteSpRequest(
    int ConnectionId,
    string DatabaseName,
    string SchemaName,
    string Name,
    string Type,   // "SP" | "VIEW"
    List<SpParameterValue>? Parameters = null,
    int TopN = 1000,
    List<ExecuteSpFilter>? Filters = null
);

public class SpParameterValue
{
    public string Name { get; set; } = "";
    public object? Value { get; set; }
    public string? DataType { get; set; }
}

public class ExecuteSpFilter
{
    public string Column { get; set; } = "";
    public string Operator { get; set; } = "eq";
    public object? Value { get; set; }
}

// -------- SP Reports --------
public record SpReportDto(
    int Id, string Name, string? Description,
    int ConnectionId, string? ConnectionName,
    string DatabaseName, string SchemaName,
    string ObjectName, string ObjectType,
    string? ParameterConfig,
    DateTime CreatedAt, DateTime? LastRunAt, int? LastRowCount);

public record SaveSpReportDto(
    string Name, string? Description,
    int ConnectionId,
    string DatabaseName, string SchemaName,
    string ObjectName, string ObjectType,
    string? ParameterConfig);

// ============================================================
// API Manager DTOs
// ============================================================

// -------- Environments --------
public record ApiEnvironmentDto(int Id, string Name, string Variables, int SortOrder, bool IsActive);
public record SaveApiEnvironmentDto(string Name, string Variables, int SortOrder = 0);

// -------- Collections --------
public record ApiCollectionDto(
    int Id, string Name, string? Description, int SortOrder, bool IsActive,
    List<ApiFolderDto> Folders, List<ApiRequestSummaryDto> Requests);

public record SaveApiCollectionDto(string Name, string? Description, int SortOrder = 0);

// -------- Folders --------
public record ApiFolderDto(
    int Id, int CollectionId, int? ParentFolderId, string Name, int SortOrder,
    List<ApiFolderDto> SubFolders, List<ApiRequestSummaryDto> Requests);

public record SaveApiFolderDto(int CollectionId, int? ParentFolderId, string Name, int SortOrder = 0);

// -------- Requests --------
public record ApiRequestSummaryDto(int Id, int CollectionId, int? FolderId,
    string Name, string Method, string Url, int SortOrder);

public record ApiRequestDto(
    int Id, int CollectionId, int? FolderId,
    string Name, string Method, string Url,
    string? Headers, string? QueryParams,
    string? AuthType, string? AuthConfig,
    string? Body, string? BodyType,
    int SortOrder, DateTime UpdatedAt);

public record SaveApiRequestDto(
    int CollectionId, int? FolderId,
    string Name, string Method, string Url,
    string? Headers, string? QueryParams,
    string? AuthType, string? AuthConfig,
    string? Body, string? BodyType,
    int SortOrder = 0);

// -------- Send / Response --------
public record SendApiRequestDto(
    string Method, string Url,
    string? Headers, string? QueryParams,
    string? AuthType, string? AuthConfig,
    string? Body, string? BodyType,
    int? EnvironmentId,
    int? RequestId);

public record ApiResponseDto(
    int StatusCode, string StatusText,
    string ResponseHeaders, string ResponseBody,
    long ResponseSizeBytes, int DurationMs);

// -------- History --------
public record ApiRequestHistoryDto(
    int Id, int RequestId, int StatusCode,
    long ResponseSizeBytes, int DurationMs, DateTime ExecutedAt);
