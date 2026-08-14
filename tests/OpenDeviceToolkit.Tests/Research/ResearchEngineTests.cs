using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Research;
using OpenDeviceToolkit.Core.Usb;
using Moq;
using Moq.Protected;

namespace OpenDeviceToolkit.Tests.Research;

public class ResearchEngineTests
{
    private readonly Workspace _workspace = new();
    private readonly AppLogger _logger = new(_workspace);
    private readonly CommandRunner _runner = new();
    private readonly ResearchEngine _engine;
    
    public ResearchEngineTests()
    {
        _engine = new ResearchEngine(_workspace, _logger, _runner);
    }
    
    [Fact]
    public void StartSession_CreatesNewSession()
    {
        var session = _engine.StartSession("test-device", "test objective");
        
        Assert.NotNull(session);
        Assert.Equal("test-device", session.DeviceId);
        Assert.Equal("test objective", session.Objective);
        Assert.Equal(ResearchStatus.InProgress, session.Status);
    }
    
    [Fact]
    public void EndSession_CompletesSession()
    {
        var session = _engine.StartSession("device", "objective");
        _engine.EndSession(ResearchStatus.Completed);
        
        Assert.Null(_engine.CurrentSession);
    }
    
    [Fact]
    public void GenerateHypotheses_CreatesHypothesesFromResults()
    {
        var results = new List<ResearchResult>
        {
            ResearchResult.Create("Test Exploit", "GitHub", "https://example.com", confidence: 0.9, RiskLevel.PotentialBrick),
            ResearchResult.Create("Another Method", "XDA", "https://xda.com", confidence: 0.7, RiskLevel.Reversible)
        };
        
        var hypotheses = _engine.GenerateHypotheses(results);
        
        Assert.Equal(2, hypotheses.Count);
        Assert.All(hypotheses, h => h.Confidence > 0);
    }
    
    [Fact]
    public void CreatePlan_CreatesPlanWithSteps()
    {
        var hypotheses = new List<ResearchHypothesis>
        {
            new("Test 1", RiskLevel.ReadOnly, 0.8, new[] { "evidence1" }),
            new("Test 2", RiskLevel.PersistentWrite, 0.6, new[] { "evidence2" })
        };
        
        var plan = _engine.CreatePlan("device", "objective", hypotheses);
        
        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("device", plan.DeviceId);
        Assert.Equal("objective", plan.Objective);
    }
    
    [Fact]
    public async Task SearchAsync_ReturnsResultsFromSources()
    {
        // This test would need mocking of IResearchSource
        // For now, just verify it doesn't throw
        var results = await _engine.SearchAsync("test query", cancellationToken: default);
        
        // Should return empty list if no sources are available or fail
        Assert.NotNull(results);
    }
    
    [Fact]
    public void RiskLevel_RequiresConfirmation()
    {
        Assert.False(RiskLevel.ReadOnly.RequiresConfirmation());
        Assert.False(RiskLevel.Reversible.RequiresConfirmation());
        Assert.True(RiskLevel.PersistentWrite.RequiresConfirmation());
        Assert.True(RiskLevel.PotentialBrick.RequiresConfirmation());
        Assert.True(RiskLevel.EWasteMode.RequiresConfirmation());
    }
    
    [Fact]
    public void RiskLevel_RequiresDoubleConfirmation()
    {
        Assert.False(RiskLevel.ReadOnly.RequiresDoubleConfirmation());
        Assert.False(RiskLevel.Reversible.RequiresDoubleConfirmation());
        Assert.False(RiskLevel.PersistentWrite.RequiresDoubleConfirmation());
        Assert.True(RiskLevel.PotentialBrick.RequiresDoubleConfirmation());
        Assert.True(RiskLevel.EWasteMode.RequiresDoubleConfirmation());
    }
}

public class ResearchSessionTests
{
    [Fact]
    public void AddHypothesis_AddsToSession()
    {
        var session = new ResearchSession("device", "objective");
        var hypothesis = new ResearchHypothesis("test", RiskLevel.ReadOnly, 0.5, new[] { "evidence" });
        
        session.AddHypothesis(hypothesis);
        
        Assert.Single(session.Hypotheses);
        Assert.Equal(hypothesis, session.Hypotheses[0]);
    }
    
    [Fact]
    public void AddResult_AddsToSession()
    {
        var session = new ResearchSession("device", "objective");
        var result = ResearchResult.Create("test", "source", "url");
        
        session.AddResult(result);
        
        Assert.Single(session.Results);
    }
    
    [Fact]
    public void GetBestHypothesis_ReturnsHighestConfidence()
    {
        var session = new ResearchSession("device", "objective");
        session.AddHypothesis(new ResearchHypothesis("low", RiskLevel.ReadOnly, 0.3, new[] { "e1" }));
        session.AddHypothesis(new ResearchHypothesis("high", RiskLevel.ReadOnly, 0.9, new[] { "e2" }));
        
        var best = session.GetBestHypothesis();
        
        Assert.NotNull(best);
        Assert.Equal("high", best.Description);
    }
    
    [Fact]
    public void Complete_SetsStatusAndTimestamp()
    {
        var session = new ResearchSession("device", "objective");
        session.Complete(ResearchStatus.Completed);
        
        Assert.Equal(ResearchStatus.Completed, session.Status);
        Assert.NotNull(session.CompletedAt);
    }
}

public class ResearchPlanTests
{
    [Fact]
    public void AddStep_AddsToPlan()
    {
        var plan = new ResearchPlan("device", "objective");
        var step = new ResearchStep("test", RiskLevel.ReadOnly);
        
        plan.AddStep(step);
        
        Assert.Single(plan.Steps);
    }
    
    [Fact]
    public void Advance_MovesToNextStep()
    {
        var plan = new ResearchPlan("device", "objective");
        plan.AddStep(new ResearchStep("step1", RiskLevel.ReadOnly));
        plan.AddStep(new ResearchStep("step2", RiskLevel.ReadOnly));
        
        Assert.True(plan.Advance());
        Assert.Equal(0, plan.CurrentStepIndex);
        Assert.True(plan.Advance());
        Assert.Equal(1, plan.CurrentStepIndex);
    }
    
    [Fact]
    public void CompleteCurrentStep_MarksStepAsComplete()
    {
        var plan = new ResearchPlan("device", "objective");
        plan.AddStep(new ResearchStep("step1", RiskLevel.ReadOnly));
        plan.Advance();
        
        var result = plan.CompleteCurrentStep(true, "success");
        
        Assert.True(result);
        Assert.True(plan.Steps[0].Success);
        Assert.Equal("success", plan.Steps[0].Result);
    }
    
    [Fact]
    public void IsComplete_ReturnsTrueWhenAllStepsDone()
    {
        var plan = new ResearchPlan("device", "objective");
        plan.AddStep(new ResearchStep("step1", RiskLevel.ReadOnly));
        
        Assert.False(plan.IsComplete);
        plan.Advance();
        plan.CompleteCurrentStep(true);
        Assert.True(plan.IsComplete);
    }
}
