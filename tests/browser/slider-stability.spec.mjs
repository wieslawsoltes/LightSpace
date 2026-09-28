import { test, expect } from '@playwright/test';
import { boot, state, stableBox, click, reveal } from './support.mjs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';

const fatalPattern = /memory access out of bounds|runtime already exited|unreachable|Aborted\(|out of memory|RuntimeError|stack overflow/i;
const nonnegative = new Set(['Grain', 'Sharpening', 'NoiseReduction']);

test('all development sliders survive repeated extremes, histogram updates, undo and export', async ({ page }, info) => {
  test.setTimeout(600000);
  const errors = [], steps = [], samples = [];
  const verifiedSettings = {};
  let current = 'startup';
  page.on('crash', () => { const entry = { current, type: 'crash', message: 'Browser renderer process crashed' }; errors.push(entry); console.error(JSON.stringify(entry)); });
  page.on('pageerror', error => { const entry = { current, type: 'pageerror', message: error.stack || error.message }; errors.push(entry); if (errors.length <= 3) console.error(JSON.stringify(entry)); });
  page.on('console', message => {
    if (fatalPattern.test(message.text()) && errors.length < 8) { const entry = { current, type: message.type(), message: message.text() }; errors.push(entry); console.error(JSON.stringify(entry)); }
  });
  const healthy = async () => {
    expect(errors, `Runtime failed while exercising ${current}`).toEqual([]);
    const snapshot = await state(page);
    expect(snapshot).toBeTruthy();
    return snapshot;
  };
  async function measure(fraction) {
    const memory = await page.evaluate(() => {
      const runtime = globalThis.getDotnetRuntime?.(0);
      const module = runtime?.Module ?? globalThis.Module;
      return { heapBytes: module?.HEAPU8?.byteLength ?? module?.HEAP8?.byteLength ?? null,
        jsHeapBytes: performance.memory?.usedJSHeapSize ?? null };
    });
    const snapshot = await healthy();
    const sample = { current, fraction, exposure: snapshot.exposure, memory, renderer: snapshot.performance.viewport };
    samples.push(sample); console.log('SLIDER_SAMPLE', JSON.stringify(sample));
  }
  async function sweep(name) {
    current = name; console.log('SLIDER_STRESS_START', name);
    await reveal(page, 'slider-' + name);
    const b = await stableBox(page, 'slider-' + name), y = b.y + b.height / 2;
    const before = await healthy();
    await measure(null);
    await page.mouse.move(b.x + b.width / 2, y); await page.mouse.down();
    for (const fraction of [.9, .1, .8, .2, .99, .01, .6, .4, .5, .75]) {
      await page.mouse.move(b.x + 5 + (b.width - 10) * fraction, y, { steps: 6 });
      await page.waitForTimeout(70);
      await measure(fraction);
    }
    await page.mouse.up();
    // A successful test must edit the intended control, not merely move over
    // stale bounds while the runtime happens to remain alive.
    await expect.poll(async () => (await healthy()).revision).toBe(before.revision + 1);
    const snapshot = await healthy();
    steps.push({ name, revision: snapshot.revision, performance: snapshot.performance });
    console.log('SLIDER_STRESS_PASS', name);
  }
  async function downloadCatalog(fileName) {
    const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
    const download = await pending; expect(await download.failure()).toBeNull();
    const path = 'artifacts/browser-exports/' + fileName; await download.saveAs(path);
    const catalog = JSON.parse(await readFile(path, 'utf8'));
    const photo = catalog.Photos.find(p => p.Id === catalog.ActivePhoto);
    expect(photo).toBeTruthy();
    for (const { name } of steps) {
      const expected = name === 'Exposure' ? 2.5 : nonnegative.has(name) ? 75 : 50;
      expect(photo.State.Develop[name], `${name} must retain the actual final slider value`).toBeCloseTo(expected, 2);
      verifiedSettings[name] = photo.State.Develop[name];
    }
    return photo.State.Develop;
  }
  try {
    await mkdir('artifacts/browser-exports', { recursive: true });
    await boot(page); await healthy();
    for (const name of ['Exposure', 'Contrast', 'Highlights', 'Shadows', 'Whites', 'Blacks', 'Temperature', 'Tint', 'Vibrance', 'Saturation']) await sweep(name);
    for (const [section, names] of [['Effects', ['Texture', 'Clarity', 'Dehaze', 'Vignette', 'Grain']], ['Detail', ['Sharpening', 'NoiseReduction']]]) {
      current = section; await reveal(page, 'section-' + section); await click(page, 'section-' + section);
      for (const name of names) await sweep(name);
    }
    current = 'undo/redo';
    for (let i = 0; i < 5; i++) { await click(page, 'Undo'); await healthy(); await click(page, 'Redo'); await healthy(); }
    current = 'export';
    await click(page, 'Export'); const pending = page.waitForEvent('download'); await click(page, 'Export file');
    const download = await pending; expect(await download.failure()).toBeNull();
    const output = 'artifacts/browser-exports/slider-stress.jpg'; await download.saveAs(output);
    const jpeg = await readFile(output);
    expect(jpeg.length).toBeGreaterThan(1000); expect(jpeg.subarray(0, 2).toString('hex')).toBe('ffd8');
    const dimensions = await page.evaluate(async encoded => {
      const bytes = Uint8Array.from(atob(encoded), c => c.charCodeAt(0));
      const bitmap = await createImageBitmap(new Blob([bytes], { type: 'image/jpeg' }));
      const result = { width: bitmap.width, height: bitmap.height }; bitmap.close(); return result;
    }, jpeg.toString('base64'));
    expect(dimensions.width).toBeGreaterThan(0); expect(dimensions.height).toBeGreaterThan(0);
    current = 'catalog values';
    const expected = await downloadCatalog('slider-stress.lightspace');
    await expect.poll(async () => (await healthy()).recovery.state).toBe('Saved');
    current = 'reload'; await boot(page); await healthy();
    expect(await downloadCatalog('slider-stress-restored.lightspace')).toEqual(expected);
    expect(steps).toHaveLength(17); expect(samples).toHaveLength(187);
    expect(Object.keys(verifiedSettings)).toHaveLength(17);
    current = 'complete';
  } finally {
    await mkdir('artifacts/browser-exports', { recursive: true });
    const result = { current, steps, samples, errors, verifiedSettings,
      scope: 'Actual Uno pointer input and exports under Chromium. Heap byte length is linear-memory capacity, not live-allocation or physical-GPU certification.' };
    await writeFile('artifacts/browser-exports/slider-stability.json', JSON.stringify(result, null, 2));
    await info.attach('slider-stability', { body: JSON.stringify(result, null, 2), contentType: 'application/json' });
  }
});
