import { test, expect } from '@playwright/test';
import { mkdir, writeFile, readFile } from 'node:fs/promises';
import { boot, state, box, click, drag, slider, reveal, shot } from './support.mjs';

test('color grading wheel, range selection, undo and schema-5 catalog export', async ({ page }) => {
  await boot(page); await click(page, 'Color grading');
  const wheel = await box(page, 'grading-wheel');
  await page.mouse.move(wheel.x + wheel.width / 2, wheel.y + wheel.height / 2); await page.mouse.down();
  await page.mouse.move(wheel.x + wheel.width / 2 + 45, wheel.y + wheel.height / 2 - 15, { steps: 6 }); await page.mouse.up();
  await expect.poll(async () => (await state(page)).grading.midtones.saturation).toBeGreaterThan(30);
  const graded = (await state(page)).grading.midtones;
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).grading.midtones.saturation).toBe(0);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).grading.midtones.saturation).toBe(graded.saturation);
  await click(page, 'grading-Shadows'); await slider(page, 'grading-Hue', .6); await slider(page, 'grading-Saturation', .22);
  await expect.poll(async () => (await state(page)).grading.shadows.saturation).toBe(22);
  await shot(page, 'color-grading'); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const expected = (await state(page)).grading; const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const download = await pending; await mkdir('artifacts/browser-exports', { recursive: true }); await download.saveAs('artifacts/browser-exports/graded.lightspace');
  const catalog = JSON.parse(await readFile('artifacts/browser-exports/graded.lightspace', 'utf8'));
  expect(catalog.SchemaVersion).toBe(5); expect(catalog.Photos[0].State.Develop.Grading.Shadows.Saturation).toBe(22);
  await boot(page); expect((await state(page)).grading).toEqual(expected);
});

test('linear gradients rotate and move through actual canvas handles', async ({ page }) => {
  await boot(page); await click(page, 'Masking'); await click(page, 'Linear gradient');
  const image = await box(page, 'image');
  await page.mouse.move(image.x + image.width * .2, image.y + image.height * .3); await page.mouse.down();
  await page.mouse.move(image.x + image.width * .75, image.y + image.height * .65, { steps: 8 }); await page.mouse.up();
  await expect.poll(async () => (await state(page)).masks).toBe(1);
  const mask = (await state(page)).maskSettings[0]; expect(mask.kind).toBe(1); expect(Math.abs(mask.angle)).toBeGreaterThan(10);
  const originalAngle = mask.angle; await drag(page, 'mask-fade-end', -65, 45);
  await expect.poll(async () => Math.abs((await state(page)).maskSettings[0].angle - originalAngle)).toBeGreaterThan(5);
  expect((await state(page)).masks).toBe(1);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).maskSettings[0].angle).toBeCloseTo(originalAngle, 3);
  const originalX = (await state(page)).maskSettings[0].x; await drag(page, 'mask-center', 45, 15);
  await expect.poll(async () => (await state(page)).maskSettings[0].x).toBeGreaterThan(originalX + .025);
  await click(page, 'Mask coverage'); await shot(page, 'rotatable-gradient');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
});

test('luminance ranges, coverage, enable/disable, duplicate and delete are functional', async ({ page }) => {
  await boot(page); await click(page, 'Masking'); await click(page, 'Luminance range');
  await expect.poll(async () => (await state(page)).masks).toBe(1); expect((await state(page)).maskSettings[0].kind).toBe(2);
  await click(page, 'Mask coverage'); await shot(page, 'luminance-mask');
  await click(page, 'mask-enabled'); await expect.poll(async () => (await state(page)).maskSettings[0].enabled).toBe(false);
  await click(page, 'mask-enabled'); await expect.poll(async () => (await state(page)).maskSettings[0].enabled).toBe(true);
  await reveal(page, 'mask-Range minimum'); await slider(page, 'mask-Range minimum', .6);
  await expect.poll(async () => (await state(page)).maskSettings[0].rangeMin).toBe(.6);
  const scroll = await box(page, 'inspector-scroll'); await page.mouse.move(scroll.x + 100, scroll.y + 100); await page.mouse.wheel(0, -1800); await page.waitForTimeout(250);
  await click(page, 'Duplicate mask'); await expect.poll(async () => (await state(page)).masks).toBe(2);
  await click(page, 'Delete mask'); await expect.poll(async () => (await state(page)).masks).toBe(1);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).masks).toBe(2);
});

test('metadata updates retain catalog controls and pixel caches', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .55);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); await page.waitForTimeout(500);
  const before = (await state(page)).performance;
  for (const rating of [1, 2, 3, 4, 5, 2, 4]) { await click(page, `Rate ${rating}`); await expect.poll(async () => (await state(page)).rating).toBe(rating); }
  await page.waitForTimeout(500); const after = (await state(page)).performance;
  expect(after.cardBuilds).toBe(before.cardBuilds); expect(after.libraryBuilds).toBe(before.libraryBuilds); expect(after.inspectorBuilds).toBe(before.inspectorBuilds);
  expect(after.viewport.imageDecodes).toBe(before.viewport.imageDecodes); expect(after.viewport.shaderBuilds).toBe(before.viewport.shaderBuilds);
  expect(after.thumbnailRenders).toBe(before.thumbnailRenders); expect(after.thumbnails.cachedBytes).toBeLessThanOrEqual(8 * 1024 * 1024);
  await mkdir('artifacts/browser-exports', { recursive: true });
  await writeFile('artifacts/browser-exports/metadata-performance.json', JSON.stringify({ scope: 'Seven real rating clicks in the unchanged All Photos page; counters, not frame-time benchmarks', before, after }, null, 2));
});

test('comparison divider moves without changing the photo revision', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .57); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const revision = (await state(page)).revision; await click(page, 'Before and after'); await drag(page, 'compare-divider', -160, 0);
  await expect.poll(async () => (await state(page)).comparisonPosition).toBeLessThan(.4);
  expect((await state(page)).revision).toBe(revision); await shot(page, 'before-after');
});
