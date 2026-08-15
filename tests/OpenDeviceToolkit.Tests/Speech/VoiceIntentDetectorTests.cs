using OpenDeviceToolkit.Core.Speech;

namespace OpenDeviceToolkit.Tests.Speech;

public class VoiceIntentDetectorTests
{
    [Fact]
    public void DetectIntent_ScanDevice_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("scan device");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
    }
    
    [Fact]
    public void DetectIntent_GainAccess_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("gain access to my phone");
        Assert.Equal(VoiceCommandType.GainAccess, result.Type);
        Assert.Equal("my phone", result.Device);
    }
    
    [Fact]
    public void DetectIntent_GenerateReport_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("generate report");
        Assert.Equal(VoiceCommandType.GenerateReport, result.Type);
    }
    
    [Fact]
    public void DetectIntent_RebootDevice_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("reboot the phone");
        Assert.Equal(VoiceCommandType.RebootDevice, result.Type);
    }
    
    [Fact]
    public void DetectIntent_Help_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("what can I say");
        Assert.Equal(VoiceCommandType.Help, result.Type);
    }
    
    [Fact]
    public void DetectIntent_Exit_Detected()
    {
        var result = VoiceIntentDetector.DetectIntent("exit");
        Assert.Equal(VoiceCommandType.Exit, result.Type);
    }
    
    [Fact]
    public void DetectIntent_WakeWord_Removed()
    {
        var result = VoiceIntentDetector.DetectIntent("hey odt scan device");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
    }
    
    [Fact]
    public void DetectIntent_CustomObjective_ReturnsInput()
    {
        var result = VoiceIntentDetector.DetectIntent("I want to unlock my LG V50");
        Assert.Equal(VoiceCommandType.CustomObjective, result.Type);
        Assert.Equal("i want to unlock my lg v50", result.Objective);
    }
    
    [Fact]
    public void DetectIntent_Empty_ReturnsUnknown()
    {
        var result = VoiceIntentDetector.DetectIntent("");
        Assert.Equal(VoiceCommandType.Unknown, result.Type);
    }
    
    [Fact]
    public void DetectIntent_Null_ReturnsUnknown()
    {
        var result = VoiceIntentDetector.DetectIntent(null!);
        Assert.Equal(VoiceCommandType.Unknown, result.Type);
    }
    
    [Fact]
    public void DetectIntent_WithContext_UsesDevice()
    {
        var context = new VoiceContext { CurrentDevice = "LG V50" };
        var result = VoiceIntentDetector.DetectIntent("unlock it", context);
        Assert.Equal(VoiceCommandType.GainAccess, result.Type);
        Assert.Equal("LG V50", result.Device);
    }
    
    [Fact]
    public void DetectIntent_Synonyms_Unlock()
    {
        var result = VoiceIntentDetector.DetectIntent("root my phone");
        Assert.Equal(VoiceCommandType.GainAccess, result.Type);
    }
    
    [Fact]
    public void DetectIntent_Synonyms_Scan()
    {
        var result = VoiceIntentDetector.DetectIntent("detect my device");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
    }
    
    [Fact]
    public void DetectIntent_DeviceDescription()
    {
        var result = VoiceIntentDetector.DetectIntent("I have a Samsung phone");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
        Assert.Equal("a samsung phone", result.Device);
    }
    
    [Fact]
    public void DetectIntent_QualcommEDL()
    {
        var result = VoiceIntentDetector.DetectIntent("put my qualcomm device in edl mode");
        Assert.Equal(VoiceCommandType.CustomObjective, result.Type);
        Assert.Contains("edl", result.Objective!.ToLower());
    }
}

public class VoiceContextTests
{
    [Fact]
    public void DefaultValues_AreSet()
    {
        var context = new VoiceContext();
        Assert.Null(context.CurrentDevice);
        Assert.Null(context.CurrentObjective);
        Assert.Null(context.LastAction);
    }
    
    [Fact]
    public void Update_SetsProperties()
    {
        var context = new VoiceContext();
        var command = new VoiceCommand { Type = VoiceCommandType.ScanDevice, Device = "LG V50" };
        context.Update(command);
        Assert.Equal("LG V50", context.CurrentDevice);
        Assert.Equal(VoiceCommandType.ScanDevice, context.LastAction);
    }
    
    [Fact]
    public void Reset_ClearsProperties()
    {
        var context = new VoiceContext { CurrentDevice = "LG V50", CurrentObjective = "test" };
        context.Reset();
        Assert.Null(context.CurrentDevice);
        Assert.Null(context.CurrentObjective);
        Assert.Null(context.LastAction);
    }
    
    [Fact]
    public void Duration_CalculatesCorrectly()
    {
        var context = new VoiceContext();
        Thread.Sleep(100);
        Assert.True(context.Duration.TotalSeconds >= 0.1);
    }
}

public class ClarificationDialogTests
{
    [Fact]
    public void GenerateClarification_NoCommands_ReturnsNotUnderstood()
    {
        var request = ClarificationDialog.GenerateClarification("test", new List<VoiceCommand>());
        Assert.False(request.IsResolved);
        Assert.Contains("didn't understand", request.Message);
    }
    
    [Fact]
    public void GenerateClarification_OneCommand_ReturnsResolved()
    {
        var commands = new List<VoiceCommand> { new VoiceCommand { Type = VoiceCommandType.ScanDevice } };
        var request = ClarificationDialog.GenerateClarification("test", commands);
        Assert.True(request.IsResolved);
        Assert.NotNull(request.ResolvedCommand);
    }
    
    [Fact]
    public void GenerateClarification_MultipleCommands_ReturnsOptions()
    {
        var commands = new List<VoiceCommand>
        {
            new VoiceCommand { Type = VoiceCommandType.ScanDevice },
            new VoiceCommand { Type = VoiceCommandType.GainAccess }
        };
        var request = ClarificationDialog.GenerateClarification("test", commands);
        Assert.False(request.IsResolved);
        Assert.Contains("Did you mean", request.Message);
    }
    
    [Fact]
    public void ParseClarificationResponse_Numeric_ReturnsNumber()
    {
        var result = ClarificationDialog.ParseClarificationResponse("1", 3);
        Assert.Equal(1, result);
    }
    
    [Fact]
    public void ParseClarificationResponse_Word_ReturnsNumber()
    {
        var result = ClarificationDialog.ParseClarificationResponse("one", 3);
        Assert.Equal(1, result);
    }
    
    [Fact]
    public void ParseClarificationResponse_Invalid_ReturnsNull()
    {
        var result = ClarificationDialog.ParseClarificationResponse("invalid", 3);
        Assert.Null(result);
    }
}

public class VoiceCommandTypeExtensionsTests
{
    [Fact]
    public void GetDescription_ReturnsCorrectDescription()
    {
        Assert.Equal("Scan for connected devices", VoiceCommandType.ScanDevice.GetDescription());
        Assert.Equal("Gain access or unlock device", VoiceCommandType.GainAccess.GetDescription());
        Assert.Equal("Generate a diagnostic report", VoiceCommandType.GenerateReport.GetDescription());
        Assert.Equal("Reboot the device", VoiceCommandType.RebootDevice.GetDescription());
        Assert.Equal("Show help and available commands", VoiceCommandType.Help.GetDescription());
        Assert.Equal("Exit the application", VoiceCommandType.Exit.GetDescription());
        Assert.Equal("Custom objective", VoiceCommandType.CustomObjective.GetDescription());
        Assert.Equal("Unknown command", VoiceCommandType.Unknown.GetDescription());
    }
}
