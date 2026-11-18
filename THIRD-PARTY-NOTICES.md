# Third-party assets and dependencies

All scene resources are bundled locally; no CDN is required at runtime.

| Component | Version / source | Licence and evidence |
| --- | --- | --- |
| Babylon.js and glTF loaders | 9.37.0 npm packages | Apache-2.0; original texts in Engine Assets/licenses and bundle headers retained |
| Kenney Cube Pets | Included original licence identifies 2.0, 26 March 2026 | CC0; original Kenney licence, four GLBs and shared colour-map texture included |
| seanstardev.RMVC | 3.2.0 NuGet | MIT; original licence copied into Engine Assets/licenses |
| Newtonsoft.Json | 13.0.3 NuGet | MIT; original package licence copied into Engine Assets/licenses |
| .NET browser runtime | Runtime pack resolved by the pinned SDK (first upload: 9.0.1) | Original runtime LICENSE and THIRD-PARTY-NOTICES copied into browser output licenses by Publish.ps1 |
| Microsoft.Web.WebView2 | 1.0.4314.35 NuGet | Original package licence/NOTICE in Engine Assets/licenses; installed runtime has its own distribution terms |

The original [Kenney page](https://kenney.nl/assets/cube-pets) returned HTTP 403.
Assets and original licence came from the public
[DesktopPet mirror at a pinned commit](https://github.com/SixOfFive/DesktopPet/tree/4eeb333e1866f52163af1ea2f9c02d499141ddce/kenney_cube-pets_1.0).
No application code from that repository was imported. Its folder says 1.0, but
the preserved original licence identifies Cube Pets 2.0.

The [asset manifest](ZooSimulator.App.Shared.Engine/Assets/asset-manifest.json)
records exact URLs, versions, sizes, SHA-256 hashes and inspected animation names.
The four GLBs total 581,936 bytes; the shared texture adds 10,915 bytes.
Chick is the clock mascot; the other three species populate the enclosure. Each model
contains static, idle, walk, run, eat, dance and positive/negative gesture clips.
No death clip is claimed: incapacitation/death use stopped motion and a pose transform.

Terrain, trees, fences, pond, signage, clock, health bars and tray are authored in
this project using geometry and browser UI. No additional environment or clock
pack was needed. Food icons use platform emoji fonts and may vary between hosts.

The application paw-and-leaf icon is original project artwork, with SVG source and
PNG/ICO variants owned by Engine Assets/Brand. It is not a downloaded asset pack.
