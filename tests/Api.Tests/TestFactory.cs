using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Api.Tests;

public class TestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var inMemory = new Dictionary<string, string>
            {
                ["Features:EnableAuth"] = "false",
                ["Authentication:Provider"] = "ApiKey",
                ["Authentication:ApiKey"] = "test-key"
            };
            cfg.AddInMemoryCollection(inMemory);
        });
    }
}
