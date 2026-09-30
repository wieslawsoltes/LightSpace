import { test, expect } from '@playwright/test';
import { mkdir, readFile } from 'node:fs/promises';
import { boot, state, click } from './support.mjs';

async function backup(page, name) {
  await mkdir('artifacts/browser-exports', { recursive: true });
  const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const download = await pending; const path = `artifacts/browser-exports/${name}.lightspace`;
  await download.saveAs(path); return JSON.parse(await readFile(path, 'utf8'));
}

test('removing the active copy in a multi-selection keeps toolbar edits on the displayed survivor', async ({ page }) => {
  await boot(page); const original = await backup(page, 'copy-selection-original');
  await page.keyboard.down('Control'); await click(page, 'photo-1'); await page.keyboard.up('Control');
  await click(page, 'Manage virtual copies'); await click(page, 'Create selected virtual copies');
  await expect.poll(async () => (await state(page)).photos).toBe(original.Photos.length + 2);
  const before = (await state(page)).copies;
  expect(before.selectedIds).toHaveLength(2);
  const survivor = before.selectedIds.find(id => id !== before.activeId);
  await click(page, 'Remove virtual copy'); await click(page, 'Remove copy');
  await expect.poll(async () => (await state(page)).copies.activeId).toBe(survivor);
  expect((await state(page)).copies.selectedIds).toEqual([survivor]);
  await click(page, 'Rate 4'); await expect.poll(async () => (await state(page)).rating).toBe(4);
  const exported = await backup(page, 'copy-selection-survivor');
  expect(exported.Photos.find(p => p.Id === survivor).State.Rating).toBe(4);
  for (const root of original.Photos) {
    const retained = exported.Photos.find(p => p.Id === root.Id);
    expect(retained.State).toEqual(root.State);
    expect(retained.Original).toBe(root.Original);
  }
  await click(page, 'Undo'); // Rating.
  await click(page, 'Undo'); // Removal restores both selected copies and their active identity.
  await expect.poll(async () => (await state(page)).copies.activeId).toBe(before.activeId);
  expect(new Set((await state(page)).copies.selectedIds)).toEqual(new Set(before.selectedIds));
  await click(page, 'Redo');
  await expect.poll(async () => (await state(page)).copies.activeId).toBe(survivor);
  expect((await state(page)).copies.selectedIds).toEqual([survivor]);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await boot(page); expect((await state(page)).copies.activeId).toBe(survivor);
});
