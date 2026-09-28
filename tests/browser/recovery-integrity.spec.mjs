import { test, expect } from '@playwright/test';
import { boot } from './support.mjs';

async function attempt(page, scenario) {
  return page.evaluate(async scenario => {
    const storage = globalThis.lightSpaceRecovery;
    const before = await storage.readManifest(); const value = JSON.parse(before);
    const references = [...new Set(Object.values(value.Sources).map(source => source.Key))].join(',');
    let encoded = '[]', calls = 0, failure = '';
    const originalDelete = IDBObjectStore.prototype.delete;
    value.Revision += 100;
    if (scenario === 'duplicate') value.Catalog.Photos[1].Id = value.Catalog.Photos[0].Id;
    if (scenario === 'hash') {
      const source = Object.values(value.Sources)[0];
      const raw = atob(await storage.readBlob(source.Key));
      // Keep length fixed while changing the encoded source content.
      encoded = JSON.stringify([{ key: source.Key, data: btoa(String.fromCharCode(raw.charCodeAt(0) ^ 1) + raw.slice(1)) }]);
    }
    if (scenario === 'length') {
      const source = Object.values(value.Sources)[0];
      encoded = JSON.stringify([{ key: source.Key, data: await storage.readBlob(source.Key) }]);
      source.Length += 1;
    }
    if (scenario === 'synchronous') {
      IDBObjectStore.prototype.delete = function(key) {
        if (this.name === 'workspace' && key === 'catalog') {
          calls++; throw new DOMException('Injected failure after the manifest put was queued', 'DataError');
        }
        return originalDelete.call(this, key);
      };
    }
    const commits = storage.statistics().commits;
    try { await storage.commit(JSON.stringify(value), references, encoded); }
    catch (error) { failure = error.message; }
    finally { IDBObjectStore.prototype.delete = originalDelete; }
    return { failure, calls, preserved: before === await storage.readManifest(), commitsBefore: commits, commitsAfter: storage.statistics().commits };
  }, scenario);
}

test('synchronous failures after queued writes abort the real IndexedDB transaction', async ({ page }) => {
  await boot(page);
  const result = await attempt(page, 'synchronous');
  expect(result.calls).toBe(1); expect(result.failure).toContain('Injected failure');
  expect(result.preserved).toBe(true); expect(result.commitsAfter).toBe(result.commitsBefore);
  const retry = await page.evaluate(async () => {
    const storage = globalThis.lightSpaceRecovery; const manifest = await storage.readManifest();
    const value = JSON.parse(manifest); const keys = [...new Set(Object.values(value.Sources).map(source => source.Key))].join(',');
    await storage.commit(manifest, keys, '[]'); return storage.statistics();
  });
  expect(retry.commits).toBe(result.commitsBefore + 1); expect(retry.durability).toBe('strict');
});

test('duplicate photo identities and mismatched source lengths cannot replace recovery', async ({ page }) => {
  await boot(page);
  for (const scenario of ['duplicate', 'length']) {
    const result = await attempt(page, scenario);
    expect(result.failure).not.toBe(''); expect(result.preserved).toBe(true);
    expect(result.commitsAfter).toBe(result.commitsBefore);
  }
});

test('altered source bytes are rejected before manifest publication', async ({ page }) => {
  await boot(page);
  const result = await attempt(page, 'hash');
  expect(result.failure).toContain('hash mismatch'); expect(result.preserved).toBe(true);
  expect(result.commitsAfter).toBe(result.commitsBefore);
});
