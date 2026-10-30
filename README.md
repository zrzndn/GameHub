# Windows Arcade Hub

Windows Arcade Hub is a .NET 10 Windows Presentation Foundation desktop application containing six locally playable games:

- Snake
- Tic-Tac-Toe
- 2048
- Minesweeper
- Memory Match
- Pong

## Requirements

- Windows 10 or later
- .NET 10 SDK to build and test

## Build and test

From the project directory:

```powershell
dotnet build WindowsITToolkit.csproj -c Release
dotnet test tests\WindowsITToolkit.Tests\WindowsITToolkit.Tests.csproj -c Release
```

## Publish a self-contained x64 build

```powershell
dotnet publish WindowsITToolkit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\artifacts\publish\win-x64
```

Run `WindowsITToolkit.exe` from that output directory. The first launch may take slightly longer while native runtime components are extracted.

## GitHub Pages project website

The static introduction site lives in `docs`. It is separate from the WPF game app; GitHub Pages cannot run the Windows desktop games in a browser.

To host the site for free:

1. Create a **public** GitHub repository and push this project to its `main` or `master` branch.
2. In the repository, open **Settings → Pages** and set the build and deployment source to **GitHub Actions**.
3. The included `.github/workflows/pages.yml` workflow deploys `docs` automatically after changes to the site are pushed. You can also start it from the **Actions** tab with **Run workflow**.
4. Open the Pages URL shown in the workflow deployment, usually `https://OWNER.github.io/REPOSITORY/`.

The website's download button points to the repository's latest GitHub Release. Create and attach a Windows package there after publishing the app. If no release has been created yet, use the build and publish commands above.

## Using the hub

Use the navigation bar to open the library, favorites, recent games, statistics, settings, or About section. Search and category filters are available in the library. Favorite games with the star control on each card. Keyboard input is captured only while its game view is active; use each game's Back to Library button to leave it.

Pong supports W/S or Up/Down to move the player paddle and Space to pause or resume. 2048 supports arrow keys and WASD. Other controls are shown in their game views.

## Local data

Preferences, favorites, recent games, and game-launch counts are stored under `%LOCALAPPDATA%\WindowsArcadeHub\settings.json`. High scores and game records are also stored in LocalApplicationData, in their existing game-specific files. Nothing is written to the project source directory during normal play.

## Statistics

Statistics report recorded game launches and records currently maintained by the games. Pong tracks completed matches, wins, and best rally. Games that do not yet persist match outcomes are not presented as if those outcomes were measured.

## Development

The application identity and project remain `WindowsITToolkit`. Game rules and physics are implemented in game-specific engines, with WPF views hosted by the existing main-window content host.
"# GameHub" 
