using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FormBuilder.API.Data;
using FormBuilder.API.DTOs;
using FormBuilder.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.API.Services;

public interface IApiManagerService
{
    // Environments
    Task<List<ApiEnvironmentDto>> GetEnvironmentsAsync();
    Task<ApiEnvironmentDto> SaveEnvironmentAsync(int? id, SaveApiEnvironmentDto dto);
    Task<bool> DeleteEnvironmentAsync(int id);

    // Collections
    Task<List<ApiCollectionDto>> GetCollectionsAsync();
    Task<ApiCollectionDto?> GetCollectionByIdAsync(int id);
    Task<ApiCollectionDto> SaveCollectionAsync(int? id, SaveApiCollectionDto dto);
    Task<bool> DeleteCollectionAsync(int id);

    // Folders
    Task<ApiFolderDto> SaveFolderAsync(int? id, SaveApiFolderDto dto);
    Task<bool> DeleteFolderAsync(int id);

    // Requests
    Task<ApiRequestDto?> GetRequestByIdAsync(int id);
    Task<ApiRequestDto> SaveRequestAsync(int? id, SaveApiRequestDto dto);
    Task<bool> DeleteRequestAsync(int id);

    // Send
    Task<ApiResponseDto> SendRequestAsync(SendApiRequestDto dto);

    // History
    Task<List<ApiRequestHistoryDto>> GetHistoryAsync(int requestId);
    Task<ApiRequestHistory?> GetHistoryDetailAsync(int historyId);
}

public class ApiManagerService : IApiManagerService
{
    private readonly FormBuilderDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public ApiManagerService(FormBuilderDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    // ?? Environments ?????????????????????????????????????????????????????

    public async Task<List<ApiEnvironmentDto>> GetEnvironmentsAsync() =>
        await _db.ApiEnvironments
            .Where(e => e.IsActive)
            .OrderBy(e => e.SortOrder)
            .Select(e => new ApiEnvironmentDto(e.Id, e.Name, e.Variables, e.SortOrder, e.IsActive))
            .ToListAsync();

    public async Task<ApiEnvironmentDto> SaveEnvironmentAsync(int? id, SaveApiEnvironmentDto dto)
    {
        ApiEnvironment entity;
        if (id.HasValue)
        {
            entity = await _db.ApiEnvironments.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Environment {id} not found.");
        }
        else
        {
            entity = new ApiEnvironment();
            _db.ApiEnvironments.Add(entity);
        }

        entity.Name      = dto.Name;
        entity.Variables  = dto.Variables;
        entity.SortOrder  = dto.SortOrder;
        entity.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new ApiEnvironmentDto(entity.Id, entity.Name, entity.Variables, entity.SortOrder, entity.IsActive);
    }

    public async Task<bool> DeleteEnvironmentAsync(int id)
    {
        var e = await _db.ApiEnvironments.FindAsync(id);
        if (e == null) return false;
        e.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // ?? Collections ??????????????????????????????????????????????????????

    public async Task<List<ApiCollectionDto>> GetCollectionsAsync()
    {
        var collections = await _db.ApiCollections
            .Where(c => c.IsActive)
            .Include(c => c.Folders.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.Requests.OrderBy(r => r.SortOrder))
            .Include(c => c.Folders.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.SubFolders.OrderBy(sf => sf.SortOrder))
                    .ThenInclude(sf => sf.Requests.OrderBy(r => r.SortOrder))
            .Include(c => c.Requests.Where(r => r.FolderId == null).OrderBy(r => r.SortOrder))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return collections.Select(MapCollection).ToList();
    }

    public async Task<ApiCollectionDto?> GetCollectionByIdAsync(int id)
    {
        var c = await _db.ApiCollections
            .Include(c => c.Folders.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.Requests.OrderBy(r => r.SortOrder))
            .Include(c => c.Folders.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.SubFolders.OrderBy(sf => sf.SortOrder))
                    .ThenInclude(sf => sf.Requests.OrderBy(r => r.SortOrder))
            .Include(c => c.Requests.Where(r => r.FolderId == null).OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        return c == null ? null : MapCollection(c);
    }

    public async Task<ApiCollectionDto> SaveCollectionAsync(int? id, SaveApiCollectionDto dto)
    {
        ApiCollection entity;
        if (id.HasValue)
        {
            entity = await _db.ApiCollections.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Collection {id} not found.");
        }
        else
        {
            entity = new ApiCollection();
            _db.ApiCollections.Add(entity);
        }

        entity.Name        = dto.Name;
        entity.Description = dto.Description;
        entity.SortOrder   = dto.SortOrder;
        entity.UpdatedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (await GetCollectionByIdAsync(entity.Id))!;
    }

    public async Task<bool> DeleteCollectionAsync(int id)
    {
        var c = await _db.ApiCollections.FindAsync(id);
        if (c == null) return false;
        c.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // ?? Folders ??????????????????????????????????????????????????????????

    public async Task<ApiFolderDto> SaveFolderAsync(int? id, SaveApiFolderDto dto)
    {
        ApiFolder entity;
        if (id.HasValue)
        {
            entity = await _db.ApiFolders.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Folder {id} not found.");
        }
        else
        {
            entity = new ApiFolder();
            _db.ApiFolders.Add(entity);
        }

        entity.CollectionId   = dto.CollectionId;
        entity.ParentFolderId = dto.ParentFolderId;
        entity.Name           = dto.Name;
        entity.SortOrder      = dto.SortOrder;
        await _db.SaveChangesAsync();

        return new ApiFolderDto(entity.Id, entity.CollectionId, entity.ParentFolderId,
            entity.Name, entity.SortOrder, [], []);
    }

    public async Task<bool> DeleteFolderAsync(int id)
    {
        var f = await _db.ApiFolders.FindAsync(id);
        if (f == null) return false;
        _db.ApiFolders.Remove(f);
        await _db.SaveChangesAsync();
        return true;
    }

    // ?? Requests ?????????????????????????????????????????????????????????

    public async Task<ApiRequestDto?> GetRequestByIdAsync(int id)
    {
        var r = await _db.ApiRequests.FirstOrDefaultAsync(r => r.Id == id);
        return r == null ? null : MapRequest(r);
    }

    public async Task<ApiRequestDto> SaveRequestAsync(int? id, SaveApiRequestDto dto)
    {
        ApiRequest entity;
        if (id.HasValue)
        {
            entity = await _db.ApiRequests.FindAsync(id.Value)
                ?? throw new KeyNotFoundException($"Request {id} not found.");
        }
        else
        {
            entity = new ApiRequest();
            _db.ApiRequests.Add(entity);
        }

        entity.CollectionId = dto.CollectionId;
        entity.FolderId     = dto.FolderId;
        entity.Name         = dto.Name;
        entity.Method       = dto.Method;
        entity.Url          = dto.Url;
        entity.Headers      = dto.Headers;
        entity.QueryParams  = dto.QueryParams;
        entity.AuthType     = dto.AuthType;
        entity.AuthConfig   = dto.AuthConfig;
        entity.Body         = dto.Body;
        entity.BodyType     = dto.BodyType;
        entity.SortOrder    = dto.SortOrder;
        entity.UpdatedAt    = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapRequest(entity);
    }

    public async Task<bool> DeleteRequestAsync(int id)
    {
        var r = await _db.ApiRequests.FindAsync(id);
        if (r == null) return false;
        _db.ApiRequests.Remove(r);
        await _db.SaveChangesAsync();
        return true;
    }

    // ?? Send ?????????????????????????????????????????????????????????????

    public async Task<ApiResponseDto> SendRequestAsync(SendApiRequestDto dto)
    {
        // 1. Resolve environment variables
        var variables = new Dictionary<string, string>();
        if (dto.EnvironmentId.HasValue)
        {
            var env = await _db.ApiEnvironments.FindAsync(dto.EnvironmentId.Value);
            if (env != null)
            {
                try
                {
                    variables = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        env.Variables, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new();
                }
                catch { }
            }
        }

        string Resolve(string? input) =>
            variables.Aggregate(input ?? "", (s, kv) => s.Replace($"{{{{{kv.Key}}}}}", kv.Value));

        // 2. Build URL with query params
        var url = Resolve(dto.Url);
        if (!string.IsNullOrWhiteSpace(dto.QueryParams))
        {
            try
            {
                var qps = JsonSerializer.Deserialize<List<KeyValueEnabled>>(dto.QueryParams,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var enabled = qps?.Where(q => q.Enabled).ToList();
                if (enabled is { Count: > 0 })
                {
                    var qs = string.Join("&", enabled.Select(q =>
                        $"{Uri.EscapeDataString(Resolve(q.Key))}={Uri.EscapeDataString(Resolve(q.Value))}"));
                    url += (url.Contains('?') ? "&" : "?") + qs;
                }
            }
            catch { }
        }

        // 3. Build HTTP request
        var client = _httpFactory.CreateClient();
        var httpReq = new HttpRequestMessage(new HttpMethod(dto.Method), url);

        // Headers (skip Content-Type � it's set automatically by the content)
        if (!string.IsNullOrWhiteSpace(dto.Headers))
        {
            try
            {
                var hdrs = JsonSerializer.Deserialize<List<KeyValueEnabled>>(dto.Headers,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                foreach (var h in hdrs?.Where(h => h.Enabled) ?? [])
                {
                    if (!string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                        httpReq.Headers.TryAddWithoutValidation(Resolve(h.Key), Resolve(h.Value));
                }
            }
            catch { }
        }

        // Auth
        if (!string.IsNullOrWhiteSpace(dto.AuthType) &&
            !string.Equals(dto.AuthType, "None", StringComparison.OrdinalIgnoreCase))
        {
            ApplyAuth(httpReq, dto.AuthType, Resolve(dto.AuthConfig ?? ""));
        }

        // Body
        if (!string.IsNullOrWhiteSpace(dto.Body) && dto.Method is not "GET" and not "HEAD")
        {
            if (IsFormBodyType(dto.BodyType))
            {
                // Form body: build FormUrlEncodedContent
                httpReq.Content = BuildFormContent(Resolve(dto.Body));
            }
            else
            {
                // JSON or raw body
                httpReq.Content = new StringContent(
                    Resolve(dto.Body), Encoding.UTF8, "application/json");
            }
        }

        // 4. Execute
        var sw = Stopwatch.StartNew();
        HttpResponseMessage httpResp;
        try
        {
            httpResp = await client.SendAsync(httpReq);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ApiResponseDto(0, ex.Message, "", "", 0, (int)sw.ElapsedMilliseconds);
        }
        sw.Stop();

        var respBody = await httpResp.Content.ReadAsStringAsync();
        var respHeaders = JsonSerializer.Serialize(
            httpResp.Headers.Concat(httpResp.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

        var result = new ApiResponseDto(
            (int)httpResp.StatusCode,
            httpResp.ReasonPhrase ?? "",
            respHeaders,
            respBody,
            Encoding.UTF8.GetByteCount(respBody),
            (int)sw.ElapsedMilliseconds);

        // 5. Save history
        if (dto.RequestId.HasValue)
        {
            _db.ApiRequestHistory.Add(new ApiRequestHistory
            {
                RequestId         = dto.RequestId.Value,
                StatusCode        = result.StatusCode,
                ResponseHeaders   = respHeaders,
                ResponseBody      = respBody,
                ResponseSizeBytes = result.ResponseSizeBytes,
                DurationMs        = result.DurationMs
            });
            await _db.SaveChangesAsync();
        }

        return result;
    }

    // ?? History ??????????????????????????????????????????????????????????

    public async Task<List<ApiRequestHistoryDto>> GetHistoryAsync(int requestId) =>
        await _db.ApiRequestHistory
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ExecutedAt)
            .Take(50)
            .Select(h => new ApiRequestHistoryDto(
                h.Id, h.RequestId, h.StatusCode,
                h.ResponseSizeBytes, h.DurationMs, h.ExecutedAt))
            .ToListAsync();

    public async Task<ApiRequestHistory?> GetHistoryDetailAsync(int historyId) =>
        await _db.ApiRequestHistory.FindAsync(historyId);

    // ?? Helpers ??????????????????????????????????????????????????????????

    private static void ApplyAuth(HttpRequestMessage req, string authType, string authConfigJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(authConfigJson);
            var root = doc.RootElement;

            switch (authType.ToLower())
            {
                case "bearer":
                    if (root.TryGetProperty("token", out var tok))
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok.GetString());
                    break;
                case "apikey":
                    var headerName = root.TryGetProperty("headerName", out var hn) ? hn.GetString() ?? "X-Api-Key" : "X-Api-Key";
                    var keyValue = root.TryGetProperty("value", out var kv) ? kv.GetString() ?? "" : "";
                    req.Headers.TryAddWithoutValidation(headerName, keyValue);
                    break;
                case "basic":
                    var user = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                    var pass = root.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
                    break;
            }
        }
        catch { }
    }

    private static ApiCollectionDto MapCollection(ApiCollection c) => new(
        c.Id, c.Name, c.Description, c.SortOrder, c.IsActive,
        c.Folders.Where(f => f.ParentFolderId == null).OrderBy(f => f.SortOrder).Select(MapFolder).ToList(),
        c.Requests.Where(r => r.FolderId == null).OrderBy(r => r.SortOrder).Select(MapRequestSummary).ToList());

    private static ApiFolderDto MapFolder(ApiFolder f) => new(
        f.Id, f.CollectionId, f.ParentFolderId, f.Name, f.SortOrder,
        f.SubFolders.OrderBy(sf => sf.SortOrder).Select(MapFolder).ToList(),
        f.Requests.OrderBy(r => r.SortOrder).Select(MapRequestSummary).ToList());

    private static ApiRequestSummaryDto MapRequestSummary(ApiRequest r) => new(
        r.Id, r.CollectionId, r.FolderId, r.Name, r.Method, r.Url, r.SortOrder);

    private static ApiRequestDto MapRequest(ApiRequest r) => new(
        r.Id, r.CollectionId, r.FolderId, r.Name, r.Method, r.Url,
        r.Headers, r.QueryParams, r.AuthType, r.AuthConfig,
        r.Body, r.BodyType, r.SortOrder, r.UpdatedAt);

    private class KeyValueEnabled
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Enabled { get; set; } = true;
    }

    private static bool IsFormBodyType(string? bodyType)
    {
        if (string.IsNullOrWhiteSpace(bodyType)) return false;
        var bt = bodyType.Trim().ToLower();
        return bt is "form" or "urlencoded" or "x-www-form-urlencoded"
            or "formdata" or "form-data" or "form-urlencoded";
    }

    private static HttpContent BuildFormContent(string body)
    {
        var pairs = new List<KeyValuePair<string, string>>();

        // Try 1: JSON array  [{"key":"grant_type","value":"client_credentials","enabled":true}]
        if (body.TrimStart().StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<KeyValueEnabled>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    pairs = items.Where(i => i.Enabled)
                        .Select(i => new KeyValuePair<string, string>(i.Key, i.Value))
                        .ToList();
                    return new FormUrlEncodedContent(pairs);
                }
            }
            catch { }
        }

        // Try 2: JSON object  {"grant_type":"client_credentials","client_id":"xxx"}
        if (body.TrimStart().StartsWith('{'))
        {
            try
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (obj != null)
                {
                    pairs = obj.Select(kv =>
                        new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                        .ToList();
                    return new FormUrlEncodedContent(pairs);
                }
            }
            catch { }
        }

        // Try 3: Raw form string  "grant_type=client_credentials&client_id=xxx"
        pairs = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part =>
            {
                var eqIdx = part.IndexOf('=');
                return eqIdx >= 0
                    ? new KeyValuePair<string, string>(
                        Uri.UnescapeDataString(part[..eqIdx]),
                        Uri.UnescapeDataString(part[(eqIdx + 1)..]))
                    : new KeyValuePair<string, string>(Uri.UnescapeDataString(part), "");
            })
            .ToList();

        return new FormUrlEncodedContent(pairs);
    }
}
