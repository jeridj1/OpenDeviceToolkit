using OpenDeviceToolkit.Hardware.Rp2040;

namespace OpenDeviceToolkit.Tests;

public sealed class PinoutDatabaseTests
{{
    [Fact]
    public void GetChipIdentifiers_IncludesNewPinouts()
    {{
        var identifiers = PinoutDatabase.GetChipIdentifiers();

        Assert.Contains("NRF52840", identifiers);
        Assert.Contains("SAMD21", identifiers);
        Assert.Contains("STM32F407", identifiers);
        Assert.Contains("ESP8266", identifiers);
        Assert.Contains("CH32V003", identifiers);
    }}

    [Fact]
    public void GetPinout_NRF52840_HasExpectedProperties()
    {{
        var pinout = PinoutDatabase.GetPinout("NRF52840");

        Assert.NotNull(pinout);
        Assert.Equal("nRF52840", pinout!.Name);
        Assert.Equal("Nordic Semiconductor", pinout.Manufacturer);
        Assert.NotEmpty(pinout.ProgrammingInterfaces);
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "SWD");
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "UART");
    }}

    [Fact]
    public void GetPinout_SAMD21_HasExpectedProperties()
    {{
        var pinout = PinoutDatabase.GetPinout("SAMD21");

        Assert.NotNull(pinout);
        Assert.Equal("SAMD21G18", pinout!.Name);
        Assert.Equal("Microchip", pinout.Manufacturer);
        Assert.NotEmpty(pinout.ProgrammingInterfaces);
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "SWD");
    }}

    [Fact]
    public void GetPinout_STM32F407_HasExpectedProperties()
    {{
        var pinout = PinoutDatabase.GetPinout("STM32F407");

        Assert.NotNull(pinout);
        Assert.Equal("STM32F407", pinout!.Name);
        Assert.Equal("STMicroelectronics", pinout.Manufacturer);
        Assert.NotEmpty(pinout.ProgrammingInterfaces);
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "SWD");
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "UART");
    }}

    [Fact]
    public void GetPinout_ESP8266_HasExpectedProperties()
    {{
        var pinout = PinoutDatabase.GetPinout("ESP8266");

        Assert.NotNull(pinout);
        Assert.Equal("ESP8266", pinout!.Name);
        Assert.Equal("Espressif", pinout.Manufacturer);
        Assert.NotEmpty(pinout.ProgrammingInterfaces);
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "UART");
    }}

    [Fact]
    public void GetPinout_CH32V003_HasExpectedProperties()
    {{
        var pinout = PinoutDatabase.GetPinout("CH32V003");

        Assert.NotNull(pinout);
        Assert.Equal("CH32V003", pinout!.Name);
        Assert.Equal("WCH", pinout.Manufacturer);
        Assert.NotEmpty(pinout.ProgrammingInterfaces);
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "SWD");
        Assert.Contains(pinout.ProgrammingInterfaces, p => p.Type == "UART");
    }}

    [Fact]
    public void FindByUsbId_NRF52840_MatchesKnownVidPid()
    {{
        var results = PinoutDatabase.FindByUsbId("1915", "521F");

        Assert.NotEmpty(results);
        Assert.Contains(results, p => p.Name == "nRF52840");
    }}

    [Fact]
    public void FindByUsbId_STM32F407_MatchesKnownVidPid()
    {{
        var results = PinoutDatabase.FindByUsbId("0483", "374B");

        Assert.NotEmpty(results);
        Assert.Contains(results, p => p.Name == "STM32F407");
    }}

    [Fact]
    public void GetPinout_CaseInsensitive_MatchesLowercase()
    {{
        var pinout = PinoutDatabase.GetPinout("nrf52840");

        Assert.NotNull(pinout);
    }}
}}
