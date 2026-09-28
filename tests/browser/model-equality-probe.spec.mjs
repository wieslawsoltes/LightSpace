import { test, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
test('pure managed equality completes before application or Skia startup', async ({ page }) => {
  const messages = []; page.on('console', message => { messages.push(message.text()); if (message.text().includes('LIGHTSPACE_PROBE')) console.log(message.text()); });
  page.on('pageerror', error => { messages.push(error.stack || error.message); console.error(error.stack || error.message); });
  page.on('crash', () => messages.push('Renderer crashed'));
  try {
    await page.goto('http://127.0.0.1:4173/LightSpace/', { waitUntil: 'domcontentloaded' });
    await expect.poll(() => messages.some(message => message.includes('LIGHTSPACE_PROBE_PASS develop-equality')), { timeout: 90000 }).toBe(true);
  } finally {
    await mkdir('artifacts', { recursive: true });
    await writeFile('artifacts/model-probe.json', JSON.stringify(messages, null, 2));
  }
});
