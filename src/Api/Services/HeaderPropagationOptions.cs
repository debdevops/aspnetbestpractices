using System.Collections.Generic;

namespace Api.Services;

public class HeaderPropagationOptions
{
    public List<string> HeadersToPropagate { get; set; } = new List<string>
    {
        "traceparent",
        "trace-state",
        "Authorization",
        "X-Request-ID",
        "X-Correlation-ID"
    };
}
