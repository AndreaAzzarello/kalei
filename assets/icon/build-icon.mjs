// Build tool (not product code): renders the two SVG sources to PNG with a Chromium-based browser and packs
// kalei.ico (16/24/32/48/256). Needs playwright-core (any location) and Edge/Chrome installed.
// Usage: node build-icon.mjs --playwright <path/to/playwright-core> --browser <path/to/msedge.exe> [--preview <dir>]
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createRequire } from 'node:module';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const args = Object.fromEntries(process.argv.slice(2).reduce((a, v, i, all) => (v.startsWith('--') ? [...a, [v.slice(2), all[i + 1]]] : a), []));
if (!args.playwright || !args.browser) { console.error('missing --playwright or --browser'); process.exit(1); }
const here = dirname(fileURLToPath(import.meta.url));
const { chromium } = createRequire(pathToFileURL(resolve(args.playwright) + '/').href)(resolve(args.playwright));
const sources = { large: readFileSync(join(here, 'kalei-app-icon.svg'), 'utf8'), small: readFileSync(join(here, 'kalei-app-icon-small.svg'), 'utf8') };
const sizes = [16, 24, 32, 48, 256];
const pngs = [];
const browser = await chromium.launch({ executablePath: args.browser });
for (const size of sizes) {
  const svg = (size <= 24 ? sources.small : sources.large).replace(/width="256" height="256"/, `width="${size}" height="${size}"`);
  const page = await browser.newPage({ viewport: { width: size, height: size }, deviceScaleFactor: 1 });
  await page.setContent(`<html><body style="margin:0;background:transparent">${svg}</body></html>`);
  pngs.push(await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } }));
  await page.close();
}
await browser.close();
// ICO container: ICONDIR + one ICONDIRENTRY per image + PNG payloads (supported since Windows Vista).
const header = Buffer.alloc(6); header.writeUInt16LE(0, 0); header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
let offset = 6 + 16 * sizes.length;
const entries = sizes.map((s, i) => {
  const e = Buffer.alloc(16);
  e.writeUInt8(s === 256 ? 0 : s, 0); e.writeUInt8(s === 256 ? 0 : s, 1); e.writeUInt8(0, 2); e.writeUInt8(0, 3);
  e.writeUInt16LE(1, 4); e.writeUInt16LE(32, 6); e.writeUInt32LE(pngs[i].length, 8); e.writeUInt32LE(offset, 12);
  offset += pngs[i].length; return e;
});
writeFileSync(join(here, 'kalei.ico'), Buffer.concat([header, ...entries, ...pngs]));
if (args.preview) { mkdirSync(args.preview, { recursive: true }); sizes.forEach((s, i) => writeFileSync(join(args.preview, `icon-${s}.png`), pngs[i])); }
console.log('wrote kalei.ico', sizes.join('/'), 'px,', offset, 'bytes');
