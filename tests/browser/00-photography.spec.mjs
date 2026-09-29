import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, stableBox, click, drag, slider, reveal, shot } from './support.mjs';

const events = new WeakMap();
const fatal = /memory access out of bounds|runtime already exited|RuntimeError|Aborted\(|out of memory/i;
test.beforeEach(async ({ page }) => {
  const log = []; events.set(page, log);
  page.on('pageerror', e => log.push({ type: 'pageerror', text: e.stack || e.message }));
  page.on('crash', () => log.push({ type: 'crash', text: 'Browser renderer crashed' }));
  page.on('console', e => { if (e.type() === 'error' || fatal.test(e.text())) log.push({ type: e.type(), text: e.text() }); });
});
test.afterEach(async ({ page }, info) => {
  const log = events.get(page) || [];
  await info.attach('photography-console', { body: JSON.stringify(log, null, 2), contentType: 'application/json' });
  if (info.status !== info.expectedStatus) {
    console.log(JSON.stringify(log));
    try { console.log('PHOTOGRAPHY_STATE', JSON.stringify(await state(page))); } catch {}
  }
  expect(log.filter(e => e.type === 'pageerror' || e.type === 'crash' || fatal.test(e.text()))).toEqual([]);
});
const photo = async page => (await state(page)).photography;
async function saved(page) { await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); }
async function importFile(page, name) {
  const pending = page.waitForEvent('filechooser'); await click(page, 'Add photos');
  await (await pending).setFiles('artifacts/fixtures/' + name);
  await expect.poll(async () => (await state(page)).activePhoto).toBe(name); await saved(page);
}
async function storedState(page) {
  await saved(page);
  return page.evaluate(async () => {
    const { Catalog: catalog } = JSON.parse(await globalThis.lightSpaceRecovery.readManifest());
    return catalog.Photos.find(p => p.Id === catalog.ActivePhoto).State;
  });
}
async function jpeg(page, name) {
  await click(page, 'Export'); const pending = page.waitForEvent('download'); await click(page, 'Export file');
  const download = await pending; expect(await download.failure()).toBeNull();
  await mkdir('artifacts/browser-exports', { recursive: true });
  const path = 'artifacts/browser-exports/' + name + '.jpg'; await download.saveAs(path);
  return readFile(path);
}
async function centerPixel(page, bytes) {
  return page.evaluate(async text => {
    const data = Uint8Array.from(atob(text), x => x.charCodeAt(0));
    const image = await createImageBitmap(new Blob([data]));
    const canvas = new OffscreenCanvas(image.width, image.height); const context = canvas.getContext('2d');
    context.drawImage(image, 0, 0);
    const result = { width: image.width, height: image.height, rgba: [...context.getImageData(Math.floor(image.width / 2), Math.floor(image.height / 2), 1, 1).data] };
    image.close(); return result;
  }, bytes.toString('base64'));
}

test('manual optics and projective geometry edit, export, undo and restore correctly', async ({ page }) => {
  await boot(page); await importFile(page, 'architecture-grid.png');
  await click(page, 'Optics panel');
  for (const [name, fraction] of [['Distortion', .75], ['Vignetting', .6], ['RedCyan', .7], ['BlueYellow', .4]])
    await slider(page, 'optics-' + name, fraction);
  await expect.poll(async () => (await photo(page)).optics.distortion).toBe(50);
  expect((await photo(page)).optics).toMatchObject({ vignetting: 20, redCyan: 40, blueYellow: -20 });
  await shot(page, 'manual-optics');
  await click(page, 'Geometry panel');
  for (const [name, fraction] of [['Vertical', .7], ['Horizontal', .35], ['Rotate', .65], ['Aspect', .6], ['Scale', .4], ['XOffset', .55], ['YOffset', .45]]) {
    await reveal(page, 'geometry-' + name); await slider(page, 'geometry-' + name, fraction);
  }
  await reveal(page, 'Constrain crop'); await click(page, 'Constrain crop');
  await expect.poll(async () => (await photo(page)).geometry.constrainCrop).toBe(true);
  expect((await photo(page)).geometry).toMatchObject({ vertical: 40, horizontal: -30, rotate: 13.5, aspect: 20, scale: 110, xOffset: 10, yOffset: -10 });
  const expected = await photo(page);
  await click(page, 'Undo'); await expect.poll(async () => (await photo(page)).geometry.constrainCrop).toBe(false);
  await click(page, 'Redo'); await expect.poll(async () => (await photo(page)).geometry).toEqual(expected.geometry);
  await shot(page, 'projective-geometry');
  const rendered = await centerPixel(page, await jpeg(page, 'corrected-architecture'));
  expect(rendered.width).toBe(640); expect(rendered.height).toBe(480);
  const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const download = await pending; const path = 'artifacts/browser-exports/corrected.lightspace'; await download.saveAs(path);
  const catalog = JSON.parse(await readFile(path, 'utf8')); const active = catalog.Photos.find(p => p.Id === catalog.ActivePhoto);
  expect(catalog.SchemaVersion).toBe(5); expect(active.State.Geometry.Rotate).toBe(13.5); expect(active.State.Optics.Distortion).toBe(50);
  await saved(page); await boot(page);
  expect((await photo(page)).geometry).toEqual(expected.geometry); expect((await photo(page)).optics).toEqual(expected.optics);
});

test('geometry-only gestures reuse the development and optical shader caches', async ({ page }) => {
  await boot(page); await importFile(page, 'architecture-grid.png'); await slider(page, 'slider-Exposure', .54);
  await click(page, 'Optics panel'); await slider(page, 'optics-Distortion', .6);
  await click(page, 'Geometry panel'); await reveal(page, 'Constrain crop'); await click(page, 'Constrain crop');
  await saved(page); await page.waitForTimeout(500); const before = await state(page);
  for (const [name, fraction] of [['Rotate', .65], ['Vertical', .6], ['Horizontal', .4]]) {
    await reveal(page, 'geometry-' + name); await slider(page, 'geometry-' + name, fraction);
  }
  await saved(page); await page.waitForTimeout(500); const after = await state(page);
  expect(after.performance.viewport.imageDecodes).toBe(before.performance.viewport.imageDecodes);
  expect(after.performance.viewport.shaderBuilds).toBe(before.performance.viewport.shaderBuilds);
  expect(after.photography.presentation.shaderBuilds).toBe(before.photography.presentation.shaderBuilds);
  expect(after.photography.presentation.geometryBuilds).toBeGreaterThan(before.photography.presentation.geometryBuilds);
  expect(after.curveLookupBuilds).toBe(before.curveLookupBuilds);
  expect(after.brushCache.dabsRasterized).toBe(before.brushCache.dabsRasterized);
  expect(after.performance.inspectorBuilds).toBe(before.performance.inspectorBuilds);
  await mkdir('artifacts/browser-exports', { recursive: true });
  await writeFile('artifacts/browser-exports/geometry-performance.json', JSON.stringify({
    before: { renderer: before.performance.viewport, presentation: before.photography.presentation },
    after: { renderer: after.performance.viewport, presentation: after.photography.presentation },
    scope: 'Three real geometry gestures with histogram updates; construction/work counters, not physical GPU timings.'
  }, null, 2));
});

test('on-canvas straightening commits once and Escape restores the opening angle', async ({ page }) => {
  await boot(page); await importFile(page, 'architecture-grid.png'); await click(page, 'Crop photo');
  await click(page, 'Straighten horizon'); const image = await stableBox(page, 'image');
  const revision = (await state(page)).revision;
  await page.mouse.move(image.x + image.width * .2, image.y + image.height * .4); await page.mouse.down();
  await page.mouse.move(image.x + image.width * .8, image.y + image.height * .5, { steps: 10 }); await page.mouse.up();
  await expect.poll(async () => (await state(page)).revision).toBe(revision + 1);
  await expect.poll(async () => (await state(page)).tool).toBe('Crop');
  const angle = (await photo(page)).geometry.rotate; expect(angle).toBeLessThan(-5); expect(angle).toBeGreaterThan(-10);
  await click(page, 'Straighten horizon');
  await page.mouse.move(image.x + image.width * .3, image.y + image.height * .6); await page.mouse.down();
  await page.mouse.move(image.x + image.width * .7, image.y + image.height * .4, { steps: 8 });
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(true);
  await page.keyboard.press('Escape'); await page.mouse.up();
  await expect.poll(async () => (await photo(page)).geometry.rotate).toBe(angle);
  expect((await state(page)).revision).toBe(revision + 1);
  await click(page, 'Constrain crop'); await shot(page, 'straightening'); await click(page, 'Apply crop');
});

test('white balance picker samples the source and produces a neutral rendered patch', async ({ page }) => {
  await boot(page); await importFile(page, 'white-balance.png');
  await click(page, 'Geometry panel'); await slider(page, 'geometry-Rotate', .6);
  await click(page, 'canvas'); await page.keyboard.press('w');
  await expect.poll(async () => (await state(page)).tool).toBe('WhiteBalance');
  const image = await stableBox(page, 'image'); const samples = (await state(page)).sourceSamplePixels;
  await page.mouse.click(image.x + image.width / 2, image.y + image.height / 2);
  await expect.poll(async () => (await state(page)).tool).toBe('Edit');
  const stored = await storedState(page); expect(stored.Develop.Temperature).toBeLessThan(0);
  expect((await state(page)).sourceSamplePixels - samples).toBe(25);
  const result = await centerPixel(page, await jpeg(page, 'neutral-white-balance'));
  expect(Math.abs(result.rgba[0] - result.rgba[1])).toBeLessThanOrEqual(3);
  expect(Math.abs(result.rgba[2] - result.rgba[1])).toBeLessThanOrEqual(3);
});

test('interactive histogram uses one transaction and clipping indicators do not edit the photo', async ({ page }) => {
  await boot(page); const b = await stableBox(page, 'histogram-plot'); const revision = (await state(page)).revision;
  await page.mouse.move(b.x + b.width * .5, b.y + b.height * .6); await page.mouse.down();
  await page.mouse.move(b.x + b.width * .65, b.y + b.height * .6, { steps: 8 }); await page.mouse.up();
  await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1.5, 1);
  expect((await state(page)).revision).toBe(revision + 1);
  const exposure = (await state(page)).exposure;
  await page.mouse.move(b.x + b.width * .5, b.y + b.height * .6); await page.mouse.down();
  await page.mouse.move(b.x + b.width * .35, b.y + b.height * .6, { steps: 6 });
  await page.keyboard.press('Escape'); await page.mouse.up();
  await expect.poll(async () => (await state(page)).exposure).toBe(exposure); expect((await state(page)).revision).toBe(revision + 1);
  await click(page, 'canvas'); await page.keyboard.press('j');
  await expect.poll(async () => (await photo(page)).clipping).toBe(3);
  await click(page, 'Highlight clipping'); await expect.poll(async () => (await photo(page)).clipping).toBe(1);
  await click(page, 'Shadow clipping'); await expect.poll(async () => (await photo(page)).clipping).toBe(0);
  expect((await state(page)).revision).toBe(revision + 1);
  await click(page, 'Highlight clipping'); await shot(page, 'clipping-indicators');
});

test('resizable panels and focus mode preserve photo state and support cancellation', async ({ page }) => {
  await boot(page); const initial = await photo(page); const revision = (await state(page)).revision;
  await drag(page, 'inspector-resize', -70, 0);
  await expect.poll(async () => (await photo(page)).inspectorWidth).toBe(initial.inspectorWidth + 70);
  await drag(page, 'library-resize', 45, 0);
  await expect.poll(async () => (await photo(page)).libraryWidth).toBe(initial.libraryWidth + 45);
  const grip = await stableBox(page, 'inspector-resize');
  await page.mouse.move(grip.x + grip.width / 2, grip.y + grip.height / 2); await page.mouse.down();
  await page.mouse.move(grip.x - 30, grip.y + grip.height / 2, { steps: 5 }); await page.keyboard.press('Escape'); await page.mouse.up();
  await expect.poll(async () => (await photo(page)).inspectorWidth).toBe(initial.inspectorWidth + 70);
  const imageBefore = await stableBox(page, 'image'); await click(page, 'Focus mode');
  await expect.poll(async () => (await photo(page)).focusMode).toBe(true);
  const focused = await stableBox(page, 'image'); expect(focused.width).toBeGreaterThan(imageBefore.width);
  await shot(page, 'focus-workspace'); await click(page, 'Focus mode');
  await click(page, 'Toggle filmstrip'); await expect.poll(async () => (await photo(page)).filmstripVisible).toBe(false);
  await click(page, 'Toggle filmstrip'); await expect.poll(async () => (await photo(page)).filmstripVisible).toBe(true);
  expect((await state(page)).revision).toBe(revision); await shot(page, 'resizable-workspace');
});
