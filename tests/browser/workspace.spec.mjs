import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
const base = process.env.LIGHTSPACE_URL || 'http://127.0.0.1:4173/LightSpace/';
const state = page => page.evaluate(() => globalThis.lightSpaceDiagnostics);
const logs = new WeakMap();
test.beforeEach(async ({ page }) => {
  const messages = []; logs.set(page, messages);
  page.on('console', message => messages.push(`${message.type()}: ${message.text()}`));
  page.on('pageerror', error => messages.push(`pageerror: ${error.stack || error.message}`));
  page.on('requestfailed', request => messages.push(`requestfailed: ${request.url()} ${request.failure()?.errorText}`));
});
test.afterEach(async ({ page }, info) => {
  const messages = logs.get(page) || [];
  await info.attach('browser-console', { body: messages.join('\n'), contentType: 'text/plain' });
  if (info.status !== info.expectedStatus) {
    console.log(messages.join('\n'));
    try { console.log('Browser state:', await page.evaluate(() => ({ ready: globalThis.lightSpaceReady, startupError: globalThis.lightSpaceStartupError, diagnostics: globalThis.lightSpaceDiagnostics, filesAvailable: !!globalThis.lightSpaceFiles, text: document.body.innerText }))); } catch {}
  }
});
async function settled(page) {
  // Arranged control geometry can precede the compositor's presented frame.
  await page.locator('.uno-loader').waitFor({ state: 'hidden', timeout: 120000 });
  await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  await page.waitForTimeout(250);
}
async function boot(page) {
  await page.goto(base + (base.includes('?') ? '&' : '?') + 'diagnostics=1', { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => globalThis.lightSpaceDiagnostics || globalThis.lightSpaceStartupError, null, { timeout: 120000 });
  const error = await page.evaluate(() => globalThis.lightSpaceStartupError);
  expect(error, 'The real Uno application must initialize').toBeFalsy();
  await expect.poll(async () => (await state(page))?.widgets?.some(w => w.id === 'slider-Exposure')).toBeTruthy();
  await settled(page);
}
async function bounds(page, id) {
  await expect.poll(async () => (await state(page))?.widgets?.some(w => w.id === id && w.width > 1)).toBeTruthy();
  return (await state(page)).widgets.find(w => w.id === id);
}
async function click(page, id) {
  const box = await bounds(page, id); await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
}
async function exposure(page, fraction) {
  const box = await bounds(page, 'slider-Exposure');
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.mouse.down();
  await page.mouse.move(box.x + 5 + (box.width - 10) * fraction, box.y + box.height / 2, { steps: 5 }); await page.mouse.up();
}
async function screenshot(page, name) {
  await settled(page); await mkdir('artifacts/screenshots', { recursive: true });
  await page.screenshot({ path: `artifacts/screenshots/${name}.png` });
}

test('real Uno layout, development gestures, undo, presets and rendered JPEG export', async ({ page }) => {
  const errors = []; page.on('pageerror', error => errors.push(error.message));
  await boot(page); expect((await state(page)).photos).toBeGreaterThanOrEqual(6);
  await screenshot(page, 'workspace');
  const before = await page.screenshot();
  await exposure(page, .62); await expect.poll(async () => (await state(page)).exposure).toBeGreaterThan(.8);
  await settled(page); const after = await page.screenshot(); expect(before.equals(after)).toBeFalsy();
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).exposure).toBe(0);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).exposure).toBeGreaterThan(.8);
  await click(page, 'Presets'); await click(page, 'Preset Alpine light');
  await expect.poll(async () => Math.round((await state(page)).exposure * 100)).toBe(20);
  await click(page, 'Back to editing'); await screenshot(page, 'developed');
  await click(page, 'Export');
  const pending = page.waitForEvent('download'); await click(page, 'Export file'); const download = await pending;
  await mkdir('artifacts/browser-exports', { recursive: true }); const path = 'artifacts/browser-exports/edited.jpg'; await download.saveAs(path);
  const bytes = await readFile(path); expect(bytes.length).toBeGreaterThan(1000); expect(bytes.subarray(0, 2).toString('hex')).toBe('ffd8');
  expect(errors).toEqual([]);
});

test('photo navigation, crop workflow, interactive mask creation and native file import', async ({ page }) => {
  await boot(page); const first = (await state(page)).activePhoto; const count = (await state(page)).photos;
  await click(page, 'photo-1'); await expect.poll(async () => (await state(page)).activePhoto).not.toBe(first);
  await click(page, 'Crop photo'); await expect.poll(async () => (await state(page)).tool).toBe('Crop');
  await click(page, 'Crop 1 × 1'); await click(page, 'Apply crop'); await expect.poll(async () => (await state(page)).tool).toBe('Edit');
  await click(page, 'Masking'); const box = await bounds(page, 'canvas');
  await page.mouse.move(box.x + box.width * .35, box.y + box.height * .35); await page.mouse.down();
  await page.mouse.move(box.x + box.width * .65, box.y + box.height * .65, { steps: 6 }); await page.mouse.up();
  await expect.poll(async () => (await state(page)).masks).toBe(1); await screenshot(page, 'local-mask');
  const chooserPromise = page.waitForEvent('filechooser'); await click(page, 'Add photos'); const chooser = await chooserPromise;
  await chooser.setFiles('artifacts/fixtures/import-fixture.png');
  await expect.poll(async () => (await state(page)).photos).toBe(count + 1);
  await expect.poll(async () => (await state(page)).activePhoto).toBe('import-fixture.png');
});

test('device recovery, keyboard rating and compact workspace layout', async ({ page }) => {
  await boot(page); await click(page, 'Rate 2'); await exposure(page, .58);
  await expect.poll(async () => (await state(page)).rating).toBe(2);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  expect((await state(page)).recovery.savedRevision).toBe((await state(page)).revision);
  const expected = (await state(page)).exposure; await boot(page);
  await expect.poll(async () => (await state(page)).rating).toBe(2);
  await expect.poll(async () => (await state(page)).exposure).toBe(expected);
  await click(page, 'canvas'); await page.keyboard.press('3');
  await expect.poll(async () => (await state(page)).rating).toBe(3);
  await click(page, 'Grid view'); await expect.poll(async () => (await state(page)).view).toBe('Grid'); await screenshot(page, 'library');
  await page.setViewportSize({ width: 1024, height: 820 }); await click(page, 'Detail view'); await screenshot(page, 'compact');
  await mkdir('artifacts/browser-exports', { recursive: true }); await writeFile('artifacts/browser-exports/diagnostics.json', JSON.stringify(await state(page), null, 2));
});


test('autosave writes committed snapshots while a slider gesture crosses the debounce deadline', async ({ page }) => {
  await boot(page);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await click(page, 'Rate 2');
  const box = await bounds(page, 'slider-Exposure');
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  await page.mouse.down();
  await page.mouse.move(box.x + 5 + (box.width - 10) * .65, box.y + box.height / 2, { steps: 6 });
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Preview');
  await expect.poll(async () => (await state(page)).recovery.savedRevision).toBe(1);
  const savedDuringDrag = await page.evaluate(async () => JSON.parse(await globalThis.lightSpaceFiles.load()));
  const active = savedDuringDrag.Photos.find(photo => photo.Id === savedDuringDrag.ActivePhoto);
  expect(active.State.Rating).toBe(2);
  expect(active.State.Develop.Exposure).toBe(0);
  expect((await state(page)).exposure).toBeGreaterThan(1);
  expect((await state(page)).recovery.hasUnsavedChanges).toBe(true);
  await page.mouse.up();
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const expected = (await state(page)).exposure;
  expect((await state(page)).recovery.savedRevision).toBe(2);
  await boot(page);
  expect((await state(page)).exposure).toBe(expected);
  expect((await state(page)).rating).toBe(2);
});

test('Escape cancels a slider preview without losing previously committed metadata', async ({ page }) => {
  await boot(page);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await click(page, 'Rate 1');
  const box = await bounds(page, 'slider-Exposure');
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.mouse.down();
  await page.mouse.move(box.x + box.width * .7, box.y + box.height / 2, { steps: 5 });
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Preview');
  await expect.poll(async () => (await state(page)).recovery.savedRevision).toBe(1);
  await page.keyboard.press('Escape'); await page.mouse.up();
  await expect.poll(async () => (await state(page)).exposure).toBe(0);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await boot(page); expect((await state(page)).rating).toBe(1); expect((await state(page)).exposure).toBe(0);
});
