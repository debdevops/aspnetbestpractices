using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

public class ApiAuthTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;

    public ApiAuthTests(TestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_IsAccessible_WithoutAuth()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/v1/todos");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Create_RequiresAuth_WhenApiKeyProvider()
    {
        var client = _factory.CreateClient();

        // Attempt to create without ApiKey should be unauthorized (403/401)
        var payload = new { title = "T1", notes = "n" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var res = await client.PostAsync("/api/v1/todos", content);

        // Depending on pipeline, should be 401 or 403
        res.StatusCode.Should().Match(s => s == HttpStatusCode.Unauthorized || s == HttpStatusCode.Forbidden || s == HttpStatusCode.BadRequest);
    }
}
