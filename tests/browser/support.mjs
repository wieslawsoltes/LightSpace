import { expect } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
export const base = process.env.LIGHTSPACE_URL || 'http://127.0.0.1:4173/LightSpace/';
export const state = page => page.evaluate(() => globalThis.lightSpaceDiagnostics);
export async function boot(page) {
  await page.goto(base + '?diagnostics=1', { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => globalThis.lightSpaceDiagnostics || globalThis.lightSpaceStartupError, null, { timeout: 120000 });
  expect(await page.evaluate(() => globalThis.lightSpaceStartupError)).toBeFalsy();
  await page.locator('.uno-loader').waitFor({ state: 'hidden', timeout: 120000 });
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await page.waitForTimeout(300);
}
export async function box(page, id) {
  await expect.poll(async () => (await state(page)).widgets.some(w => w.id === id && w.width > 0 && w.height > 0)).toBe(true);
  return (await state(page)).widgets.find(w => w.id === id);
}
export async function stableBox(page, id) {
  // Input must target fresh arranged bounds, not two reads of the same cached
  // diagnostic snapshot before a visibility change has reached layout.
  await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  let previous, previousSequence = -1;
  await expect.poll(async () => {
    const snapshot = await state(page);
    if (!Number.isSafeInteger(snapshot.diagnosticSequence)) throw new Error('Missing diagnostic sequence');
    if (snapshot.diagnosticSequence === previousSequence) return false;
    previousSequence = snapshot.diagnosticSequence;
    const current = snapshot.widgets.find(w => w.id === id && w.width > 0 && w.height > 0);
    const stable = current && previous && ['x', 'y', 'width', 'height'].every(key => Math.abs(current[key] - previous[key]) < .5);
    previous = current;
    return !!stable;
  }, { intervals: [100, 200, 400], timeout: 30000 }).toBe(true);
  return previous;
}
export async function click(page, id) {
  const b = await stableBox(page, id);
  await page.mouse.click(b.x + b.width / 2, b.y + b.height / 2);
}
export async function drag(page, id, dx, dy) {
  const b = await stableBox(page, id); const x = b.x + b.width / 2, y = b.y + b.height / 2;
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x + dx, y + dy, { steps: 8 }); await page.mouse.up();
}
export async function slider(page, id, fraction) {
  const b = await stableBox(page, id); await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2); await page.mouse.down();
  await page.mouse.move(b.x + 5 + (b.width - 10) * fraction, b.y + b.height / 2, { steps: 5 }); await page.mouse.up();
}
export async function reveal(page, id) {
  for (let i = 0; i < 24; i++) {
    const b = (await state(page)).widgets.find(w => w.id === id);
    const scroll = await box(page, 'inspector-scroll');
    const top = Math.max(65, scroll.y + 4), bottom = Math.min(page.viewportSize().height - 12, scroll.y + scroll.height - 4);
    if (b && b.y >= top && b.y + b.height <= bottom) { await stableBox(page, id); return; }
    await page.mouse.move(scroll.x + scroll.width / 2, scroll.y + scroll.height / 2);
    await page.mouse.wheel(0, b && b.y < top ? -160 : 160);
    await page.waitForTimeout(450);
  }
  throw new Error(`Could not reveal ${id}`);
}
export async function shot(page, name) {
  await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  await page.waitForTimeout(350); await mkdir('artifacts/screenshots', { recursive: true });
  await page.screenshot({ path: `artifacts/screenshots/${name}.png` });
}
