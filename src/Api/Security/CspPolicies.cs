namespace Api.Security;

public sealed class CspOptions
{
    public string DefaultSrc     { get; set; } = "'self'";
    public string ScriptSrc      { get; set; } = "'self'";
    public string StyleSrc       { get; set; } = "'self'";
    public string ImgSrc         { get; set; } = "'self' data:";
    public string FontSrc        { get; set; } = "'self' data:";
    public string ConnectSrc     { get; set; } = "'self'";
    public string FrameAncestors { get; set; } = "'none'";
    public string ObjectSrc      { get; set; } = "'none'";
    public string BaseUri        { get; set; } = "'self'";
    public string FormAction     { get; set; } = "'self'";
    public bool   EnableReportOnly { get; set; } = false;
}

public static class CspPolicies
{
    public static string Build(CspOptions o) =>
        string.Join("; ", new[]
        {
            $"default-src {o.DefaultSrc}",
            $"script-src {o.ScriptSrc}",
            $"style-src {o.StyleSrc}",
            $"img-src {o.ImgSrc}",
            $"font-src {o.FontSrc}",
            $"connect-src {o.ConnectSrc}",
            $"frame-ancestors {o.FrameAncestors}",
            $"object-src {o.ObjectSrc}",
            $"base-uri {o.BaseUri}",
            $"form-action {o.FormAction}"
        });
}
