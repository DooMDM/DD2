# Third-party components and interoperability

- **Oodle / RAD Game Tools / Epic Games:** the owner-requested LOCAL bundled executable embeds the owner's existing Oodle 9 x64 runtime. It is not downloaded by the application. The source ZIP does not contain the DLL. This project's MIT license does not grant rights to the proprietary runtime. Confirm applicable redistribution permission before publishing the bundled EXE, even if distribution is free. See [Oodle](https://www.radgametools.com/oodle.htm) and [RAD licensing](https://www.radgametools.com/sales.htm). No blanket redistribution authorization is asserted here.
- **.NET 8.0.30 and Windows Desktop runtime:** included in the portable EXE. Original licenses and notices from the official NuGet runtime packages are in `licenses/`. Source: [dotnet/runtime](https://github.com/dotnet/runtime), [dotnet/winforms](https://github.com/dotnet/winforms), [dotnet/wpf](https://github.com/dotnet/wpf).
- **STALKER 2 identifiers:** game and effect names identify compatible structures. Core/effect-types.json contains identifier-to-type classifications, not prototype bodies, balancing values, artwork, audio or quests. Trademarks belong to their owners. The project is unaffiliated with GSC Game World.

No private game saves or developer settings are included. The source archive also excludes game configuration files, proprietary native libraries and the third-party parser used to derive the compatibility catalog. Tools/BuildCatalog.mjs requires developer-supplied local inputs, not runtime dependencies.

The utility has no online service, update downloader, analytics, telemetry, fees or subscription.
