# Open Device Toolkit - Windows 11 Installer Guide

## Quick Start - What You Need to Do

You want **two things**:
1. **Windows 11 Application** - Runs on your computer
2. **RP2040 Firmware** - Flashes to your Raspberry Pi Pico

Here are your options:

---

## OPTION A: Use Pre-Built Files (Fastest)

### If you have .NET 8 SDK installed:

1. **Build the Windows App:**
   ```powershell
   cd /workspace/github__jeridj1__OpenDeviceToolkit
   .\scripts\build-windows-installer.ps1
   ```
   
   This creates: `installer-output\OpenDeviceToolkit-0.6-windows-x64\`

2. **Install:**
   - Navigate to: `installer-output\OpenDeviceToolkit-0.6-windows-x64\`
   - Run: `install.bat` (as Administrator)
   - Follow prompts

3. **Flash RP2040:**
   - Navigate to: `firmware\rp2040-bridge\`
   - You need to **build the firmware first** (see below)
   - Then drag-and-drop the `.uf2` file onto your Pico

---

## OPTION B: Complete Package (Recommended)

### This creates everything in one package:

```powershell
cd /workspace/github__jeridj1__OpenDeviceToolkit
.\scripts\create-installer-package.ps1
```

This creates: `installer-output\OpenDeviceToolkit-0.6-Windows-Package\`

Contains:
- `OpenDeviceToolkit-App/` - Windows application (needs building)
- `RP2040-Firmware/` - All firmware source and build scripts
- `Documentation/` - Complete guides
- `Scripts/` - Helper scripts

### Then:

1. **Build the firmware** (see below)
2. **Build the application** (see below)
3. **Install** by running: `OpenDeviceToolkit-App\install.bat`

---

## Building the RP2040 Firmware

### What you need:
- Raspberry Pi Pico SDK: https://github.com/raspberrypi/pico-sdk
- ARM GNU Toolchain: https://developer.arm.com/downloads/-/arm-gnu-toolchain-downloads
- CMake: https://cmake.org/download/

### Quick setup:

1. **Install Pico SDK:**
   ```bash
   git clone https://github.com/raspberrypi/pico-sdk.git
   cd pico-sdk
   git submodule update --init --recursive
   ```

2. **Install ARM Toolchain:**
   - Download from ARM website
   - Set environment variable: `PICO_TOOLCHAIN_PATH=C:\path\to\arm-none-eabi-bin`

3. **Set environment variables:**
   ```powershell
   $env:PICO_SDK_PATH = "C:\path\to\pico-sdk"
   $env:PICO_TOOLCHAIN_PATH = "C:\path\to\arm-none-eabi-bin"
   ```

4. **Build firmware:**
   ```powershell
   cd firmware\rp2040-bridge
   .\build-firmware.ps1
   ```
   
   This creates: `odt_rp2040_bridge.uf2`

5. **Flash to Pico:**
   - Hold BOOTSEL button
   - Plug into USB
   - Release BOOTSEL
   - Drag and drop `odt_rp2040_bridge.uf2` onto RPI-RP2 drive

---

## Building the Windows Application

### What you need:
- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### Build:

```powershell
cd /workspace/github__jeridj1__OpenDeviceToolkit

# Restore and build
dotnet restore
dotnet build --configuration Release

# Publish as self-contained single file
dotnet publish src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    --output .\installer-output\app\publish
```

This creates: `installer-output\app\publish\OpenDeviceToolkit.App.exe`

---

## What You Get

### Windows Application Features:
- ✅ Natural language voice interaction
- ✅ Android device discovery (ADB/Fastboot)
- ✅ USB device enumeration
- ✅ RP2040 hardware bridge control
- ✅ Guided probe workflow
- ✅ Logic analyzer with waveform display
- ✅ Research engine (GitHub, XDA, offline)
- ✅ Pinout database (10+ chips)
- ✅ Firmware validation
- ✅ Recovery workflows
- ✅ E-Waste Mode for experimental research

### RP2040 Firmware Features:
- ✅ USB serial communication (115200 baud)
- ✅ GPIO control (high-impedance safe mode)
- ✅ ADC voltage measurement
- ✅ PIO logic capture
- ✅ Commands: HELLO, SAFE, VOLTAGE, SNIFF, CAPTURE

---

## For Your Tesla Neurio W2-tesla

### What you'll do:

1. **Install ODT on Windows 11** (using one of the methods above)

2. **Flash your RP2040** (Raspberry Pi Pico) with ODT firmware

3. **Connect to your Neurio:**
   - Solder wires to the unmarked jumper pads
   - Connect to RP2040 GPIO pins
   - Note which wire goes to which pad

4. **Start ODT and click "RP2040 Bridge"**
   - Follow the guided probe workflow
   - Measure voltage on each pad
   - Identify power, ground, and data lines
   - Test for UART, SPI, I2C, JTAG, SWD

5. **Enable E-Waste Mode** when prompted (understand the risks!)

6. **Let ODT systematically explore** the device to find:
   - Debug UART interfaces
   - Programming interfaces (JTAG/SWD)
   - Internal communication protocols
   - Firmware extraction possibilities
   - Wireless configuration options

---

## Quick Reference: Files Created

```
installer-output/
├── OpenDeviceToolkit-0.6-Windows-Package/
│   ├── OpenDeviceToolkit-App/
│   │   ├── install.bat                    # Install the Windows app
│   │   ├── uninstall.bat                  # Uninstall
│   │   └── create-shortcut.bat            # Create Start Menu entry
│   │
│   ├── RP2040-Firmware/
│   │   ├── main.c                         # Firmware source
│   │   ├── logic_capture.c                # Logic capture code
│   │   ├── logic_capture.pio              # PIO program
│   │   ├── CMakeLists.txt                 # Build config
│   │   ├── build-firmware.bat             # Windows build script
│   │   ├── build-firmware.ps1             # PowerShell build script
│   │   ├── FLASHING_GUIDE.txt             # Complete flashing guide
│   │   └── README.md                      # Firmware docs
│   │
│   ├── Documentation/
│   │   ├── USER_GUIDE.txt                 # Complete user guide
│   │   ├── QUICK_START.txt                # Quick start
│   │   ├── COMMAND_LINE.txt               # CLI usage
│   │   └── LICENSE.txt                    # License
│   │
│   ├── Scripts/
│   │   └── build-all.bat                  # Build everything
│   │
│   ├── README.txt                          # Main readme
│   └── PACKAGE_SUMMARY.txt                # Package info
│
├── build-windows-installer.ps1           # Windows app builder
└── create-installer-package.ps1           # Complete package creator
```

---

## Troubleshooting

### .NET 8 not found:
- Install from: https://dotnet.microsoft.com/download/dotnet/8.0
- Verify: `dotnet --version` should show 8.x.x

### Pico SDK not found:
- Clone from: https://github.com/raspberrypi/pico-sdk
- Set: `PICO_SDK_PATH` environment variable

### ARM toolchain not found:
- Download from: https://developer.arm.com/downloads/-/arm-gnu-toolchain-downloads
- Set: `PICO_TOOLCHAIN_PATH` environment variable

### RP2040 not detected:
- Check USB cable (must be data-capable, not power-only)
- Try different USB port
- Verify firmware is flashed (RPI-RP2 drive should appear when in bootloader mode)

### Application won't start:
- Verify .NET 8 is installed
- Try running from command prompt to see error
- Check all files were copied

---

## Need Help?

- **GitHub**: https://github.com/jeridj1/OpenDeviceToolkit
- **Documentation**: Check the `Documentation/` folder
- **Build Issues**: Run the PowerShell scripts from PowerShell (not CMD)

---

## Summary: Your Next Steps

1. ✅ **Decide**: Use Option A (fast) or Option B (complete package)

2. ✅ **Build**: 
   - Run the appropriate PowerShell script
   
3. ✅ **Install**:
   - Run `install.bat` as Administrator

4. ✅ **Flash RP2040**:
   - Build firmware using `build-firmware.ps1`
   - Drag and drop `.uf2` file onto Pico

5. ✅ **Use with Tesla Neurio**:
   - Connect wires to jumper pads
   - Run ODT
   - Click "RP2040 Bridge"
   - Start exploring!

---

**You now have everything you need to create a standalone Windows 11 installer and RP2040 firmware for your Tesla Neurio W2-tesla repurposing project!**
