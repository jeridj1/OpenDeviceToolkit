using OpenDeviceToolkit.Core.Speech;

namespace OpenDeviceToolkit.Tests.Speech;

public class VoiceCommandParserTests
{
    [Fact]
    public void Parse_ScanDevice_Detected()
    {
        var result = VoiceCommandParser.Parse("scan device");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
    }
    
    [Fact]
    public void Parse_GainAccess_Detected()
    {
        var result = VoiceCommandParser.Parse("gain access to my phone");
        Assert.Equal(VoiceCommandType.GainAccess, result.Type);
        Assert.Equal("to my phone", result.Objective);
    }
    
    [Fact]
    public void Parse_GenerateReport_Detected()
    {
        var result = VoiceCommandParser.Parse("generate report");
        Assert.Equal(VoiceCommandType.GenerateReport, result.Type);
    }
    
    [Fact]
    public void Parse_RebootDevice_Detected()
    {
        var result = VoiceCommandParser.Parse("reboot the phone");
        Assert.Equal(VoiceCommandType.RebootDevice, result.Type);
    }
    
    [Fact]
    public void Parse_Help_Detected()
    {
        var result = VoiceCommandParser.Parse("what can I say");
        Assert.Equal(VoiceCommandType.Help, result.Type);
    }
    
    [Fact]
    public void Parse_Exit_Detected()
    {
        var result = VoiceCommandParser.Parse("exit");
        Assert.Equal(VoiceCommandType.Exit, result.Type);
    }
    
    [Fact]
    public void Parse_WakeWord_Removed()
    {
        var result = VoiceCommandParser.Parse("hey odt scan device");
        Assert.Equal(VoiceCommandType.ScanDevice, result.Type);
    }
    
    [Fact]
    public void Parse_CustomObjective_ReturnsInput()
    {
        var result = VoiceCommandParser.Parse("I want to unlock my LG V50");
        Assert.Equal(VoiceCommandType.CustomObjective, result.Type);
        Assert.Equal("i want to unlock my lg v50", result.Objective);
    }
    
    [Fact]
    public void Parse_Empty_ReturnsUnknown()
    {
        var result = VoiceCommandParser.Parse("");
        Assert.Equal(VoiceCommandType.Unknown, result.Type);
    }
    
    [Fact]
    public void Parse_Null_ReturnsUnknown()
    {
        var result = VoiceCommandParser.Parse(null!);
        Assert.Equal(VoiceCommandType.Unknown, result.Type);
    }
}

public class VoiceCommandTests
{
    [Fact]
    public void DefaultValues_AreSet()
    {
        var command = new VoiceCommand();
        Assert.Equal(VoiceCommandType.Unknown, command.Type);
        Assert.Null(command.Device);
        Assert.Null(command.Objective);
    }
    
    [Fact]
    public void Init_SetsProperties()
    {
        var command = new VoiceCommand
        {
            Type = VoiceCommandType.ScanDevice,
            Device = "phone",
            Objective = "scan"
        };
        
        Assert.Equal(VoiceCommandType.ScanDevice, command.Type);
        Assert.Equal("phone", command.Device);
        Assert.Equal("scan", command.Objective);
    }
}
