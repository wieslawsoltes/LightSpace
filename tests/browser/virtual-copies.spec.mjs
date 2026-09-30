import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, stableBox, click, slider, shot } from './support.mjs';
const saved = page => expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
const current = async page => (await state(page)).copies;
async function backup(page, name) {
  await mkdir('artifacts/browser-exports', { recursive: true });
  const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const download = await pending; const path = `artifacts/browser-exports/${name}.lightspace`;
  await download.saveAs(path); return { path, catalog: JSON.parse(await readFile(path, 'utf8')) };
}
async function enter(page, id, text) {
  await click(page, id); await page.keyboard.press('Control+A'); await page.keyboard.type(text);
}
async function pixels(page) {
  await page.mouse.move(20, 20); await page.waitForTimeout(350); const b = await stableBox(page, 'image');
  return page.screenshot({ clip: { x: Math.ceil(b.x + 3), y: Math.ceil(b.y + 3), width: Math.floor(b.width - 6), height: Math.floor(b.height - 6) } });
}

test('virtual copy edits are independent, source-sharing, and survive recovery reload', async ({ page }) => {
  await boot(page); const initial = await state(page); const root = initial.copies.activeId;
  await slider(page, 'slider-Exposure', .55); await saved(page); const originalPixels = await pixels(page);
  const before = (await state(page)).performance.viewport;
  await click(page, 'Manage virtual copies'); await click(page, 'Create virtual copy');
  await expect.poll(async () => (await state(page)).photos).toBe(initial.photos + 1);
  const copyId = (await current(page)).activeId; expect((await current(page)).masterId).toBe(root);
  expect((await state(page)).performance.viewport.imageDecodes).toBe(before.imageDecodes);
  await click(page, 'Back to editing'); await slider(page, 'slider-Exposure', .7); await saved(page);
  expect((await pixels(page)).equals(originalPixels)).toBe(false);
  const record = (await backup(page, 'virtual-copy-independent')).catalog;
  expect(record.SchemaVersion).toBe(6);
  const master = record.Photos.find(p => p.Id === root), copy = record.Photos.find(p => p.Id === copyId);
  expect(master.State.Develop.Exposure).toBeCloseTo(.5, 2); expect(copy.State.Develop.Exposure).toBeCloseTo(2, 2);
  expect(copy.MasterPhotoId).toBe(root); expect(copy.Original).toBe(''); expect(master.Original.length).toBeGreaterThan(1000);
  await boot(page); expect((await current(page)).activeId).toBe(copyId); expect((await state(page)).exposure).toBeCloseTo(2, 2);
  await click(page, 'Manage virtual copies'); await click(page, 'copy-family-' + root);
  await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(.5, 2);
  await click(page, 'Back to editing'); expect((await pixels(page)).equals(originalPixels)).toBe(true);
});

test('rename and copy-only removal keep originals, cache pixels and restore through undo', async ({ page }) => {
  await boot(page); const original = (await backup(page, 'virtual-copy-before')).catalog;
  await click(page, 'Manage virtual copies'); await click(page, 'Create virtual copy'); await saved(page);
  const copyId = (await current(page)).activeId;
  await page.waitForTimeout(400); const before = await state(page);
  await enter(page, 'Virtual copy name', 'Warm alternative'); await click(page, 'Save copy name');
  await expect.poll(async () => (await current(page)).name).toBe('Warm alternative'); await saved(page);
  const after = await state(page);
  expect(after.performance.viewport.imageDecodes).toBe(before.performance.viewport.imageDecodes);
  expect(after.performance.viewport.shaderBuilds).toBe(before.performance.viewport.shaderBuilds);
  expect(after.persistence.blobWrites).toBe(before.persistence.blobWrites); expect(after.persistence.bytesHashed).toBe(before.persistence.bytesHashed);
  await shot(page, 'virtual-copy-manager');
  await writeFile('artifacts/browser-exports/virtual-copy-performance.json', JSON.stringify({
    scope: 'Real pointer-driven virtual-copy rename after creation; no source rehash/write or pixel shader rebuild',
    before: { renderer: before.performance.viewport, persistence: before.persistence },
    after: { renderer: after.performance.viewport, persistence: after.persistence }
  }, null, 2));
  await click(page, 'Remove virtual copy'); await click(page, 'Remove copy');
  await expect.poll(async () => (await state(page)).photos).toBe(original.Photos.length);
  const removed = (await backup(page, 'virtual-copy-removed')).catalog;
  expect(removed.Photos.map(p => [p.Id, p.Original, p.State])).toEqual(original.Photos.map(p => [p.Id, p.Original, p.State]));
  await click(page, 'Undo'); await expect.poll(async () => (await current(page)).activeId).toBe(copyId);
  expect((await current(page)).name).toBe('Warm alternative'); await saved(page);
  const pending = page.waitForEvent('download'); await click(page, 'Export'); await click(page, 'Export file');
  const download = await pending; expect(download.suggestedFilename()).toContain('Warm alternative');
  await download.saveAs('artifacts/browser-exports/virtual-copy.jpg');
  await boot(page); expect((await current(page)).activeId).toBe(copyId); expect((await current(page)).name).toBe('Warm alternative');
});

test('copy families work with filters and Survey; undo never leaves a removed candidate active', async ({ page }) => {
  await boot(page); const count = (await state(page)).photos;
  await click(page, 'Manage virtual copies'); await click(page, 'Create virtual copy'); await click(page, 'Create virtual copy');
  const family = (await current(page)).family; expect(family).toHaveLength(3);
  const root = (await current(page)).masterId;
  expect(family.filter(p => p.isVirtualCopy)).toHaveLength(2);
  await click(page, 'Virtual copies'); await expect.poll(async () => (await current(page)).visibleIds.length).toBe(2);
  await shot(page, 'virtual-copies-grid');
  await click(page, 'Originals'); await expect.poll(async () => (await current(page)).visibleIds.length).toBe(count);
  await click(page, 'All photos'); await click(page, 'Manage virtual copies'); await click(page, 'Compare copy family');
  await expect.poll(async () => (await state(page)).survey.ready).toBe(3);
  expect((await state(page)).survey.renderer.cachedSources).toBe(1);
  await shot(page, 'virtual-copies-survey');
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).view).toBe('Detail');
  expect((await state(page)).photos).toBe(count + 1);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).photos).toBe(count);
  expect((await current(page)).activeId).toBe(root); expect((await current(page)).masterId).toBeNull();
});

test('batch copy creation is one transaction and portable import restores root references', async ({ page }) => {
  await boot(page); const count = (await state(page)).photos;
  await page.keyboard.down('Control'); await click(page, 'photo-1'); await page.keyboard.up('Control');
  expect((await current(page)).selectedIds).toHaveLength(2);
  await click(page, 'Manage virtual copies'); const revision = (await state(page)).revision;
  await click(page, 'Create selected virtual copies');
  await expect.poll(async () => (await state(page)).photos).toBe(count + 2);
  expect((await state(page)).revision).toBe(revision + 1);
  const written = await backup(page, 'virtual-copies-batch');
  const copies = written.catalog.Photos.filter(p => p.MasterPhotoId != null);
  expect(copies).toHaveLength(2); expect(copies.every(p => p.Original === '')).toBe(true);
  await click(page, 'Undo'); await expect.poll(async () => (await state(page)).photos).toBe(count);
  await click(page, 'Redo'); await expect.poll(async () => (await state(page)).photos).toBe(count + 2);
  const chooser = page.waitForEvent('filechooser'); await click(page, 'Open catalog'); await (await chooser).setFiles(written.path);
  await saved(page); const reopened = (await backup(page, 'virtual-copies-roundtrip')).catalog;
  expect(reopened.Photos.map(p => [p.Id, p.MasterPhotoId, p.CopyName, p.State])).toEqual(written.catalog.Photos.map(p => [p.Id, p.MasterPhotoId, p.CopyName, p.State]));
});
