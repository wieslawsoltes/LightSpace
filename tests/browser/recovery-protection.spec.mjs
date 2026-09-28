import { test, expect } from '@playwright/test';
import { base, state, click } from './support.mjs';
async function boot(page) {
  await page.goto(base + '?diagnostics=1', { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => globalThis.lightSpaceDiagnostics || globalThis.lightSpaceStartupError, null, { timeout: 120000 });
  expect(await page.evaluate(() => globalThis.lightSpaceStartupError)).toBeFalsy();
  await page.locator('.uno-loader').waitFor({ state: 'hidden', timeout: 120000 });
}
test('unreadable recovery is preserved until replacement is explicitly confirmed', async ({ page }) => {
  await boot(page); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const corrupt = '{"SchemaVersion":987}'; await page.evaluate(value => globalThis.lightSpaceFiles.save(value), corrupt);
  await boot(page); await expect.poll(async () => (await state(page)).status).toContain('Recovery could not be opened');
  await click(page, 'Rate 1'); await page.waitForTimeout(1300);
  expect(await page.evaluate(() => globalThis.lightSpaceFiles.load())).toBe(corrupt);
  await click(page, 'Save recovery now'); await click(page, 'Replace recovery');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const recovery = await page.evaluate(async () => JSON.parse(await globalThis.lightSpaceFiles.load()));
  expect(recovery.SchemaVersion).toBe(4);
  expect(recovery.Photos.find(photo => photo.Id === recovery.ActivePhoto).State.Rating).toBe(1);
});
