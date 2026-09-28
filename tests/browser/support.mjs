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
  await expect.poll(async () => (await state(page)).widgets.some(w => w.id === id)).toBe(true);
  return (await state(page)).widgets.find(w => w.id === id);
}
export async function click(page, id) { const b = await box(page, id); await page.mouse.click(b.x + b.width / 2, b.y + b.height / 2); }
export async function drag(page, id, dx, dy) {
  const b = await box(page, id); const x = b.x + b.width / 2, y = b.y + b.height / 2;
  await page.mouse.move(x, y); await page.mouse.down(); await page.mouse.move(x + dx, y + dy, { steps: 8 }); await page.mouse.up();
}
export async function slider(page, id, fraction) {
  const b = await box(page, id); await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2); await page.mouse.down();
  await page.mouse.move(b.x + 5 + (b.width - 10) * fraction, b.y + b.height / 2, { steps: 5 }); await page.mouse.up();
}
export async function reveal(page, id) {
  for (let i = 0; i < 20; i++) {
    const b = (await state(page)).widgets.find(w => w.id === id);
    if (b && b.y >= 65 && b.y + b.height < page.viewportSize().height - 12) return;
    const scroll = await box(page, 'inspector-scroll'); await page.mouse.move(scroll.x + scroll.width / 2, scroll.y + scroll.height / 2);
    await page.mouse.wheel(0, b && b.y < 65 ? -180 : 180); await page.waitForTimeout(200);
  }
  throw new Error(`Could not reveal ${id}`);
}
export async function shot(page, name) {
  await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  await page.waitForTimeout(350); await mkdir('artifacts/screenshots', { recursive: true });
  await page.screenshot({ path: `artifacts/screenshots/${name}.png` });
}
