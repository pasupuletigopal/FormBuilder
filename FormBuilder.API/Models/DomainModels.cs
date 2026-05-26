// ============================================================
// Models/Domain Entities
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace FormBuilder.API.Models;

public class ControlType
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string DisplayName { get; set; } = "";
    public string? Icon { get; set; }
    public string? Description { get; set; }
    [MaxLength(100)] public string Category { get; set; } = "Input";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DataType
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string DisplayName { get; set; } = "";
    [Required, MaxLength(200)] public string DotNetType { get; set; } = "";
    public bool IsCollection { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DataSource
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [MaxLength(50)] public string SourceType { get; set; } = "Static";
    public string? ApiUrl { get; set; }
    public string? ApiMethod { get; set; } = "GET";
    public string? ApiHeaders { get; set; }  // JSON: {"Authorization":"Bearer xxx","X-Api-Key":"..."}
    [MaxLength(100)] public string ValueField { get; set; } = "value";
    [MaxLength(100)] public string LabelField { get; set; } = "label";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DataSourceItem> Items { get; set; } = new List<DataSourceItem>();

    // Database source configuration
    public int? ExternalConnectionId { get; set; }
    [MaxLength(256)] public string? DatabaseName { get; set; }
    [MaxLength(128)] public string? SchemaName { get; set; } = "dbo";
    [MaxLength(256)] public string? TableName { get; set; }
    [MaxLength(128)] public string? ValueColumn { get; set; }
    [MaxLength(128)] public string? LabelColumn { get; set; }
    [MaxLength(500)] public string? FilterExpression { get; set; }
    [MaxLength(256)] public string? OrderByExpression { get; set; }

    // Navigation
    public ExternalConnection? ExternalConnection { get; set; }
}

public class DataSourceItem
{
    public int Id { get; set; }
    public int DataSourceId { get; set; }
    [Required, MaxLength(500)] public string Value { get; set; } = "";
    [Required, MaxLength(500)] public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DataSource? DataSource { get; set; }
}

public class ValidationRule
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string DisplayName { get; set; } = "";
    public string? Pattern { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Form
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Title { get; set; }
    public string? SubmitUrl { get; set; }
    [MaxLength(10)] public string SubmitMethod { get; set; } = "POST";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    // Styling
    public string? BackgroundColor { get; set; } = "#FFFFFF";
    public string? PrimaryColor { get; set; } = "#1976D2";
    public string? FontFamily { get; set; } = "Segoe UI";
    public int? FontSize { get; set; } = 14;
    public int? BorderRadius { get; set; } = 4;
    public int? Padding { get; set; } = 24;
    public int? MaxWidth { get; set; } = 800;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<FormControl> Controls { get; set; } = new List<FormControl>();
}

public class FormControl
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public int ControlTypeId { get; set; }
    public int? DataTypeId { get; set; }
    public int? DataSourceId { get; set; }

    // Identity
    [Required, MaxLength(200)] public string FieldName { get; set; } = "";
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
    public string? Width { get; set; } = "100%";
    public string? Height { get; set; }
    public string? MinWidth { get; set; }
    public string? MaxWidth { get; set; }

    // Styling
    public string? LabelColor { get; set; }
    public string? ControlColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public int? BorderWidth { get; set; } = 1;
    public int? BorderRadius { get; set; } = 4;
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public string? FontWeight { get; set; } = "normal";
    public string? FontStyle { get; set; } = "normal";
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
    public string? CustomValidation { get; set; }

    // Button
    public string? ButtonType { get; set; }
    public string? ButtonVariant { get; set; }
    public string? ClickAction { get; set; }

    // TextArea
    public int? Rows { get; set; } = 3;
    public bool? AutoResize { get; set; }

    // Extended options as JSON
    public string? ExtendedOptions { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Form? Form { get; set; }
    public ControlType? ControlType { get; set; }
    public DataType? DataType { get; set; }
    public DataSource? DataSource { get; set; }
    public ICollection<FormControlValidation> Validations { get; set; } = new List<FormControlValidation>();
}

public class FormControlValidation
{
    public int Id { get; set; }
    public int FormControlId { get; set; }
    public int ValidationRuleId { get; set; }
    public string? CustomMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FormControl? FormControl { get; set; }
    public ValidationRule? ValidationRule { get; set; }
}

public class FormSubmission
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public int FormVersion { get; set; } = 1;
    [Required] public string Data { get; set; } = "{}";
    public string? SubmittedBy { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Form? Form { get; set; }
}

public class FormConnector
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public int? ExternalConnectionId { get; set; }
    public string DatabaseName { get; set; } = "";
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "";
    public string PrimaryKeyColumn { get; set; } = "Id";
    public string? ExcludeColumns { get; set; }
    public string? DefaultFilter { get; set; }
    public string? DefaultOrderBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Form? Form { get; set; }
    public ExternalConnection? ExternalConnection { get; set; }
}

public class ExternalConnection
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(500)] public string ServerName { get; set; } = "";
    [Required, MaxLength(50)] public string AuthType { get; set; } = "Windows";   // Windows | SqlServer
    public string? Username { get; set; }                // encrypted at rest
    public string? Password { get; set; }                // encrypted at rest
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestOk { get; set; }
    public ICollection<FormConnector> FormConnectors { get; set; } = new List<FormConnector>();
}

public class Dashboard
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ExternalConnectionId { get; set; }
    public string DatabaseName { get; set; } = "";
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "";
    public string? DateColumn { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ExternalConnection? ExternalConnection { get; set; }
    public ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();
}
public class DashboardWidget
{
    public int Id { get; set; }
    public int DashboardId { get; set; }
    public string Title { get; set; } = "";
    public string WidgetType { get; set; } = "KPI"; // KPI|Bar|Line|Pie|Donut|Table
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 4;
    public int Height { get; set; } = 3;
    public string Config { get; set; } = "{}";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dashboard? Dashboard { get; set; }
}

public class Report
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ExternalConnectionId { get; set; }
    public string DatabaseName { get; set; } = "";
    public string SchemaName { get; set; } = "dbo";
    public string QueryConfig { get; set; } = "{}";
    public string? LastGeneratedSql { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public int? LastRowCount { get; set; }
    public ExternalConnection? ExternalConnection { get; set; }
    public ReportSchedule? Schedule { get; set; }
    public ICollection<ReportRunHistory> RunHistory { get; set; } = new List<ReportRunHistory>();
}

public class ReportSchedule
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Frequency { get; set; } = "Daily"; // Daily|Weekly|Monthly
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int RunAtHour { get; set; } = 8;
    public string EmailTo { get; set; } = "";
    public string? EmailSubject { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Report? Report { get; set; }
}

public class ReportRunHistory
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public int? RowCount { get; set; }
    public int? DurationMs { get; set; }
    public string Status { get; set; } = "Success";
    public string? ErrorMsg { get; set; }
    public string? TriggeredBy { get; set; }
    public Report? Report { get; set; }
}

public class SpReport
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ConnectionId { get; set; }
    public string DatabaseName { get; set; } = "";
    public string SchemaName { get; set; } = "dbo";
    public string ObjectName { get; set; } = "";
    public string ObjectType { get; set; } = "StoredProcedure";
    public string? ParameterConfig { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public int? LastRowCount { get; set; }
    public ExternalConnection? Connection { get; set; }
}

// ============================================================
// API Manager
// ============================================================

public class ApiEnvironment
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";  // Dev, UAT, Prod
    public string Variables { get; set; } = "{}";                       // JSON: {"baseUrl":"https://...","apiKey":"xxx"}
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ApiCollection
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ApiFolder> Folders { get; set; } = new List<ApiFolder>();
    public ICollection<ApiRequest> Requests { get; set; } = new List<ApiRequest>();
}

public class ApiFolder
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public int? ParentFolderId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ApiCollection? Collection { get; set; }
    public ApiFolder? ParentFolder { get; set; }
    public ICollection<ApiFolder> SubFolders { get; set; } = new List<ApiFolder>();
    public ICollection<ApiRequest> Requests { get; set; } = new List<ApiRequest>();
}

public class ApiRequest
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public int? FolderId { get; set; }
    [Required, MaxLength(300)] public string Name { get; set; } = "";
    [MaxLength(10)] public string Method { get; set; } = "GET";        // GET|POST|PUT|DELETE|PATCH
    [Required] public string Url { get; set; } = "";                    // may contain {{baseUrl}}
    public string? Headers { get; set; }                                // JSON: [{"key":"Content-Type","value":"application/json","enabled":true}]
    public string? QueryParams { get; set; }                            // JSON: [{"key":"page","value":"1","enabled":true}]
    public string? AuthType { get; set; }                               // None|Bearer|ApiKey|Basic
    public string? AuthConfig { get; set; }                             // JSON: {"token":"xxx"} or {"username":"u","password":"p"}
    public string? Body { get; set; }                                   // raw JSON body
    public string? BodyType { get; set; } = "json";                    // json|form|none
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ApiCollection? Collection { get; set; }
    public ApiFolder? Folder { get; set; }
    public ICollection<ApiRequestHistory> History { get; set; } = new List<ApiRequestHistory>();
}

public class ApiRequestHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseSizeBytes { get; set; }
    public int DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public ApiRequest? Request { get; set; }
}


