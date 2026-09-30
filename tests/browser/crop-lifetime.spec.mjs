import { test, expect } from '@playwright/test';
import { boot, state, stableBox, click, drag, shot } from './support.mjs';

const crop = async page => (await state(page)).cropTool;

test('A changes only the crop lock and cancels a captured gesture without committing', async ({ page }) => {
  await boot(page); await click(page, 'Crop photo'); await click(page, 'Crop 3 × 2');
  await drag(page, 'crop-bottom-right', -70, -40);
  const before = await state(page);
  const handle = await stableBox(page, 'crop-right');
  await page.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
  await page.mouse.down(); await page.mouse.move(handle.x - 45, handle.y + handle.height / 2, { steps: 5 });
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(true);
  await page.keyboard.press('a');
  await expect.poll(async () => (await crop(page)).locked).toBe(false);
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(false);
  await page.mouse.up();
  expect((await crop(page)).bounds).toEqual(before.cropTool.bounds);
  expect((await state(page)).revision).toBe(before.revision);
  await page.keyboard.press('a');
  await expect.poll(async () => (await crop(page)).locked).toBe(true);
  expect((await state(page)).revision).toBe(before.revision);
});

test('focus-layout changes cancel captured crop edits and the next drag commits normally', async ({ page }) => {
  await boot(page); await click(page, 'Crop photo'); await click(page, 'Crop 4 × 3');
  await drag(page, 'crop-bottom-right', -65, -35);
  const before = await state(page);
  const handle = await stableBox(page, 'crop-right');
  await page.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
  await page.mouse.down(); await page.mouse.move(handle.x - 40, handle.y + handle.height / 2, { steps: 5 });
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(true);
  await page.keyboard.press('F6');
  await expect.poll(async () => (await state(page)).recovery.hasActiveGesture).toBe(false);
  await stableBox(page, 'canvas'); await page.mouse.up();
  expect((await crop(page)).bounds).toEqual(before.cropTool.bounds);
  expect((await state(page)).revision).toBe(before.revision);
  await page.keyboard.press('F6'); await stableBox(page, 'canvas');
  await drag(page, 'crop-bottom-right', -20, -10);
  await expect.poll(async () => (await state(page)).revision).toBe(before.revision + 1);
  expect((await crop(page)).outputAspect).toBeCloseTo(4 / 3, 4);
  await shot(page, 'crop-capture-recovery');
});
