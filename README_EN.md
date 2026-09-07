# STALKER 2 Fire Save Repair 0.6.3

Free offline Windows x64 utility. **English is selected on every launch**; the **EN / RU** buttons switch all application controls, scan results, explanations and repair confirmations immediately. Native file pickers follow the Windows language. One self-contained EXE; no installation, login, subscription, runtime download or extra component selection.

Version 0.6.3 uses a media-page style dark interface with separate cards for the save source, scan results and selected-save details. The scan button sits directly next to the folder picker, launcher leftovers and technical glyphs are removed, and English/Russian switching works across controls, results and confirmations.

Scans a save folder, flags player FireBreathDamage and repairs only the supported permanent-fire case. Confirmed format: **182**, game **2.0.4**. Not a general save editor or all-negative-effects remover. Fire detection alone does not prove a stuck effect: the save may have been made inside an anomaly.

Select `Data` / `SaveGames`, click **Scan folder**, select an eligible row, close the game, then click **Repair selected save**.

The selected file is **always replaced in place**. **Create backup** controls ONLY whether an original backup is created. Checked by default: verified backup before replacement. Unchecked: replacement without a backup, after an explicit warning. No hidden permanent SAV copy is created in no-backup mode. A temporary file is used for atomic writing.

Reports and optional `original/<same save name>.sav` are stored in a unique subfolder under `FireSaveRepair-results` beside the selected save. Open it with **Report / backup**. No manual installation is needed after success. Other saves and CampaignsSave.sav are untouched. Pause cloud sync when replacing.

Test that exact slot in game with the trainer OFF and outside an anomaly. If needed, close the game and restore the same-named original backup. There is no built-in rollback when backup was disabled.

**Fire · ready to repair**: supported fire case. **Fire · repair blocked**: fire found, but concurrent Damage (for example radiation), unknown effects or unsupported state prevent repair. **No fire effect** only means FireBreathDamage was not found. **Could not check** means unknown/invalid input, not a healthy save. A short reason appears below the list; **Details** shows the complete explanation. File timestamps are not quest names, playtime or thumbnails.

The repair removes a 36-byte fire record and 8-byte Damage aggregate, clears the matching Other-source Damage additive cache, and updates counts and the dictionary pointer. Every other raw byte is preserved. No donor save is used. CRC, structure, known effect types, coexisting damage, exact decode round trip, source hashes and optional backups guard the operation.

The save schema is partial. Limits: 128 MiB packed / 256 MiB raw / 500 saves. Do not use with mods that redefine effect types. Unknown cases fail closed. No network calls, telemetry or mod changes. Unsigned community utility, unaffiliated with GSC Game World; do not disable Windows protection to run it.

MIT applies to our source, not automatically to third-party components. See THIRD_PARTY.md and licenses/ for their separate terms. Private saves and native proprietary components are excluded from the source archive. See README.md, TESTING.md and docs/FORMAT.md for details.
