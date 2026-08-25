using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FormBuilder.API.DTOs;

namespace FormBuilder.API.Services;

public interface IAiService
{
    Task<AiFormDraftDto> GenerateFormAsync(AiGenerateFormRequest req);
    Task<AiLayoutSuggestionResultDto> SuggestLayoutAsync(AiSuggestLayoutRequest req);
    Task<AiValidationSuggestionDto> SuggestValidationsAsync(AiSuggestValidationRequest req);
    Task<AiReportQuerySuggestionDto> SuggestReportQueryAsync(AiSuggestReportQueryRequest req);
    Task<AiDashboardSummaryDto> SummarizeDashboardAsync(AiSummarizeDashboardRequest req);
    Task<AiAssistantResponseDto> AssistantAsync(AiAssistantRequest req);
}

public class AiService : IAiService
{
    private const string Model = "claude-sonnet-4-6";
    private const string AnthropicUrl = "https://api.anthropic.com/v1/messages";

    private readonly IMasterDataService _masterData;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _apiKey;

    public AiService(IMasterDataService masterData, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _masterData = masterData;
        _httpFactory = httpFactory;
        _apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured. Set it via user-secrets or an environment variable — see README.");
    }

    // ==================== Shared: raw Anthropic call ====================

    private async Task<(string? Text, JsonElement? ToolUse)> CallClaudeAsync(
        string systemPrompt, string userMessage, int maxTokens, object[]? tools = null)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        object body = tools is null
            ? new { model = Model, max_tokens = maxTokens, system = systemPrompt, messages = new[] { new { role = "user", content = userMessage } } }
            : new { model = Model, max_tokens = maxTokens, system = systemPrompt, tools, messages = new[] { new { role = "user", content = userMessage } } };

        var response = await client.PostAsJsonAsync(AnthropicUrl, body);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("Empty response from Anthropic API");

        string? text = null;
        JsonElement? toolUse = null;
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            if (type == "text") text = block.GetProperty("text").GetString();
            else if (type == "tool_use" && toolUse is null) toolUse = block.Clone();
        }

        return (text, toolUse);
    }

    private static T ParseJson<T>(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("AI returned an empty response.");
        try
        {
            return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("AI returned a null object.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("AI returned an unparseable response.", ex);
        }
    }

    // ==================== 1. Form Generator ====================

    public async Task<AiFormDraftDto> GenerateFormAsync(AiGenerateFormRequest req)
    {
        var controlTypes = await _masterData.GetControlTypesAsync();
        var validationRules = await _masterData.GetValidationRulesAsync();

        var validTypeNames = controlTypes.Where(c => c.IsActive).Select(c => c.Name).ToList();
        var validRuleNames = validationRules.Where(r => r.IsActive).Select(r => r.Name).ToList();

        var systemPrompt = $$"""
            You design web form schemas. Given a user's description, output ONLY a JSON object
            (no markdown fences, no prose) matching exactly this shape:

            {
              "name": string,
              "title": string,
              "description": string,
              "controls": [
                {
                  "controlTypeName": string,   // MUST be one of: {{string.Join(", ", validTypeNames)}}
                  "fieldName": string,          // camelCase, unique within the form
                  "label": string,
                  "placeholder": string | null,
                  "helperText": string | null,
                  "isRequired": boolean,
                  "colSpan": number,            // 1-12
                  "rowIndex": number,           // 0-based, increment per row
                  "sortOrder": number,          // 0-based, matches control order
                  "dataSourceItems": [{ "value": string, "label": string }] | null,  // only for dropdown/select/radio controls
                  "validationRuleNames": string[] | null  // subset of: {{string.Join(", ", validRuleNames)}}
                }
              ]
            }

            Keep forms to a sensible number of fields (typically 4-15) unless the user asks for more.
            Group related fields on the same rowIndex when it makes sense (e.g. firstName/lastName).
            """;

        var (text, _) = await CallClaudeAsync(systemPrompt, req.Prompt, 2000);
        var draft = ParseJson<AiFormDraftDto>(text);

        var validTypesSet = validTypeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fallbackType = validTypeNames.FirstOrDefault(t => t.Equals("TextBox", StringComparison.OrdinalIgnoreCase))
                            ?? validTypeNames.FirstOrDefault() ?? "TextBox";

        var warnings = new List<string>();
        var repairedControls = draft.Controls.Select(c =>
        {
            if (!validTypesSet.Contains(c.ControlTypeName))
            {
                warnings.Add($"Unknown control type '{c.ControlTypeName}' for field '{c.FieldName}' — used '{fallbackType}' instead.");
                return c with { ControlTypeName = fallbackType };
            }
            return c;
        }).ToList();

        return draft with { Controls = repairedControls, Warnings = warnings.Count > 0 ? warnings : null };
    }

    // ==================== 2. Layout suggestions ====================

    public async Task<AiLayoutSuggestionResultDto> SuggestLayoutAsync(AiSuggestLayoutRequest req)
    {
        if (req.Controls.Count == 0)
            throw new ArgumentException("No controls to lay out.");

        var systemPrompt = """
            You are a form-layout expert. You receive a JSON array of form fields, each with its
            current rowIndex/colIndex/colSpan/sortOrder on a 12-column responsive grid, plus its
            fieldName, label, and controlTypeName.

            Return ONLY a JSON object (no markdown fences, no prose) matching exactly:

            {
              "suggestions": [
                {
                  "fieldName": string,     // MUST exactly match an input fieldName, one entry per input control
                  "rowIndex": number,
                  "colIndex": number,
                  "colSpan": number,       // 1-12
                  "sortOrder": number,
                  "reason": string         // short (<12 words) justification
                }
              ],
              "summary": string           // one sentence describing the overall change
            }

            Layout rules to apply:
            - Group logically related short fields on the same row (e.g. First Name + Last Name,
              City + State + Zip) so their colSpans sum to <= 12.
            - Full-width elements (headings, dividers, text areas, rich text, sections) get colSpan 12
              on their own row.
            - Checkboxes, toggles, and short pickers (date/time/color) get a narrow colSpan (3-4).
            - Standard text/number/email/dropdown fields default to colSpan 6 unless grouped.
            - Preserve every fieldName exactly; do not add, remove, or rename fields.
            - sortOrder must increase monotonically in the same reading order as the rows/columns you assign.
            """;

        var userMessage = $"Current fields:\n{JsonSerializer.Serialize(req.Controls)}";
        var (text, _) = await CallClaudeAsync(systemPrompt, userMessage, 2000);
        var result = ParseJson<AiLayoutSuggestionResultDto>(text);

        var validFieldNames = req.Controls.Select(c => c.FieldName).ToHashSet(StringComparer.Ordinal);
        var cleaned = result.Suggestions
            .Where(s => validFieldNames.Contains(s.FieldName))
            .Select(s => s with { ColSpan = Math.Clamp(s.ColSpan, 1, 12) })
            .ToList();

        return result with { Suggestions = cleaned };
    }

    // ==================== 3. Validation suggestions ====================

    public async Task<AiValidationSuggestionDto> SuggestValidationsAsync(AiSuggestValidationRequest req)
    {
        var rules = (await _masterData.GetValidationRulesAsync()).Where(r => r.IsActive).ToList();
        var ruleCatalog = string.Join("\n", rules.Select(r =>
            $"- {r.Name} ({r.DisplayName}){(string.IsNullOrEmpty(r.Pattern) ? "" : $": {r.Pattern}")}"));

        var systemPrompt = $$"""
            You suggest validation configuration for a single web form field. Return ONLY a JSON
            object (no markdown fences, no prose) matching exactly:

            {
              "isRequired": boolean,
              "minLength": number | null,
              "maxLength": number | null,
              "minValue": string | null,
              "maxValue": string | null,
              "pattern": string | null,
              "validationRuleNames": string[],   // ONLY from this exact catalog (use the Name, not DisplayName):
            {{ruleCatalog}}
              "reason": string   // one short sentence explaining the recommendation
            }

            Only fill minLength/maxLength for text-like fields, minValue/maxValue for numeric or
            date-like fields, and pattern only if nothing in validationRuleNames already covers it.
            If nothing meaningful applies, return isRequired: false, nulls, and an empty
            validationRuleNames array — don't force a suggestion.
            """;

        var (text, _) = await CallClaudeAsync(systemPrompt, JsonSerializer.Serialize(req), 800);
        var suggestion = ParseJson<AiValidationSuggestionDto>(text);

        var validNames = rules.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var keptNames = new List<string>();
        foreach (var name in suggestion.ValidationRuleNames)
        {
            if (validNames.Contains(name)) keptNames.Add(name);
            else warnings.Add($"Dropped unknown validation rule '{name}'.");
        }

        return suggestion with { ValidationRuleNames = keptNames, Warnings = warnings.Count > 0 ? warnings : null };
    }

    // ==================== 5. Report query suggestions ====================

    public async Task<AiReportQuerySuggestionDto> SuggestReportQueryAsync(AiSuggestReportQueryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PrimaryTable))
            throw new ArgumentException("primaryTable is required.");

        var schemaJson = JsonSerializer.Serialize(new
        {
            primaryTable = req.PrimaryTable,
            joins = req.Joins,
            availableColumns = req.AvailableColumns
        });

        var systemPrompt = """
            You translate a plain-English report request into a partial query configuration
            for a visual report builder. You are given the tables already chosen (primaryTable
            + joins) and the exact column names available on each. Return ONLY a JSON object
            (no markdown fences, no prose) matching exactly:

            {
              "columns": [
                { "table": string, "column": string, "alias": string, "aggregation": string }
                // aggregation is one of: "", "COUNT", "SUM", "AVG", "MIN", "MAX"
              ],
              "filters": [
                { "table": string, "column": string, "operator": string, "value": string }
                // operator is one of: eq, neq, gt, gte, lt, lte, contains, startswith
              ],
              "groupBy": string[],     // column names (unqualified) that appear in an aggregated column list
              "orderBy": [
                { "table": string, "column": string, "direction": string }  // direction: ASC or DESC
              ],
              "topN": number,          // row limit, default 1000 if the user didn't ask for a specific count
              "reason": string         // one short sentence describing what you built
            }

            Rules:
            - "table" and "column" values MUST come only from the availableColumns map provided — never invent a name.
            - Only include groupBy when you used an aggregation in columns.
            - Keep the columns list focused (typically 2-8) unless the prompt asks for "everything".
            """;

        var userMessage = $"Schema:\n{schemaJson}\n\nRequest: {req.Prompt}";
        var (text, _) = await CallClaudeAsync(systemPrompt, userMessage, 1500);
        var result = ParseJson<AiReportQuerySuggestionDto>(text);

        bool ColumnValid(string table, string column) =>
            req.AvailableColumns.TryGetValue(table, out var cols) && cols.Contains(column, StringComparer.OrdinalIgnoreCase);

        var warnings = new List<string>();

        var cleanColumns = result.Columns.Where(c =>
        {
            var ok = ColumnValid(c.Table, c.Column);
            if (!ok) warnings.Add($"Dropped unknown column '{c.Table}.{c.Column}'.");
            return ok;
        }).ToList();

        var cleanFilters = result.Filters.Where(f =>
        {
            var ok = ColumnValid(f.Table, f.Column);
            if (!ok) warnings.Add($"Dropped filter on unknown column '{f.Table}.{f.Column}'.");
            return ok;
        }).ToList();

        var cleanOrderBy = result.OrderBy.Where(o =>
        {
            var ok = ColumnValid(o.Table, o.Column);
            if (!ok) warnings.Add($"Dropped sort on unknown column '{o.Table}.{o.Column}'.");
            return ok;
        }).ToList();

        return result with
        {
            Columns = cleanColumns,
            Filters = cleanFilters,
            OrderBy = cleanOrderBy,
            TopN = Math.Clamp(result.TopN <= 0 ? 1000 : result.TopN, 1, 50000),
            Warnings = warnings.Count > 0 ? warnings : null
        };
    }

    // ==================== 6. Dashboard summary ====================

    public async Task<AiDashboardSummaryDto> SummarizeDashboardAsync(AiSummarizeDashboardRequest req)
    {
        if (req.Widgets.Count == 0)
            throw new ArgumentException("No widget data to summarize.");

        var widgetsJson = JsonSerializer.Serialize(req.Widgets.Select(w => new { w.Title, w.WidgetType, Data = w.Data }));

        var systemPrompt = """
            You write a short, plain-English summary of a business dashboard given its
            widgets' current data (KPIs, chart series, table samples). Return ONLY a JSON
            object (no markdown fences, no prose) matching exactly:

            {
              "summary": string,      // 2-4 sentences, plain language, no jargon
              "insights": string[]    // 3-6 short bullet points calling out specific numbers,
                                       // trends, or outliers you can see in the data
            }

            Be concrete — cite actual figures from the data rather than vague language.
            Do not invent numbers that aren't present in the provided widget data.
            """;

        var userMessage = $"Dashboard: {req.DashboardName}\n\nWidgets:\n{widgetsJson}";
        var (text, _) = await CallClaudeAsync(systemPrompt, userMessage, 1000);
        return ParseJson<AiDashboardSummaryDto>(text);
    }

    // ==================== 7. Cross-cutting assistant ====================

    public async Task<AiAssistantResponseDto> AssistantAsync(AiAssistantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            throw new ArgumentException("Message is required.");

        var tools = new object[]
        {
            new {
                name = "navigate_to_view",
                description = "Switch the app's active view/screen.",
                input_schema = new {
                    type = "object",
                    properties = new {
                        view = new { type = "string", @enum = new[] {
                            "forms","datasources","controltypes","connections","analytics","reports","sp-reports","api-manager"
                        } }
                    },
                    required = new[] { "view" }
                }
            },
            new {
                name = "open_form_by_name",
                description = "Open an existing form's designer, matched fuzzily by name.",
                input_schema = new {
                    type = "object",
                    properties = new { name = new { type = "string" } },
                    required = new[] { "name" }
                }
            },
            new {
                name = "open_ai_form_generator",
                description = "Open the AI form generator dialog so the user can describe a new form to create.",
                input_schema = new { type = "object", properties = new { } }
            },
            new {
                name = "optimize_current_form_layout",
                description = "Open the AI layout-suggestion dialog for whichever form is currently open in the designer.",
                input_schema = new { type = "object", properties = new { } }
            },
            new {
                name = "create_form",
                description = "Create a new empty form with the given name and open its designer.",
                input_schema = new {
                    type = "object",
                    properties = new { name = new { type = "string" } },
                    required = new[] { "name" }
                }
            }
        };

        var systemPrompt = """
            You are an in-app assistant for a drag-and-drop form builder tool. Help the user
            navigate and perform actions using the tools available to you. When you call a
            tool, ALSO include a short (one sentence) message describing what you're doing —
            never call a tool silently. If no tool applies, just answer conversationally;
            you can explain what the app can do (forms, data sources, connections, analytics,
            reports, stored-procedure reports, API manager) without calling any tool.
            """;

        // NOTE: this call only sends req.Message as the latest user turn — full History
        // replay across multiple Anthropic message objects is left as a follow-up if true
        // multi-turn context is needed; today the assistant reasons on the latest message alone.
        var (text, toolUse) = await CallClaudeAsync(systemPrompt, req.Message, 500, tools);

        AiAssistantActionDto? action = null;
        if (toolUse is { } tu)
        {
            var name = tu.GetProperty("name").GetString() ?? "";
            var input = tu.GetProperty("input");
            var paramsDict = new Dictionary<string, object>();
            foreach (var prop in input.EnumerateObject())
            {
                paramsDict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True or JsonValueKind.False => prop.Value.GetBoolean(),
                    _ => prop.Value.ToString()
                };
            }
            action = new AiAssistantActionDto(name, paramsDict);
        }

        var reply = text;
        if (string.IsNullOrWhiteSpace(reply) && action is not null) reply = "On it.";
        if (string.IsNullOrWhiteSpace(reply)) reply = "Sorry, I didn't catch that — could you rephrase?";

        return new AiAssistantResponseDto(reply, action);
    }
}
