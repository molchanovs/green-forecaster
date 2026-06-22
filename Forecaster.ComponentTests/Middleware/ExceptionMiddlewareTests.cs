using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Forecaster.ComponentTests.Middleware;

[Collection(ForecasterCollection.Name)]
public class ExceptionMiddlewareTests
{
    private readonly HttpClient _client;

    public ExceptionMiddlewareTests(ForecasterApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WhenUnhandledExceptionIsThrown_Returns500StatusCode()
    {
        var response = await _client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task WhenUnhandledExceptionIsThrown_ReturnsApplicationJsonContentType()
    {
        var response = await _client.GetAsync("/test/throw");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task WhenUnhandledExceptionIsThrown_ReturnsGenericErrorMessage()
    {
        var response = await _client.GetAsync("/test/throw");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var message = body.GetProperty("message").GetString();

        Assert.Equal("An unexpected error occurred.", message);
    }

    [Fact]
    public async Task WhenUnhandledExceptionIsThrown_ReturnsStatusCodeInBody()
    {
        var response = await _client.GetAsync("/test/throw");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var statusCode = body.GetProperty("statusCode").GetInt32();

        Assert.Equal(500, statusCode);
    }

    [Fact]
    public async Task WhenDifferentExceptionTypeIsThrown_StillReturns500WithGenericMessage()
    {
        var response = await _client.GetAsync("/test/throw-argument");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("message").GetString());
    }
}

