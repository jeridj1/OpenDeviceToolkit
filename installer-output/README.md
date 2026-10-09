# Open Device Toolkit - Installer Output

This directory contains the output from installer creation scripts.

## What's Here

### Created by the build scripts:
- `OpenDeviceToolkit-0.6-Windows-Package/` - Complete installation package (when built)
- `INSTALLER_GUIDE.md` - Complete guide for building and installing

### Source files copied here:
- `firmware/` - RP2040 bridge firmware source
- `scripts/` - Build scripts

---

## Quick Start

### To create a complete Windows 11 installer:

```powershell
# From the repository root:
cd /workspace/github__jeridj1__OpenDeviceToolkit

# Option 1: Build just the Windows app
.\scripts\build-windows-installer.ps1

# Option 2: Create complete package (recommended)
.\scripts\create-installer-package.ps1
```

### To build the RP2040 firmware:

```powershell
# From the firmware directory:
cd firmware\rp2040-bridge

# Build the firmware
.\build-firmware.ps1

# This creates: odt_rp2040_bridge.uf2
```

### To flash the RP2040:

1. Hold BOOTSEL button on your Raspberry Pi Pico
2. Plug into USB
3. Release BOOTSEL button
4. Drag and drop `odt_rp2040_bridge.uf2` onto the RPI-RP2 drive that appears

---

## For Your Tesla Neurio W2-tesla Project

### What you need to do:

1. **Build the Windows application** (using one of the scripts above)
2. **Build and flash the RP2040 firmware**
3. **Install the Windows application**
4. **Connect your RP2040 to the Neurio's jumper pads**
5. **Run ODT and start the RP2040 Bridge workflow**

### The workflow will help you:
- Measure voltage on each unmarked pad
- Identify power, ground, and data lines
- Test for UART, SPI, I2C, JTAG, SWD interfaces
- Observe signals with the logic analyzer
- Discover undocumented capabilities
- Potentially unlock wireless standalone mode

---

## Important Notes

### RP2040 GPIO is NOT 5V tolerant!
- Always check voltage levels before connecting
- Use level shifters if the target device uses 5V
- RP2040 can be damaged by 5V on GPIO pins

### E-Waste Mode
- Required for experimental operations
- May cause permanent device damage
- Only enable on devices you accept may be destroyed
- Requires explicit, repeated confirmation

### Safety First
- Start with read-only operations
- Document all connections
- Use appropriate protection (resistors, etc.)
- Verify before connecting

---

## Documentation

- **Complete Guide**: `INSTALLER_GUIDE.md` - Step-by-step instructions
- **User Guide**: In `OpenDeviceToolkit-0.6-Windows-Package/Documentation/USER_GUIDE.txt`
- **Quick Start**: In `OpenDeviceToolkit-0.6-Windows-Package/Documentation/QUICK_START.txt`
- **Flashing Guide**: In `OpenDeviceToolkit-0.6-Windows-Package/RP2040-Firmware/FLASHING_GUIDE.txt`

---

## Support

- **GitHub**: https://github.com/jeridj1/OpenDeviceToolkit
- **Issues**: https://github.com/jeridj1/OpenDeviceToolkit/issues

---

## File Structure (After Building)

```
installer-output/
├── INSTALLER_GUIDE.md              # This guide
├── README.md                       # This file
│
├── OpenDeviceToolkit-0.6-Windows-Package/  # Created by create-installer-package.ps1
│   ├── OpenDeviceToolkit-App/
│   │   ├── OpenDeviceToolkit.App.exe    # Windows application
│   │   ├── install.bat                    # Installer
│   │   ├── uninstall.bat                  # Uninstaller
│   │   └── README.txt                     # App docs
│   │
│   ├── RP2040-Firmware/
│   │   ├── odt_rp2040_bridge.uf2          # Pre-built firmware (after building)
│   │   ├── main.c                         # Source
│   │   ├── build-firmware.ps1             # Build script
│   │   └── FLASHING_GUIDE.txt             # Flashing instructions
│   │
│   └── Documentation/
│       ├── USER_GUIDE.txt                 # Complete guide
│       ├── QUICK_START.txt                # Quick start
│       └── LICENSE.txt                    # License
│
└── scripts/
    ├── build-windows-installer.ps1       # Windows app builder
    └── create-installer-package.ps1       # Complete package creator
```

---

## Version Information

- **ODT Version**: 0.6 Alpha
- **Firmware Version**: 0.3.0
- **Target**: Windows 10/11 (64-bit)
- **Requirements**: .NET 8 SDK (for building), .NET 8 Runtime (included in published app)

---

## Next Steps

1. **Choose your path**:
   - [ ] Build Windows app only
   - [ ] Build complete package
   - [ ] Just build firmware

2. **Run the appropriate script** from the repository root

3. **Follow the INSTALLER_GUIDE.md** for detailed instructions

---

**Good luck with your Tesla Neurio W2-tesla repurposing project!**
