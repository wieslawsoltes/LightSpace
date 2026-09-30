import { test, expect } from '@playwright/test';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { boot, state, stableBox, click, slider, shot } from './support.mjs';

async function ready(page) {
  await expect.poll(async () => (await state(page)).view).toBe('Survey');
  await expect.poll(async () => {
    const survey = (await state(page)).survey;
    return survey.ready === survey.visibleIds.length && survey.failed === 0;
  }).toBe(true);
}
async function catalog(page, name) {
  await mkdir('artifacts/browser-exports', { recursive: true });
  const pending = page.waitForEvent('download'); await click(page, 'Save catalog');
  const file = await pending; const path = 'artifacts/browser-exports/' + name + '.lightspace';
  await file.saveAs(path); return JSON.parse(await readFile(path, 'utf8'));
}

test('survey culls independently, retains source records, and restores excluded candidates', async ({ page }) => {
  await boot(page); await slider(page, 'slider-Exposure', .6);
  await expect.poll(async () => (await state(page)).exposure).toBeCloseTo(1, 2);
  const start = await state(page);
  await click(page, 'Survey view'); await ready(page);
  expect((await state(page)).survey.total).toBe(start.photos);
  async function photoPixels() {
    await page.mouse.move(20, 15); const b = await stableBox(page, 'survey-photo-0');
    await page.waitForTimeout(250);
    return page.screenshot({ clip: { x: b.x + b.width / 4, y: b.y + b.height / 4, width: b.width / 2, height: b.height / 2 } });
  }
  const hit = await stableBox(page, 'survey-photo-0');
  expect(hit.width).toBeGreaterThan(100); expect(hit.height).toBeGreaterThan(80);
  const edited = await photoPixels();
  await click(page, 'Survey original');
  expect((await photoPixels()).equals(edited)).toBe(false);
  await click(page, 'Survey original');
  expect((await photoPixels()).equals(edited)).toBe(true);
  const first = (await state(page)).survey.visibleIds[0];
  await click(page, 'survey-exclude-0'); await ready(page);
  expect((await state(page)).survey.excluded).toBe(1);
  expect((await state(page)).survey.visibleIds).not.toContain(first);
  expect((await state(page)).photos).toBe(start.photos);
  expect((await state(page)).revision).toBe(start.revision);
  await click(page, 'Survey restore excluded'); await ready(page);
  expect((await state(page)).survey.visibleIds[0]).toBe(first);
  await shot(page, 'survey-culling');
  await click(page, 'Survey done');
  await expect.poll(async () => (await state(page)).view).toBe('Detail');
  expect((await state(page)).survey.renderer.cachedBytes).toBe(0);
});

test('survey keyboard rates only the active candidate with undo and stable warm caches', async ({ page }) => {
  await boot(page);
  // Select multiple photos; ratings must still affect only the active candidate in Survey.
  await page.keyboard.down('Control'); await click(page, 'photo-1'); await page.keyboard.up('Control');
  await click(page, 'Survey view'); await ready(page);
  expect((await state(page)).survey.total).toBe(2);
  const before = await catalog(page, 'survey-before');
  const baseline = (await state(page)).survey;
  await click(page, 'survey-photo-1'); const id = (await state(page)).survey.activeId;
  await page.keyboard.press('3'); await expect.poll(async () => (await state(page)).rating).toBe(3);
  await page.keyboard.press('p'); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const after = await catalog(page, 'survey-after');
  expect(after.Photos.find(p => p.Id === id).State.Rating).toBe(3);
  expect(after.Photos.find(p => p.Id === id).State.Flag).toBe(1);
  for (const source of before.Photos.filter(p => p.Id !== id))
    expect(after.Photos.find(p => p.Id === source.Id).State).toEqual(source.State);
  const counters = (await state(page)).survey;
  expect(counters.renderer.imageDecodes).toBe(baseline.renderer.imageDecodes);
  expect(counters.renderer.shaderBuilds).toBe(baseline.renderer.shaderBuilds);
  expect(counters.cardBuilds).toBe(baseline.cardBuilds);
  expect(counters.layoutBuilds).toBe(baseline.layoutBuilds);
  await writeFile('artifacts/browser-exports/survey-performance.json', JSON.stringify({ scope: 'Actual two-candidate Survey, targeted rating and flag; counters, not GPU timing', before: baseline, after: counters }, null, 2));
  await click(page, 'Undo'); const undone = await catalog(page, 'survey-undo');
  expect(undone.Photos.find(p => p.Id === id).State.Flag).toBe(before.Photos.find(p => p.Id === id).State.Flag);
  await click(page, 'Survey done'); await page.keyboard.press('n'); await ready(page);
  await click(page, 'survey-photo-0'); await page.keyboard.press('Enter');
  await expect.poll(async () => (await state(page)).view).toBe('Detail');
});

test('survey pages twelve previews without dropping candidates and survives resize and reload', async ({ page }) => {
  await boot(page);
  const fixture = await readFile('artifacts/fixtures/import-fixture.png');
  const files = Array.from({ length: 15 }, (_, i) => ({ name: `survey-${i}.png`, mimeType: 'image/png', buffer: fixture }));
  const picker = page.waitForEvent('filechooser'); await click(page, 'Add photos'); await (await picker).setFiles(files);
  await expect.poll(async () => (await state(page)).activePhoto).toBe('survey-14.png');
  await click(page, 'Survey view'); await ready(page);
  const survey = (await state(page)).survey;
  expect(survey.total).toBeGreaterThanOrEqual(21); expect(survey.pageCount).toBe(2);
  await click(page, 'Survey previous page'); await ready(page);
  expect((await state(page)).survey.visibleIds).toHaveLength(12);
  await page.setViewportSize({ width: 1024, height: 820 }); await stableBox(page, 'survey-canvas');
  const host = await stableBox(page, 'survey-canvas');
  for (let i = 0; i < 12; i++) {
    const b = await stableBox(page, 'survey-photo-' + i);
    expect(b.width).toBeGreaterThan(100); expect(b.height).toBeGreaterThan(60);
    expect(b.x).toBeGreaterThanOrEqual(host.x); expect(b.x + b.width).toBeLessThanOrEqual(host.x + host.width + 1);
    expect(b.y + b.height).toBeLessThanOrEqual(host.y + host.height + 1);
  }
  expect((await state(page)).survey.renderer.cachedSources).toBeLessThanOrEqual(12);
  expect((await state(page)).survey.renderer.cachedBytes).toBeLessThanOrEqual(48 * 1024 * 1024);
  await shot(page, 'survey-twelve-compact');
  await click(page, 'Survey next page'); await ready(page);
  expect((await state(page)).survey.page).toBe(1);
  await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  await boot(page); expect((await state(page)).survey.visible).toBe(false);
  expect((await state(page)).photos).toBe(survey.total);
});

test('survey reject hiding is view-only and source/edited toggle does not change settings', async ({ page }) => {
  await boot(page); await click(page, 'Survey view'); await ready(page);
  await click(page, 'survey-reject-0'); await expect.poll(async () => (await state(page)).recovery.state).toBe('Saved');
  const revision = (await state(page)).revision;
  await click(page, 'Survey hide rejected'); await ready(page);
  expect((await state(page)).survey.excluded).toBeGreaterThan(0);
  await click(page, 'Survey original'); await click(page, 'Survey original');
  expect((await state(page)).revision).toBe(revision);
  await click(page, 'Survey restore excluded'); await ready(page);
  expect((await state(page)).survey.excluded).toBe(0);
});


test('leaving a fallback survey selects the displayed Detail photo rather than a hidden target', async ({ page }) => {
  await boot(page); const before = await catalog(page, 'survey-handoff-before');
  await click(page, 'Survey view'); await ready(page);
  await click(page, 'survey-photo-1');
  const id = (await state(page)).survey.activeId; expect(id).not.toBe(before.ActivePhoto);
  const revision = (await state(page)).revision;
  await click(page, 'Survey done');
  await expect.poll(async () => (await state(page)).view).toBe('Detail');
  expect((await state(page)).revision).toBe(revision);
  await click(page, 'Rate 4'); await expect.poll(async () => (await state(page)).rating).toBe(4);
  const after = await catalog(page, 'survey-handoff-after');
  expect(after.ActivePhoto).toBe(id);
  for (const photo of before.Photos) {
    const actual = after.Photos.find(candidate => candidate.Id === photo.Id);
    if (photo.Id === id) expect(actual.State.Rating).toBe(4);
    else expect(actual.State).toEqual(photo.State);
  }
});
