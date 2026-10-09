<#
.SYNOPSIS
    Open Device Toolkit - Single File Installer for Windows 10/11

.DESCRIPTION
    Downloads and installs Open Device Toolkit from GitHub.
    This is a single-file installer - just download and run this script.

    Features:
    - Downloads .NET 8 Runtime if needed
    - Downloads Open Device Toolkit application
    - Creates Start Menu and Desktop shortcuts
    - Optionally downloads RP2040 firmware
    - Self-contained, no external dependencies

.NOTES
    Version: 0.6
    Author: OpenDeviceToolkit
    Website: https://github.com/jeridj1/OpenDeviceToolkit

    Usage:
    1. Download this file (install-odt.ps1)
    2. Right-click -> "Run with PowerShell"
    3. OR: Open PowerShell and run: .\install-odt.ps1
#>

# =============================================================================
# Open Device Toolkit - Single File Installer
# =============================================================================

param(
    [string]$InstallPath = "$env:ProgramFiles\OpenDeviceToolkit",
    [switch]$Silent,
    [switch]$NoShortcuts,
    [switch]$IncludeFirmware,
    [switch]$SkipDotNet
)

# =============================================================================
# Configuration
# =============================================================================

$ScriptVersion = "0.6"
$RepoUrl = "https://github.com/jeridj1/OpenDeviceToolkit"
$ReleaseUrl = "$RepoUrl/releases/latest/download"
$RawUrl = "https://raw.githubusercontent.com/jeridj1/OpenDeviceToolkit/main"

# GitHub API URL for latest release
$ApiUrl = "https://api.github.com/repos/jeridj1/OpenDeviceToolkit/releases/latest"

# Temp directory
$TempDir = "$env:TEMP\ODT_Install_$([Guid]::NewGuid().Guid)"

# =============================================================================
# Helper Functions
# =============================================================================

function Write-Header {
    param([string]$Text)
    if (-not $Silent) {
        Write-Host "`n================================================================================`" -ForegroundColor Cyan
        Write-Host "  $Text`" -ForegroundColor White
        Write-Host "================================================================================`n" -ForegroundColor Cyan
    }
}

function Write-Status {
    param([string]$Text, [string]$Color = "White")
    if (-not $Silent) {
        Write-Host "  [$Color]$Text`" -ForegroundColor $Color
    }
}

function Write-Section {
    param([string]$Text)
    if (-not $Silent) {
        Write-Host "`n--- $Text ---`" -ForegroundColor Yellow
    }
}

function Test-IsAdmin {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-Download {
    param(
        [string]$Url,
        [string]$Output,
        [string]$Description = "Downloading"
    )
    
    if (-not $Silent) {
        Write-Host "  $Description..." -ForegroundColor Gray
    }
    
    try {
        $ProgressPreference = 'SilentlyContinue'
        if (Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue) {
            Start-BitsTransfer -Source $Url -Destination $Output -ErrorAction Stop | Out-Null
        } else {
            $webClient = New-Object System.Net.WebClient
            $webClient.DownloadFile($Url, $Output)
        }
        return $true
    } catch {
        Write-Status "Failed to download: $($_.Exception.Message)" Red
        return $false
    }
}

function Show-Banner {
    $banner = @"
   ___  ____   ___  _____ _   _ _____ _____ ____
  / _ \/ ___| / _ \(_   _)| | | |_   _|_   _/ ___|
 | | | \___ \| | | | | | | |_| | | | \___ \
 | |_| |___) | |_| | | | |  _  | | |  ___) |
  \___/|____/  \___/  |_| |_| |_| |_| |_| |____/

      Open Device Toolkit v$ScriptVersion
      Hardware Research & Repurposing Workbench
"@
    Write-Host $banner -ForegroundColor Cyan
}

# =============================================================================
# Main Installation
# =============================================================================

# Create temp directory
if (-not (Test-Path $TempDir)) {
    New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
}

# Show banner
if (-not $Silent) {
    Clear-Host
    Show-Banner
    Write-Header "Open Device Toolkit v$ScriptVersion - Installer"
}

# Check Windows version
$os = Get-CimInstance Win32_OperatingSystem
if ($os.Caption -notmatch "Windows 10|Windows 11") {
    Write-Status "ERROR: This installer requires Windows 10 or 11" Red
    Write-Status "Detected: $($os.Caption)" Red
    if (-not $Silent) { Read-Host "Press ENTER to exit" }
    exit 1
}

Write-Status "Detected: $($os.Caption)" Green

# Check if we need admin
$needsAdmin = $InstallPath -match "Program Files"
if ($needsAdmin -and -not (Test-IsAdmin)) {
    Write-Status "Installing to Program Files requires Administrator rights" Yellow
    Write-Status "Please run this script as Administrator" Yellow
    
    # Try to restart with admin
    $scriptPath = $MyInvocation.MyCommand.Definition
    $arguments = "-InstallPath `$($InstallPath.Replace('\','\\'))"
    if ($IncludeFirmware) { $arguments += " -IncludeFirmware" }
    if ($SkipDotNet) { $arguments += " -SkipDotNet" }
    if ($NoShortcuts) { $arguments += " -NoShortcuts" }
    if ($Silent) { $arguments += " -Silent" }
    
    Write-Status "Restarting with elevated privileges..." Yellow
    Start-Process PowerShell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `$scriptPath $arguments" -Verb RunAs
    exit 0
}

# Create installation directory
Write-Section "Installation Directory"
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Status "Created: $InstallPath" Green
} else {
    Write-Status "Using existing: $InstallPath" Green
}

# =============================================================================
# Step 1: Install .NET 8 Runtime (if needed)
# =============================================================================

if (-not $SkipDotNet) {
    Write-Section "Checking .NET 8 Runtime"
    
    # Check if .NET 8 is already installed
    $dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    if ($dotnetPath -and (dotnet --list-runtimes 2>$null | Select-String "Microsoft.NETCore.App 8\.")) {
        Write-Status ".NET 8 Runtime is already installed" Green
    } else {
        Write-Status ".NET 8 Runtime not found, downloading..." Yellow
        
        # .NET 8 Runtime download URL (x64)
        $dotnetUrl = "https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-8.0.0-windows-x64-installer"
        $dotnetInstaller = "$TempDir\dotnet-runtime-8.0.0-win-x64.exe"
        
        if (Invoke-Download -Url $dotnetUrl -Output $dotnetInstaller -Description "Downloading .NET 8 Runtime") {
            Write-Status "Installing .NET 8 Runtime..." Yellow
            Start-Process -FilePath $dotnetInstaller -ArgumentList "/quiet /norestart" -Wait
            
            # Verify installation
            if (dotnet --list-runtimes 2>$null | Select-String "Microsoft.NETCore.App 8\.") {
                Write-Status ".NET 8 Runtime installed successfully" Green
            } else {
                Write-Status "WARNING: .NET 8 Runtime may not have installed correctly" Yellow
                Write-Status "You can manually install from: https://dotnet.microsoft.com/download/dotnet/8.0" Yellow
            }
        } else {
            Write-Status "Failed to download .NET 8 Runtime" Red
            Write-Status "Please install manually from: https://dotnet.microsoft.com/download/dotnet/8.0" Yellow
        }
    }
}

# =============================================================================
# Step 2: Download Open Device Toolkit
# =============================================================================

Write-Section "Downloading Open Device Toolkit"

# Try to get latest release from GitHub API
$releaseInfo = $null
try {
    $headers = @{ "Accept" = "application/vnd.github.v3+json" }
    $releaseInfo = Invoke-RestMethod -Uri $ApiUrl -Headers $headers -ErrorAction SilentlyContinue
} catch {
    Write-Status "Could not fetch release info from GitHub API" Yellow
}

# Determine download URL
if ($releaseInfo -and $releaseInfo.assets) {
    # Find the Windows zip asset
    $windowsAsset = $releaseInfo.assets | Where-Object { $_.name -match "windows|win|Windows" -and $_.name -match "\.zip$|\.exe$" } | Select-Object -First 1
    if ($windowsAsset) {
        $downloadUrl = $windowsAsset.browser_download_url
        Write-Status "Found release: $($releaseInfo.name)" Green
    }
}

# Fallback: Use a direct URL pattern
if (-not $downloadUrl) {
    # Try common patterns
    $possibleUrls = @(
        "$ReleaseUrl/OpenDeviceToolkit-0.6-windows-x64.zip",
        "$ReleaseUrl/OpenDeviceToolkit-win-x64.zip",
        "$ReleaseUrl/ODT-Windows.zip"
    )
    
    foreach ($url in $possibleUrls) {
        try {
            $response = Invoke-WebRequest -Uri $url -Method Head -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $downloadUrl = $url
                break
            }
        } catch {}
    }
}

# If we couldn't find a pre-built release, we'll need to build from source
# But for this single-file installer, let's use the raw GitHub files
if (-not $downloadUrl) {
    Write-Status "No pre-built release found, using source from main branch" Yellow
    
    # We'll need to download the source and build it
    # But this requires .NET 8 SDK, not just runtime
    Write-Status "This requires .NET 8 SDK (not just runtime)" Yellow
    
    # Check for SDK
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue | Where-Object { $_.Source -match "sdk\/8\." })) {
        Write-Status "Downloading .NET 8 SDK..." Yellow
        $sdkUrl = "https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-8.0.100-windows-x64-installer"
        $sdkInstaller = "$TempDir\dotnet-sdk-8.0.100-win-x64.exe"
        
        if (Invoke-Download -Url $sdkUrl -Output $sdkInstaller -Description "Downloading .NET 8 SDK") {
            Start-Process -FilePath $sdkInstaller -ArgumentList "/quiet /norestart" -Wait
        }
    }
    
    # Download source and build
    Write-Status "Downloading source code..." Yellow
    $sourceZip = "$TempDir\source.zip"
    $sourceUrl = "$RepoUrl/archive/refs/heads/main.zip"
    
    if (Invoke-Download -Url $sourceUrl -Output $sourceZip -Description "Downloading source") {
        Write-Status "Extracting source..." Yellow
        Expand-Archive -Path $sourceZip -DestinationPath $TempDir -Force
        
        # Find the extracted directory
        $sourceDir = Get-ChildItem -Path $TempDir -Directory | Where-Object { $_.Name -match "OpenDeviceToolkit" } | Select-Object -First 1
        if ($sourceDir) {
            $sourcePath = $sourceDir.FullName
            
            Write-Status "Building Open Device Toolkit..." Yellow
            Set-Location $sourcePath
            
            # Restore and build
            dotnet restore > $TempDir\build.log 2>&1
            dotnet build --configuration Release --no-restore >> $TempDir\build.log 2>&1
            
            # Publish as self-contained single file
            $publishDir = "$sourcePath\publish"
            dotnet publish src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj \
                --configuration Release \
                --runtime win-x64 \
                --self-contained true \
                /p:PublishSingleFile=true \
                --output $publishDir >> $TempDir\build.log 2>&1
            
            if (Test-Path "$publishDir\OpenDeviceToolkit.App.exe") {
                Write-Status "Build successful!" Green
                
                # Copy to install directory
                Copy-Item -Path "$publishDir\*" -Destination $InstallPath -Recurse -Force
                Write-Status "Installed to: $InstallPath" Green
            } else {
                Write-Status "Build failed. Check: $TempDir\build.log" Red
                if (-not $Silent) {
                    Notepad $TempDir\build.log
                }
                exit 1
            }
        }
    } else {
        Write-Status "Failed to download source" Red
        exit 1
    }
} else {
    # Download pre-built release
    $zipFile = "$TempDir\odt.zip"
    if (Invoke-Download -Url $downloadUrl -Output $zipFile -Description "Downloading ODT") {
        Write-Status "Extracting..." Yellow
        Expand-Archive -Path $zipFile -DestinationPath $InstallPath -Force
        Write-Status "Extracted to: $InstallPath" Green
    } else {
        Write-Status "Failed to download ODT" Red
        exit 1
    }
}

# =============================================================================
# Step 3: Download RP2040 Firmware (optional)
# =============================================================================

if ($IncludeFirmware) {
    Write-Section "Downloading RP2040 Firmware"
    
    $firmwareDir = "$InstallPath\RP2040-Firmware"
    if (-not (Test-Path $firmwareDir)) {
        New-Item -ItemType Directory -Path $firmwareDir -Force | Out-Null
    }
    
    # Download firmware files from raw GitHub
    $firmwareFiles = @(
        @{ Name = "main.c"; Url = "$RawUrl/firmware/rp2040-bridge/main.c" },
        @{ Name = "logic_capture.c"; Url = "$RawUrl/firmware/rp2040-bridge/logic_capture.c" },
        @{ Name = "logic_capture.pio"; Url = "$RawUrl/firmware/rp2040-bridge/logic_capture.pio" },
        @{ Name = "CMakeLists.txt"; Url = "$RawUrl/firmware/rp2040-bridge/CMakeLists.txt" },
        @{ Name = "README.md"; Url = "$RawUrl/firmware/rp2040-bridge/README.md" },
        @{ Name = "build-firmware.ps1"; Url = "$RawUrl/firmware/rp2040-bridge/build-firmware.ps1" }
    )
    
    foreach ($file in $firmwareFiles) {
        Invoke-Download -Url $file.Url -Output "$firmwareDir\$($file.Name)" -Description "  $($file.Name)"
    }
    
    # Create a simple flashing guide
    $flashingGuide = @"
RP2040 FIRMWARE - QUICK FLASHING GUIDE
=======================================

To flash your Raspberry Pi Pico:

1. Hold the BOOTSEL button on your Pico
2. Plug into USB
3. Release BOOTSEL (RPI-RP2 drive appears)
4. Build the firmware:
   cd RP2040-Firmware
   .\build-firmware.ps1
5. Drag and drop the .uf2 file onto RPI-RP2 drive

For complete instructions, see: README.md
"@
    $flashingGuide | Out-File -FilePath "$firmwareDir\FLASH_ME.txt" -Encoding ASCII
    
    Write-Status "Firmware files downloaded to: $firmwareDir" Green
}

# =============================================================================
# Step 4: Create Shortcuts
# =============================================================================

if (-not $NoShortcuts) {
    Write-Section "Creating Shortcuts"
    
    $exePath = "$InstallPath\OpenDeviceToolkit.App.exe"
    
    # Create Start Menu shortcut
    $startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\OpenDeviceToolkit"
    if (-not (Test-Path $startMenuDir)) {
        New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
    }
    
    $wscript = @"
`$startMenuShortcut = New-Object -ComObject WScript.Shell
`$shortcut = `$startMenuShortcut.CreateShortcut("$startMenuDir\Open Device Toolkit.lnk")
`$shortcut.TargetPath = "$exePath"
`$shortcut.WorkingDirectory = "$InstallPath"
`$shortcut.Description = "Open Device Toolkit - Hardware Research Workbench"
`$shortcut.IconLocation = "$exePath,0"
`$shortcut.Save()
"@
    
    try {
        Invoke-Expression $wscript | Out-Null
        Write-Status "Start Menu shortcut created" Green
    } catch {
        Write-Status "Could not create Start Menu shortcut" Yellow
    }
    
    # Create Desktop shortcut (optional)
    if (-not $Silent) {
        $createDesktop = Read-Host "Create Desktop shortcut? (Y/n)"
        if ($createDesktop -ne "n" -and $createDesktop -ne "N") {
            $desktopShortcut = @"
`$desktopShortcut = New-Object -ComObject WScript.Shell
`$shortcut = `$desktopShortcut.CreateShortcut("$env:USERPROFILE\Desktop\Open Device Toolkit.lnk")
`$shortcut.TargetPath = "$exePath"
`$shortcut.WorkingDirectory = "$InstallPath"
`$shortcut.Description = "Open Device Toolkit - Hardware Research Workbench"
`$shortcut.IconLocation = "$exePath,0"
`$shortcut.Save()
"@
            Invoke-Expression $desktopShortcut | Out-Null
            Write-Status "Desktop shortcut created" Green
        }
    } else {
        # Silent mode: always create desktop shortcut
        $desktopShortcut = @"
`$desktopShortcut = New-Object -ComObject WScript.Shell
`$shortcut = `$desktopShortcut.CreateShortcut("$env:USERPROFILE\Desktop\Open Device Toolkit.lnk")
`$shortcut.TargetPath = "$exePath"
`$shortcut.WorkingDirectory = "$InstallPath"
`$shortcut.Description = "Open Device Toolkit - Hardware Research Workbench"
`$shortcut.IconLocation = "$exePath,0"
`$shortcut.Save()
"@
        Invoke-Expression $desktopShortcut | Out-Null
        Write-Status "Desktop shortcut created" Green
    }
}

# =============================================================================
# Step 5: Create Uninstaller
# =============================================================================

Write-Section "Creating Uninstaller"

$uninstallScript = @"
@echo off
setlocal

echo Open Device Toolkit - Uninstaller
echo ==================================
echo.

:: Remove Start Menu shortcut
set SHORTCUT_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\OpenDeviceToolkit
if exist "%SHORTCUT_DIR%" (
    del "%SHORTCUT_DIR%\*.*" /q >nul 2>&1
    rmdir "%SHORTCUT_DIR%" /q >nul 2>&1
    echo Removed Start Menu shortcut
)

:: Remove Desktop shortcut
set DESKTOP_LINK=%USERPROFILE%\Desktop\Open Device Toolkit.lnk
if exist "%DESKTOP_LINK%" (
    del "%DESKTOP_LINK%" /q >nul 2>&1
    echo Removed Desktop shortcut
)

echo.
echo Note: The application files are NOT automatically deleted.
echo To completely remove Open Device Toolkit, delete:
echo   $InstallPath

echo.
pause
"@

$uninstallScript | Out-File -FilePath "$InstallPath\uninstall.bat" -Encoding ASCII
Write-Status "Uninstaller created: $InstallPath\uninstall.bat" Green

# =============================================================================
# Step 6: Create README
# =============================================================================

$readme = @"
Open Device Toolkit v$ScriptVersion
=================================

INSTALLED TO: $InstallPath

QUICK START:
-----------
Double-click: OpenDeviceToolkit.App.exe

Or from Start Menu: Open Device Toolkit

FEATURES:
---------
✓ Natural language voice interaction
✓ Android device discovery (ADB/Fastboot)
✓ RP2040 hardware bridge control
✓ Guided probe workflow
✓ Logic analyzer with waveform display
✓ Research engine (GitHub, XDA, offline)
✓ Pinout database (10+ chips)
✓ Firmware validation
✓ Recovery workflows
✓ E-Waste Mode for experimental research

REQUIREMENTS:
------------
- Windows 10 or 11 (64-bit)
- .NET 8 Runtime (installed automatically)
- For Android: ADB and Fastboot (optional)
- For hardware: RP2040 with ODT firmware (optional)

RP2040 FIRMWARE:
---------------
To use hardware features, flash your RP2040:
1. Navigate to: $InstallPath\RP2040-Firmware\ (if downloaded)
2. Or download from: $RepoUrl/tree/main/firmware/rp2040-bridge
3. See: FLASH_ME.txt for instructions

UNINSTALL:
----------
Run: uninstall.bat
Or manually delete: $InstallPath

SUPPORT:
--------
GitHub: $RepoUrl

TROUBLESHOOTING:
---------------
If the application doesn't start:
- Verify .NET 8 Runtime is installed
- Try running from Command Prompt to see errors
- Check all files were copied correctly

For RP2040 connection issues:
- Verify USB cable is data-capable
- Check Device Manager for COM port
- Try different baud rate (115200 default)
"@

$readme | Out-File -FilePath "$InstallPath\README.txt" -Encoding ASCII
Write-Status "README created: $InstallPath\README.txt" Green

# =============================================================================
# Step 7: Final Verification
# =============================================================================

Write-Section "Final Verification"

$exePath = "$InstallPath\OpenDeviceToolkit.App.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Status "Installation complete!" Green
    Write-Status "Executable: $exePath" Green
    Write-Status "Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" Green
    Write-Status "Installed to: $InstallPath" Green
    
    if ($IncludeFirmware -and (Test-Path "$InstallPath\RP2040-Firmware")) {
        Write-Status "RP2040 Firmware: Included" Green
    }
} else {
    Write-Status "WARNING: Executable not found at $exePath" Red
    Write-Status "Installation may have failed" Red
}

# =============================================================================
# Step 8: Success Message
# =============================================================================

if (-not $Silent) {
    Write-Header "Installation Complete!"
    Write-Status "Open Device Toolkit v$ScriptVersion is ready to use" Green
    Write-Status ""
    Write-Status "To start:" Yellow
    Write-Status "  1. From Start Menu: Open Device Toolkit" Yellow
    Write-Status "  2. From Desktop: Open Device Toolkit shortcut" Yellow
    Write-Status "  3. Direct: $exePath" Yellow
    Write-Status ""
    Write-Status "For Tesla Neurio W2-tesla:" Yellow
    Write-Status "  1. Flash RP2040 firmware (see: $InstallPath\RP2040-Firmware\FLASH_ME.txt)" Yellow
    Write-Status "  2. Connect to jumper pads" Yellow
    Write-Status "  3. Run ODT and click 'RP2040 Bridge'" Yellow
    Write-Status "  4. Follow guided workflow to explore device" Yellow
    Write-Status ""
    Write-Status "Support: $RepoUrl" Cyan
    Write-Status ""
    
    if (-not $Silent) {
        Read-Host "Press ENTER to exit"
    }
}

# Cleanup temp directory
try {
    Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
} catch {}

# Exit successfully
exit 0
