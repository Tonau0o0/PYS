using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PYS.Client.Models;

namespace PYS.Client.Services;

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string[] Details { get; }

    public ApiException(HttpStatusCode statusCode, string message, string[]? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details ?? Array.Empty<string>();
    }
}

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.GetAsync(path, ct);
        return await ReadAsync<T>(res, ct);
    }

    public async Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.PostAsJsonAsync(path, body, JsonOpts, ct);
        return await ReadAsync<T>(res, ct);
    }

    public async Task PostAsync(string path, object body, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.PostAsJsonAsync(path, body, JsonOpts, ct);
        await EnsureSuccessAsync(res, ct);
    }

    public async Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.PutAsJsonAsync(path, body, JsonOpts, ct);
        return await ReadAsync<T>(res, ct);
    }

    public async Task PutAsync(string path, object body, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.PutAsJsonAsync(path, body, JsonOpts, ct);
        await EnsureSuccessAsync(res, ct);
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ApplyAuth();
        var res = await _http.DeleteAsync(path, ct);
        await EnsureSuccessAsync(res, ct);
    }

    /// <summary>multipart/form-data ile tek dosya (+ opsiyonel metin alanları) yükler.</summary>
    public async Task<T> PostFileAsync<T>(string path, Stream content, string fileName,
        IReadOnlyDictionary<string, string>? fields = null, string fieldName = "file", CancellationToken ct = default)
    {
        ApplyAuth();

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, fieldName, fileName);

        if (fields is not null)
            foreach (var kv in fields) form.Add(new StringContent(kv.Value), kv.Key);

        var res = await _http.PostAsync(path, form, ct);
        return await ReadAsync<T>(res, ct);
    }

    private void ApplyAuth()
    {
        var token = _auth.Current?.AccessToken;
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage res, CancellationToken ct)
    {
        await EnsureSuccessAsync(res, ct);
        var data = await res.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        return data ?? throw new ApiException(res.StatusCode, "Empty response body.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;

        var raw = await res.Content.ReadAsStringAsync(ct);
        string? message = null;
        string[]? details = null;

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("error", out var err) && err.ValueKind == System.Text.Json.JsonValueKind.String)
                        message = err.GetString();
                    else if (root.TryGetProperty("title", out var title) && title.ValueKind == System.Text.Json.JsonValueKind.String)
                        message = title.GetString();
                    else if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == System.Text.Json.JsonValueKind.String)
                        message = detail.GetString();

                    if (root.TryGetProperty("details", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Array)
                        details = d.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
                    else if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == System.Text.Json.JsonValueKind.Object)
                        details = errs.EnumerateObject()
                            .SelectMany(p => p.Value.EnumerateArray().Select(v => $"{p.Name}: {v.GetString()}"))
                            .ToArray();
                }
            }
            catch
            {
                message = raw.Length > 256 ? raw[..256] + "..." : raw;
            }
        }

        message ??= res.ReasonPhrase ?? "Request failed.";
        throw new ApiException(res.StatusCode, message, details);
    }
}
