import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, stableBox, click, drag, slider, reveal, shot } from './support.mjs';

async function saved(page) { await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved'); }
async function pixels(page, id) {
  await page.mouse.move(20, 15); await page.waitForTimeout(350);
  const b = await stableBox(page, id);
  return page.screenshot({ clip: { x: Math.ceil(b.x + 3), y: Math.ceil(b.y + 3), width: Math.floor(b.width - 6), height: Math.floor(b.height - 6) } });
}
async function catalog(page, name) {
  await mkdir('artifacts/browser-exports', { recursive: true });
  const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const download = await pending; const path = 'artifacts/browser-exports/' + name + '.lightspace';
  await download.saveAs(path); return JSON.parse(await readFile(path, 'utf8'));
}
async function chooseLight(page) {
  await click(page, 'settings-None'); await click(page, 'group-Light');
}

test('reference pins a frozen look, shares source decode and does not create a catalog photo', async ({ page }) => {
  await boot(page); const original = await state(page);
  await click(page, 'Reference view');
  await expect.poll(async () => (await state(page)).reference.visible).toBe(true);
  const initialReference = await pixels(page, 'reference-image');
  const initialActive = await pixels(page, 'image');
  const opened = await state(page);
  expect(opened.revision).toBe(original.revision);
  expect(opened.photos).toBe(original.photos);
  expect(opened.performance.viewport.imageDecodes).toBe(original.performance.viewport.imageDecodes);
  expect(opened.performance.viewport.cachedSources).toBe(original.performance.viewport.cachedSources);
  expect(opened.performance.viewport.sharedSourceHits).toBeGreaterThan(original.performance.viewport.sharedSourceHits);
  await slider(page, 'slider-Exposure', .6);
  await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1, 2);
  expect((await state(page)).reference.exposure).toBe(0);
  expect((await pixels(page, 'reference-image')).equals(initialReference)).toBe(true);
  expect((await pixels(page, 'image')).equals(initialActive)).toBe(false);
  await shot(page, 'reference-comparison');
  const before = await state(page);
  for (const rating of [1, 2, 3]) { await click(page, 'Rate ' + rating); await expect.poll(async () => (await state(page)).rating).toBe(rating); }
  await saved(page); const after = await state(page);
  expect(after.performance.viewport.imageDecodes).toBe(before.performance.viewport.imageDecodes);
  expect(after.performance.viewport.shaderBuilds).toBe(before.performance.viewport.shaderBuilds);
  const portable = await catalog(page, 'reference-active'); expect(portable.Photos.length).toBe(original.photos);
  await writeFile('artifacts/browser-exports/reference-performance.json', JSON.stringify({
    scope: 'Real reference/active view with shared original; three rating edits; counters not GPU durations',
    before: before.performance.viewport, after: after.performance.viewport, sourcePhotos: portable.Photos.length
  }, null, 2));
  await click(page, 'Close reference'); await expect.poll(async () => (await state(page)).reference.visible).toBe(false);
  expect((await state(page)).performance.viewport.cachedImages).toBeLessThan(after.performance.viewport.cachedImages);
  await boot(page); expect((await state(page)).reference.visible).toBe(false); expect((await state(page)).exposure).toBeCloseTo(1, 2);
});

test('reference stays pinned across navigation and supports stacked and linked views', async ({ page }) => {
  await boot(page); await click(page, 'Reference view');
  const name = (await state(page)).activePhoto; await click(page, 'photo-1');
  await expect.poll(async () => (await state(page)).activePhoto).not.toBe(name);
  expect((await state(page)).reference.name).toBe(name);
  const revision = (await state(page)).revision;
  const b = await stableBox(page, 'reference-canvas');
  await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2); await page.mouse.wheel(0, 120);
  await expect.poll(async () => (await state(page)).reference.referenceNavigation.zoom).not.toBe(1);
  let current = (await state(page)).reference;
  expect(current.activeNavigation.zoom).toBeCloseTo(current.referenceNavigation.zoom, 5);
  await click(page, 'Link reference navigation'); const previous = (await state(page)).reference.activeNavigation.zoom;
  await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2); await page.mouse.wheel(0, 120);
  await expect.poll(async () => (await state(page)).reference.referenceNavigation.zoom).not.toBe(current.referenceNavigation.zoom);
  expect((await state(page)).reference.activeNavigation.zoom).toBe(previous);
  await click(page, 'Reference fit both'); await click(page, 'Reference layout');
  await expect.poll(async () => (await state(page)).reference.stacked).toBe(true);
  const reference = await stableBox(page, 'reference-canvas'); const active = await stableBox(page, 'canvas');
  expect(reference.y + reference.height).toBeLessThanOrEqual(active.y + 1);
  expect((await state(page)).revision).toBe(revision); await shot(page, 'reference-stacked');
  await click(page, 'Grid view'); expect((await state(page)).view).toBe('Grid');
  await click(page, 'Detail view'); expect((await state(page)).reference.visible).toBe(true);
  await click(page, 'Pin active reference'); expect((await state(page)).reference.name).toBe((await state(page)).activePhoto);
});

test('selective copy and paste retain target white balance, crop and rating and undo atomically', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .65);
  await click(page, 'Copy settings'); await chooseLight(page); await shot(page, 'selective-copy'); await click(page, 'Copy selected settings');
  await click(page, 'photo-1'); await reveal(page, 'slider-Temperature'); await slider(page, 'slider-Temperature', .65);
  await click(page, 'Rate 2'); await click(page, 'Crop photo'); await click(page, 'Crop 1 × 1'); await click(page, 'Apply crop');
  const before = await catalog(page, 'selective-before'); const targetId = before.ActivePhoto;
  const target = before.Photos.find(p => p.Id === targetId);
  const revision = (await state(page)).revision;
  await click(page, 'Paste selected settings'); await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1.5, 2);
  expect((await state(page)).revision).toBe(revision + 1);
  const after = await catalog(page, 'selective-after'); const actual = after.Photos.find(p => p.Id === targetId);
  expect(actual.State.Develop.Temperature).toBe(target.State.Develop.Temperature);
  expect(actual.State.Crop).toEqual(target.State.Crop); expect(actual.State.Rating).toBe(2);
  expect(actual.Original).toBe(target.Original);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).exposure).toBe(target.State.Develop.Exposure);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1.5, 2);
});

test('selective synchronization and reference matching use the captured source look', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .6); await click(page, 'Reference view');
  await slider(page, 'slider-Exposure', .7);
  await click(page, 'Apply reference settings'); await chooseLight(page); await click(page, 'Apply selected settings');
  await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1, 2);
  await click(page, 'Close reference');
  await click(page, 'photo-1'); await page.keyboard.down('Control'); await click(page, 'photo-0'); await page.keyboard.up('Control');
  await click(page, 'Synchronize settings'); await chooseLight(page); await shot(page, 'selective-sync');
  const revision = (await state(page)).revision; await click(page, 'Apply selected settings');
  await expect.poll(async () => (await state(page)).revision).toBe(revision + 1);
  const result = await catalog(page, 'synchronized');
  expect(result.Photos[0].State.Develop.Exposure).toBeCloseTo(1, 2);
  expect(result.Photos[1].State.Develop.Exposure).toBeCloseTo(1, 2);
  await click(page, 'Undo'); const undone = await catalog(page, 'synchronized-undo');
  expect(undone.Photos[1].State.Develop.Exposure).toBe(0);
  expect(undone.Photos[0].State.Develop.Exposure).toBeCloseTo(1, 2);
});
