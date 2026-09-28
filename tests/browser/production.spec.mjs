import { test, expect } from '@playwright/test';
const base = process.env.LIGHTSPACE_URL || 'http://127.0.0.1:4173/LightSpace/';

test('normal browser session edits and saves without diagnostic tree walking', async ({ page }) => {
  const errors = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto(base, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(async () => {
    if (globalThis.lightSpaceStartupError) return true;
    if (!globalThis.lightSpaceFiles) return false;
    try { return !!(await globalThis.lightSpaceFiles.load()); } catch { return false; }
  }, null, { timeout: 120000 });
  expect(await page.evaluate(() => globalThis.lightSpaceStartupError)).toBeFalsy();
  await page.locator('.uno-loader').waitFor({ state: 'hidden', timeout: 120000 });
  expect(await page.evaluate(() => globalThis.lightSpaceDiagnosticsEnabled())).toBe(false);
  expect(await page.evaluate(() => globalThis.lightSpaceDiagnostics)).toBeUndefined();
  // Real input to the image region. Persistence is observed, not mutated, via
  // the same local-storage adapter used by the actual application.
  await page.mouse.click(650, 400); await page.keyboard.press('2');
  await expect.poll(async () => page.evaluate(async () => {
    const catalog = JSON.parse(await globalThis.lightSpaceFiles.load());
    return catalog.Photos.find(photo => photo.Id === catalog.ActivePhoto).State.Rating;
  })).toBe(2);
  expect(await page.evaluate(() => globalThis.lightSpaceDiagnostics)).toBeUndefined();
  expect(errors).toEqual([]);
});
