namespace Api.Middleware;

public sealed class JsonOnlyMiddleware : IMiddleware
{
    private static bool MethodNormallyHasBody(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (MethodNormallyHasBody(context.Request.Method) && (context.Request.ContentLength ?? 0) > 0)
        {
            var ct = context.Request.ContentType ?? string.Empty;
            var isJson =
                ct.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
                (ct.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
                 ct.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

            if (!isJson)
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.io/415",
                    title = "Unsupported Media Type",
                    status = 415,
                    detail = "Only application/json is supported."
                });
                return;
            }
        }

        await next(context);
    }
}
