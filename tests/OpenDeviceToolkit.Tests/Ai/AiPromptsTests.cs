using OpenDeviceToolkit.Core.Ai;
using OpenDeviceToolkit.Core.Research;
using Xunit;

namespace OpenDeviceToolkit.Tests.Ai;

public class AiPromptsTests
{
    private const string ValidJson =
        "{\"summary\":\"Identify the UART console\",\"warnings\":[\"Board voltage unverified\"],\"actions\":[{\"description\":\"Listen passively on the suspected TX pin\",\"mode\":\"UART\",\"risk\":\"ReadOnly\",\"parameters\":{\"baud\":\"115200\"}}]}";

    [Fact]
    public void Parse_ValidJson_MapsAllFields()
    {
        var rec = AiPrompts.ParseRecommendation(ValidJson, AiProviderKind.Mistral);
        Assert.Equal("Identify the UART console", rec.Summary);
        Assert.Single(rec.Warnings);
        var action = Assert.Single(rec.Actions);
        Assert.Equal("UART", action.Mode);
        Assert.Equal(RiskLevel.ReadOnly, action.Risk);
        Assert.Equal(AiActionStatus.Proposed, action.Status);
        Assert.Equal("115200", action.Parameters["baud"]);
        Assert.Equal(AiProviderKind.Mistral, rec.Provider);
    }

    [Fact]
    public void Parse_FencedJson_IsStillParsed()
    {
        // Build the markdown fence without literal backticks in source.
        var fence = new string((char)0x60, 3);
        var fenced = fence + "json\n" + ValidJson + "\n" + fence;
        var rec = AiPrompts.ParseRecommendation(fenced, AiProviderKind.OpenRouter);
        Assert.Equal("Identify the UART console", rec.Summary);
        Assert.Single(rec.Actions);
    }

    [Fact]
    public void Parse_UnknownRisk_MarksActionMalformed()
    {
        var json = "{\"summary\":\"s\",\"warnings\":[],\"actions\":[{\"description\":\"d\",\"mode\":\"UART\",\"risk\":\"GlitchAttack\"}]}";
        var rec = AiPrompts.ParseRecommendation(json, AiProviderKind.Mistral);
        var action = Assert.Single(rec.Actions);
        Assert.Equal(AiActionStatus.Malformed, action.Status);
        Assert.NotEmpty(action.StatusReason);
    }

    [Fact]
    public void Parse_UnknownMode_MarksActionMalformed()
    {
        var json = "{\"summary\":\"s\",\"warnings\":[],\"actions\":[{\"description\":\"d\",\"mode\":\"LaserCannon\",\"risk\":\"ReadOnly\"}]}";
        var rec = AiPrompts.ParseRecommendation(json, AiProviderKind.Mistral);
        Assert.Equal(AiActionStatus.Malformed, Assert.Single(rec.Actions).Status);
    }

    [Fact]
    public void Parse_NonJsonResponse_ProducesUnparseableRecommendation()
    {
        var rec = AiPrompts.ParseRecommendation("Sorry, I cannot answer that.", AiProviderKind.Mistral);
        Assert.Empty(rec.Actions);
        Assert.Contains(rec.Warnings, w => w.Contains("parse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_EmptyActionArray_IsValid()
    {
        var json = "{\"summary\":\"Nothing safe to try yet\",\"warnings\":[],\"actions\":[]}";
        var rec = AiPrompts.ParseRecommendation(json, AiProviderKind.Mistral);
        Assert.Empty(rec.Actions);
        Assert.Equal("Nothing safe to try yet", rec.Summary);
    }

    [Fact]
    public void ExtractJsonObject_ReturnsNullWithoutBraces()
    {
        Assert.Null(AiPrompts.ExtractJsonObject("no braces here"));
    }

    [Fact]
    public void ExtractJsonObject_ExtractsAroundSurroundingText()
    {
        var extracted = AiPrompts.ExtractJsonObject("Sure! Here it is: {\"a\":1} hope that helps");
        Assert.Equal("{\"a\":1}", extracted);
    }
}
