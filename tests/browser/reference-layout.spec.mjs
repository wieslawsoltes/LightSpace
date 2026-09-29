import { test, expect } from '@playwright/test';
import { boot, state, stableBox, click, drag, shot } from './support.mjs';

test('linked comparison preserves normalized pan during workspace resizing', async ({ page }) => {
  await boot(page); await click(page, 'Reference view');
  const b = await stableBox(page, 'reference-canvas');
  await page.mouse.move(b.x + b.width / 2, b.y + b.height / 2); await page.mouse.wheel(0, 120);
  await drag(page, 'reference-canvas', 25, 18);
  const before = (await state(page)).reference;
  const revision = (await state(page)).revision;
  await click(page, 'Reference layout');
  await expect.poll(async () => (await state(page)).reference.stacked).toBe(true);
  await stableBox(page, 'canvas');
  let after = (await state(page)).reference;
  expect(after.activeNavigation.zoom).toBeCloseTo(before.activeNavigation.zoom, 4);
  expect(after.activeNavigation.panX).toBeCloseTo(before.activeNavigation.panX, 4);
  expect(after.activeNavigation.panY).toBeCloseTo(before.activeNavigation.panY, 4);
  expect(after.referenceNavigation.panX).toBeCloseTo(after.activeNavigation.panX, 4);
  expect(after.referenceNavigation.panY).toBeCloseTo(after.activeNavigation.panY, 4);
  await page.setViewportSize({ width: 1100, height: 850 }); await stableBox(page, 'canvas');
  after = (await state(page)).reference;
  expect(after.referenceNavigation.zoom).toBeCloseTo(after.activeNavigation.zoom, 4);
  expect(after.referenceNavigation.panX).toBeCloseTo(after.activeNavigation.panX, 4);
  expect(after.referenceNavigation.panY).toBeCloseTo(after.activeNavigation.panY, 4);
  expect((await state(page)).revision).toBe(revision);
  await click(page, 'Reference fit both'); await shot(page, 'reference-compact');
});

test('settings dialog fits its contents and keeps confirmation in the visible sheet', async ({ page }) => {
  await boot(page); await click(page, 'Copy settings');
  const sheet = await stableBox(page, 'dialog-sheet');
  const apply = await stableBox(page, 'Copy selected settings');
  expect(sheet.height).toBeLessThan(700);
  expect(sheet.y).toBeGreaterThan(100);
  expect(apply.y).toBeGreaterThan(sheet.y);
  expect(apply.y + apply.height).toBeLessThan(sheet.y + sheet.height);
  await click(page, 'settings-None'); await click(page, 'group-Light');
  await shot(page, 'compact-settings-dialog'); await click(page, 'Copy selected settings');
});
