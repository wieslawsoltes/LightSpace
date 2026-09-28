import { test, expect } from '@playwright/test';
import { boot, stableBox } from './support.mjs';
import { mkdir, writeFile } from 'node:fs/promises';

test('trace managed entry and exit around the original neutral transition', async ({ page }) => {
  const messages = []; const append = text => { messages.push(text); if (messages.length > 1500) messages.shift(); };
  page.on('console', message => append(message.text())); page.on('pageerror', error => append(error.stack || error.message));
  page.on('crash', () => append('BROWSER_RENDERER_CRASH'));
  try {
    await boot(page);
    const b = await stableBox(page, 'slider-Exposure'), y = b.y + b.height / 2;
    await page.mouse.move(b.x + b.width / 2, y); await page.mouse.down();
    for (const fraction of [.9, .1, .8, .2, .99, .01, .6, .4, .5, .75]) {
      append('DIAGNOSTIC_EXPOSURE_FRACTION ' + fraction);
      await page.mouse.move(b.x + 5 + (b.width - 10) * fraction, y, { steps: 6 });
      await page.waitForTimeout(70);
    }
    await page.mouse.up(); await page.waitForTimeout(500);
    expect(messages.some(m => /memory access out of bounds|BROWSER_RENDERER_CRASH/.test(m))).toBe(false);
  } finally {
    await mkdir('artifacts', { recursive: true });
    await writeFile('artifacts/managed-call-trace.json', JSON.stringify(messages, null, 2));
    console.log(messages.slice(-250).join('\n'));
  }
});
