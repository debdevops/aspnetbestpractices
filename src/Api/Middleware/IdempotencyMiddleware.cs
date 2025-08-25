using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Middleware;

public sealed class IdempotencyMiddleware : IMiddleware
{
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(12);
    private const int MaxKeyLength = 128;
    private const int MaxCacheBytes = 256 * 1024;
    private const int MaxBodyToHashBytes = 512 * 1024;

    private static readonly MemoryCacheEntryOptions LockOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) };
    private const string LockPrefix = "idem:lock:";

    public IdempotencyMiddleware(IMemoryCache cache) => _cache = cache;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyVals))
        {
            await next(context);
            return;
        }

        var key = keyVals.ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
        {
            await WriteProblemAsync(context, 400, "Invalid Idempotency-Key", "Idempotency-Key must be present and ≤ 128 characters.");
            return;
        }

        if (!Guid.TryParse(key, out _))
        {
            await WriteProblemAsync(context, 400, "Invalid Idempotency-Key", "Idempotency-Key must be a GUID.");
            return;
        }

        if (!IsJsonContentType(context.Request.ContentType))
        {
            await next(context);
            return;
        }

        var (bodyString, bodyHash) = await ReadBodyAndHashAsync(context.Request, MaxBodyToHashBytes);
        var cacheKey = BuildCacheKey(context.Request.Path, key, bodyHash);

        if (_cache.TryGetValue(cacheKey, out CachedResponse? cached) && cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            if (!string.IsNullOrEmpty(cached.ETag))
                context.Response.Headers.ETag = cached.ETag!;
            context.Response.Headers["Idempotency-Cache"] = "hit";
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        var lockKey = LockPrefix + cacheKey;
        if (!_cache.TryGetValue(lockKey, out SemaphoreSlim? keyLock) || keyLock is null)
        {
            keyLock = new SemaphoreSlim(1, 1);
            _cache.Set(lockKey, keyLock, LockOptions);
        }

        await keyLock.WaitAsync(context.RequestAborted);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                context.Response.StatusCode = cached.StatusCode;
                if (!string.IsNullOrEmpty(cached.ETag))
                    context.Response.Headers.ETag = cached.ETag!;
                context.Response.Headers["Idempotency-Cache"] = "hit";
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cached.Body);
                return;
            }

            var originalBody = context.Response.Body;
            await using var mem = new MemoryStream();
            context.Response.Body = mem;

            context.Response.Headers["Idempotency-Cache"] = "miss";
            await next(context);

            mem.Position = 0;
            var responseBody = await new StreamReader(mem, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true).ReadToEndAsync();
            mem.Position = 0;
            await mem.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var payloadBytes = Encoding.UTF8.GetByteCount(responseBody);
                if (payloadBytes <= MaxCacheBytes)
                {
                    var etagVal = context.Response.Headers.ETag.ToString();
                    var toCache = new CachedResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        Body = responseBody,
                        ETag = string.IsNullOrWhiteSpace(etagVal) ? null : etagVal
                    };

                    _cache.Set(cacheKey, toCache, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = Ttl
                    });
                }
            }
        }
        finally
        {
            keyLock.Release();
            _cache.Set(lockKey, keyLock, LockOptions);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new { type = $"https://httpstatuses.io/{status}", title, status, detail });
    }

    private static string BuildCacheKey(PathString path, string key, string bodyHash)
        => $"idem:{path.ToString().ToLowerInvariant()}|{key}|{bodyHash}";

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || (contentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase)
                && contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(string body, string hash)> ReadBodyAndHashAsync(HttpRequest request, int maxBytes)
    {
        request.EnableBuffering();

        using var ms = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int total = 0;
        while (total < maxBytes)
        {
            var toRead = Math.Min(buffer.Length, maxBytes - total);
            var read = await request.Body.ReadAsync(buffer.AsMemory(0, toRead));
            if (read <= 0) break;
            await ms.WriteAsync(buffer.AsMemory(0, read));
            total += read;
        }

        request.Body.Position = 0;

        var bytes = ms.ToArray();
        var bodyString = Encoding.UTF8.GetString(bytes);
        var sha = SHA256.HashData(bytes);
        var hash = Convert.ToHexString(sha);

        return (bodyString, hash);
    }

    private sealed class CachedResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ETag { get; set; }
    }
}
