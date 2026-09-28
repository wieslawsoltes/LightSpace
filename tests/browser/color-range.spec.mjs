import { test, expect } from '@playwright/test';
import { mkdir, readFile } from 'node:fs/promises';
import { boot, state, box, click, slider, reveal, shot, stableBox } from './support.mjs';

async function importStripes(page) {
  const chooser = page.waitForEvent('filechooser'); await click(page, 'Add photos');
  await (await chooser).setFiles('artifacts/fixtures/color-ranges.png');
  await expect.poll(async () => (await state(page)).activePhoto).toBe('color-ranges.png');
}
async function sample(page, x, y, modifier) {
  const image = await box(page, 'image');
  if (modifier) await page.keyboard.down(modifier);
  await page.mouse.click(image.x + image.width * x, image.y + image.height * y);
  if (modifier) await page.keyboard.up(modifier);
}
const colors = async page => (await state(page)).maskSettings[0].colorRange.samples;

test('source color samples add, remove, undo and persist through catalog recovery', async ({ page }) => {
  await boot(page); await importStripes(page); await click(page, 'Masking'); await click(page, 'Color range');
  await expect.poll(async () => (await state(page)).tool).toBe('ColorRange');
  await sample(page, .17, .5);
  await expect.poll(async () => (await colors(page)).length).toBe(1);
  let selected = (await colors(page))[0]; expect(selected.red).toBeCloseTo(180 / 255, 2); expect(selected.green).toBeCloseTo(45 / 255, 2);
  await sample(page, .83, .5, 'Shift'); await expect.poll(async () => (await colors(page)).length).toBe(2);
  expect((await colors(page))[1].blue).toBeCloseTo(180 / 255, 2);
  await sample(page, .83, .5, 'Alt'); await expect.poll(async () => (await colors(page)).length).toBe(1);
  await click(page, 'Undo'); await expect.poll(async () => (await colors(page)).length).toBe(2);
  await reveal(page, 'color-tolerance'); await slider(page, 'color-tolerance', .2);
  await expect.poll(async () => (await state(page)).maskSettings[0].colorRange.tolerance).toBeCloseTo(.2, 2);
  await shot(page, 'sampled-color-range');
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await boot(page); expect((await state(page)).activePhoto).toBe('color-ranges.png'); expect((await colors(page)).length).toBe(2);
  expect((await state(page)).maskSettings[0].colorRange.tolerance).toBeCloseTo(.2, 2);
});

test('color picking samples source values through crop and rotation rather than developed pixels', async ({ page }) => {
  await boot(page); await importStripes(page);
  await slider(page, 'slider-Exposure', .8); await expect.poll(async () => (await state(page)).exposure).toBeGreaterThan(2);
  await click(page, 'Crop photo'); await click(page, 'Rotate right'); await click(page, 'Apply crop');
  await click(page, 'Masking'); await click(page, 'Color range');
  // Source-left red stripe becomes the top stripe after a clockwise quarter turn.
  await sample(page, .5, .17); await expect.poll(async () => (await colors(page)).length).toBe(1);
  expect((await colors(page))[0].red).toBeCloseTo(180 / 255, 2);
  for (const y of [.35, .5, .65, .83]) await sample(page, .5, y, 'Shift');
  await expect.poll(async () => (await colors(page)).length).toBe(5);
  await sample(page, .5, .9, 'Shift'); expect((await colors(page)).length).toBe(5);
  await expect.poll(async () => (await state(page)).status).toContain('Five color samples');
  await reveal(page, 'color-swatch-0'); await click(page, 'color-swatch-0'); await expect.poll(async () => (await colors(page)).length).toBe(4);
});

test('color-range JPEG export changes the selected stripe without painting the coverage overlay', async ({ page }) => {
  await boot(page); await importStripes(page); await click(page, 'Masking'); await click(page, 'Color range');
  await sample(page, .17, .5); await expect.poll(async () => (await colors(page)).length).toBe(1);
  await reveal(page, 'mask-Exposure'); await slider(page, 'mask-Exposure', .6);
  await expect.poll(async () => (await state(page)).maskSettings[0].exposure).toBeCloseTo(1, 1);
  await click(page, 'Export'); const pending = page.waitForEvent('download'); await click(page, 'Export file');
  const download = await pending; await mkdir('artifacts/browser-exports', { recursive: true });
  const path = 'artifacts/browser-exports/color-range.jpg'; await download.saveAs(path);
  const bytes = await readFile(path);
  const pixels = await page.evaluate(async encoded => {
    const raw = atob(encoded); const data = Uint8Array.from(raw, c => c.charCodeAt(0));
    const image = await createImageBitmap(new Blob([data], { type: 'image/jpeg' }));
    const canvas = new OffscreenCanvas(image.width, image.height); const context = canvas.getContext('2d'); context.drawImage(image, 0, 0);
    const sample = x => [...context.getImageData(x, 90, 1, 1).data]; image.close();
    return [sample(50), sample(150), sample(250)];
  }, bytes.toString('base64'));
  expect(pixels[0][0]).toBeGreaterThan(230); expect(Math.abs(pixels[1][1] - 170)).toBeLessThan(5); expect(Math.abs(pixels[2][2] - 180)).toBeLessThan(5);
});

test('adding and removing the first color swatch does not shift adjustment rails', async ({ page }) => {
  await boot(page); await importStripes(page); await click(page, 'Masking'); await click(page, 'Color range');
  await expect.poll(async () => (await state(page)).tool).toBe('ColorRange');
  await reveal(page, 'mask-Exposure'); const empty = await stableBox(page, 'mask-Exposure');
  await sample(page, .17, .5); await expect.poll(async () => (await colors(page)).length).toBe(1);
  const populated = await stableBox(page, 'mask-Exposure');
  expect(populated.y).toBeCloseTo(empty.y, 1);
  await click(page, 'color-swatch-0'); await expect.poll(async () => (await colors(page)).length).toBe(0);
  expect((await stableBox(page, 'mask-Exposure')).y).toBeCloseTo(empty.y, 1);
  await sample(page, .17, .5); await expect.poll(async () => (await colors(page)).length).toBe(1);
  await slider(page, 'mask-Exposure', .6);
  await expect.poll(async () => (await state(page)).maskSettings[0].exposure).toBeCloseTo(1, 1);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
});
