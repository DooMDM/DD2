// Development-only: generate an interoperability table of effect identifiers
// and their numeric-type classification. No game assets or prototype bodies.
// node BuildCatalog.mjs <local binCfgParser.mts> <local EffectPrototypes.cfg.bin>
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';
const [parser, cfg] = process.argv.slice(2);
assert(parser && cfg, 'Supply local parser and local game configuration paths');
const { readBinaryCfg } = await import(pathToFileURL(parser));
const records = readBinaryCfg(readFileSync(cfg));
const types = {};
for (const r of records) {
  assert(typeof r.SID === 'string' && typeof r.Type === 'string');
  assert(!Object.hasOwn(types, r.SID), `Duplicate ${r.SID}`);
  types[r.SID] = r.Type === 'EEffectType::Damage' ? 'Damage' : 'Other';
}
assert.equal(types.FireBreathDamage, 'Damage');
assert.equal(types.HealthMechanics, 'Other');
const out = new URL('../Core/effect-types.json', import.meta.url);
assert(!existsSync(out), 'Will not overwrite an existing catalog');
writeFileSync(out, JSON.stringify({ format: 182, game: '2.0.4', types }, null, 2) + '\n', { flag: 'wx' });
console.log(`Generated ${Object.keys(types).length} identifier classifications; no prototype data copied.`);
