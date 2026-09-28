import { test, expect } from '@playwright/test';
import { boot, state, stableBox, click, reveal } from './support.mjs';
import { mkdir, writeFile } from 'node:fs/promises';

const fatalPattern = /memory access out of bounds|runtime already exited|unreachable|Aborted\(|out of memory|RuntimeError|stack overflow/i;

test('all development sliders survive repeated extremes, histogram updates, undo and export', async ({ page }, info) => {
  test.setTimeout(600000);
  const errors = [], steps = [], samples = [];
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
        jsHeapBytes: performance.memory?.usedJSHeapSize ?? null,
        runtimeKeys: runtime ? Object.keys(runtime) : [], modulePresent: !!module };
    });
    const snapshot = await healthy();
    const sample = { current, fraction, exposure: snapshot.exposure, memory, renderer: snapshot.performance.viewport };
    samples.push(sample); console.log('SLIDER_SAMPLE', JSON.stringify(sample));
  }
  async function sweep(name) {
    current = name; console.log('SLIDER_STRESS_START', name);
    await reveal(page, 'slider-' + name);
    const b = await stableBox(page, 'slider-' + name), y = b.y + b.height / 2;
    await measure(null);
    await page.mouse.move(b.x + b.width / 2, y); await page.mouse.down();
    for (const fraction of [.9, .1, .8, .2, .99, .01, .6, .4, .5, .75]) {
      console.log('SLIDER_MOVE', name, fraction);
      await page.mouse.move(b.x + 5 + (b.width - 10) * fraction, y, { steps: 6 });
      await page.waitForTimeout(70);
      await measure(fraction);
    }
    await page.mouse.up(); await page.waitForTimeout(250);
    const snapshot = await healthy();
    steps.push({ name, revision: snapshot.revision, performance: snapshot.performance });
    console.log('SLIDER_STRESS_PASS', name);
  }
  try {
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
    await expect.poll(async () => (await healthy()).recovery.state).toBe('Saved');
    current = 'reload'; await boot(page); await healthy();
  } finally {
    await mkdir('artifacts/browser-exports', { recursive: true });
    const result = { current, steps, samples, errors };
    await writeFile('artifacts/browser-exports/slider-stability.json', JSON.stringify(result, null, 2));
    await info.attach('slider-stability', { body: JSON.stringify(result, null, 2), contentType: 'application/json' });
  }
});
