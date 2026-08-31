# Contributing to Open Device Toolkit

Thanks for helping improve ODT! ODT is a .NET 8 Windows workbench for device
diagnostics, firmware, recovery, and hardware work. This guide gets you started.

## Development setup

- .NET 8 SDK (Windows recommended; the App target is net8.0-windows and uses
  Windows Forms).
- Windows for the GUI/App project. The Core, Android, and Hardware libraries build
  cross-platform on .NET 8.

### Build

    dotnet restore
    dotnet build src/OpenDeviceToolkit.Core/OpenDeviceToolkit.Core.csproj --configuration Release
    dotnet build src/OpenDeviceToolkit.Android/OpenDeviceToolkit.Android.csproj --configuration Release
    dotnet build src/OpenDeviceToolkit.Hardware/OpenDeviceToolkit.Hardware.csproj --configuration Release
    dotnet build src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj --configuration Release

### Tests

    dotnet test tests/OpenDeviceToolkit.Tests/OpenDeviceToolkit.Tests.csproj --configuration Release

CI (.github/workflows/build.yml) builds each project and runs the tests on
windows-latest. Your PR must keep CI green.

## Workflow

1. Open an issue describing the change (use the issue templates).
2. Branch from main (e.g. feature/<short-name> or fix/<short-name>).
3. Keep PRs focused. Use the pull request template.
4. Ensure dotnet build and dotnet test pass locally before requesting review.

## Safety rules for code changes

ODT can modify persistent device state. Reviewers will reject changes that:

- Bypass or weaken the explicit-confirmation guard for write/flash/erase/reboot
  operations.
- Allow operations to run without an identified target.
- Skip checksum/hash validation when handling firmware artifacts.
- Introduce command-injection or unsafe argument escaping in tool invocation.
- Trust device identity by assumption rather than observed evidence.

If you add a new operation, classify it (read-only / reversible / destructive),
require explicit confirmation for reversible/destructive actions, and document the
risk and verification steps.

## Code style

The repo uses ImplicitUsings=enable, Nullable=enable, and WarningsAsErrors=true -
warnings fail the build. Follow the .editorconfig for formatting. Keep public APIs
documented and null-safe.
