import { test, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import { boot, base, state, click, shot } from './support.mjs';
const stats = page => page.evaluate(() => globalThis.lightSpaceRecovery.statistics());
const manifest = page => page.evaluate(async () => JSON.parse(await globalThis.lightSpaceRecovery.readManifest()));
const saved = page => expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
async function removeSource(page, key) {
  await page.evaluate(async key => {
    const db = await globalThis.lightSpaceRecovery.open();
    await new Promise((resolve, reject) => {
      const tx = db.transaction('originals', 'readwrite'); tx.objectStore('originals').delete(key);
      tx.oncomplete = resolve; tx.onabort = () => reject(tx.error);
    }); db.close();
  }, key);
}
async function bootProtected(page) {
  await page.goto(base + '?diagnostics=1', { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => globalThis.lightSpaceDiagnostics || globalThis.lightSpaceStartupError, null, { timeout: 120000 });
  expect(await page.evaluate(() => globalThis.lightSpaceStartupError)).toBeFalsy();
  await page.locator('.uno-loader').waitFor({ state: 'hidden', timeout: 120000 });
  await expect.poll(async () => (await state(page)).status).toContain('Recovery could not be opened');
}

test('warm metadata autosave avoids original bytes, hashing and IndexedDB blob reads', async ({ page }) => {
  await boot(page); const before = await stats(page); const engine = (await state(page)).persistence;
  for (const rating of [1, 2, 3]) {
    await click(page, 'Rate ' + rating); await expect.poll(async () => (await state(page)).rating).toBe(rating);
    await click(page, 'Save recovery now'); await saved(page);
  }
  const after = await stats(page); const next = (await state(page)).persistence; const stored = await manifest(page);
  expect(after.blobWrites).toBe(before.blobWrites); expect(after.blobReads).toBe(before.blobReads); expect(after.blobBytes).toBe(before.blobBytes);
  expect(next.bytesHashed).toBe(engine.bytesHashed); expect(after.commits).toBeGreaterThanOrEqual(before.commits + 3);
  expect(stored.Catalog.Photos.every(photo => photo.Original === '')).toBe(true);
  expect(Object.keys(stored.Sources).length).toBe(stored.Catalog.Photos.length);
  await mkdir('artifacts/browser-exports', { recursive: true });
  await writeFile('artifacts/browser-exports/recovery-performance.json', JSON.stringify({ before, after, persistenceBefore: engine, persistenceAfter: next,
    manifestBytes: new TextEncoder().encode(JSON.stringify(stored)).length, sourceBytesReferenced: Object.values(stored.Sources).reduce((n, value) => n + value.Length, 0),
    scope: 'Real browser three committed rating changes, work counters; not total frame latency or physical GPU timing.' }, null, 2));
  await boot(page); expect((await state(page)).rating).toBe(3);
  const restored = await stats(page); const restoredEngine = (await state(page)).persistence;
  await click(page, 'Rate 4'); await click(page, 'Save recovery now'); await saved(page);
  expect((await stats(page)).blobWrites).toBe(0); expect((await stats(page)).blobReads).toBe(restored.blobReads);
  expect((await state(page)).persistence.bytesHashed).toBe(restoredEngine.bytesHashed);
});

test('missing blob aborts publication and explicit retry restages sources', async ({ page }) => {
  await boot(page); const previous = await manifest(page); const key = Object.values(previous.Sources)[0].Key;
  await removeSource(page, key); await click(page, 'Rate 1');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Failed');
  expect((await manifest(page)).Revision).toBe(previous.Revision);
  await click(page, 'Save recovery now'); await saved(page);
  expect((await manifest(page)).Revision).toBe((await state(page)).revision);
  await boot(page); expect((await state(page)).rating).toBe(1);
});

test('missing original during restore is protected until explicit replacement', async ({ page }) => {
  await boot(page); const original = await manifest(page); const key = Object.values(original.Sources)[0].Key;
  await removeSource(page, key); await bootProtected(page);
  await click(page, 'Rate 1'); await page.waitForTimeout(1200);
  expect((await manifest(page)).Revision).toBe(original.Revision);
  await click(page, 'Save recovery now'); await click(page, 'Replace recovery'); await saved(page);
  await shot(page, 'incremental-recovery'); await boot(page); expect((await state(page)).rating).toBe(1);
});

test('legacy portable recovery migrates on the next commit without losing originals', async ({ page }) => {
  await boot(page); const portable = await page.evaluate(() => globalThis.lightSpaceFiles.load());
  const old = JSON.parse(portable); old.SchemaVersion = 3; old.Photos[0].State.Rating = 2;
  await page.evaluate(value => globalThis.lightSpaceFiles.save(value), JSON.stringify(old));
  await boot(page); expect((await state(page)).rating).toBe(2);
  await click(page, 'Rate 4'); await click(page, 'Save recovery now'); await saved(page);
  const newManifest = await manifest(page); expect(newManifest.Catalog.SchemaVersion).toBe(5);
  const hydrated = await page.evaluate(async () => JSON.parse(await globalThis.lightSpaceFiles.load()));
  expect(hydrated.Photos.map(p => p.Original)).toEqual(old.Photos.map(p => p.Original));
  expect(hydrated.Photos[0].State.Rating).toBe(4);
});
