# Validation of 0.6.3

Version 0.6.3: **47 engine/filesystem checks + 71 UI/localization/layout checks passed**, no build warnings or errors. Tests supplied no external DLL: extraction and use of the embedded runtime were exercised. Engine tests are in `SelfTests.cs`; presentation tests are in `UI/UiTests.cs`.

UI checks verify English on every fresh form, switching existing scan results EN -> RU -> EN without rescanning, both no-backup warnings, translated dynamic and system error messages, English numeric formatting, all Cyrillic engine diagnostic fragments covered, compact window bounds, media-page card surfaces, no technical glyphs in button labels, no multiline scrolling text boxes on the main window, and visible/fitting primary controls at the minimum window size in both languages.

The failed 0.4 startup-load experiment was removed after the game opened to the main menu. The application does not launch the game, pass console commands, rewrite campaign metadata/timestamps, install a mod, change persistent launch options, or inject input. Repair still requires its own confirmation.

- Standard CRC32 vectors, valid envelopes, missing CRC, byte corruption and short input.
- Unsupported raw version, bad dictionary pointer/encoding, truncated input, NaN, invalid SID, oversized array and ambiguous player state.
- Unknown or concurrent Damage effects, changed Fire flags/source and mismatched aggregate/contribution values all refuse repair.
- Fire need not be the first effect. Repaired raw data is 44 bytes shorter; rerunning repair refuses to write again.
- 14 privately held raw fixtures parsed: six supported fire states; healthy, repaired and deliberately incomplete earlier repair states. Six supported fire fixtures repair and reparse successfully.
- The automatic repair result is byte-identical to the raw result that the original player confirmed stopped the burning **in the game**, without rolling back progress.
- Real original SAV decoding and repaired SAV compression + CRC + exact decode were checked. Earlier historical output without CRC is rejected.
- Disposable synthetic files verify scan detection, exclusion/refusal of CampaignsSave.sav, atomic replacement in BOTH checkbox modes, exact optional backups, unique report folders and changed-source refusal. Unchecked backup must produce no permanent original/repaired SAV copy in the report folder.
- User-assisted GUI scan of 25 live saves displayed 10 FireBreathDamage cases, of which 7 were eligible and 3 blocked. The provided screenshot showed a blocked case with concurrent RadiationLevel10Damage. This scan was read-only; it does not prove in-game health for every slot.

The user's working fix is one in-game confirmation, not broad compatibility validation across multiple players, platforms and game builds. GUI construction and scanning were exercised; the replacement transaction was exercised on synthetic disposable files. Do not claim the GUI has game-tested every generated save.

Reproduce public synthetic checks:

```powershell
.\Stalker2FireSaveRepair.exe --self-test
.\Stalker2FireSaveRepair.exe --ui-test
.\Stalker2FireSaveRepair.exe --scan 'C:\copied-test-saves'
```

Bundled builds test the embedded codec without a separate DLL argument. Developer builds without a native resource can use `--dll <trusted-runtime-path>`. For maintainer-held private fixtures, also pass `--corpus <raw-folder>`, `--original-raw <file>`, `--repaired-raw <confirmed-file>`, `--original-sav <file>`, and `--invalid-sav <missing-CRC-file>`. Private saves are deliberately excluded from distribution and are not required for the public synthetic tests.
