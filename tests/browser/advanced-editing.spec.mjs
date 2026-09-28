import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, box, click, slider, reveal, shot } from './support.mjs';

async function stroke(page, points, { erase = false, cancel = false } = {}) {
  const image = await box(page, 'image');
  const position = ([x, y]) => [image.x + image.width * x, image.y + image.height * y];
  if (erase) await page.keyboard.down('Alt');
  await page.mouse.move(...position(points[0])); await page.mouse.down();
  for (const point of points.slice(1)) await page.mouse.move(...position(point), { steps: 8 });
  if (cancel) await page.keyboard.press('Escape');
  await page.mouse.up(); if (erase) await page.keyboard.up('Alt');
  await page.mouse.move(20, 20);
}
async function photoPixels(page) {
  await page.mouse.move(20, 20); await page.waitForTimeout(300);
  const image = await box(page, 'image');
  return page.screenshot({ clip: { x: image.x + 2, y: image.y + 2, width: image.width - 4, height: image.height - 4 } });
}

test('arbitrary RGB curves edit real pixels, undo, cancel and recover', async ({ page }) => {
  await boot(page); await click(page, 'RGB curves'); await click(page, 'Curve Red');
  const before = await photoPixels(page); const plot = await box(page, 'rgb-curve');
  const x = plot.x + 10 + (plot.width - 20) * .48;
  const y = plot.y + 10 + (plot.height - 20) * .30;
  await page.mouse.click(x, y);
  await expect.poll(async () => (await state(page)).curves.red.points.length).toBe(3);
  const edited = (await state(page)).curves.red;
  expect(edited.points[1].y).toBeGreaterThan(.65);
  expect((await photoPixels(page)).equals(before)).toBe(false);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).curves.red.points.length).toBe(2);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).curves.red.points.length).toBe(3);
  const point = (await state(page)).curves.red.points[1];
  await page.mouse.move(plot.x + 10 + (plot.width - 20) * point.x, plot.y + 10 + (plot.height - 20) * (1 - point.y));
  await page.mouse.down(); await page.mouse.move(x + 30, y + 40, { steps: 6 });
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(true);
  await page.keyboard.press('Escape'); await page.mouse.up();
  await expect.poll(async () => (await state(page)).curves.red).toEqual(edited);
  await click(page, 'Curve interpolation'); await expect.poll(async () => (await state(page)).curves.red.interpolation).toBe(0);
  await shot(page, 'rgb-curves'); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const saved = (await state(page)).curves; await boot(page); expect((await state(page)).curves).toEqual(saved);
});

test('brush paint, temporary erase, undo and cancellation preserve gesture boundaries', async ({ page }) => {
  await boot(page); await click(page, 'Brush tool');
  const before = await photoPixels(page);
  await stroke(page, [[.3, .45], [.5, .5], [.7, .45]]);
  await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(1);
  expect((await state(page)).maskSettings[0].kind).toBe(3);
  expect((await state(page)).brushes[0].dabs).toBeGreaterThan(10);
  expect((await photoPixels(page)).equals(before)).toBe(false);
  await stroke(page, [[.4, .48], [.6, .48]], { erase: true });
  await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(2);
  expect((await state(page)).brushes[0].lastErase).toBe(true);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(1);
  const revision = (await state(page)).revision;
  await stroke(page, [[.35, .7], [.65, .7]], { cancel: true });
  await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(1);
  expect((await state(page)).revision).toBe(revision);
  await click(page, 'Mask coverage'); await shot(page, 'brush-mask');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const brushes = (await state(page)).brushes; await boot(page); expect((await state(page)).brushes).toEqual(brushes);
});

test('brush density controls and local adjustments reuse coverage textures', async ({ page }) => {
  await boot(page); await click(page, 'Masking'); await click(page, 'New brush mask');
  await click(page, 'section-Brush settings');
  await slider(page, 'advanced-Brush size', .28); await slider(page, 'advanced-Brush flow', .8);
  await stroke(page, [[.25, .55], [.5, .35], [.75, .55]]);
  await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(1);
  await click(page, 'section-Brush settings');
  await reveal(page, 'mask-Exposure');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); await page.waitForTimeout(400);
  const before = await state(page);
  for (const fraction of [.6, .65, .55]) await slider(page, 'mask-Exposure', fraction);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); await page.waitForTimeout(400);
  const after = await state(page);
  expect(after.brushCache.dabsRasterized).toBe(before.brushCache.dabsRasterized);
  expect(after.brushCache.textureBuilds).toBe(before.brushCache.textureBuilds);
  expect(after.curveLookupBuilds).toBe(before.curveLookupBuilds);
  expect(after.performance.inspectorBuilds).toBe(before.performance.inspectorBuilds);
  expect(after.brushes[0].strokes).toBe(1);
  await mkdir('artifacts/browser-exports', { recursive: true });
  await writeFile('artifacts/browser-exports/brush-performance.json', JSON.stringify({ scope: 'Three real local-exposure gestures after a painted mask; work-avoidance counters, not GPU timings', before: { brush: before.brushCache, curveTables: before.curveLookupBuilds, inspectorBuilds: before.performance.inspectorBuilds }, after: { brush: after.brushCache, curveTables: after.curveLookupBuilds, inspectorBuilds: after.performance.inspectorBuilds } }, null, 2));
});

const fixture = `<?xml version="1.0" encoding="utf-8"?>
<x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
<rdf:Description rdf:about="" xmlns:xmp="http://ns.adobe.com/xap/1.0/" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/" xmp:Rating="4" crs:Exposure2012="0.75" crs:Temperature="5600">
<dc:description><rdf:Alt><rdf:li xml:lang="x-default">Światło &amp; mountains</rdf:li></rdf:Alt></dc:description>
<dc:subject><rdf:Bag><rdf:li>alpine</rdf:li><rdf:li>sidecar</rdf:li></rdf:Bag></dc:subject>
<crs:ToneCurvePV2012Blue><rdf:Seq><rdf:li>0, 0</rdf:li><rdf:li>128, 175</rdf:li><rdf:li>255, 255</rdf:li></rdf:Seq></crs:ToneCurvePV2012Blue>
</rdf:Description></rdf:RDF></x:xmpmeta>`;
async function importFixture(page, xml = fixture) {
  await click(page, 'Photo information');
  const pending = page.waitForEvent('filechooser'); await click(page, 'Import XMP'); const picker = await pending;
  await picker.setFiles({ name: 'interop-fixture.xmp', mimeType: 'application/rdf+xml', buffer: Buffer.from(xml) });
  await box(page, 'Apply XMP');
}

test('XMP sidecar import reports unsupported fields, applies undoably and exports losslessly', async ({ page }) => {
  await boot(page); await importFixture(page); await shot(page, 'xmp-import');
  expect((await state(page)).exposure).toBe(0);
  await click(page, 'Apply XMP'); await expect.poll(async () => (await state(page)).exposure).toBe(.75);
  expect((await state(page)).rating).toBe(4); expect((await state(page)).caption).toBe('Światło & mountains');
  expect((await state(page)).keywords).toEqual(['alpine', 'sidecar']); expect((await state(page)).curves.blue.points.length).toBe(3);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).exposure).toBe(0);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).exposure).toBe(.75);
  await click(page, 'Export XMP'); const pending = page.waitForEvent('download'); await click(page, 'Save XMP'); const download = await pending;
  await mkdir('artifacts/browser-exports', { recursive: true }); const path = 'artifacts/browser-exports/roundtrip.xmp'; await download.saveAs(path);
  const xml = await readFile(path, 'utf8'); expect(xml).toContain('ls:Settings'); expect(xml).toContain('crs:Exposure2012="0.75"');
  expect(xml).toContain('Światło &amp; mountains'); expect(xml).toContain('ls:SchemaVersion="4"');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); const expected = await state(page);
  await importFixture(page, xml); await click(page, 'Apply XMP');
  await expect.poll(async () => (await state(page)).curves).toEqual(expected.curves);
  expect((await state(page)).revision).toBe(expected.revision);
});

test('XMP metadata-only import does not alter processing or brush masks', async ({ page }) => {
  await boot(page); await click(page, 'Brush tool'); await stroke(page, [[.3,.5],[.7,.5]]);
  await expect.poll(async () => (await state(page)).brushes[0]?.strokes).toBe(1);
  const before = (await state(page)).brushes;
  await importFixture(page); await click(page, 'XMP metadata only'); await click(page, 'Apply XMP');
  await expect.poll(async () => (await state(page)).rating).toBe(4);
  expect((await state(page)).exposure).toBe(0); expect((await state(page)).brushes).toEqual(before);
  expect((await state(page)).curves.blue.points.length).toBe(2);
});
