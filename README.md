# Little Wild — Zoo Simulator

A zoo simulator for the Benefact Group technical exercise. Fifteen animated animals
live in three habitats, with health, survival, feeding and accelerated zoo time
owned by a C# RMVC application. WPF and browser hosts share the same Engine scene.

[Play in the browser](https://seanstar.co.uk/ZooSimulator/).
Drag food onto an animal or its health card to feed that animal. **Feed whole zoo**
performs the assignment's species-shared feeding operation. **Reset game** starts
a fresh population and a full twenty-second interval.

## Build and publish the browser app

Install the .NET 9 SDK selected by [global.json](global.json): 9.0.102 or a newer
9.0.1xx patch. From this repository's root, run in PowerShell:

```powershell
./ZooSimulator.App.Web/Publish.ps1
```

The script restores and publishes Release, then creates a fresh timestamped
`../.tmp.ai/web-delivery/` folder containing:

- `ZooSimulator/`: complete static upload, including runtime, assets and licences.
- `ZooSimulator-upload.zip`: an alternative to transferring the unpacked folder.
- `UPLOAD-INSTRUCTIONS.txt`: deployment steps and basic troubleshooting.

An explicit output path must be a new directory:

```powershell
./ZooSimulator.App.Web/Publish.ps1 -OutputDirectory ../.tmp.ai/web-delivery/my-build
```

Upload **the contents** of `ZooSimulator/`, including `.htaccess` and `_framework`,
into the website's game directory. For the live demo this is
`/public_html/ZooSimulator/`; `index.html` sits directly inside it. Extract the ZIP
locally first if using it. No PHP application, Node service or server-side .NET
installation is needed. Do not open the HTML directly from disk.

The server must serve WASM and JavaScript modules with suitable MIME types. The
folder-local Apache `.htaccess` provides conditional MIME/compression/cache settings.
If it causes HTTP 500 on a restrictive host, disable only that game's `.htaccess`
and check hosting configuration. After replacing an upload, hard-refresh the page.

Use the script for distribution: it passes `EngineEmbedAssets=false` to the isolated
publish graph, avoiding a second copy of the assets in the Engine assembly.
Ordinary solution/editor builds use the standard Engine reference. Asset-only and
warm publish runs took about 6.4 seconds locally, including ZIP creation. C# edits
can rerun compilation/trimming; clean builds or dependency changes take longer.

## Build and test the solution

The integrated solution includes a Windows WPF host. On Windows with the selected
SDK installed, run from this directory:

```powershell
dotnet restore ZooSimulator.sln
dotnet build ZooSimulator.sln --configuration Release --no-restore
dotnet test ZooSimulator.sln --configuration Release --no-build
```

The core and Engine target .NET Standard 2.0; hosts/tests use .NET 9. RMVC is the
published `seanstardev.RMVC` 3.2.0 NuGet package. No adjacent private checkout is
required to build. Current suites contain **33 tests, with none skipped**.

## Run the WPF app

Install Microsoft's Evergreen WebView2 Runtime, then run:

```powershell
dotnet run --project ZooSimulator.App.WPF --configuration Release
```

In Visual Studio, select `ZooSimulator.App.WPF` as the startup project. Engine
exports its embedded scene into an `Engine` folder beside the executable, so that
location must be writable. The WebView2 profile lives in a `.WebView2` sibling.
The scene uses bundled assets with no runtime CDN or JavaScript build step.

## Project layout

| Project | Responsibility |
| --- | --- |
| [ZooSimulator.Rmvc](ZooSimulator.Rmvc/Facade.cs) | Population, species rules, randomness, time, commands and presentation contracts |
| [ZooSimulator.App.Shared.Engine](ZooSimulator.App.Shared.Engine/GameEngine.cs) | Four thin view adapters, JSON protocol, shared HTML/JS renderer, assets and desktop asset export |
| [ZooSimulator.App.WPF](ZooSimulator.App.WPF/MainWindow.xaml.cs) | Windows shell, package-free MVVM status, WebView2 and dispatcher-aware transport |
| [ZooSimulator.App.Web](ZooSimulator.App.Web/Program.cs) | Browser composition, shell, JS interop and static publishing |

Tests live in each library's `.Test` folder. Start with the core's
[Facade](ZooSimulator.Rmvc/Facade.cs), [CommandBase](ZooSimulator.Rmvc/CommandBase.cs),
[ZooModel](ZooSimulator.Rmvc/Rmvc/Model/Zoo/ZooModel.cs) and
[simulation commands](ZooSimulator.Rmvc/Rmvc/Command/Simulation).

The application exposes separate `IMainView`, `IZooView`, `IClockView` and
`IFeedingView` contracts. Engine supplies four separate adapters. MainView readiness
starts the simulation after graphics/interop are ready; commands coordinate model
updates and mediator presentation. Views receive immutable snapshots and reject
older revisions. Rendering and wandering never change health or authoritative time.

Models, shared JavaScript, CSS and artwork have one source under Engine. The web
project links these files during publishing; its source `wwwroot` holds only the
browser bootstrap and hosting configuration. WPF embeds/exports them; the browser
serves them as static files without also embedding them in its Engine assembly.

## Assignment coverage

| Requirement | Implementation and verification |
| --- | --- |
| Five monkeys, giraffes and elephants, initially 100% | ZooModel initialisation; initial-population tests |
| Floating-point health | Animal uses double; arithmetic/boundary tests |
| One simulated hour per twenty real seconds | ClockProxy/TimerScheduler; scheduler tests and real browser timer checks |
| Independent 0–20% decay per animal per hour | AdvanceHourCmd; scripted journey verifies all fifteen draws and ranges |
| Three 10–25% feeding values, shared by species | FeedZooCmd; scripted species-sharing and draw-count checks |
| Proportional changes capped at 100% | Animal; arithmetic and cap tests |
| Monkeys below 30%, giraffes below 50% die | Species entities; exact/below-boundary tests |
| Elephants below 70% cannot walk and have a recovery window | Elephant; decline, recovery and subsequent-hour tests |
| Periodic animal status, zoo time and feeding | Separate view contracts/mediators; Engine and integration checks |

## Rule interpretations and extensions

- Random percentages are integers with both endpoints included. Health remains an
  unrounded double; display rounding is presentation-only.
- Each percentage applies to current health, rather than subtracting/adding points.
- An elephant first below 70% at hour H has until H+1 to recover. Feeding to at least
  70% clears its deadline. At H+1 an uncleared deadline causes death before new decay.
  A recovered elephant that declines again starts a new window.
- Death is permanent within a game. Dead animals retain their health and ignore
  feeding/decay; hourly operations still draw fifteen values and whole-zoo feeding
  still draws three. The population always contains fifteen animals.
- Time begins at zero. Each current-schedule callback advances one hour. There is no
  wall-time catch-up, pause or persistence. Background tabs can throttle callbacks.
- A simulation command lock coordinates startup, feeding, reset, hourly work and
  notifications. Reset restarts the full interval; queued old callbacks are rejected.
- Target-only drag feeding, reset and visual feedback are deliberate extensions.
  Whole-zoo feeding remains the assignment path. No finite inventory is implemented.

## Verification and remaining limits

The deterministic journey checks draw ranges/counts, proportional results, death,
concurrent commits, view replacement, reset and cleanup. Its authorised substitution
point uses `IRandomProxy` contract lookup; production registers `RandomProxy`, while
the fixture registers `ScriptedRandomProxy`. Other actor accessors remain concrete.
The journey stages share one RMVC creation/shutdown lifetime.

Local browser checks cover moving models, actual twenty-second intervals,
target-only and whole-zoo feeding, reset, drag cancellation and first-frame label
placement on resize. Input is coalesced; card hit-test reads precede render writes.
The main scene is capped at 30fps and the small clock mascot at 15fps. These are
bounded local checks, not guarantees for all hardware or browsers.

The first live browser run is confirmed. Physical ergonomics, long sessions,
mobile layout, other browsers and low-end performance remain refinement work.
The current browser build is interpreted, not AOT; conservative assembly retention
is required and publishing reports known RMVC/Newtonsoft.Json trimming warnings.
The pinned .NET 9 toolchain/runtime needs a support-version review before a
long-term supported release.

WPF's optional `--verify-scene <directory>` argument captures local JSON/PNG evidence.
It finishes with presentation-only weak/dead examples; restart normally afterwards.
Normal launches do not inject test state or open a diagnostic port.

## Assets and licences

See [third-party notices](THIRD-PARTY-NOTICES.md) and the
[asset manifest](ZooSimulator.App.Shared.Engine/Assets/asset-manifest.json) for
versions, provenance, original licences and verified hashes. Keep the shared
`models/Textures/colormap.png` with the GLBs. Terrain/UI and the paw-and-leaf icon
are project-authored. Food emoji appearance depends on the platform's fonts.
