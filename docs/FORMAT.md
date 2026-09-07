# Supported repair profile: raw version 182

This is a conservative partial parser, not a specification of the whole game save format.

## SAV envelope

`uint32LE rawLength | Oodle payload | uint32LE CRC32`

CRC32 is the standard reflected polynomial 0xEDB88320 over the raw-length header and compressed payload, excluding the final four checksum bytes. The Oodle decoder can silently ignore a trailing checksum, which makes an unverified compression-only implementation produce a save the game rejects. This utility validates the envelope before decoding and writes the CRC after encoding.

Verified codec calls: OodleLZ_Decompress with fuzz safety enabled, all decode phases; OodleLZ_Compress with Kraken (8), normal level (4), default options. Encoded output must decode exactly to the intended bytes before any save write.

## Raw data and state location

- uint32 version 182 at offset 0; supported flag byte 0 at offset 4.
- uint32 absolute trailing-dictionaries pointer at offset 5.
- Two trailing dictionary sections with 11 groups each. Groups contain uint16 string counts and uint16 byte-length-prefixed UTF-8 strings. Parsing must end exactly at EOF. Section 1 / group 0 starts with `Player`, then an empty string.
- Search at most 64 KiB after the first validated `struct FUnitModel` marker. The unit's variable-sized prefix is not fully decoded. A candidate state must parse entirely, contain the five anchors BleedingMechanics / HealthMechanics / PsyMechanics / HungerMechanics / SleepMechanics, and be unique in that window.
- Effect array: uint16 count, 36-byte records with supported leading byte 0, uint16 SID index at +1, uint16 source at +7 and float value at +30.
- Aggregate map: uint16 count and 8-byte `(uint32 type, float value)` entries. Type 31 is Damage.
- Four zero bytes, then a uint16-counted contribution map: `(uint32 type, uint16 sourceCount)` followed by 9-byte `(byte source, float additive, float percentage)` values.

Every read is bounded; maps must have unique keys and finite floats. Unknown versions, unexpected markers, encodings, sources, non-unique candidates and unknown effect SIDs prevent repair. The compatibility table is for unmodified 2.0.4 effect types; same-name modded prototypes cannot be identified from a save alone.

## Eligibility and mutation

Require exactly one FireBreathDamage of the confirmed permanent Other-source record shape; no other Damage effects; a positive Damage aggregate equal to the sole nonzero Other-source additive contribution. The Damage contribution must have exactly source 1 (zero additive/percentage) and source 6 (positive additive, zero percentage), in the verified order.

Remove the fire record (36 bytes) and Damage aggregate (8 bytes). Set only source 6's Damage additive float to +0. Decrement the effect count and aggregate count. Decrease the dictionary pointer by 44. Retain dictionary entries and every other byte; do not transplant state from another save. Reparse and verify. Program tests compare the complete result with the successful in-game repair's raw bytes.

The whitelist limits edits before splicing. The untouched tail after the parsed player state and the complete dictionary tail are explicitly compared. The fully compressed file is CRC-checked and decompressed for exact equality. Version 0.2 always atomically replaces the selected original; the checkbox controls ONLY whether a verified original backup is retained. No-backup mode requires an explicit warning and creates no permanent SAV copy. Both modes write an audit report and use a transient file for atomic replacement. Re-check the source hash before commit. A cloud sync restoring a different file later cannot be prevented by this application.

This profile does **not** remove arbitrary negative effects, residual-only damage after earlier manual edits, concurrent radiation damage, new save versions or legitimately re-applied environmental effects.
