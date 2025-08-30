# SrvSurvey

SrvSurvey is an independent third-party application for use with [Elite Dangerous](https://www.elitedangerous.com) by [Frontier Developments](https://frontier.co.uk). It provides on-screen assistance when a player is near a planet in the SRV, on foot or in ships. It has 3 main functions:
- **Organic scans:** Track the location of organic scans and the distance required for the next scan.
- **Ground target tracking:** Aiming guidance towards latitude/longitude co-ordinates.
- **Guardian sites:** Track visited areas and the locations of items within Guardian ruins and structures.

The application works by analyzing journal files written by the game when played on a PC, tracking the location of the player at the time of various events. It uses this information to render overlay windows atop the game, updated in real-time as the player moves about. For the most part the application is fully automatic, players need to start the application and then just play the game. It will remain hidden until triggered by events from the game.



# Linux (Avalonia) build

This repository now contains an experimental cross‑platform UI built with Avalonia, alongside the existing Windows Forms app. The Avalonia app is minimal for now but compiles and runs on Linux.

## Build (Linux)

Prerequisites: .NET SDK 8.

Steps:

1. Build the Avalonia app only:

   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   dotnet build SrvSurvey.UI.Avalonia/SrvSurvey.UI.Avalonia.csproj -c Release
   ```

2. Publish self‑contained single‑file binary (linux‑x64):

   ```bash
   dotnet publish SrvSurvey.UI.Avalonia/SrvSurvey.UI.Avalonia.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o publish/linux
   ```

3. Run:

   ```bash
   ./publish/linux/SrvSurvey.UI.Avalonia
   ```

Notes:
- The legacy Windows Forms app remains Windows‑only and will fail to restore on Linux.
- Cross‑platform services (clipboard, hotkeys, input devices) are stubbed; functionality will be ported incrementally.

# Installation

Srv Survey is distributed two ways:

- **(Recommended)** An official signed build is available through [the Windows App Store](https://www.microsoft.com/store/productId/9NGT6RRH6B7N). This is updated less often but with higher quality.

- An unsigned build is available through [GitHub releases](https://github.com/njthomson/SrvSurvey/releases), updated frequently. Simply download the .zip file and run `setup.exe`. You will need to manually uninstall a previous version, but you will not lose your prior settings or surveys.



# General usage:

Please see [the wiki](https://github.com/njthomson/SrvSurvey/wiki) for guidance on all the features of SrvSurvey, or ask questions on the [Guardian Science Corps](https://discord.gg/GJjTFa9fsz) Discord server.



# Feedback

Feedback, suggestions or bug reports are always welcome - please [use this form](https://github.com/njthomson/SrvSurvey/issues/new?template=bug_report.md&title=) for bugs or suggestions.



