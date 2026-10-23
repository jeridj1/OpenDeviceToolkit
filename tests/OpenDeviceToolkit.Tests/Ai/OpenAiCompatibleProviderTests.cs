using System.Net.Http;
using System.Text;
using System.Text.Json;
using OpenDeviceToolkit.Core.Ai;
using Xunit;

namespace OpenDeviceToolkit.Tests.Ai;

public class OpenAiCompatibleProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public StubHandler(HttpResponseMessage response) => _response = response;
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    private static string Completion(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content } } } });

    private static HttpResponseMessage JsonResponse(string body) =>
        new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task Analyze_ParsesCompletion_AndSendsBearerKey()
    {
        var inner = JsonSerializer.Serialize(new
        {
            summary = "ok",
            warnings = Array.Empty<string>(),
            actions = Array.Empty<object>()
        });
        var handler = new StubHandler(JsonResponse(Completion(inner)));
        var provider = new OpenAiCompatibleProvider(
            AiProviderKind.Mistral, "Mistral",
            "https://api.mistral.ai/v1/chat/completions", "mistral-small-latest",
            () => "test-key", new HttpClient(handler));

        var rec = await provider.AnalyzeAsync(new AiRequest { Objective = "identify chip" });

        Assert.Equal("ok", rec.Summary);
        Assert.Equal(AiProviderKind.Mistral, rec.Provider);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("test-key", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public void Provider_WithoutKey_IsNotConfigured()
    {
        var provider = new OpenAiCompatibleProvider(
            AiProviderKind.OpenAi, "OpenAI",
            "https://api.openai.com/v1/chat/completions", "gpt-4o-mini",
            () => null, new HttpClient(new StubHandler(JsonResponse("{}"))));

        Assert.False(provider.IsConfigured);
    }

    [Fact]
    public async Task Analyze_MissingKey_ThrowsProviderException()
    {
        var provider = new OpenAiCompatibleProvider(
            AiProviderKind.OpenRouter, "OpenRouter",
            "https://openrouter.ai/api/v1/chat/completions", "m",
            () => null, new HttpClient(new StubHandler(JsonResponse("{}"))));

        await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeAsync(new AiRequest { Objective = "x" }));
    }

    [Fact]
    public async Task Analyze_ErrorStatus_ThrowsWithStatusCode()
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"boom\"}", Encoding.UTF8, "application/json")
        };
        var provider = new OpenAiCompatibleProvider(
            AiProviderKind.Mistral, "Mistral",
            "https://api.mistral.ai/v1/chat/completions", "m",
            () => "k", new HttpClient(new StubHandler(response)));

        var ex = await Assert.ThrowsAsync<AiProviderException>(
            () => provider.AnalyzeAsync(new AiRequest { Objective = "x" }));

        Assert.Contains("500", ex.Message);
    }
}
