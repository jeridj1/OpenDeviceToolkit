using System.Text.Json;
using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Ai;
using OpenDeviceToolkit.Core.Research;
using Xunit;

namespace OpenDeviceToolkit.Tests.Ai;

public class AiRouterTests
{
    private sealed class FakeProvider : IAiProvider
    {
        public AiProviderKind Kind { get; set; } = AiProviderKind.Mistral;
        public string DisplayName { get; set; } = "Fake provider";
        public bool IsConfigured { get; set; } = true;
        public bool Fail { get; set; }
        public Func<AiRequest, AiRecommendation>? Handler { get; set; }
        public int Calls { get; private set; }

        public Task<AiRecommendation> AnalyzeAsync(AiRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fail)
                throw new AiProviderException(DisplayName + " is unavailable");
            return Task.FromResult(Handler is null ? new AiRecommendation() : Handler(request));
        }
    }

    private static AiRecommendation Recommendation(params AiProposedAction[] actions)
    {
        var rec = new AiRecommendation { Summary = "done" };
        rec.Actions.AddRange(actions);
        return rec;
    }

    [Fact]
    public async Task Analyze_FallsBackToNextProviderOnFailure()
    {
        var failing = new FakeProvider { Fail = true };
        var working = new FakeProvider { Handler = _ => Recommendation() };
        var router = new AiRouter(new[] { failing, working });

        var result = await router.AnalyzeAsync(new AiRequest { Objective = "identify the chip" });

        Assert.Equal(AiRouterStatus.Ok, result.Status);
        Assert.Equal(1, failing.Calls);
        Assert.Equal(1, working.Calls);
        Assert.NotNull(result.Recommendation);
    }

    [Fact]
    public async Task Analyze_NoConfiguredProvider_ReturnsNoProviderConfigured()
    {
        var router = new AiRouter(new[] { new FakeProvider { IsConfigured = false } });

        var result = await router.AnalyzeAsync(new AiRequest { Objective = "identify the chip" });

        Assert.Equal(AiRouterStatus.NoProviderConfigured, result.Status);
    }

    [Fact]
    public async Task Analyze_AllProvidersFail_ReportsEachError()
    {
        var a = new FakeProvider { Fail = true, DisplayName = "A" };
        var b = new FakeProvider { Fail = true, DisplayName = "B" };
        var router = new AiRouter(new IAiProvider[] { a, b });

        var result = await router.AnalyzeAsync(new AiRequest { Objective = "identify the chip" });

        Assert.Equal(AiRouterStatus.AllProvidersFailed, result.Status);
        Assert.Equal(2, result.ProviderErrors.Count);
    }

    [Fact]
    public async Task Analyze_EmptyRequest_ReturnsInvalidRequest()
    {
        var provider = new FakeProvider();
        var router = new AiRouter(new[] { provider });

        var result = await router.AnalyzeAsync(new AiRequest());

        Assert.Equal(AiRouterStatus.InvalidRequest, result.Status);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Analyze_BlocksActionsAboveAuthorizedEnvelope()
    {
        var provider = new FakeProvider
        {
            Handler = _ => Recommendation(
                new AiProposedAction { Description = "listen passively", Mode = "UART", Risk = RiskLevel.ReadOnly },
                new AiProposedAction { Description = "write to flash", Mode = "SPI", Risk = RiskLevel.PersistentWrite })
        };
        var router = new AiRouter(new[] { provider });
        var request = new AiRequest { Objective = "recover the board", AuthorizedEnvelope = RiskLevel.ReadOnly };

        var result = await router.AnalyzeAsync(request);

        Assert.Equal(AiRouterStatus.Ok, result.Status);
        var actions = result.Recommendation!.Actions;
        Assert.Equal(AiActionStatus.Proposed, actions[0].Status);
        Assert.Equal(AiActionStatus.BlockedByEnvelope, actions[1].Status);
        Assert.NotEmpty(actions[1].StatusReason);
    }

    [Fact]
    public void FromConfig_BuildsProvidersInPriorityOrder()
    {
        var config = new AppConfig();
        config.Ai.ProviderPriority = new List<string> { "Mistral", "Ollama" };

        var router = AiRouter.FromConfig(config);

        Assert.Equal(2, router.Providers.Count);
        Assert.Equal(AiProviderKind.Mistral, router.Providers[0].Kind);
        Assert.Equal(AiProviderKind.Ollama, router.Providers[1].Kind);
    }

    [Fact]
    public void FromConfig_EmptyPriority_BuildsNoProviders()
    {
        var config = new AppConfig();
        config.Ai.ProviderPriority = new List<string>();

        var router = AiRouter.FromConfig(config);

        Assert.Empty(router.Providers);
        Assert.False(router.AnyConfigured);
    }

    [Fact]
    public void AppConfig_ParsesAiSection()
    {
        var json = "{\"ai\":{\"enabled\":false,\"providerPriority\":[\"Mistral\"],\"mistralModel\":\"test-model\"}}";
        var config = JsonSerializer.Deserialize<AppConfig>(json);
        Assert.NotNull(config);
        Assert.False(config!.Ai.Enabled);
        Assert.Equal("test-model", config.Ai.MistralModel);
        Assert.Equal("Mistral", Assert.Single(config.Ai.ProviderPriority));
    }
}
