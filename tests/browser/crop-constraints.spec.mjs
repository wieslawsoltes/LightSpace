import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, stableBox, click, drag, slider, reveal, shot } from './support.mjs';
const crop = async page => (await state(page)).cropTool;
const saved = page => expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
const near = (a, b) => expect(a).toBeCloseTo(b, 4);
const bounds = value => value.bounds;
async function dragWith(page, id, dx, dy, modifier, cancel = false) {
  const box = await stableBox(page, id); const x = box.x + box.width / 2, y = box.y + box.height / 2;
  if (modifier) await page.keyboard.down(modifier);
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x + dx, y + dy, { steps: 10 });
  if (cancel) await page.keyboard.press('Escape');
  await page.mouse.up(); if (modifier) await page.keyboard.up(modifier);
}
async function backup(page, name) {
  await mkdir('artifacts/browser-exports', { recursive: true });
  const waiting = page.waitForEvent('download'); await click(page, 'Save catalog');
  const path = `artifacts/browser-exports/${name}.lightspace`; await (await waiting).saveAs(path);
  const catalog = JSON.parse(await readFile(path, 'utf8')); return catalog.Photos.find(p => p.Id === catalog.ActivePhoto);
}
async function fillRatio(page, id, text) {
  // This is genuine keyboard input to the Uno TextBox, not diagnostic state mutation.
  await reveal(page, id); await click(page, id); await page.keyboard.press('ControlOrMeta+A'); await page.keyboard.insertText(text);
}

test('locked crop handles preserve output aspect, anchors, transactions and shader caches', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .55); await saved(page);
  await click(page, 'Crop photo'); await click(page, 'Crop 3 × 2');
  await expect.poll(async () => (await crop(page)).locked).toBe(true);
  const initial = bounds(await crop(page)); await saved(page); await page.waitForTimeout(300);
  const before = await state(page);
  await drag(page, 'crop-bottom-right', -90, -40);
  await expect.poll(async () => (await state(page)).revision).toBe(before.revision + 1);
  let current = await crop(page); near(current.outputAspect, 1.5); near(current.bounds.left, initial.left); near(current.bounds.top, initial.top);
  const afterCorner = current.bounds;
  await drag(page, 'crop-left', 45, 35); current = await crop(page); near(current.outputAspect, 1.5);
  near(current.bounds.right, afterCorner.right); near(current.bounds.top + current.bounds.bottom, afterCorner.top + afterCorner.bottom);
  const after = await state(page);
  expect(after.performance.viewport.imageDecodes).toBe(before.performance.viewport.imageDecodes);
  expect(after.performance.viewport.shaderBuilds).toBe(before.performance.viewport.shaderBuilds);
  expect(after.performance.inspectorBuilds).toBe(before.performance.inspectorBuilds);
  expect(after.photography.presentation).toEqual(before.photography.presentation);
  await click(page, 'Undo'); await expect.poll(async () => bounds(await crop(page))).toEqual(afterCorner);
  await click(page, 'Undo'); await expect.poll(async () => bounds(await crop(page))).toEqual(initial);
  await click(page, 'Redo'); await expect.poll(async () => bounds(await crop(page))).toEqual(afterCorner);
  const revision = (await state(page)).revision;
  await dragWith(page, 'crop-bottom-right', -30, -30, undefined, true);
  expect((await state(page)).revision).toBe(revision); expect(bounds(await crop(page))).toEqual(afterCorner);
  await shot(page, 'crop-locked-handles'); await mkdir('artifacts/browser-exports', { recursive: true });
  await writeFile('artifacts/browser-exports/crop-performance.json', JSON.stringify({ before: before.performance, after: after.performance,
    beforePresentation: before.photography.presentation, afterPresentation: after.photography.presentation,
    scope: 'Real locked corner/edge pointer drags, cached source and shader work; not physical GPU timing.' }, null, 2));
});

test('center resize, free resize and temporary Shift locking obey independent gesture policies', async ({ page }) => {
  await boot(page); await click(page, 'Crop photo'); await click(page, 'Crop 1 × 1');
  const start = bounds(await crop(page));
  await dragWith(page, 'crop-bottom-right', -55, -35, 'Alt');
  let current = await crop(page); near(current.outputAspect, 1);
  near(current.bounds.left + current.bounds.right, start.left + start.right);
  near(current.bounds.top + current.bounds.bottom, start.top + start.bottom);
  await click(page, 'Crop aspect lock'); expect((await crop(page)).locked).toBe(false);
  const lockedAspect = (await crop(page)).outputAspect;
  await dragWith(page, 'crop-right', -40, 0, 'Shift'); near((await crop(page)).outputAspect, lockedAspect);
  expect((await crop(page)).locked).toBe(false);
  await drag(page, 'crop-right', -40, 0);
  expect(Math.abs((await crop(page)).outputAspect - lockedAspect)).toBeGreaterThan(.03);
  const previous = bounds(await crop(page)); await drag(page, 'crop-center', -2000, -2000);
  current = await crop(page); near(current.bounds.left, 0); near(current.bounds.top, 0);
  near(current.bounds.right - current.bounds.left, previous.right - previous.left);
  near(current.bounds.bottom - current.bounds.top, previous.bottom - previous.top);
});

test('custom ratios and X swap use output orientation and persist correct exported dimensions', async ({ page }) => {
  await boot(page); await click(page, 'Crop photo'); await click(page, 'Rotate right');
  await fillRatio(page, 'crop-ratio-width', '7'); await fillRatio(page, 'crop-ratio-height', '5');
  await click(page, 'Apply custom crop ratio'); await expect.poll(async () => (await crop(page)).outputAspect).toBeCloseTo(1.4, 4);
  expect((await crop(page)).bounds.quarterTurns).toBe(1);
  const previous = bounds(await crop(page)); const rating = (await state(page)).rating;
  // Move keyboard focus away from the text fields without changing framing.
  const frame = await stableBox(page, 'canvas'); await page.mouse.click(frame.x + 4, frame.y + 4);
  await page.keyboard.press('x'); await expect.poll(async () => (await crop(page)).outputAspect).toBeCloseTo(5 / 7, 4);
  expect((await state(page)).rating).toBe(rating); expect((await crop(page)).bounds.quarterTurns).toBe(1);
  const revision = (await state(page)).revision;
  await fillRatio(page, 'crop-ratio-width', 'NaN'); await click(page, 'Apply custom crop ratio');
  expect((await state(page)).revision).toBe(revision); near((await crop(page)).outputAspect, 5 / 7);
  await reveal(page, 'Apply crop'); await click(page, 'Apply crop'); await saved(page);
  const photo = await backup(page, 'crop-constrained'); const expected = bounds(await crop(page));
  expect(photo.State.Crop).toMatchObject({ Left: expected.left, Top: expected.top, Right: expected.right, Bottom: expected.bottom, QuarterTurns: 1 });
  await click(page, 'Export'); const waiting = page.waitForEvent('download'); await click(page, 'Export file');
  const path = 'artifacts/browser-exports/crop-constrained.jpg'; await (await waiting).saveAs(path);
  const bytes = await readFile(path); const dimensions = await page.evaluate(async base64 => {
    const blob = new Blob([Uint8Array.from(atob(base64), c => c.charCodeAt(0))]); const image = await createImageBitmap(blob);
    const size = { width: image.width, height: image.height }; image.close(); return size;
  }, bytes.toString('base64'));
  expect(Math.abs(dimensions.width - dimensions.height * 5 / 7)).toBeLessThan(2);
  await boot(page); expect(bounds(await crop(page))).toEqual(expected);
  expect((await crop(page)).locked).toBe(false); // Session tool state is intentionally not catalog data.
});

test('composition guides are view-only and keyboard shortcuts leave photo flags and settings intact', async ({ page }) => {
  await boot(page); await click(page, 'Crop photo'); await click(page, 'Crop 4 × 5'); await saved(page);
  const before = await state(page); const photo = await backup(page, 'crop-before-guides');
  const canvas = await stableBox(page, 'canvas'); await page.mouse.click(canvas.x + 4, canvas.y + 4);
  for (const guide of [1, 2, 3, 4, 5, 0]) { await page.keyboard.press('o'); await expect.poll(async () => (await crop(page)).guide).toBe(guide); }
  for (let i = 0; i < 4; i++) await page.keyboard.press('o');
  await expect.poll(async () => (await crop(page)).guide).toBe(4);
  await page.keyboard.press('Shift+O'); await expect.poll(async () => (await crop(page)).reversed).toBe(true);
  await shot(page, 'crop-triangle-guide');
  const after = await state(page); expect(after.revision).toBe(before.revision);
  expect(after.performance.viewport.shaderBuilds).toBe(before.performance.viewport.shaderBuilds);
  const unchanged = await backup(page, 'crop-after-guides'); expect(unchanged.State).toEqual(photo.State);
  await page.mouse.click(canvas.x + 4, canvas.y + 4); await page.keyboard.press('Enter');
  await expect.poll(async () => (await state(page)).tool).toBe('Edit');
});

test('crop tools retain correct interaction and compact layout on a resized workspace', async ({ page }) => {
  await page.setViewportSize({ width: 1100, height: 850 }); await boot(page);
  await click(page, 'Crop photo'); await click(page, 'Crop 16 × 9');
  await drag(page, 'crop-bottom-right', -40, -25); near((await crop(page)).outputAspect, 16 / 9);
  const start = bounds(await crop(page)); const revision = (await state(page)).revision;
  const handle = await stableBox(page, 'crop-right'); await page.mouse.move(handle.x + 7, handle.y + 7); await page.mouse.down();
  await page.mouse.move(handle.x - 30, handle.y + 7, { steps: 5 });
  // Browser viewport emulation is not an acknowledgement that Uno processed
  // the root/layout notification. Observe the app's cancellation while capture
  // is still held, then release; do not race release against deferred layout.
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(true);
  await page.setViewportSize({ width: 1024, height: 800 });
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(false);
  await stableBox(page, 'canvas'); await page.mouse.up();
  await expect.poll(async () => bounds(await crop(page))).toEqual(start);
  expect((await state(page)).revision).toBe(revision);
  await shot(page, 'crop-compact');
  await reveal(page, 'Apply crop'); await click(page, 'Apply crop'); await expect.poll(async () => (await state(page)).tool).toBe('Edit');
});
