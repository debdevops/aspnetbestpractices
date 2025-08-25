namespace Api.Services;

public interface IDownstreamClient
{
    Task<string> GetStatusAsync(CancellationToken ct = default);
}

public sealed class DownstreamClient : IDownstreamClient
{
    private readonly HttpClient _http;
    public DownstreamClient(HttpClient http) => _http = http;

    public async Task<string> GetStatusAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("status", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
